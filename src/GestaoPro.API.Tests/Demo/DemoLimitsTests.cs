using System.Net;
using System.Net.Http.Headers;
using System.Text;
using GestaoPro.API.Services.Demo;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Demo;

// A demonstração pública roda num Atlas gratuito (512 MB). Com o token do síndico demo, um script não pode
// encher o banco: teto de escritas por usuário e por IP, e teto de documentos por coleção no condomínio demo.
public class DemoQuotaFilterTests
{
    private readonly Mock<IDemoRecordCounter> _counter = new();

    private static DemoCatalog Catalog(bool enabled)
    {
        var catalog = DemoCatalog.FromConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:Enabled"] = enabled.ToString() }).Build());
        catalog.CondominiumId = CondominioA;
        return catalog;
    }

    private static IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:LimitePorColecao"] = "50", ["Demo:IntervaloResetMinutos"] = "90" }).Build();

    private async Task<(IActionResult? Result, bool NextCalled)> RunAsync(bool enabled, long count)
    {
        _counter.Setup(c => c.CountAsync("announcements", CondominioA, true, It.IsAny<CancellationToken>())).ReturnsAsync(count);
        var filter = new DemoQuotaFilter(Catalog(enabled), _counter.Object, Config(), "announcements", true);
        var context = new ActionExecutingContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()));
        });
        return (context.Result, nextCalled);
    }

    [Fact]
    public async Task Abaixo_do_teto_a_criacao_segue()
    {
        var (result, nextCalled) = await RunAsync(enabled: true, count: 49);

        Assert.True(nextCalled);
        Assert.Null(result);
    }

    [Fact]
    public async Task No_teto_responde_409_com_mensagem_amigavel()
    {
        var (result, nextCalled) = await RunAsync(enabled: true, count: 50);

        Assert.False(nextCalled);
        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("90 minutos", conflict.Value!.GetType().GetProperty("message")!.GetValue(conflict.Value) as string);
    }

    [Fact]
    public async Task Sem_o_modo_demo_nao_conta_nada()
    {
        var (_, nextCalled) = await RunAsync(enabled: false, count: 10_000);

        Assert.True(nextCalled);
        _counter.Verify(c => c.CountAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class DemoWriteLimitTests : IDisposable
{
    private const string SecretKey = "chave-de-teste-com-mais-de-32-caracteres-0123456789";
    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;
    private readonly Mock<IAnnouncementService> _announcements = new();

    public DemoWriteLimitTests()
    {
        _announcements.Setup(s => s.CreateAsync(It.IsAny<CreateAnnouncementDto>()))
            .ReturnsAsync((CreateAnnouncementDto dto) => new GestaoPro.API.Models.Announcement { Id = "665f1c2e9b1e8a0000000a10", CondominiumId = dto.CondominiumId, Title = dto.Title });
    }

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private sealed class FakeReset : IDemoReset
    {
        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoRecords : IDemoRecordCounter
    {
        public Task<long> CountAsync(string collection, string condominiumId, bool byCondominium, CancellationToken cancellationToken) => Task.FromResult(0L);
    }

    private WebApplicationFactory<Program> CreateFactory(bool demo) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseContentRoot(_contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500");
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", SecretKey);
            builder.UseSetting("JwtSettings:Issuer", "GestaoPro.API");
            builder.UseSetting("JwtSettings:Audience", "GestaoPro.API");
            builder.UseSetting("Demo:Enabled", demo.ToString());
            builder.UseSetting("Demo:LimiteEscrita:PorUsuarioPorMinuto", "3");
            builder.UseSetting("Demo:LimiteEscrita:PorIpPorMinuto", "100");
            builder.ConfigureTestServices(services =>
            {
                foreach (var hosted in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(hosted);
                services.AddScoped<IDemoReset, FakeReset>();
                services.AddSingleton<IDemoRecordCounter, NoRecords>();
                services.AddScoped(_ => _announcements.Object);
            });
        });

    private static async Task<HttpStatusCode> PublishAsync(WebApplicationFactory<Program> factory, HttpClient client)
    {
        var token = new TokenService(Options.Create(new JwtSettings
        {
            SecretKey = SecretKey, Issuer = "GestaoPro.API", Audience = "GestaoPro.API", ExpirationHours = 1,
        }), factory.Services.GetRequiredService<DemoCatalog>()).GenerateToken(new Credential
        {
            UserId = "665f1c2e9b1e8a00000000d1", Role = "sindico", CondominiumId = CondominioA, RegistrationNumber = "sindico@demo.gestaopro.local",
        }).token;
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/announcements")
        {
            Content = new StringContent($"{{\"condominiumId\":\"{CondominioA}\",\"authorId\":\"665f1c2e9b1e8a00000000d1\",\"title\":\"Aviso\",\"content\":\"Texto\",\"category\":\"Geral\"}}", Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.SendAsync(request)).StatusCode;
    }

    [Fact]
    public async Task Na_demo_o_mesmo_usuario_passa_do_teto_de_escritas_e_recebe_429()
    {
        await using var factory = CreateFactory(demo: true);
        var client = factory.CreateClient();

        var results = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++) results.Add(await PublishAsync(factory, client));

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, results.Take(3));
        Assert.Equal(HttpStatusCode.TooManyRequests, results[3]);
    }

    [Fact]
    public async Task Sem_o_modo_demo_nao_ha_teto_de_escritas()
    {
        await using var factory = CreateFactory(demo: false);
        var client = factory.CreateClient();

        var results = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++) results.Add(await PublishAsync(factory, client));

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, results);
    }
}
