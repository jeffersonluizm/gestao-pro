using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GestaoPro.API.Tests.Startup;

// Endurecimento da API em produção: Swagger fechado, CORS só para origens configuradas,
// cabeçalhos de segurança em toda resposta e erro inesperado como ProblemDetails sem detalhe interno.
public class SecurityHardeningTests : IDisposable
{
    private const string PainelOrigin = "https://painel.gestaopro.exemplo";

    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private WebApplicationFactory<Program> CreateFactory(
        Action<IServiceCollection>? services = null, params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseContentRoot(_contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500");
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", "chave-de-teste-com-mais-de-32-caracteres-0123456789");
            builder.UseSetting("Cors:AllowedOrigins:0", PainelOrigin);
            foreach (var (key, value) in settings) builder.UseSetting(key, value);
            builder.ConfigureTestServices(collection =>
            {
                collection.Remove(collection.Single(d =>
                    d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(VoteIndexInitializer)));
                services?.Invoke(collection);
            });
        });

    [Fact]
    public async Task Em_producao_o_Swagger_fica_fechado()
    {
        await using var factory = CreateFactory();

        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_abre_so_quando_a_configuracao_libera()
    {
        await using var factory = CreateFactory(settings: ("Swagger:Enabled", "true"));

        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Toda_resposta_leva_os_cabecalhos_de_seguranca()
    {
        await using var factory = CreateFactory();

        var response = await factory.CreateClient().GetAsync("/api/users");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Theory]
    [InlineData(PainelOrigin, true)]
    [InlineData("https://site-malicioso.exemplo", false)]
    public async Task CORS_libera_so_as_origens_configuradas(string origin, bool allowed)
    {
        await using var factory = CreateFactory();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await factory.CreateClient().SendAsync(request);

        var hasHeader = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values);
        Assert.Equal(allowed, hasHeader);
        if (allowed) Assert.Equal(origin, values!.Single());
    }

    [Fact]
    public async Task Erro_inesperado_vira_ProblemDetails_sem_detalhe_interno()
    {
        var service = new Mock<ICondominiumService>();
        service.Setup(s => s.GetPublicAsync()).ThrowsAsync(new InvalidOperationException("segredo interno da conexao"));
        await using var factory = CreateFactory(services => services.AddScoped(_ => service.Object));

        var response = await factory.CreateClient().GetAsync("/api/condominiums/public");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("segredo interno", body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
    }
}
