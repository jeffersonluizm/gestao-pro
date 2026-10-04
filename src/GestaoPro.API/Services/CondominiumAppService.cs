using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

public class CondominiumAppService : ICondominiumService
{
    private readonly CondominiumRepository _condominiumRepository;
    private readonly UserRepository _userRepository;

    public CondominiumAppService(CondominiumRepository condominiumRepository, UserRepository userRepository)
    {
        _condominiumRepository = condominiumRepository;
        _userRepository = userRepository;
    }

    // Busca todos os condomínios e converte para CondominiumResponse.
    public async Task<List<CondominiumResponse>> GetAllAsync()
    {
        var condominiums = await _condominiumRepository.GetAllAsync();
        var responses = new List<CondominiumResponse>();
        foreach (var condominium in condominiums)
            responses.Add(await MapToResponseAsync(condominium));
        return responses;
    }

    // Busca um condomínio pelo ID.
    public async Task<CondominiumResponse?> GetByIdAsync(string id)
    {
        var condominium = await _condominiumRepository.GetByIdAsync(id);
        return condominium is null ? null : await MapToResponseAsync(condominium);
    }

    // Cria condominio e sub classe endereço.
    public async Task<CondominiumResponse> CreateAsync(CreateCondominiumRequest request)
    {
        // Verifica se já existe condomínio com esse CNPJ
        var existing = await _condominiumRepository.GetByCnpjAsync(request.Cnpj);
        if (existing is not null)
            throw new InvalidOperationException("Já existe um condomínio com esse CNPJ.");

        var condominium = new Condominium
        {
            Name = request.Name,
            Cnpj = request.Cnpj,
            Address = new Address
            {
                Street = request.Street,
                Number = request.Number,
                Neighborhood = request.Neighborhood,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode
            },
            DefaultPassword = HashPasswordIfNeeded(string.IsNullOrWhiteSpace(request.DefaultPassword) ? "123456" : request.DefaultPassword),
            Rules = request.Rules ?? string.Empty,
            Regulations = request.Regulations ?? string.Empty
        };

        var created = await _condominiumRepository.CreateAsync(condominium);
        return await MapToResponseAsync(created);
    }

    // Atualiza condominio e sub classe endereço.
    public async Task<CondominiumResponse?> UpdateAsync(string id, UpdateCondominiumRequest request)
    {
        var condominium = await _condominiumRepository.GetByIdAsync(id);
        if (condominium is null) return null;

        if (request.Name is not null) condominium.Name = request.Name;
        if (request.Rules is not null) condominium.Rules = request.Rules;
        if (request.Regulations is not null) condominium.Regulations = request.Regulations;
        if (request.DefaultPassword is not null) condominium.DefaultPassword = HashPasswordIfNeeded(request.DefaultPassword);

        // Atualiza endereço
        if (request.Street is not null) condominium.Address.Street = request.Street;
        if (request.Number is not null) condominium.Address.Number = request.Number;
        if (request.Neighborhood is not null) condominium.Address.Neighborhood = request.Neighborhood;
        if (request.City is not null) condominium.Address.City = request.City;
        if (request.State is not null) condominium.Address.State = request.State;
        if (request.ZipCode is not null) condominium.Address.ZipCode = request.ZipCode;

        await _condominiumRepository.UpdateAsync(id, condominium);
        return await MapToResponseAsync(condominium);
    }

    // Verifica se existe antes de deletar.
    public async Task<bool> DeleteAsync(string id)
    {
        var condominium = await _condominiumRepository.GetByIdAsync(id);
        if (condominium is null) return false;
        await _condominiumRepository.DeleteAsync(id);
        return true;
    }

    // Busca a senha padrão do condomínio (Usado pelo UserAppService quando criar um novo usuário ele chama esse método para buscar a senha padrão do condomínio e atribuir ao novo usuário automaticamente).
    public async Task<string?> GetDefaultPasswordHashAsync(string condominiumId)
    {
        var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);
        if (condominium is null || string.IsNullOrWhiteSpace(condominium.DefaultPassword))
            return null;

        if (IsBcryptHash(condominium.DefaultPassword))
            return condominium.DefaultPassword;

        condominium.DefaultPassword = HashPasswordIfNeeded(condominium.DefaultPassword);
        await _condominiumRepository.UpdateAsync(condominium.Id!, condominium);
        return condominium.DefaultPassword;
    }

    // Converte o objeto Condominium do banco em CondominiumResponse.
    private async Task<CondominiumResponse> MapToResponseAsync(Condominium c)
    {
        var users = await _userRepository.GetByCondominiumAsync(c.Id!);
        var residentCount = users.Count(u =>
            u.Role == UserRole.morador || u.Role == UserRole.sindico);

        return new CondominiumResponse
        {
            Id = c.Id!,
            Name = c.Name,
            Cnpj = c.Cnpj,
            Street = c.Address.Street,
            Number = c.Address.Number,
            Neighborhood = c.Address.Neighborhood,
            City = c.Address.City,
            State = c.Address.State,
            ZipCode = c.Address.ZipCode,
            Rules = c.Rules,
            Regulations = c.Regulations,
            DefaultPassword = string.IsNullOrWhiteSpace(c.DefaultPassword) ? string.Empty : "********",
            CreatedAt = c.CreatedAt,
            ResidentCount = residentCount
        };
    }

    private static string HashPasswordIfNeeded(string password) =>
        IsBcryptHash(password) ? password : BCrypt.Net.BCrypt.HashPassword(password);

    private static bool IsBcryptHash(string value) =>
        value.StartsWith("$2a$") ||
        value.StartsWith("$2b$") ||
        value.StartsWith("$2y$");
}

