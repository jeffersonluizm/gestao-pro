using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace GestaoPro.API.Settings;

// appsettings.Local.json: segredos de quem desenvolve (fora do Git). Entra logo antes das variáveis de
// ambiente e da linha de comando, para que um ambiente descartável (carga, CI) sempre consiga sobrepor.
public static class LocalSettings
{
    public const string FileName = "appsettings.Local.json";

    public static IConfigurationBuilder AddLocalSettingsFile(this IConfigurationBuilder builder, string contentRoot)
    {
        var source = new JsonConfigurationSource
        {
            Path = FileName,
            Optional = true,
            ReloadOnChange = false,
            FileProvider = new PhysicalFileProvider(contentRoot),
        };

        // Logo depois do último JSON (appsettings.json, appsettings.{Ambiente}.json, user secrets) e antes das
        // variáveis de ambiente sem prefixo e da linha de comando. No host real a primeira fonte de variáveis é a de
        // prefixo ASPNETCORE_, que fica ANTES dos appsettings: por isso a referência é o último JSON.
        var lastJson = -1;
        for (var i = 0; i < builder.Sources.Count; i++)
            if (builder.Sources[i] is JsonConfigurationSource) lastJson = i;

        var firstOverrideAfterJson = -1;
        for (var i = lastJson + 1; i < builder.Sources.Count; i++)
        {
            if (builder.Sources[i] is EnvironmentVariablesConfigurationSource or CommandLineConfigurationSource)
            {
                firstOverrideAfterJson = i;
                break;
            }
        }

        if (firstOverrideAfterJson >= 0)
            builder.Sources.Insert(firstOverrideAfterJson, source);
        else if (lastJson >= 0)
            builder.Sources.Insert(lastJson + 1, source);
        else
            builder.Sources.Add(source);

        return builder;
    }
}
