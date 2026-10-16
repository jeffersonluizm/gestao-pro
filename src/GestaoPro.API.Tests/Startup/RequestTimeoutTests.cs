using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GestaoPro.API.Tests.Startup;

// O tempo máximo de requisição existe para o reset da demonstração não esperar escritas lentas. Vale só para as
// escritas em /api: leituras (listas, relatórios) e a conexão longa do hub de notificações ficam de fora.
// No teste o limite é de 1 s (Limites:TimeoutEscritaSegundos), para não demorar.
public class RequestTimeoutTests : IDisposable
{
    private const string SecretKey = "chave-de-teste-com-mais-de-32-caracteres-0123456789";
    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private sealed class FixedAdministratorLookup : IAdministratorLookup
    {
        public Task<bool> HasAnyAdministratorAsync() => Task.FromResult(true);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseContentRoot(_contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500");
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", SecretKey);
            builder.UseSetting("JwtSettings:Issuer", "GestaoPro.API");
            builder.UseSetting("JwtSettings:Audience", "GestaoPro.API");
            builder.UseSetting("Limites:TimeoutEscritaSegundos", "1");
            builder.ConfigureTestServices(services =>
            {
                foreach (var hosted in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(hosted);
                services.AddSingleton<IAdministratorLookup, FixedAdministratorLookup>();
            });
        });

    private static string Token() => new TokenService(Options.Create(new JwtSettings
    {
        SecretKey = SecretKey, Issuer = "GestaoPro.API", Audience = "GestaoPro.API", ExpirationHours = 1,
    })).GenerateToken(new Credential
    {
        UserId = "665f1c2e9b1e8a0012345678", Role = "morador", CondominiumId = "665f1c2e9b1e8a0012345679", RegistrationNumber = "morador@teste.com",
    }).token;

    // Escrita lenta é cortada no limite. Um cliente TCP manda o cabeçalho e só um pedaço do corpo e para de enviar:
    // o servidor (Kestrel de verdade, numa porta livre escolhida pelo sistema) responde 504 ou fecha a conexão em poucos
    // segundos (limite de 1 s no teste, mais 2 s de tolerância), em vez de esperar o cliente.
    [Fact]
    public async Task Escrita_lenta_e_cortada_no_limite()
    {
        await using var factory = CreateFactory();
        factory.UseKestrel(0);
        factory.StartServer();

        var address = new Uri(factory.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!.Addresses.First());
        using var tcp = new System.Net.Sockets.TcpClient();
        await tcp.ConnectAsync(address.Host == "[::]" || address.Host == "+" ? "localhost" : address.Host, address.Port);
        var stream = tcp.GetStream();
        var started = System.Diagnostics.Stopwatch.StartNew();
        await stream.WriteAsync(Encoding.ASCII.GetBytes(
            "POST /api/demo/entrar HTTP/1.1\r\nHost: localhost\r\nContent-Type: application/json\r\n" +
            "Transfer-Encoding: chunked\r\n\r\n5\r\n{\"pap\r\n"));

        var received = new StringBuilder();
        var buffer = new byte[1024];
        using var window = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer, window.Token);
                if (read == 0) break; // conexão fechada pelo servidor
                received.Append(Encoding.ASCII.GetString(buffer, 0, read));
                if (received.ToString().Contains("\r\n\r\n")) break; // cabeçalho da resposta chegou
            }
        }
        catch (IOException)
        {
            // Conexão abortada pelo servidor.
        }

        Assert.True(started.Elapsed < TimeSpan.FromSeconds(5), $"levou {started.Elapsed.TotalSeconds:F1} s");
        var response = received.ToString();
        Assert.True(response.Length == 0 || response.StartsWith("HTTP/1.1 504"), response);
    }

    // Conexão longa do hub (Server-Sent Events) continua aberta bem depois do limite das escritas.
    [Fact]
    public async Task Conexao_do_hub_nao_cai_pelo_limite_de_tempo()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var token = Token();

        var negotiate = new HttpRequestMessage(HttpMethod.Post, "/notificacaoHub/negotiate?negotiateVersion=1");
        negotiate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var negotiated = await client.SendAsync(negotiate);
        using var json = JsonDocument.Parse(await negotiated.Content.ReadAsStringAsync());
        var connectionToken = json.RootElement.GetProperty("connectionToken").GetString();

        var stream = new HttpRequestMessage(HttpMethod.Get, $"/notificacaoHub?id={connectionToken}");
        stream.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        stream.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        using var response = await client.SendAsync(stream, HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();

        // Lê o que chegar por 2,5 s: se a conexão cair, a leitura devolve 0 (fim do fluxo) ou lança exceção.
        var closed = false;
        var buffer = new byte[256];
        using var window = new CancellationTokenSource(TimeSpan.FromSeconds(2.5));
        try
        {
            while (true)
            {
                if (await body.ReadAsync(buffer, window.Token) == 0) { closed = true; break; }
            }
        }
        catch (OperationCanceledException) when (window.IsCancellationRequested)
        {
            // A janela acabou com a conexão ainda aberta: é o esperado.
        }

        Assert.False(closed);
    }

    // Leitura não tem o limite das escritas (listas grandes, relatórios).
    [Fact]
    public async Task Leitura_nao_tem_o_limite_das_escritas()
    {
        Assert.True(GestaoPro.API.Middleware.WriteTimeoutMiddleware.AppliesTo("POST", "/api/announcements"));
        Assert.False(GestaoPro.API.Middleware.WriteTimeoutMiddleware.AppliesTo("GET", "/api/announcements"));
        Assert.False(GestaoPro.API.Middleware.WriteTimeoutMiddleware.AppliesTo("POST", "/notificacaoHub/negotiate"));
        await Task.CompletedTask;
    }
}
