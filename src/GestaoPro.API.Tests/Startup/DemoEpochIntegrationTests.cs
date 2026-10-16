using System.Net;
using System.Net.Http.Headers;
using GestaoPro.API.Services.Demo;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Startup;

// O reset da demonstração recria contas e dados com ids novos. Um token emitido antes do reset (ou antes de o
// servidor dormir e acordar) apontaria para ids que não existem mais: listas vazias, perfil 404. Por isso cada
// reset troca a época da demonstração, e token de outra época responde 401 (o painel e o app voltam ao login).
public class DemoEpochIntegrationTests : IDisposable
{
    private const string UnreachableMongo = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";
    private const string SecretKey = "chave-de-teste-com-mais-de-32-caracteres-0123456789";
    private const string BillId = "665f1c2e9b1e8a0000000b01";
    private const string OwnerId = "665f1c2e9b1e8a0000000001";

    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;
    private readonly Mock<IBillService> _billService = new();

    public DemoEpochIntegrationTests()
    {
        _billService.Setup(s => s.GetBillByIdAsync(BillId)).ReturnsAsync(new BillModel
        {
            Id = BillId, UserId = OwnerId, CondominiumId = CondominioA, Description = "Taxa condominial", Barcode = "123",
        });
    }

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    // Reset de mentira: não toca no banco, só troca a época como o de verdade.
    private sealed class FakeReset(DemoCatalog catalog) : IDemoReset
    {
        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            catalog.RenewEpoch();
            return Task.CompletedTask;
        }
    }

    private WebApplicationFactory<Program> CreateFactory(bool demo) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseContentRoot(_contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", UnreachableMongo);
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", SecretKey);
            builder.UseSetting("JwtSettings:Issuer", "GestaoPro.API");
            builder.UseSetting("JwtSettings:Audience", "GestaoPro.API");
            builder.UseSetting("Demo:Enabled", demo.ToString());
            builder.ConfigureTestServices(services =>
            {
                foreach (var hosted in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(hosted);
                services.AddScoped<IDemoReset>(provider => new FakeReset(provider.GetRequiredService<DemoCatalog>()));
                services.AddScoped(_ => _billService.Object);
                // O BillsController também recebe o diretório de usuários (banco); a leitura testada não o usa.
                services.AddScoped(_ => new Mock<IUserDirectory>().Object);
            });
        });

    private static string TokenFor(WebApplicationFactory<Program> factory) =>
        new TokenService(Options.Create(new JwtSettings
        {
            SecretKey = SecretKey, Issuer = "GestaoPro.API", Audience = "GestaoPro.API", ExpirationHours = 8,
        }), factory.Services.GetRequiredService<DemoCatalog>()).GenerateToken(new Credential
        {
            UserId = OwnerId, Role = "morador", CondominiumId = CondominioA, RegistrationNumber = "morador@demo.gestaopro.local",
        }).token;

    private static async Task<HttpStatusCode> ReadBillAsync(HttpClient client, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Bills/{BillId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.SendAsync(request)).StatusCode;
    }

    [Fact]
    public async Task Token_da_epoca_atual_vale_e_depois_do_reset_vira_401()
    {
        await using var factory = CreateFactory(demo: true);
        var client = factory.CreateClient();
        var token = TokenFor(factory);

        Assert.Equal(HttpStatusCode.OK, await ReadBillAsync(client, token));

        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IDemoReset>().ResetAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, await ReadBillAsync(client, token));
        Assert.Equal(HttpStatusCode.OK, await ReadBillAsync(client, TokenFor(factory)));
    }

    // O servidor dormiu e acordou: é outro processo, com outra época. O token de antes também vira 401.
    [Fact]
    public async Task Token_de_antes_do_servidor_reiniciar_vira_401()
    {
        string token;
        await using (var before = CreateFactory(demo: true))
        {
            before.CreateClient();
            token = TokenFor(before);
        }

        await using var after = CreateFactory(demo: true);
        Assert.Equal(HttpStatusCode.Unauthorized, await ReadBillAsync(after.CreateClient(), token));
    }

    // Sem a flag, nada muda: token sem época continua valendo.
    [Fact]
    public async Task Sem_a_flag_o_token_comum_continua_valendo()
    {
        await using var factory = CreateFactory(demo: false);

        Assert.Equal(HttpStatusCode.OK, await ReadBillAsync(factory.CreateClient(), TokenFor(factory)));
    }
}
