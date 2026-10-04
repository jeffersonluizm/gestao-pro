using System.Security.Cryptography;

namespace GestaoPro.API.Services.Demo;

// Conta de demonstração publicada em GET /api/demo (fonte única para o painel e o app).
public sealed record DemoAccount(string Role, string Name, string Email, string Password);

// Modo demonstração: liga com Demo:Enabled=true (desligado por padrão, independente do ambiente).
// Nada de senha no código: Demo:Senha e Demo:CodigoCondominio vêm da configuração (fortes) ou são
// sorteados na partida; quem explora a demo descobre as credenciais pelo endpoint, não pelo código-fonte.
public sealed class DemoCatalog
{
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

        var password = Configured(configuration, "Demo:Senha", PasswordPolicy.IsStrong) ?? RandomSecret("Demo");
        var code = Configured(configuration, "Demo:CodigoCondominio", CondominiumCodePolicy.IsStrong) ?? RandomSecret("Codigo");

        return new DemoCatalog(true, code, new[]
        {
            new DemoAccount("sindico", "Ana Ribeiro", $"sindico{EmailDomain}", password),
            new DemoAccount("morador", "Maria Souza", $"morador{EmailDomain}", password),
            new DemoAccount("porteiro", "Carlos Pereira", $"porteiro{EmailDomain}", password),
        });
    }

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
