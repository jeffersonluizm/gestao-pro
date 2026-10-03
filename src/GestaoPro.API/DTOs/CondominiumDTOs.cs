using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public record CreateCondominiumRequest(
    [Required] string Name,
    [Required][RegularExpression(@"^\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}$",
        ErrorMessage = "CNPJ deve seguir o formato 00.000.000/0000-00")] string Cnpj,
    [Required] string Street,
    [Required] string Number,
    [Required] string Neighborhood,
    [Required] string City,
    [Required] string State,
    [Required][RegularExpression(@"^\d{5}-\d{3}$",
        ErrorMessage = "CEP deve seguir o formato 00000-000")] string ZipCode,
    string? DefaultPassword,
    string? Rules,
    string? Regulations
);

public record UpdateCondominiumRequest(
    string? Name,
    string? Street,
    string? Number,
    string? Neighborhood,
    string? City,
    string? State,
    string? ZipCode,
    string? DefaultPassword,
    string? Rules,
    string? Regulations
);

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

