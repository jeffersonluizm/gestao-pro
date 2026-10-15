using GestaoPro.API.Models;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.DTOs;

public record CreatePetRequest(
    [StringLength(100)][Required] string OwnerId,
    [StringLength(100)][Required] string UnitId,
    [StringLength(100)][Required] string CondominiumId,
    [StringLength(200)][Required] string Name,
    [Required] PetSpecies Species,
    [StringLength(200)] string? Breed,
    [StringLength(200)] string? Color,
    bool Vaccination,
    [StringLength(4000)] string? Observations
);

public record UpdatePetRequest(
    [StringLength(200)] string? Name,
    PetSpecies? Species,
    [StringLength(200)] string? Breed,
    [StringLength(200)] string? Color,
    bool? Vaccination,
    [StringLength(4000)] string? Observations,
    bool? IsActive
);

public class PetResponse
{
    public string Id { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string UnitId { get; set; } = string.Empty;
    public string CondominiumId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PetSpecies Species { get; set; }
    public string Breed { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool Vaccination { get; set; }
    public string Observations { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

