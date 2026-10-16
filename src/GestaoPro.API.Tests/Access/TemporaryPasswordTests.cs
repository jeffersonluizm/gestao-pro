namespace GestaoPro.API.Tests.Access;

// Senha temporária (conta criada sem senha ou senha redefinida pela administração): aleatória, forte pela
// política e diferente a cada vez. Nunca o código do condomínio, que todos os moradores conhecem.
public class TemporaryPasswordTests
{
    [Fact]
    public void Senha_temporaria_e_forte_e_unica()
    {
        var senhas = Enumerable.Range(0, 50).Select(_ => TemporaryPassword.Generate()).ToList();

        Assert.All(senhas, senha => Assert.True(PasswordPolicy.IsStrong(senha), senha));
        Assert.Equal(senhas.Count, senhas.Distinct().Count());
    }
}
