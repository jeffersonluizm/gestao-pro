using Microsoft.Extensions.Logging.Abstractions;

namespace GestaoPro.API.Tests.Startup;

// Versões antigas criavam, em desenvolvimento, uma conta administradora e um código de condomínio com valores
// públicos (estavam no código e na tela de login). Bancos que já passaram por essas versões continuam com eles:
// na partida, a API desativa essa conta e troca esse código, se ainda estiverem com os valores antigos.
public class LegacySeedRetirementTests
{
    private readonly Mock<ILegacySeedStore> _store = new();

    private LegacySeedRetirement Service() => new(_store.Object, NullLogger<LegacySeedRetirement>.Instance);

    [Fact]
    public async Task Conta_antiga_com_a_senha_publica_e_desativada()
    {
        _store.Setup(s => s.GetCredentialAsync(LegacySeedRetirement.LegacyAdminEmail)).ReturnsAsync(new Credential
        {
            UserId = "admin-antigo", IsActive = true, PasswordHash = BCrypt.Net.BCrypt.HashPassword(LegacySeedRetirement.LegacyAdminPassword, 4),
        });

        await Service().RunAsync();

        _store.Verify(s => s.DeactivateCredentialAsync("admin-antigo"), Times.Once);
    }

    [Fact]
    public async Task Conta_que_ja_trocou_a_senha_continua_ativa()
    {
        _store.Setup(s => s.GetCredentialAsync(LegacySeedRetirement.LegacyAdminEmail)).ReturnsAsync(new Credential
        {
            UserId = "admin-antigo", IsActive = true, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Senha-Nova-Do-Admin-2026", 4),
        });

        await Service().RunAsync();

        _store.Verify(s => s.DeactivateCredentialAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("Admin@123")]
    [InlineData("Demo-Condominio-2026")]
    public async Task Codigo_publico_do_condominio_antigo_e_trocado_por_um_aleatorio(string codigoAntigo)
    {
        _store.Setup(s => s.GetCondominiumCodeHashAsync(LegacySeedRetirement.LegacyCondominiumCnpj))
            .ReturnsAsync(BCrypt.Net.BCrypt.HashPassword(codigoAntigo, 4));
        string? novoHash = null;
        _store.Setup(s => s.ReplaceCondominiumCodeHashAsync(LegacySeedRetirement.LegacyCondominiumCnpj, It.IsAny<string>()))
            .Callback((string _, string hash) => novoHash = hash).Returns(Task.CompletedTask);

        await Service().RunAsync();

        Assert.NotNull(novoHash);
        Assert.False(BCrypt.Net.BCrypt.Verify(codigoAntigo, novoHash));
    }

    [Fact]
    public async Task Banco_sem_os_dados_antigos_nao_e_alterado()
    {
        await Service().RunAsync();

        _store.Verify(s => s.DeactivateCredentialAsync(It.IsAny<string>()), Times.Never);
        _store.Verify(s => s.ReplaceCondominiumCodeHashAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
