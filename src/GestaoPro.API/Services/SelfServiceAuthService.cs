using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;

namespace GestaoPro.API.Services;

// Dados usados pelas rotas anônimas de autoatendimento (separados para teste com Moq).
public interface ISelfServiceAccounts
{
    Task<User?> GetUserByCpfAsync(string cpf);
    Task UpdateUserAsync(User user);
    Task UpdateUserPasswordAsync(string userId, string passwordHash);
    Task<Credential?> GetCredentialAsync(string userId);
    Task CreateCredentialAsync(Credential credential);
    Task UpdateCredentialPasswordAsync(string userId, string passwordHash, bool mustChangePassword);
    // Hash (BCrypt) da senha padrão do condomínio, usada como código do condomínio no cadastro.
    Task<string?> GetCondominiumCodeHashAsync(string condominiumId);
}

// Verificação de senha/código (BCrypt), separada para os testes contarem quantas vezes roda.
public interface IPasswordVerifier
{
    bool Verify(string text, string hash);
}

public class BcryptPasswordVerifier : IPasswordVerifier
{
    public bool Verify(string text, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(text, hash); }
        catch { return false; } // valor guardado que não é hash BCrypt
    }
}

public class MongoSelfServiceAccounts : ISelfServiceAccounts
{
    private readonly UserRepository _users;
    private readonly CredentialRepository _credentials;
    private readonly CondominiumAppService _condominiums;

    public MongoSelfServiceAccounts(UserRepository users, CredentialRepository credentials, CondominiumAppService condominiums)
    {
        _users = users;
        _credentials = credentials;
        _condominiums = condominiums;
    }

    public Task<User?> GetUserByCpfAsync(string cpf) => _users.GetByCpfAsync(cpf);

    public Task UpdateUserAsync(User user) => _users.UpdateAsync(user.Id!, user);

    public Task UpdateUserPasswordAsync(string userId, string passwordHash) => _users.UpdatePasswordAsync(userId, passwordHash);

    public Task<Credential?> GetCredentialAsync(string userId) => _credentials.GetByUserIdAsync(userId);

    public Task CreateCredentialAsync(Credential credential) => _credentials.CreateAsync(credential);

    public Task UpdateCredentialPasswordAsync(string userId, string passwordHash, bool mustChangePassword) =>
        _credentials.UpdatePasswordAsync(userId, passwordHash, mustChangePassword);

    public Task<string?> GetCondominiumCodeHashAsync(string condominiumId) =>
        _condominiums.GetDefaultPasswordHashAsync(condominiumId);
}

// Autoatendimento sem login: verificar CPF, ativar a própria conta e trocar a senha no primeiro acesso.
// - Só morador ativa conta por aqui; síndico, admin e porteiro recebem credencial da administração.
// - O cadastro exige o código do condomínio (a senha padrão do condomínio, que a administração entrega
//   aos moradores e é a senha inicial das contas criadas por ela): prova o vínculo com o condomínio.
// - As recusas têm sempre a mesma mensagem e não devolvem nome nem dado pessoal, para a rota não servir
//   de consulta de CPF. O código é conferido antes de olhar o CPF e o BCrypt roda sempre uma vez (hash fixo
//   quando o condomínio não tem código), para o tempo de resposta não revelar se o CPF existe.
// - O código é comparado sem espaços nas pontas (CondominiumCodePolicy.Normalize); os clientes mandam como digitado.
// - Qualquer credencial já existente (ativa ou desativada) impede o cadastro: reativar é com a administração.
public class SelfServiceAuthService
{
    public const string CheckRefusedMessage =
        "Não foi possível continuar com esses dados. Confira o CPF, o condomínio e o código do condomínio ou procure a administração.";
    public const string RegisterRefusedMessage = CheckRefusedMessage;
    public const string ChangePasswordRefusedMessage =
        "Não foi possível trocar a senha. Confira a senha atual ou procure a administração.";

    // Hash de um valor aleatório, com o custo padrão: usado quando o condomínio não tem código, para gastar o mesmo tempo.
    private static readonly string UnknownCondominiumHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));

    private readonly ISelfServiceAccounts _accounts;
    private readonly IPasswordVerifier _verifier;

    public SelfServiceAuthService(ISelfServiceAccounts accounts, IPasswordVerifier verifier)
    {
        _accounts = accounts;
        _verifier = verifier;
    }

    public async Task<CheckCpfResponse> CheckCpfAsync(CheckCpfRequest request)
    {
        var user = await FindEligibleResidentAsync(request.Cpf, request.CondominiumId, request.CondominiumCode);
        return user is null
            ? new CheckCpfResponse(false, false, CheckRefusedMessage)
            : new CheckCpfResponse(true, IsSkeletonUser(user), "CPF verificado. Complete o cadastro.");
    }

    public async Task<SelfRegisterResponse> SelfRegisterAsync(SelfRegisterRequest request)
    {
        var user = await FindEligibleResidentAsync(request.Cpf, request.CondominiumId, request.CondominiumCode)
            ?? throw new InvalidOperationException(RegisterRefusedMessage);

        var registrationNumber = user.RegistrationNumber;

        if (IsSkeletonUser(user) || !string.IsNullOrWhiteSpace(request.Email))
        {
            if (!string.IsNullOrWhiteSpace(request.Name)) user.Name = request.Name.Trim();
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var email = request.Email.Trim().ToLowerInvariant();
                user.Email = email;
                user.RegistrationNumber = email;
                registrationNumber = email;
            }
            if (!string.IsNullOrWhiteSpace(request.Phone))
                user.Phone = new string(request.Phone.Where(char.IsDigit).ToArray());
            try { await _accounts.UpdateUserAsync(user); }
            catch { throw new InvalidOperationException("E-mail já cadastrado no sistema."); }
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _accounts.CreateCredentialAsync(new Credential
        {
            UserId = user.Id!,
            RegistrationNumber = registrationNumber,
            PasswordHash = passwordHash,
            Role = UserRole.morador.ToString(),
            CondominiumId = user.CondominiumId,
            MustChangePassword = false,
            IsActive = true
        });

        await _accounts.UpdateUserPasswordAsync(user.Id!, passwordHash);

        return new SelfRegisterResponse(registrationNumber, user.Name);
    }

    // Rota anônima: só para o primeiro acesso (credencial marcada para trocar a senha), com a senha atual.
    public async Task ChangePasswordOnFirstAccessAsync(AuthChangePasswordRequest request)
    {
        var credential = await _accounts.GetCredentialAsync(request.UserId);
        if (credential is null || !credential.IsActive || !credential.MustChangePassword ||
            !_verifier.Verify(request.CurrentPassword, credential.PasswordHash))
        {
            throw new InvalidOperationException(ChangePasswordRefusedMessage);
        }

        await _accounts.UpdateCredentialPasswordAsync(
            request.UserId,
            BCrypt.Net.BCrypt.HashPassword(request.NewPassword),
            false);
    }

    // Código do condomínio certo e forte, depois morador ativo desse condomínio sem nenhuma credencial.
    private async Task<User?> FindEligibleResidentAsync(string cpf, string condominiumId, string condominiumCode)
    {
        var code = CondominiumCodePolicy.Normalize(condominiumCode);
        var codeHash = string.IsNullOrWhiteSpace(condominiumId)
            ? null
            : await _accounts.GetCondominiumCodeHashAsync(condominiumId);

        // Sempre uma verificação BCrypt, exista ou não o condomínio, antes de qualquer consulta pelo CPF.
        var codeMatches = _verifier.Verify(code, codeHash ?? UnknownCondominiumHash);
        // Código fraco (condomínio ainda com o padrão antigo, como 123456) não serve de prova de vínculo.
        if (codeHash is null || !codeMatches || !CondominiumCodePolicy.IsStrong(code)) return null;

        var digits = new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        var user = await _accounts.GetUserByCpfAsync(digits);
        if (user is null || !user.IsActive || user.Role != UserRole.morador ||
            !string.Equals(user.CondominiumId, condominiumId, StringComparison.Ordinal))
        {
            return null;
        }

        return await _accounts.GetCredentialAsync(user.Id!) is null ? user : null;
    }

    private static bool IsSkeletonUser(User user) =>
        user.Email.EndsWith("@pending.gestaopro.local") ||
        string.IsNullOrWhiteSpace(user.Name) ||
        user.Name.EndsWith("(Teste)");
}
