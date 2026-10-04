using System.Net;
using System.Net.Http.Headers;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GestaoPro.API.Tests.Startup;

// Sobe a API de verdade (Production, sem seeder) com MongoDB inalcançável e content root temporário,
// como em DemoSeedStartupTests. O IAdministratorLookup é trocado por um valor fixo porque o
// ApiAccessMiddleware o consulta em toda escrita e o negotiate do SignalR é um POST.
public class NotificacaoHubAuthTests : IDisposable
{
    private const string UnreachableMongo =
        "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";
    private const string SecretKey = "chave-de-teste-com-mais-de-32-caracteres-0123456789";
    private const string Issuer = "GestaoPro.API";
    private const string Audience = "GestaoPro.API";

    private sealed class FixedAdministratorLookup : IAdministratorLookup
    {
        public Task<bool> HasAnyAdministratorAsync() => Task.FromResult(true);
    }

    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;

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
                services.AddSingleton<IAdministratorLookup, FixedAdministratorLookup>();
            });
        });
    }

    private static string TokenFor(string role)
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
            UserId = "665f1c2e9b1e8a0012345678",
            Role = role,
            CondominiumId = "665f1c2e9b1e8a0012345679",
            RegistrationNumber = "morador@teste.com",
            MustChangePassword = false,
        });
        return token;
    }

    [Fact]
    public async Task Negotiate_sem_token_e_recusado()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/notificacaoHub/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Com skipNegotiation o cliente vai direto ao GET do hub: sem token, não pode nem chegar ao SignalR.
    [Fact]
    public async Task Conexao_direta_ao_hub_sem_token_e_recusada()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/notificacaoHub");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // O hub notifica moradores: qualquer papel logado precisa conseguir conectar, não só admin/síndico.
    [Theory]
    [InlineData("morador")]
    [InlineData("porteiro")]
    [InlineData("admin")]
    public async Task Negotiate_com_token_valido_e_aceito_para_qualquer_papel(string role)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/notificacaoHub/negotiate?negotiateVersion=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(role));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Navegador não manda cabeçalho em WebSocket: o cliente SignalR envia o token em access_token.
    // Sem connection id o SignalR responde 400, o que prova que a autenticação passou.
    [Fact]
    public async Task Conexao_direta_com_token_na_query_passa_pela_autenticacao()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/notificacaoHub?access_token={TokenFor("morador")}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // O access_token na query vale só para o hub: no resto da API o token na URL vazaria em logs e histórico.
    [Fact]
    public async Task Token_valido_na_query_fora_do_hub_continua_recusado()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/Bills/user/x?access_token={TokenFor("admin")}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Sem isso, uma conexão WebSocket aberta segue recebendo notificações depois que o JWT expira.
    [Fact]
    public async Task Hub_fecha_a_conexao_quando_o_token_expira()
    {
        await using var factory = CreateFactory();
        factory.CreateClient();

        // As opções do MapHub ficam nos metadados do endpoint de negotiate do hub.
        var options = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<HubMetadata>()?.HubType == typeof(NotificacaoHub))
            .Select(endpoint => endpoint.Metadata.GetMetadata<HttpConnectionDispatcherOptions>())
            .FirstOrDefault(found => found is not null);

        Assert.NotNull(options);
        Assert.True(options.CloseOnAuthenticationExpiration);
    }
}
