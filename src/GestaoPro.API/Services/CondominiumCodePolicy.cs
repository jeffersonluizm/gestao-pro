namespace GestaoPro.API.Services;

// Regra do código do condomínio (a senha padrão do condomínio), que prova o vínculo no autoatendimento.
// O código é sempre comparado sem espaços nas pontas (Normalize) na criação, na edição e na verificação.
public static class CondominiumCodePolicy
{
    public const int MinimumLength = 8;

    public const string WeakCodeMessage =
        "O código do condomínio precisa ter ao menos 8 caracteres e não pode ser óbvio (como 123456, 12345678 ou letras repetidas).";

    private static readonly HashSet<string> CommonCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456", "1234567", "12345678", "123456789", "1234567890", "87654321", "password", "senha123",
        "senha1234", "qwerty12", "qwerty123", "qwertyui", "abc12345", "admin123", "condominio", "12341234",
    };

    public static string Normalize(string? code) => (code ?? string.Empty).Trim();

    public static bool IsStrong(string? code)
    {
        var value = Normalize(code);
        if (value.Length < MinimumLength) return false;
        if (CommonCodes.Contains(value)) return false;
        if (value.Distinct().Count() < 4) return false;
        return !IsSequence(value);
    }

    // "12345678", "87654321", "abcdefgh": cada caractere é o anterior mais (ou menos) um.
    private static bool IsSequence(string value)
    {
        var lower = value.ToLowerInvariant();
        bool ascending = true, descending = true;
        for (var i = 1; i < lower.Length; i++)
        {
            ascending &= lower[i] - lower[i - 1] == 1;
            descending &= lower[i] - lower[i - 1] == -1;
        }
        return ascending || descending;
    }
}
