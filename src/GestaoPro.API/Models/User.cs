using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace GestaoPro.API.Models;

public enum UserRole
{
    admin,
    sindico,
    morador,
    porteiro
}

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string CondominiumId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? UnitId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // No lugar de matricula foi utilizado o nome RegistrationNumber pois foi solicitado manter o padrão e uma unica linguagem.
    public string RegistrationNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Cpf { get; set; } = string.Empty;

    public DateTime? BirthDate { get; set; }
    public UserRole Role { get; set; } = UserRole.morador;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

