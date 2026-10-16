using System.Security.Cryptography;

namespace GestaoPro.API.Services;

// Matrícula do cadastro feito pela administração: COND-ano-6 dígitos, conferida antes de usar.
// Na colisão (rara com 1 milhão de combinações por ano) sorteia de novo, até MaxAttempts vezes.
public static class RegistrationNumber
{
    public const int MaxAttempts = 5;
    public const string ExhaustedMessage = "Não foi possível gerar uma matrícula livre agora. Tente cadastrar de novo.";

    public static async Task<string> NextAsync(Func<string, Task<bool>> exists, int? year = null)
    {
        var prefix = $"COND-{year ?? DateTime.UtcNow.Year}-";
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = prefix + RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            if (!await exists(candidate)) return candidate;
        }
        throw new InvalidOperationException(ExhaustedMessage);
    }
}
