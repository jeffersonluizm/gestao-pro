using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public record CreateCondominiumRequest(
    [StringLength(200)][Required] string Name,
    [StringLength(40)][Required][RegularExpression(@"^\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}$",
        ErrorMessage = "CNPJ deve seguir o formato 00.000.000/0000-00")] string Cnpj,
    [StringLength(200)][Required] string Street,
    [StringLength(40)][Required] string Number,
    [StringLength(200)][Required] string Neighborhood,
    [StringLength(200)][Required] string City,
    [StringLength(40)][Required] string State,
    [StringLength(40)][Required][RegularExpression(@"^\d{5}-\d{3}$",
        ErrorMessage = "CEP deve seguir o formato 00000-000")] string ZipCode,
    [StringLength(128)] string? DefaultPassword,
    [StringLength(4000)] string? Rules,
    [StringLength(4000)] string? Regulations
);

public record UpdateCondominiumRequest(
    [StringLength(200)] string? Name,
    [StringLength(200)] string? Street,
    [StringLength(40)] string? Number,
    [StringLength(200)] string? Neighborhood,
    [StringLength(200)] string? City,
    [StringLength(40)] string? State,
    [StringLength(40)] string? ZipCode,
    [StringLength(128)] string? DefaultPassword,
    [StringLength(4000)] string? Rules,
    [StringLength(4000)] string? Regulations
);

// Lista pública (tela de cadastro, sem login): só o necessário para escolher o condomínio.
public sealed record CondominiumSummary(string Id, string Name);

public class CondominiumResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string DefaultPassword { get; set; } = string.Empty;
    public string? Rules { get; set; }
    public string? Regulations { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ActiveSince =>
        $"{DateTime.UtcNow.Year - CreatedAt.Year} ano(s) e " +
        $"{Math.Abs(DateTime.UtcNow.Month - CreatedAt.Month)} mês(es)";
    public int ResidentCount { get; set; }
}

