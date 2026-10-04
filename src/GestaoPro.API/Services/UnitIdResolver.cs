using GestaoPro.API.Models;
using MongoDB.Bson;

namespace GestaoPro.API.Services;

// Resolve a unidade informada no cadastro/edição de usuário (id ou rótulo como "201").
// Só aceita unidade do condomínio do usuário: id de unidade de outro condomínio (ou inexistente) vira null,
// e o chamador responde "Unidade não encontrada", sem revelar que a unidade existe em outro condomínio.
public static class UnitIdResolver
{
    public static async Task<string?> ResolveAsync(
        string condominiumId,
        string? unitIdOrLabel,
        string? unitLabel,
        Func<string, Task<Unit?>> getById,
        Func<string, string, Task<Unit?>> findByLabel)
    {
        var value = NormalizeOptionalId(unitIdOrLabel);
        if (value is not null)
        {
            if (ObjectId.TryParse(value, out _))
            {
                var unit = await getById(value);
                return unit is not null && string.Equals(unit.CondominiumId, condominiumId, StringComparison.Ordinal)
                    ? unit.Id
                    : null;
            }

            return (await findByLabel(condominiumId, value))?.Id;
        }

        if (!string.IsNullOrWhiteSpace(unitLabel))
            return (await findByLabel(condominiumId, unitLabel))?.Id;

        return null;
    }

    private static string? NormalizeOptionalId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed is "[]" or "null" or "undefined" ? null : trimmed;
    }
}
