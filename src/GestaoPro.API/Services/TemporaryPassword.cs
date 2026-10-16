using System.Security.Cryptography;

namespace GestaoPro.API.Services;

// Senha temporária para conta criada sem senha ou senha redefinida: aleatória, mostrada uma vez para quem
// administra e trocada no primeiro acesso. Substitui o código do condomínio, que é um segredo compartilhado.
public static class TemporaryPassword
{
    private const string Alphabet = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        string Part(int size) => new(Enumerable.Range(0, size).Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]).ToArray());
        return $"Temp-{Part(5)}-{Part(5)}";
    }
}
