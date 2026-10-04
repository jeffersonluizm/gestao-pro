namespace GestaoPro.API.Tests.Access;

// O código do condomínio (senha padrão) é a prova de vínculo no autoatendimento: não pode ser trivial.
public class CondominiumCodePolicyTests
{
    [Theory]
    [InlineData("Jardim-Azul-2026")]
    [InlineData("Demo-Condominio-2026")]
    [InlineData("  ipe-roxo-77  ")]
    public void Codigos_fortes_sao_aceitos(string code)
    {
        Assert.True(CondominiumCodePolicy.IsStrong(code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123456")]
    [InlineData("1234567")]
    [InlineData("12345678")]
    [InlineData("87654321")]
    [InlineData("abcdefgh")]
    [InlineData("11111111")]
    [InlineData("abababab")]
    [InlineData("password")]
    [InlineData("Senha123")]
    [InlineData("   12345678   ")]
    public void Codigos_curtos_repetitivos_sequenciais_ou_comuns_sao_recusados(string? code)
    {
        Assert.False(CondominiumCodePolicy.IsStrong(code));
    }

    // Criação e edição de condomínio recusam código fraco antes de tocar no banco.
    [Fact]
    public async Task Criar_condominio_com_codigo_fraco_ou_sem_codigo_e_recusado()
    {
        var service = new CondominiumAppService(null!, null!);

        foreach (var code in new string?[] { null, "123456", "12345678" })
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new CreateCondominiumRequest(
                "Condomínio", "00.000.000/0001-00", "Rua", "1", "Centro", "BH", "MG", "30000-000", code, null, null)));
            Assert.Equal(CondominiumCodePolicy.WeakCodeMessage, error.Message);
        }
    }

    [Fact]
    public async Task Editar_condominio_com_codigo_fraco_e_recusado()
    {
        var service = new CondominiumAppService(null!, null!);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(
            "665f1c2e9b1e8a00000000a1",
            new UpdateCondominiumRequest(null, null, null, null, null, null, null, "123456", null, null)));

        Assert.Equal(CondominiumCodePolicy.WeakCodeMessage, error.Message);
    }
}
