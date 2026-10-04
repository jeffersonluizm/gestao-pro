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
public class DemoSeedStartupTests : IDisposable
{
    private const string UnreachableMongo =
        "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";

    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private WebApplicationFactory<Program> CreateFactory(string environment)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseContentRoot(_contentRoot);
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

    // Os repositórios do seeder acessam o banco já no construtor: o timeout prova que o
    // DataSeeder foi acionado na partida (não verifica a conta criada, que exigiria um MongoDB).
    [Fact]
    public async Task Em_desenvolvimento_a_API_aciona_o_seeder_na_partida()
    {
        await using var factory = CreateFactory("Development");

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.IsType<TimeoutException>(exception?.GetBaseException());
    }
}
