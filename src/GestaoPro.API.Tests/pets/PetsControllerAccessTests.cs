using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using Microsoft.AspNetCore.Mvc;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.PetTests;

// Pet é da unidade: o morador vê os próprios e os da sua unidade (o app mostra "Pets da sua unidade");
// a administração vê e altera os do condomínio.
public class PetsControllerAccessTests
{
    private readonly Mock<IPetService> _service = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly Mock<IUnitService> _units = new();
    private readonly PetsController _controller;

    private static readonly PetResponse RexDoJoao = Make("pet-joao", "joao", "unidade-101", CondominioA);
    private static readonly PetResponse MiaDaAna = Make("pet-ana", "ana", "unidade-101", CondominioA);
    private static readonly PetResponse BobDaMaria = Make("pet-maria", "maria", "unidade-202", CondominioA);
    private static readonly PetResponse ThorDoCarlos = Make("pet-carlos", "carlos", "unidade-901", CondominioB);

    public PetsControllerAccessTests()
    {
        _controller = new PetsController(_service.Object, _users.Object, _units.Object);
        _users.Setup(u => u.GetByIdAsync("maria")).ReturnsAsync(new UserResponse { Id = "maria", CondominiumId = CondominioA });
        _users.Setup(u => u.GetByIdAsync("carlos")).ReturnsAsync(new UserResponse { Id = "carlos", CondominiumId = CondominioB });
        _units.Setup(u => u.GetByIdAsync(UnidadeA))
            .ReturnsAsync(new UnitResponse(UnidadeA, CondominioA, "202", null, 2, default, new List<string> { "maria" }, 1, null, null, DateTime.UtcNow));
        _units.Setup(u => u.GetByIdAsync(UnidadeB))
            .ReturnsAsync(new UnitResponse(UnidadeB, CondominioB, "901", null, 9, default, new List<string> { "carlos" }, 1, null, null, DateTime.UtcNow));
        var all = new List<PetResponse> { RexDoJoao, MiaDaAna, BobDaMaria, ThorDoCarlos };
        _service.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(p => p.Id == id));
        _service.Setup(s => s.GetAllByCondominiumAsync(It.IsAny<string>()))
            .ReturnsAsync((string condominiumId) => all.Where(p => p.CondominiumId == condominiumId).ToList());
        _service.Setup(s => s.GetByUnitAsync(It.IsAny<string>()))
            .ReturnsAsync((string unitId) => all.Where(p => p.UnitId == unitId).ToList());
        _service.Setup(s => s.GetByOwnerAsync(It.IsAny<string>()))
            .ReturnsAsync((string ownerId) => all.Where(p => p.OwnerId == ownerId).ToList());
        _service.Setup(s => s.DeleteAsync(It.IsAny<string>())).ReturnsAsync(true);
        _users.Setup(u => u.GetByIdAsync("joao"))
            .ReturnsAsync(new UserResponse { Id = "joao", CondominiumId = CondominioA, UnitId = "unidade-101" });
    }

    [Fact]
    public async Task Morador_ve_os_pets_da_propria_unidade_e_nao_os_de_outra()
    {
        var unit = await _controller.As("joao", "morador").GetByUnit("unidade-101");
        var otherUnit = await _controller.As("joao", "morador").GetByUnit("unidade-202");
        var unitMate = await _controller.As("joao", "morador").GetById("pet-ana");
        var neighbour = await _controller.As("joao", "morador").GetById("pet-maria");

        Assert.Equal(new[] { "pet-joao", "pet-ana" }, Ids(unit));
        Assert.Empty(Ids(otherUnit));
        Assert.Equal(200, StatusOf(unitMate));
        Assert.Equal(403, StatusOf(neighbour));
    }

    [Fact]
    public async Task Lista_por_condominio_morador_ve_so_os_da_unidade_admin_todos_e_outro_condominio_e_404()
    {
        var morador = await _controller.As("joao", "morador").GetAllByCondominium(CondominioA);
        var admin = await _controller.As("admin-a", "admin").GetAllByCondominium(CondominioA);
        var other = await _controller.As("admin-a", "admin").GetAllByCondominium(CondominioB);

        Assert.Equal(new[] { "pet-joao", "pet-ana" }, Ids(morador));
        Assert.Equal(new[] { "pet-joao", "pet-ana", "pet-maria" }, Ids(admin));
        Assert.Equal(404, StatusOf(other));
    }

    [Fact]
    public async Task Lista_por_dono_de_outro_e_403_para_morador_e_admin_de_outro_condominio_recebe_404()
    {
        var morador = await _controller.As("joao", "morador").GetByOwner("maria");
        var admin = await _controller.As("admin-b", "admin", CondominioB).GetById("pet-joao");

        Assert.Equal(403, StatusOf(morador));
        Assert.Equal(404, StatusOf(admin));
    }

    [Fact]
    public async Task Admin_cadastra_altera_e_remove_so_no_proprio_condominio()
    {
        var create = await _controller.As("admin-a", "admin").Create(
            new CreatePetRequest("carlos", "unidade-901", CondominioB, "Thor", default, null, null, true, null));
        var update = await _controller.As("admin-a", "admin").Update(
            "pet-carlos", new UpdatePetRequest("Thor II", null, null, null, null, null, null));
        var delete = await _controller.As("admin-a", "admin").Delete("pet-carlos");
        var ownDelete = await _controller.As("admin-a", "admin").Delete("pet-maria");

        Assert.Equal(404, StatusOf(create));
        Assert.Equal(404, StatusOf(update));
        Assert.Equal(404, StatusOf(delete));
        Assert.Equal(204, StatusOf(ownDelete));
        _service.Verify(s => s.CreateAsync(It.IsAny<CreatePetRequest>()), Times.Never);
        _service.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdatePetRequest>()), Times.Never);
        _service.Verify(s => s.DeleteAsync("pet-carlos"), Times.Never);
    }

    private static IEnumerable<string> Ids(IActionResult result) =>
        ValueOf<IEnumerable<PetResponse>>(result).Select(p => p.Id);

    private static PetResponse Make(string id, string ownerId, string unitId, string condominiumId) => new()
    {
        Id = id,
        OwnerId = ownerId,
        UnitId = unitId,
        CondominiumId = condominiumId,
        Name = id,
    };

    private const string UnidadeA = "665f1c2e9b1e8a0000000a21";
    private const string UnidadeB = "665f1c2e9b1e8a0000000b21";

    // Tutor e unidade precisam ser do condomínio do pet. A resposta é a mesma para tutor inexistente,
    // tutor de outro condomínio e unidade de outro condomínio, para não revelar o que existe lá.
    [Fact]
    public async Task Cadastro_de_pet_recusa_tutor_ou_unidade_de_outro_condominio_com_a_mesma_mensagem()
    {
        var controller = _controller.As("admin-a", "admin");

        var otherOwner = await controller.Create(new CreatePetRequest("carlos", UnidadeA, CondominioA, "Rex", default, null, null, true, null));
        var missingOwner = await controller.Create(new CreatePetRequest("sumiu", UnidadeA, CondominioA, "Rex", default, null, null, true, null));
        var otherUnit = await controller.Create(new CreatePetRequest("maria", UnidadeB, CondominioA, "Rex", default, null, null, true, null));

        var messages = new[] { otherOwner, missingOwner, otherUnit }.Select(result =>
        {
            Assert.Equal(400, StatusOf(result));
            return ValueOf<object>(result).GetType().GetProperty("message")!.GetValue(ValueOf<object>(result));
        }).Distinct();
        Assert.Single(messages);
        _service.Verify(s => s.CreateAsync(It.IsAny<CreatePetRequest>()), Times.Never);
    }
}
