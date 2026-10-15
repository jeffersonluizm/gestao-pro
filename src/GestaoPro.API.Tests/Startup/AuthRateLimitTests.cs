using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GestaoPro.API.Tests.Startup;

// Limites das rotas anônimas de autenticação (Program.cs):
// - um balde por IP para cada grupo: login; check-cpf + register; change-password;
// - check-cpf + register também têm um balde por condomínio (força bruta do código vinda de vários IPs);
// - acima do limite: 429 com Retry-After;
// - o IP do X-Forwarded-For só vale com Proxy:ConfiarNoProxy=true.
// Limites baixos via configuração só para o teste; os serviços reais (que acessam o banco) são trocados.
public class AuthRateLimitTests : IDisposable
{
    private sealed class FixedAdministratorLookup : IAdministratorLookup
    {
        public Task<bool> HasAnyAdministratorAsync() => Task.FromResult(true);
    }

    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private WebApplicationFactory<Program> CreateFactory(params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseContentRoot(_contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500");
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", "chave-de-teste-com-mais-de-32-caracteres-0123456789");
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "2");
            builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
            foreach (var (key, value) in settings) builder.UseSetting(key, value);
            builder.ConfigureTestServices(services =>
            {
                services.Remove(services.Single(d =>
                    d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(VoteIndexInitializer)));
                services.AddSingleton<IAdministratorLookup, FixedAdministratorLookup>();
                services.AddScoped(_ => new AuthAppService(null!, null!, null!));
                services.AddScoped(_ => new SelfServiceAuthService(new Mock<ISelfServiceAccounts>().Object, new BcryptPasswordVerifier()));
                services.AddScoped(_ => new Mock<IUserDirectory>().Object);
            });
        });

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, string method, string path, string body = "{}", string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        if (forwardedFor is not null) request.Headers.Add("X-Forwarded-For", forwardedFor);
        return await client.SendAsync(request);
    }

    [Theory]
    [InlineData("POST", "/api/auth/login")]
    [InlineData("POST", "/api/auth/check-cpf")]
    [InlineData("POST", "/api/auth/register")]
    [InlineData("PATCH", "/api/auth/change-password")]
    public async Task Passar_do_limite_de_tentativas_responde_429_com_Retry_After(string method, string path)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var responses = new List<HttpResponseMessage>();
        for (var attempt = 0; attempt < 3; attempt++)
            responses.Add(await SendAsync(client, method, path));

        Assert.All(responses.Take(2), response => Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode));
        Assert.Equal(HttpStatusCode.TooManyRequests, responses[2].StatusCode);
        Assert.True(responses[2].Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task Estourar_o_login_nao_bloqueia_o_cadastro_nem_a_troca_de_senha()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        for (var attempt = 0; attempt < 3; attempt++) await SendAsync(client, "POST", "/api/auth/login");
        var login = await SendAsync(client, "POST", "/api/auth/login");
        var register = await SendAsync(client, "POST", "/api/auth/register");
        var changePassword = await SendAsync(client, "PATCH", "/api/auth/change-password");

        Assert.Equal(HttpStatusCode.TooManyRequests, login.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, register.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, changePassword.StatusCode);
    }

    // Vários IPs tentando o código do mesmo condomínio: o balde do condomínio segura; outro condomínio segue livre.
    [Fact]
    public async Task Limite_por_condominio_vale_mesmo_com_IPs_diferentes()
    {
        await using var factory = CreateFactory(
            ("RateLimiting:Auth:PermitLimit", "100"),
            ("RateLimiting:Auth:PerCondominiumPermitLimit", "2"));
        var client = factory.CreateClient();
        static string Body(string condominiumId) =>
            $$"""{"cpf":"11111111111","condominiumId":"{{condominiumId}}","condominiumCode":"Jardim-Azul-2026"}""";

        var first = await SendAsync(client, "POST", "/api/auth/check-cpf", Body("condominio-a"));
        var second = await SendAsync(client, "POST", "/api/auth/check-cpf", Body("condominio-a"));
        var third = await SendAsync(client, "POST", "/api/auth/check-cpf", Body("condominio-a"));
        var otherCondominium = await SendAsync(client, "POST", "/api/auth/check-cpf", Body("condominio-b"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.True(third.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        Assert.Equal(HttpStatusCode.OK, otherCondominium.StatusCode);
    }

    [Fact]
    public async Task X_Forwarded_For_so_separa_os_IPs_com_o_proxy_confiado()
    {
        await using var trusted = CreateFactory(("Proxy:ConfiarNoProxy", "true"));
        await using var untrusted = CreateFactory();

        async Task<HttpStatusCode> ThirdAttemptFromAnotherIp(WebApplicationFactory<Program> factory)
        {
            var client = factory.CreateClient();
            await SendAsync(client, "POST", "/api/auth/login", forwardedFor: "203.0.113.10");
            await SendAsync(client, "POST", "/api/auth/login", forwardedFor: "203.0.113.10");
            return (await SendAsync(client, "POST", "/api/auth/login", forwardedFor: "203.0.113.20")).StatusCode;
        }

        Assert.NotEqual(HttpStatusCode.TooManyRequests, await ThirdAttemptFromAnotherIp(trusted));
        Assert.Equal(HttpStatusCode.TooManyRequests, await ThirdAttemptFromAnotherIp(untrusted));
    }

    // Se o proxy da hospedagem (por exemplo, a borda do Render) põe o IP do visitante em outro cabeçalho, como o
    // True-Client-IP, a configuração Proxy:CabecalhoIp aponta para ele (ver docs/deploy.md, "Conferir o IP real").
    [Fact]
    public async Task Cabecalho_do_ip_do_proxy_e_configuravel()
    {
        await using var factory = CreateFactory(("Proxy:ConfiarNoProxy", "true"), ("Proxy:CabecalhoIp", "True-Client-IP"));
        var client = factory.CreateClient();

        async Task<HttpStatusCode> LoginFrom(string ip)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
            request.Headers.Add("True-Client-IP", ip);
            request.Headers.Add("X-Forwarded-For", "10.0.0.1");
            return (await client.SendAsync(request)).StatusCode;
        }

        await LoginFrom("203.0.113.10");
        await LoginFrom("203.0.113.10");

        Assert.Equal(HttpStatusCode.TooManyRequests, await LoginFrom("203.0.113.10"));
        Assert.NotEqual(HttpStatusCode.TooManyRequests, await LoginFrom("203.0.113.20"));
    }

    [Fact]
    public async Task Rotas_autenticadas_nao_entram_no_limite_de_autenticacao()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 3; attempt++)
            statuses.Add((await client.GetAsync("/api/users")).StatusCode);

        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
    }
}
