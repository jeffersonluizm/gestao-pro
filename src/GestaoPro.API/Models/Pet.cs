using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.Models;

public enum PetSpecies
{
    Dog,
    Cat,
    Bird,
    Rabbit,
    Other
}

public class Pet
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [Required]
    public string OwnerId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [Required]
    public string UnitId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string CondominiumId { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public PetSpecies Species { get; set; } = PetSpecies.Other;

    public string Breed { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool Vaccination { get; set; } = false;
    public string Observations { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

