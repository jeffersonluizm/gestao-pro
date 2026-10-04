using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;
using DeliveryModel = GestaoPro.API.Models.Delivery;

namespace GestaoPro.API.Tests.DeliveryTests;

// Encomenda é do morador: ele vê e retira as próprias; portaria e administração cuidam das do condomínio.
// Encomendas antigas não guardam o condomínio: ele vem do cadastro do morador.
public class DeliveryControllerAccessTests
{
    private readonly Mock<IDeliveryService> _service = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly DeliveryController _controller;

    private static readonly DeliveryModel DoJoao = Make("encomenda-joao", "joao", CondominioA);
    private static readonly DeliveryModel DaMariaAntiga = Make("encomenda-maria", "maria", condominiumId: null);
    private static readonly DeliveryModel DoCarlosAntiga = Make("encomenda-carlos", "carlos", condominiumId: null);

    public DeliveryControllerAccessTests()
    {
        _controller = new DeliveryController(_service.Object, _users.Object);
        var all = new List<DeliveryModel> { DoJoao, DaMariaAntiga, DoCarlosAntiga };
        _service.Setup(s => s.GetAsync()).ReturnsAsync(all);
        _service.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(d => d.Id == id));
        _service.Setup(s => s.GetByResidentAsync(It.IsAny<string>()))
            .ReturnsAsync((string residentId) => all.Where(d => d.ResidentId == residentId).ToList());
        foreach (var (id, condominiumId) in new[] { ("joao", CondominioA), ("maria", CondominioA), ("carlos", CondominioB) })
        {
            _users.Setup(u => u.GetByIdAsync(id))
                .ReturnsAsync(new UserResponse { Id = id, CondominiumId = condominiumId });
        }
    }

    [Fact]
    public async Task Morador_ve_so_as_proprias_encomendas()
    {
        var all = await _controller.As("joao", "morador").GetAll();
        var other = await _controller.As("joao", "morador").GetById("encomenda-maria");
        var byOther = await _controller.As("joao", "morador").GetByResident("maria");

        Assert.Equal(new[] { "encomenda-joao" }, Ids(all));
        Assert.Equal(403, StatusOf(other));
        Assert.Equal(403, StatusOf(byOther));
    }

    [Theory]
    [InlineData("porteiro")]
    [InlineData("sindico")]
    public async Task Portaria_ve_as_do_condominio_inclusive_antigas_pelo_cadastro_do_morador(string role)
    {
        var all = await _controller.As("funcionario", role).GetAll();
        var other = await _controller.As("funcionario", role).GetById("encomenda-carlos");
        var qrCode = await _controller.As("funcionario", role).GenerateQRCode("encomenda-carlos");

        Assert.Equal(new[] { "encomenda-joao", "encomenda-maria" }, Ids(all));
        Assert.Equal(404, StatusOf(other));
        Assert.Equal(404, StatusOf(qrCode));
    }

    [Fact]
    public async Task Porteiro_cadastra_para_morador_do_condominio_gravando_o_condominio()
    {
        var created = new DeliveryModel { TrackingCode = "BR1", ResidentId = "maria" };

        var result = await _controller.As("porteiro-a", "porteiro").Create(created);

        Assert.Equal(201, StatusOf(result));
        Assert.Equal(CondominioA, created.CondominiumId);
        _service.Verify(s => s.CreateAsync(created), Times.Once);
    }

    [Fact]
    public async Task Porteiro_nao_cadastra_para_morador_de_outro_condominio()
    {
        var result = await _controller.As("porteiro-a", "porteiro").Create(new DeliveryModel { TrackingCode = "BR2", ResidentId = "carlos" });

        Assert.Equal(404, StatusOf(result));
        _service.Verify(s => s.CreateAsync(It.IsAny<DeliveryModel>()), Times.Never);
    }

    [Fact]
    public async Task Edicao_e_remocao_so_no_proprio_condominio_e_sem_trocar_para_morador_de_outro()
    {
        var update = await _controller.As("porteiro-a", "porteiro").Update("encomenda-carlos", new UpdateDeliveryDto { Status = "picked_up" });
        var moveAway = await _controller.As("porteiro-a", "porteiro").Update("encomenda-joao", new UpdateDeliveryDto { ResidentId = "carlos" });
        var delete = await _controller.As("admin-a", "admin").Delete("encomenda-carlos");
        var ownDelete = await _controller.As("admin-a", "admin").Delete("encomenda-maria");

        Assert.Equal(404, StatusOf(update));
        Assert.Equal(404, StatusOf(moveAway));
        Assert.Equal(404, StatusOf(delete));
        Assert.Equal(204, StatusOf(ownDelete));
        _service.Verify(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateDeliveryDto>()), Times.Never);
        _service.Verify(s => s.RemoveAsync("encomenda-carlos"), Times.Never);
    }

    [Fact]
    public async Task Morador_retira_a_propria_encomenda_e_nao_a_de_outro()
    {
        var own = await _controller.As("joao", "morador").MarkAsPickedUp("encomenda-joao", "João");
        var other = await _controller.As("joao", "morador").MarkAsPickedUp("encomenda-maria", "João");
        var doorman = await _controller.As("porteiro-a", "porteiro").MarkAsPickedUp("encomenda-maria", "Maria");

        Assert.Equal(204, StatusOf(own));
        Assert.Equal(403, StatusOf(other));
        Assert.Equal(204, StatusOf(doorman));
        _service.Verify(s => s.MarkAsPickedUpAsync("encomenda-maria", "João"), Times.Never);
    }

    private static IEnumerable<string?> Ids<T>(Microsoft.AspNetCore.Mvc.ActionResult<T> result) =>
        ValueOf<T, IEnumerable<DeliveryModel>>(result).Select(d => d.Id);

    private static DeliveryModel Make(string id, string residentId, string? condominiumId) => new()
    {
        Id = id,
        ResidentId = residentId,
        CondominiumId = condominiumId,
        TrackingCode = id,
    };
}
