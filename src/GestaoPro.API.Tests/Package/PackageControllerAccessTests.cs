using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using Microsoft.AspNetCore.Mvc;
using static GestaoPro.API.Tests.Access.TestSession;
using PackageModel = GestaoPro.API.Models.Package;

namespace GestaoPro.API.Tests.PackageTests;

// Pacote é do destinatário: morador vê os próprios; portaria e administração, os do condomínio.
public class PackageControllerAccessTests
{
    private readonly Mock<IPackageRepository> _repository = new();
    private readonly PackageController _controller;

    private static readonly PackageModel DoJoao = Make("pacote-joao", "joao", CondominioA);
    private static readonly PackageModel DaMaria = Make("pacote-maria", "maria", CondominioA);
    private static readonly PackageModel DoCarlos = Make("pacote-carlos", "carlos", CondominioB);

    public PackageControllerAccessTests()
    {
        _controller = new PackageController(new PackageAppService(_repository.Object));
        var all = new List<PackageModel> { DoJoao, DaMaria, DoCarlos };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(all);
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(p => p.Id == id));
        _repository.Setup(r => r.GetByCondominiumAsync(It.IsAny<string>()))
            .ReturnsAsync((string condominiumId) => all.Where(p => p.CondominiumId == condominiumId).ToList());
        _repository.Setup(r => r.GetPendingByCondominiumAsync(It.IsAny<string>()))
            .ReturnsAsync((string condominiumId) => all.Where(p => p.CondominiumId == condominiumId).ToList());
        _repository.Setup(r => r.CreateAsync(It.IsAny<PackageModel>()))
            .ReturnsAsync((PackageModel package) => { package.Id = "novo"; return package; });
    }

    [Fact]
    public async Task Morador_ve_so_os_proprios_pacotes()
    {
        var all = await _controller.As("joao", "morador").GetAll();
        var other = await _controller.As("joao", "morador").GetById("pacote-maria");
        var pending = await _controller.As("joao", "morador").GetPending(CondominioA);

        Assert.Equal(new[] { "pacote-joao" }, Ids(all));
        Assert.Equal(403, StatusOf(other));
        Assert.Equal(new[] { "pacote-joao" }, Ids(pending));
    }

    [Fact]
    public async Task Porteiro_ve_os_pacotes_do_condominio_e_404_nos_de_outro()
    {
        var all = await _controller.As("porteiro-a", "porteiro").GetAll();
        var other = await _controller.As("porteiro-a", "porteiro").GetById("pacote-carlos");
        var otherCondominium = await _controller.As("porteiro-a", "porteiro").GetByCondominium(CondominioB);

        Assert.Equal(new[] { "pacote-joao", "pacote-maria" }, Ids(all));
        Assert.Equal(404, StatusOf(other));
        Assert.Equal(404, StatusOf(otherCondominium));
    }

    [Fact]
    public async Task Admin_registra_no_proprio_condominio_mesmo_sem_cabecalho_e_404_em_outro()
    {
        var own = await _controller.As("admin-a", "admin").Register(new CreatePackageDto { TrackingCode = "BR1", RecipientId = "joao" });
        _controller.Request.Headers["X-Condominium-Id"] = CondominioB;
        var other = await _controller.Register(new CreatePackageDto { TrackingCode = "BR2", RecipientId = "carlos" });

        Assert.Equal(201, StatusOf(own));
        _repository.Verify(r => r.CreateAsync(It.Is<PackageModel>(p => p.CondominiumId == CondominioA)), Times.Once);
        Assert.Equal(404, StatusOf(other));
    }

    [Fact]
    public async Task Admin_altera_retira_e_remove_so_no_proprio_condominio()
    {
        var update = await _controller.As("admin-a", "admin").Update("pacote-carlos", new UpdatePackageDto { Status = "picked_up" });
        var moveAway = await _controller.As("admin-a", "admin").Update("pacote-joao", new UpdatePackageDto { CondominiumId = CondominioB });
        var pickup = await _controller.As("admin-a", "admin").PickUp("pacote-carlos");
        var delete = await _controller.As("admin-a", "admin").Delete("pacote-carlos");
        var ownDelete = await _controller.As("admin-a", "admin").Delete("pacote-joao");

        Assert.Equal(404, StatusOf(update));
        Assert.Equal(404, StatusOf(moveAway));
        Assert.Equal(404, StatusOf(pickup));
        Assert.Equal(404, StatusOf(delete));
        Assert.Equal(204, StatusOf(ownDelete));
        _repository.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<PackageModel>()), Times.Never);
        _repository.Verify(r => r.PickUpAsync(It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.DeleteAsync("pacote-carlos"), Times.Never);
    }

    private static IEnumerable<string?> Ids(IActionResult result) =>
        ValueOf<IEnumerable<PackageModel>>(result).Select(p => p.Id);

    private static PackageModel Make(string id, string recipientId, string condominiumId) => new()
    {
        Id = id,
        RecipientId = recipientId,
        CondominiumId = condominiumId,
        TrackingCode = id,
    };
}
