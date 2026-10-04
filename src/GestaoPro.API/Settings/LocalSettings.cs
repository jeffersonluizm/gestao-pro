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

        var firstOverride = builder.Sources
            .Select((item, index) => (item, index))
            .FirstOrDefault(entry =>
                entry.item is EnvironmentVariablesConfigurationSource or CommandLineConfigurationSource);

        if (firstOverride.item is null)
            builder.Sources.Add(source);
        else
            builder.Sources.Insert(firstOverride.index, source);

        return builder;
    }
}
