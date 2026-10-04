using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Bill;

public class BillsControllerWriteTests
{
    private readonly Mock<IBillService> _billService = new();
    private readonly BillsController _controller;

    public BillsControllerWriteTests()
    {
        _controller = new BillsController(_billService.Object);
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
}
