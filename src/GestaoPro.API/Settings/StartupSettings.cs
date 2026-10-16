using System.Globalization;

namespace GestaoPro.API.Settings;

// Confere na partida as chaves Demo:*, Proxy:* e Limites:*: valor errado impede a API de subir, com a chave (no formato
// de variável de ambiente) e o motivo, em vez de virar 500 na primeira requisição que usar o valor.
public static class StartupSettings
{
    private static readonly string[] Booleans = { "Demo:Enabled", "Proxy:ConfiarNoProxy" };

    // O código lê esses valores como int: a faixa vai até int.MaxValue (ou menos, onde faz sentido).
    private static readonly (string Key, int Min, int Max)[] Integers =
    {
        ("Demo:LimitePorColecao", 1, int.MaxValue),
        ("Demo:IntervaloResetMinutos", 15, 1440),
        ("Demo:LimiteEscrita:PorUsuarioPorMinuto", 1, int.MaxValue),
        ("Demo:LimiteEscrita:PorIpPorMinuto", 1, int.MaxValue),
        ("Proxy:Saltos", 1, 10),
        ("Limites:CorpoMaximoBytes", 1024, 16 * 1024 * 1024),
        ("RateLimiting:DemoEntrar:PermitLimit", 1, int.MaxValue),
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
        foreach (var (key, min, max) in Integers)
        {
            var value = configuration[key];
            if (value is null) continue;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                problems.Add($"{Env(key)}=\"{value}\": use um número inteiro de {min} a {max}.");
            else if (number < min || number > max)
                problems.Add($"{Env(key)}={number}: use de {min} a {max}.");
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
