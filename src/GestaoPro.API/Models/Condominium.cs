using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.Models;

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public class Condominium
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Cnpj { get; set; } = string.Empty;

    public Address Address { get; set; } = new();
    public string Rules { get; set; } = string.Empty;
    public string Regulations { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // O Adm define senha inicial padrão para todos novos usuários antes da redefinição de senha obrigatória, deve ser definida uma por condominio.
    public string DefaultPassword { get; set; } = string.Empty;
}

