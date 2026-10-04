using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Bill;

public class BillsControllerWriteTests
{
    private readonly Mock<IBillService> _billService = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly BillsController _controller;

    public BillsControllerWriteTests()
    {
        _controller = new BillsController(_billService.Object, _users.Object);
        foreach (var (userId, condominiumId) in new[] { ("joao", CondominioA), ("maria", CondominioA), ("carlos", CondominioB) })
            _users.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(new UserResponse { Id = userId, CondominiumId = condominiumId });

        _billService.Setup(s => s.GetBillByIdAsync("cobranca-a"))
            .ReturnsAsync(new BillModel { Id = "cobranca-a", UserId = "joao", CondominiumId = CondominioA, Description = "Taxa", Barcode = "1" });
        _billService.Setup(s => s.GetBillByIdAsync("cobranca-b"))
            .ReturnsAsync(new BillModel { Id = "cobranca-b", UserId = "carlos", CondominiumId = CondominioB, Description = "Taxa", Barcode = "2" });
        _billService.Setup(s => s.UpdateBillAsync(It.IsAny<string>(), It.IsAny<UpdateBillDto>()))
            .ReturnsAsync(new BillModel { Id = "cobranca-a", UserId = "joao", CondominiumId = CondominioA, Description = "Taxa", Barcode = "1" });
    }

    [Fact]
    public async Task Admin_nao_emite_cobranca_em_outro_condominio()
    {
        var result = await _controller.As("admin-a", "admin")
            .Create(new CreateBillDto(CondominioB, "carlos", "Taxa", 100, DateTime.UtcNow));

        Assert.Equal(404, StatusOf(result));
        _billService.Verify(s => s.CreateBillAsync(It.IsAny<CreateBillDto>()), Times.Never);
    }

    [Fact]
    public async Task Admin_altera_so_cobranca_do_proprio_condominio_sem_mover_para_outro()
    {
        var own = await _controller.As("admin-a", "admin").Update("cobranca-a", new UpdateBillDto(null, null, "Nova", null, null, null, null));
        var other = await _controller.As("admin-a", "admin").Update("cobranca-b", new UpdateBillDto(null, null, "Nova", null, null, null, null));
        var moveAway = await _controller.As("admin-a", "admin").Update("cobranca-a", new UpdateBillDto(CondominioB, null, null, null, null, null, null));

        Assert.Equal(200, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
        Assert.Equal(404, StatusOf(moveAway));
        _billService.Verify(s => s.UpdateBillAsync("cobranca-b", It.IsAny<UpdateBillDto>()), Times.Never);
    }

    // A cobrança é do morador: ele precisa ser do mesmo condomínio da cobrança.
    [Fact]
    public async Task Admin_nao_emite_nem_transfere_cobranca_para_morador_de_outro_condominio()
    {
        _billService.Setup(s => s.CreateBillAsync(It.IsAny<CreateBillDto>()))
            .ReturnsAsync(new BillModel { Id = "nova", UserId = "maria", CondominiumId = CondominioA, Description = "Taxa", Barcode = "1" });

        var create = await _controller.As("admin-a", "admin").Create(new CreateBillDto(CondominioA, "carlos", "Taxa", 100, DateTime.UtcNow));
        var update = await _controller.As("admin-a", "admin").Update("cobranca-a", new UpdateBillDto(null, "carlos", null, null, null, null, null));
        var own = await _controller.As("admin-a", "admin").Create(new CreateBillDto(CondominioA, "maria", "Taxa", 100, DateTime.UtcNow));

        Assert.Equal(404, StatusOf(create));
        Assert.Equal(404, StatusOf(update));
        Assert.Equal(201, StatusOf(own));
        _billService.Verify(s => s.CreateBillAsync(It.Is<CreateBillDto>(d => d.UserId == "carlos")), Times.Never);
        _billService.Verify(s => s.UpdateBillAsync(It.IsAny<string>(), It.IsAny<UpdateBillDto>()), Times.Never);
    }
}
