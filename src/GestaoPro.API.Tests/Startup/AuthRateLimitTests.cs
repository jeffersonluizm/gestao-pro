using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GestaoPro.API.Tests.Startup;

// As rotas anônimas de autenticação aceitam poucas tentativas por IP em uma janela fixa:
// passar do limite responde 429. Limite baixo via configuração só para o teste.
// Corpo vazio: a validação do modelo responde 400 sem chegar ao banco (MongoDB inalcançável).
public class AuthRateLimitTests : IDisposable
{
    private sealed class FixedAdministratorLookup : IAdministratorLookup
    {
        public Task<bool> HasAnyAdministratorAsync() => Task.FromResult(true);
    }

    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseContentRoot(_contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500");
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", "chave-de-teste-com-mais-de-32-caracteres-0123456789");
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "2");
            builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
            builder.ConfigureTestServices(services =>
            {
                services.Remove(services.Single(d =>
                    d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(VoteIndexInitializer)));
                services.AddSingleton<IAdministratorLookup, FixedAdministratorLookup>();
                // Os serviços reais acessam o banco ao serem criados; o alvo aqui é só o limite.
                services.AddScoped(_ => new AuthAppService(null!, null!, null!));
                services.AddScoped(_ => new SelfServiceAuthService(new Mock<ISelfServiceAccounts>().Object));
                services.AddScoped(_ => new Mock<IUserDirectory>().Object);
            });
        });

    [Theory]
    [InlineData("POST", "/api/auth/login")]
    [InlineData("POST", "/api/auth/check-cpf")]
    [InlineData("POST", "/api/auth/register")]
    [InlineData("PATCH", "/api/auth/change-password")]
    public async Task Passar_do_limite_de_tentativas_responde_429(string method, string path)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), path)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
            statuses.Add((await client.SendAsync(request)).StatusCode);
        }

        Assert.All(statuses.Take(2), status => Assert.NotEqual(HttpStatusCode.TooManyRequests, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[2]);
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
