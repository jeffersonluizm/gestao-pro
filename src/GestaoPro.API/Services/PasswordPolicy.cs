namespace GestaoPro.API.Services;

// Regra de senha aplicada pela API em todo lugar que define senha (cadastro, primeiro acesso, troca,
// definição pela administração): de 8 a 128 caracteres e não óbvia (mesmas regras do código do condomínio).
public static class PasswordPolicy
{
    public const int MaximumLength = 128;

    public const string WeakPasswordMessage =
        "A senha precisa ter de 8 a 128 caracteres e não pode ser óbvia (como 12345678, senha123 ou letras repetidas).";

    public static bool IsStrong(string? password) =>
        password is not null &&
        password.Length <= MaximumLength &&
        CondominiumCodePolicy.IsStrong(password);
}
