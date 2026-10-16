using System.Globalization;

namespace GestaoPro.API.Settings;

// Confere na partida as chaves Demo:*, Proxy:* e Limites:*: valor errado impede a API de subir, com a chave (no formato
// de variável de ambiente) e o motivo, em vez de virar 500 na primeira requisição que usar o valor.
public static class StartupSettings
{
    private static readonly string[] Booleans = { "Demo:Enabled", "Proxy:ConfiarNoProxy" };

    private static readonly (string Key, long Min)[] Integers =
    {
        ("Demo:LimitePorColecao", 1),
        ("Demo:IntervaloResetMinutos", 1),
        ("Demo:LimiteEscrita:PorUsuarioPorMinuto", 1),
        ("Demo:LimiteEscrita:PorIpPorMinuto", 1),
        ("Proxy:Saltos", 1),
        ("Limites:CorpoMaximoBytes", 1024),
        ("RateLimiting:DemoEntrar:PermitLimit", 1),
    };

    public static List<string> Problems(IConfiguration configuration)
    {
        var problems = new List<string>();
        foreach (var key in Booleans)
        {
            var value = configuration[key];
            if (value is not null && !bool.TryParse(value, out _))
                problems.Add($"{Env(key)}=\"{value}\": use true ou false.");
        }
        foreach (var (key, min) in Integers)
        {
            var value = configuration[key];
            if (value is null) continue;
            if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                problems.Add($"{Env(key)}=\"{value}\": use um número inteiro.");
            else if (number < min)
                problems.Add($"{Env(key)}={number}: o mínimo é {min}.");
        }
        return problems;
    }

    public static void Validate(IConfiguration configuration)
    {
        var problems = Problems(configuration);
        if (problems.Count > 0)
            throw new InvalidOperationException("Configuração inválida: " + string.Join(" ", problems));
    }

    private static string Env(string key) => key.Replace(":", "__");
}
