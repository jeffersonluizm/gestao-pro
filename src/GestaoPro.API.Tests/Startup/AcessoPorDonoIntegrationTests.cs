using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Startup;

// Prova a fiação de ponta a ponta: o JWT do TokenService passa pela autenticação e as claims
// (NameIdentifier, Role, condominiumId) chegam aos controllers. Serviços de dados trocados por mocks;
// MongoDB inalcançável e content root temporário, como nos outros testes de partida.
public class AcessoPorDonoIntegrationTests : IDisposable
{
    private const string UnreachableMongo =
        "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";
    private const string SecretKey = "chave-de-teste-com-mais-de-32-caracteres-0123456789";
    private const string Issuer = "GestaoPro.API";
    private const string Audience = "GestaoPro.API";

    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;
    private readonly Mock<IBillService> _billService = new();
    private readonly Mock<IUserDirectory> _userDirectory = new();

    public AcessoPorDonoIntegrationTests()
    {
        _billService.Setup(s => s.GetBillByIdAsync("665f1c2e9b1e8a0000000b01")).ReturnsAsync(new BillModel
        {
            Id = "665f1c2e9b1e8a0000000b01",
            UserId = "665f1c2e9b1e8a0000000001",
            CondominiumId = CondominioA,
            Description = "Taxa condominial",
            Barcode = "123",
        });
        _userDirectory.Setup(d => d.GetAllActiveAsync()).ReturnsAsync(new List<UserResponse>
        {
            new() { Id = "665f1c2e9b1e8a0000000001", CondominiumId = CondominioA, Name = "João", Cpf = "11111111111", Email = "joao@teste.com", Phone = "31999999999", Role = UserRole.morador, IsActive = true },
            new() { Id = "665f1c2e9b1e8a0000000002", CondominiumId = CondominioA, Name = "Maria", Cpf = "22222222222", Email = "maria@teste.com", Phone = "31988888888", Role = UserRole.morador, IsActive = true },
        });
    }

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseContentRoot(_contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", UnreachableMongo);
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", SecretKey);
            builder.UseSetting("JwtSettings:Issuer", Issuer);
            builder.UseSetting("JwtSettings:Audience", Audience);
            builder.ConfigureTestServices(services =>
            {
                var voteIndexInitializer = services.Single(d =>
                    d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(VoteIndexInitializer));
                services.Remove(voteIndexInitializer);
                services.AddScoped(_ => _billService.Object);
                services.AddScoped(_ => _userDirectory.Object);
                // O UsersController também recebe o UserAppService (escritas), cujos repositórios acessam o banco
                // ao serem criados; as leituras testadas aqui não o usam.
                services.AddScoped(_ => new UserAppService(null!, null!, null!, null!));
            });
        });
    }

    private static string TokenFor(string userId, string role, string condominiumId)
    {
        var tokenService = new TokenService(Options.Create(new JwtSettings
        {
            SecretKey = SecretKey,
            Issuer = Issuer,
            Audience = Audience,
            ExpirationHours = 1,
        }));
        var (token, _) = tokenService.GenerateToken(new Credential
        {
            UserId = userId,
            Role = role,
            CondominiumId = condominiumId,
            RegistrationNumber = "usuario@teste.com",
            MustChangePassword = false,
        });
        return token;
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Morador_lendo_cobranca_de_outro_morador_recebe_403_com_mensagem()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await GetAsync(client, "/api/Bills/665f1c2e9b1e8a0000000b01",
            TokenFor("665f1c2e9b1e8a0000000002", "morador", CondominioA));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("permissão", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Dono_le_a_propria_cobranca()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await GetAsync(client, "/api/Bills/665f1c2e9b1e8a0000000b01",
            TokenFor("665f1c2e9b1e8a0000000001", "morador", CondominioA));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_de_outro_condominio_recebe_o_mesmo_404_de_cobranca_inexistente()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var token = TokenFor("665f1c2e9b1e8a0000000009", "admin", CondominioB);

        var otherCondominium = await GetAsync(client, "/api/Bills/665f1c2e9b1e8a0000000b01", token);
        var missing = await GetAsync(client, "/api/Bills/665f1c2e9b1e8a0000000b99", token);

        Assert.Equal(HttpStatusCode.NotFound, otherCondominium.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Lista_de_usuarios_para_morador_nao_traz_cpf_email_nem_telefone()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await GetAsync(client, "/api/Users", TokenFor("665f1c2e9b1e8a0000000001", "morador", CondominioA));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var users = json.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, users.Count);
        Assert.Equal("Maria", users[1].GetProperty("name").GetString());
        Assert.All(users, user =>
        {
            Assert.False(user.TryGetProperty("cpf", out _));
            Assert.False(user.TryGetProperty("email", out _));
            Assert.False(user.TryGetProperty("phone", out _));
        });
    }
}
