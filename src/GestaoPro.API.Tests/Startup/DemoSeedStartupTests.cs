using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GestaoPro.API.Tests.Startup;

// Sobe a API de verdade apontando para um MongoDB inalcançável: qualquer acesso ao banco
// na partida estoura o timeout. Content root temporário para não carregar appsettings.Local.json.
// O VoteIndexInitializer (índice de votos, legítimo em produção) é removido por não ser o alvo do teste.
public class DemoSeedStartupTests
{
    private const string UnreachableMongo =
        "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";

    private static WebApplicationFactory<Program> CreateFactory(string environment)
    {
        var contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseContentRoot(contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", UnreachableMongo);
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", "chave-de-teste-com-mais-de-32-caracteres-0123456789");
            builder.UseSetting("JwtSettings:Issuer", "GestaoPro.API");
            builder.UseSetting("JwtSettings:Audience", "GestaoPro.API");
            builder.ConfigureTestServices(services =>
            {
                var voteIndexInitializer = services.Single(d =>
                    d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(VoteIndexInitializer));
                services.Remove(voteIndexInitializer);
            });
        });
    }

    [Fact]
    public async Task Em_producao_a_API_sobe_sem_semear_a_conta_demo()
    {
        await using var factory = CreateFactory("Production");

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Em_desenvolvimento_a_API_semeia_a_conta_demo_na_partida()
    {
        await using var factory = CreateFactory("Development");

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.IsType<TimeoutException>(exception?.GetBaseException());
    }
}
