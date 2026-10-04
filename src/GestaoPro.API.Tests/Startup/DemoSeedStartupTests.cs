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

    private WebApplicationFactory<Program> CreateFactory(string environment, bool demo = false)
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
            builder.UseSetting("Demo:Enabled", demo.ToString());
            builder.ConfigureTestServices(services =>
            {
                var voteIndexInitializer = services.Single(d =>
                    d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(VoteIndexInitializer));
                services.Remove(voteIndexInitializer);
            });
        });
    }

    // Sem a flag Demo:Enabled não existe conta de demonstração, nem em desenvolvimento.
    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task Sem_a_flag_a_API_sobe_sem_semear_e_sem_o_endpoint_de_demo(string environment)
    {
        await using var factory = CreateFactory(environment);

        var client = factory.CreateClient();
        var users = await client.GetAsync("/api/users");
        var demo = await client.GetAsync("/api/demo");

        Assert.Equal(HttpStatusCode.Unauthorized, users.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, demo.StatusCode);
    }

    // Os repositórios do seeder acessam o banco já no construtor: o timeout prova que o
    // DataSeeder foi acionado na partida (não verifica a conta criada, que exigiria um MongoDB).
    [Fact]
    public async Task Com_a_flag_a_API_aciona_o_seeder_de_demonstracao_mesmo_em_producao()
    {
        await using var factory = CreateFactory("Production", demo: true);

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.IsType<TimeoutException>(exception?.GetBaseException());
    }
}
