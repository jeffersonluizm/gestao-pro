using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Payment;

// Pagamento segue a cobrança: só se registra ou altera pagamento de cobrança do próprio condomínio.
public class PaymentsControllerWriteTests
{
    private readonly Mock<IPaymentService> _paymentService = new();
    private readonly Mock<IBillService> _billService = new();
    private readonly PaymentsController _controller;

    public PaymentsControllerWriteTests()
    {
        _controller = new PaymentsController(_paymentService.Object, _billService.Object);
        _billService.Setup(s => s.GetBillByIdAsync("cobranca-a"))
            .ReturnsAsync(new BillModel { Id = "cobranca-a", UserId = "joao", CondominiumId = CondominioA, Description = "Taxa", Barcode = "1" });
        _billService.Setup(s => s.GetBillByIdAsync("cobranca-b"))
            .ReturnsAsync(new BillModel { Id = "cobranca-b", UserId = "carlos", CondominiumId = CondominioB, Description = "Taxa", Barcode = "2" });
        _paymentService.Setup(s => s.GetByIdAsync("pagamento-a")).ReturnsAsync(new PaymentModel { Id = "pagamento-a", BillId = "cobranca-a" });
        _paymentService.Setup(s => s.GetByIdAsync("pagamento-b")).ReturnsAsync(new PaymentModel { Id = "pagamento-b", BillId = "cobranca-b" });
        _paymentService.Setup(s => s.ProcessPaymentAsync(It.IsAny<CreatePaymentDto>())).ReturnsAsync(new PaymentModel { Id = "novo" });
        _paymentService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdatePaymentDto>())).ReturnsAsync(new PaymentModel { Id = "pagamento-a" });
    }

    [Fact]
    public async Task Admin_registra_pagamento_so_de_cobranca_do_proprio_condominio()
    {
        var own = await _controller.As("admin-a", "admin").PostPayment(new CreatePaymentDto("cobranca-a", 100, "pix", null));
        var other = await _controller.As("admin-a", "admin").PostPayment(new CreatePaymentDto("cobranca-b", 100, "pix", null));

        Assert.Equal(200, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
        _paymentService.Verify(s => s.ProcessPaymentAsync(It.Is<CreatePaymentDto>(d => d.BillId == "cobranca-b")), Times.Never);
    }

    [Fact]
    public async Task Admin_altera_so_pagamento_do_proprio_condominio_sem_mover_para_cobranca_de_outro()
    {
        var own = await _controller.As("admin-a", "admin").Update("pagamento-a", new UpdatePaymentDto(null, null, 120, null, null));
        var other = await _controller.As("admin-a", "admin").Update("pagamento-b", new UpdatePaymentDto(null, null, 120, null, null));
        var moveAway = await _controller.As("admin-a", "admin").Update("pagamento-a", new UpdatePaymentDto("cobranca-b", null, null, null, null));

        Assert.Equal(200, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
        Assert.Equal(404, StatusOf(moveAway));
        _paymentService.Verify(s => s.UpdateAsync("pagamento-b", It.IsAny<UpdatePaymentDto>()), Times.Never);
    }
}
