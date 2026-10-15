using System.Security.Cryptography;

namespace GestaoPro.API.Services.Demo;

// Conta de demonstração. Não tem senha publicada: a entrada é por POST /api/demo/entrar com o papel.
public sealed record DemoAccount(string Role, string Name, string Email);

// Modo demonstração: liga com Demo:Enabled=true (desligado por padrão, independente do ambiente).
// As contas demo guardam o hash de um valor aleatório (login por senha nelas falha); quem explora a demo entra
// pelo papel. Demo:CodigoCondominio vem da configuração (forte) ou é sorteado na partida e fica visível em
// GET /api/demo, porque serve para demonstrar a ativação de acesso por CPF.
public sealed class DemoCatalog
{
    // Claim com a época da demonstração. Cada reset troca a época; token de outra época vira 401.
    public const string EpochClaim = "demoEpoch";

    public const string EmailDomain = "@demo.gestaopro.local";
    public const string CondominiumName = "Residencial Demonstração";
    public const string CondominiumCnpj = "11.222.333/0001-81";

    // Moradores pré-cadastrados sem acesso: servem para experimentar "Ative o seu acesso" com o código.
    public static readonly IReadOnlyList<(string Cpf, string Name)> PreRegisteredResidents = new[]
    {
        ("11111111111", "João da Silva"),
        ("22222222222", "Paula Andrade"),
        ("33333333333", "Pedro Oliveira"),
    };

    public bool Enabled { get; }
    public string CondominiumCode { get; }
    public IReadOnlyList<DemoAccount> Accounts { get; }

    // Preenchido pelo seeder depois de criar o condomínio (para proteger o condomínio da demonstração).
    public string? CondominiumId { get; set; }

    public string Epoch { get; private set; } = NewEpoch();

    // Ids gravados pelo seeder (contas, áreas, unidade, pré-cadastrados): não podem ser removidos nem trocados.
    private volatile HashSet<string> _seededRecords = new(StringComparer.Ordinal);

    private DemoCatalog(bool enabled, string code, IReadOnlyList<DemoAccount> accounts)
    {
        Enabled = enabled;
        CondominiumCode = code;
        Accounts = accounts;
    }

    public static readonly DemoCatalog Disabled = new(false, string.Empty, Array.Empty<DemoAccount>());

    public static DemoCatalog FromConfiguration(IConfiguration configuration)
    {
        if (!configuration.GetValue("Demo:Enabled", false))
            return Disabled;

        var code = Configured(configuration, "Demo:CodigoCondominio", CondominiumCodePolicy.IsStrong) ?? RandomSecret("Codigo");

        return new DemoCatalog(true, code, new[]
        {
            new DemoAccount("sindico", "Ana Ribeiro", $"sindico{EmailDomain}"),
            new DemoAccount("morador", "Maria Souza", $"morador{EmailDomain}"),
            new DemoAccount("porteiro", "Carlos Pereira", $"porteiro{EmailDomain}"),
        });
    }

    public DemoAccount? AccountFor(string? role) =>
        Accounts.FirstOrDefault(account => string.Equals(account.Role, role?.Trim(), StringComparison.OrdinalIgnoreCase));

    public void RenewEpoch() => Epoch = NewEpoch();

    public bool IsCurrentEpoch(string? epoch) => !Enabled || string.Equals(epoch, Epoch, StringComparison.Ordinal);

    public void SetSeededRecords(IEnumerable<string> ids) =>
        _seededRecords = new HashSet<string>(ids.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);

    public bool IsSeededRecord(string? id) => Enabled && id is not null && _seededRecords.Contains(id);

    // Hash de um valor aleatório que ninguém conhece: a conta existe, mas não entra por senha.
    public static string UnusablePasswordHash() =>
        BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    private static string NewEpoch() => Convert.ToHexString(RandomNumberGenerator.GetBytes(8));

    // Só as contas publicadas em GET /api/demo são protegidas.
    public bool IsDemoEmail(string? email) =>
        Enabled && !string.IsNullOrWhiteSpace(email) &&
        Accounts.Any(account => string.Equals(account.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

    // O domínio das contas demo é reservado: ninguém cria outra conta nele durante a demonstração.
    public bool IsReservedDomain(string? email) =>
        Enabled && !string.IsNullOrWhiteSpace(email) && email.Trim().EndsWith(EmailDomain, StringComparison.OrdinalIgnoreCase);

    public bool IsDemoCondominium(string? condominiumId) =>
        Enabled && !string.IsNullOrWhiteSpace(condominiumId) && string.Equals(condominiumId, CondominiumId, StringComparison.Ordinal);

    // Valor configurado precisa ser forte: fraco impede a partida (uma demo pública não pode ter senha óbvia).
    private static string? Configured(IConfiguration configuration, string key, Func<string, bool> isStrong)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!isStrong(value))
            throw new InvalidOperationException(
                $"{key.Replace(":", "__")} é fraco: use de 8 a 128 caracteres, sem sequências, repetições ou senhas comuns.");
        return value.Trim();
    }

    // Ex.: "Demo-k3Fq9xP2-2026": forte pela política e fácil de digitar.
    private static string RandomSecret(string prefix)
    {
        const string alphabet = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = Enumerable.Range(0, 10).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray();
        return $"{prefix}-{new string(chars)}-{DateTime.UtcNow.Year}";
    }
}
