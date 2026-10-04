using GestaoPro.API.Settings;
using Microsoft.Extensions.Configuration;

namespace GestaoPro.API.Tests.Startup;

// O appsettings.Local.json guarda os segredos de quem desenvolve (banco real). Ele precisa ter
// prioridade sobre o appsettings.json, mas NUNCA sobre variáveis de ambiente e linha de comando:
// é por elas que um ambiente descartável (teste de carga, CI) aponta para outro banco.
public class LocalSettingsPrecedenceTests : IDisposable
{
    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-local-").FullName;
    private readonly string _prefix = $"GPTESTE_{Guid.NewGuid():N}_";

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(_prefix + "Banco", null);
        Directory.Delete(_contentRoot, recursive: true);
    }

    private ConfigurationManager BuildConfiguration(params string[] commandLine)
    {
        File.WriteAllText(Path.Combine(_contentRoot, "appsettings.json"),
            """{ "Banco": "do-appsettings", "SoNoJson": "do-appsettings" }""");
        File.WriteAllText(Path.Combine(_contentRoot, "appsettings.Local.json"),
            """{ "Banco": "do-arquivo-local", "SoNoJson": "do-arquivo-local" }""");

        var configuration = new ConfigurationManager();
        configuration.SetBasePath(_contentRoot);
        configuration.AddJsonFile("appsettings.json", optional: true);
        configuration.AddEnvironmentVariables(_prefix);
        configuration.AddCommandLine(commandLine);

        configuration.AddLocalSettingsFile(_contentRoot);
        return configuration;
    }

    [Fact]
    public void Variavel_de_ambiente_vence_o_arquivo_local()
    {
        Environment.SetEnvironmentVariable(_prefix + "Banco", "do-ambiente");

        var configuration = BuildConfiguration();

        Assert.Equal("do-ambiente", configuration["Banco"]);
    }

    [Fact]
    public void Linha_de_comando_vence_o_arquivo_local()
    {
        var configuration = BuildConfiguration("--Banco=da-linha-de-comando");

        Assert.Equal("da-linha-de-comando", configuration["Banco"]);
    }

    [Fact]
    public void Arquivo_local_vence_o_appsettings_json()
    {
        var configuration = BuildConfiguration();

        Assert.Equal("do-arquivo-local", configuration["SoNoJson"]);
    }

    [Fact]
    public void Sem_arquivo_local_a_configuracao_segue_normal()
    {
        File.Delete(Path.Combine(_contentRoot, "appsettings.Local.json"));
        var configuration = new ConfigurationManager();
        configuration.AddEnvironmentVariables(_prefix);

        configuration.AddLocalSettingsFile(_contentRoot);

        Assert.Null(configuration["Banco"]);
    }
}
