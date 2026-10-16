using System.Collections.Concurrent;
using System.Net;
using System.Text;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestaoPro.API.Tests.Startup;

// Configuração errada aparece na partida, com a chave e o motivo, e não como 500 em toda criação.
public class StartupValidationTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value)).Build();

    [Theory]
    [InlineData("Demo:LimitePorColecao", "abc")]
    [InlineData("Demo:LimitePorColecao", "0")]
    [InlineData("Demo:IntervaloResetMinutos", "noventa")]
    [InlineData("Demo:LimiteEscrita:PorUsuarioPorMinuto", "-1")]
    [InlineData("Demo:LimiteEscrita:PorIpPorMinuto", "x")]
    [InlineData("Demo:Enabled", "talvez")]
    [InlineData("Proxy:ConfiarNoProxy", "sim")]
    [InlineData("Proxy:Saltos", "0")]
    [InlineData("Proxy:Saltos", "dois")]
    [InlineData("Limites:CorpoMaximoBytes", "grande")]
    public void Valor_invalido_aponta_a_chave(string key, string value)
    {
        var problems = StartupSettings.Problems(Config((key, value)));

        Assert.Contains(problems, problem => problem.Contains(key.Replace(":", "__")));
    }

    [Fact]
    public void Configuracao_valida_ou_ausente_passa()
    {
        Assert.Empty(StartupSettings.Problems(Config()));
        Assert.Empty(StartupSettings.Problems(Config(("Demo:Enabled", "true"), ("Demo:LimitePorColecao", "300"),
            ("Proxy:ConfiarNoProxy", "true"), ("Proxy:Saltos", "2"), ("Proxy:CabecalhoIp", "True-Client-IP"))));
    }
}

public class StartupValidationIntegrationTests : IDisposable
{
    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILoggerProvider, ILogger
    {
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            messages.Enqueue($"{logLevel}: {formatter(state, exception)} {exception}");
        public void Dispose() { }
    }

    private WebApplicationFactory<Program> CreateFactory(ConcurrentQueue<string> logs, params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseContentRoot(_contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500");
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", "chave-de-teste-com-mais-de-32-caracteres-0123456789");
            foreach (var (key, value) in settings) builder.UseSetting(key, value);
            builder.ConfigureLogging(logging => logging.AddProvider(new CapturingLogger(logs)));
            builder.ConfigureTestServices(services =>
            {
                foreach (var hosted in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(hosted);
            });
        });

    [Fact]
    public async Task Configuracao_invalida_impede_a_partida_com_mensagem()
    {
        await using var factory = CreateFactory(new ConcurrentQueue<string>(), ("Demo:LimitePorColecao", "abc"));

        var error = Record.Exception(() => factory.CreateClient());

        Assert.Contains("Demo__LimitePorColecao", error?.GetBaseException().Message);
    }

    [Fact]
    public async Task Registra_o_proxy_efetivo_e_avisa_quando_o_cabecalho_configurado_nao_chega()
    {
        var logs = new ConcurrentQueue<string>();
        await using var factory = CreateFactory(logs, ("Proxy:ConfiarNoProxy", "true"), ("Proxy:CabecalhoIp", "True-Client-IP"));

        await factory.CreateClient().GetAsync("/");

        Assert.Contains(logs, log => log.Contains("Proxy") && log.Contains("True-Client-IP") && log.Contains("saltos"));
        Assert.Contains(logs, log => log.StartsWith("Warning") && log.Contains("True-Client-IP") && log.Contains("não chegou"));
    }

    // Corpo sem Content-Length (chunked) acima de 64 KB: 413 com mensagem, não 500 com exceção no log.
    [Fact]
    public async Task Corpo_chunked_acima_do_limite_responde_413()
    {
        var logs = new ConcurrentQueue<string>();
        await using var factory = CreateFactory(logs);
        // Rota sem acesso ao banco (o controller só lê o corpo): o teste isola o limite de leitura.
        var content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"papel\":\"" + new string('a', 70 * 1024) + "\"}")));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/demo/entrar") { Content = content };
        request.Headers.TransferEncodingChunked = true;

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.DoesNotContain(logs, log => log.StartsWith("Error"));
    }
}
