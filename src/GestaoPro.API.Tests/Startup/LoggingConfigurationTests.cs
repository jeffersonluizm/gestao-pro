using Microsoft.Extensions.Configuration;

namespace GestaoPro.API.Tests.Startup;

// O cliente SignalR manda o JWT em ?access_token= e o log "Request starting" da hospedagem
// (Microsoft.AspNetCore.Hosting.Diagnostics, nível Information) imprime a URL inteira no console.
// Lê só o appsettings.Development.json copiado para a saída dos testes.
public class LoggingConfigurationTests
{
    [Fact]
    public void Em_desenvolvimento_o_log_de_requisicoes_da_hospedagem_fica_em_Warning()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        Assert.Equal("Warning", configuration["Logging:LogLevel:Microsoft.AspNetCore.Hosting.Diagnostics"]);
    }
}
