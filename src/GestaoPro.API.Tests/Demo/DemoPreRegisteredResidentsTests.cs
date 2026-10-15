using GestaoPro.API.Services.Demo;
using Microsoft.Extensions.Logging.Abstractions;

namespace GestaoPro.API.Tests.Demo;

// CPFs de exemplo da demonstração ("Ative o seu acesso"). Se um desses CPFs já pertence a alguém de outro
// condomínio, o seeder não pode mover essa pessoa para o condomínio demo: ignora o CPF e registra no log.
public class DemoPreRegisteredResidentsTests
{
    private const string Demo = "665f1c2e9b1e8a00000000c1";
    private readonly Mock<IDemoResidentStore> _store = new();

    private DemoPreRegisteredResidents Service() => new(_store.Object, NullLogger<DemoPreRegisteredResidents>.Instance);

    [Fact]
    public async Task Cria_os_cpfs_que_faltam_no_condominio_demo()
    {
        await Service().EnsureAsync(Demo);

        foreach (var (cpf, _) in DemoCatalog.PreRegisteredResidents)
            _store.Verify(s => s.CreateAsync(It.Is<User>(u => u.Cpf == cpf && u.CondominiumId == Demo)), Times.Once);
    }

    [Fact]
    public async Task Cpf_de_outro_condominio_nao_e_movido_nem_recriado()
    {
        var (cpf, _) = DemoCatalog.PreRegisteredResidents[0];
        _store.Setup(s => s.GetByCpfAsync(cpf)).ReturnsAsync(new User { Id = "outro", Cpf = cpf, CondominiumId = "665f1c2e9b1e8a00000000ff" });

        var ids = await Service().EnsureAsync(Demo);

        _store.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<User>()), Times.Never);
        _store.Verify(s => s.CreateAsync(It.Is<User>(u => u.Cpf == cpf)), Times.Never);
        Assert.DoesNotContain("outro", ids);
    }

    [Fact]
    public async Task Cpf_que_ja_esta_no_condominio_demo_e_mantido()
    {
        var (cpf, _) = DemoCatalog.PreRegisteredResidents[0];
        _store.Setup(s => s.GetByCpfAsync(cpf)).ReturnsAsync(new User { Id = "ja-existe", Cpf = cpf, CondominiumId = Demo });

        var ids = await Service().EnsureAsync(Demo);

        _store.Verify(s => s.CreateAsync(It.Is<User>(u => u.Cpf == cpf)), Times.Never);
        Assert.Contains("ja-existe", ids);
    }
}
