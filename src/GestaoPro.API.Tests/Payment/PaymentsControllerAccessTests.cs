using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Payment;

// Pagamento não guarda dono nem condomínio: o acesso segue o da cobrança a que pertence.
public class PaymentsControllerAccessTests
{
    private readonly Mock<IPaymentService> _paymentService = new();
    private readonly Mock<IBillService> _billService = new();
    private readonly PaymentsController _controller;

    public PaymentsControllerAccessTests()
    {
        _controller = new PaymentsController(_paymentService.Object, _billService.Object);
        _billService.Setup(s => s.GetBillByIdAsync("cobranca-joao")).ReturnsAsync(new BillModel
        {
            Id = "cobranca-joao", UserId = "joao", CondominiumId = CondominioA, Description = "Taxa", Barcode = "1",
        });
        _paymentService.Setup(s => s.GetByIdAsync("pagamento-joao"))
            .ReturnsAsync(new PaymentModel { Id = "pagamento-joao", BillId = "cobranca-joao", AmountPaid = 100 });
        _paymentService.Setup(s => s.GetByIdAsync("pagamento-orfao"))
            .ReturnsAsync(new PaymentModel { Id = "pagamento-orfao", BillId = "cobranca-apagada", AmountPaid = 100 });
        _paymentService.Setup(s => s.GetByBillIdAsync("cobranca-joao"))
            .ReturnsAsync(new[] { new PaymentModel { Id = "pagamento-joao", BillId = "cobranca-joao" } });
    }

    [Fact]
    public async Task Morador_le_os_pagamentos_da_propria_cobranca()
    {
        var byBill = await _controller.As("joao", "morador").GetByBillId("cobranca-joao");
        var byId = await _controller.As("joao", "morador").GetById("pagamento-joao");

        Assert.Single(ValueOf<IEnumerable<PaymentModel>>(byBill));
        Assert.Equal(200, StatusOf(byId));
    }

    [Fact]
    public async Task Morador_lendo_pagamentos_de_cobranca_alheia_recebe_403()
    {
        var byBill = await _controller.As("maria", "morador").GetByBillId("cobranca-joao");
        var byId = await _controller.As("maria", "morador").GetById("pagamento-joao");

        Assert.Equal(403, StatusOf(byBill));
        Assert.Equal(403, StatusOf(byId));
        _paymentService.Verify(s => s.GetByBillIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Admin_de_outro_condominio_recebe_404()
    {
        var byBill = await _controller.As("admin-b", "admin", CondominioB).GetByBillId("cobranca-joao");
        var byId = await _controller.As("admin-b", "admin", CondominioB).GetById("pagamento-joao");

        Assert.Equal(404, StatusOf(byBill));
        Assert.Equal(404, StatusOf(byId));
    }

    [Fact]
    public async Task Sindico_do_condominio_le_os_pagamentos()
    {
        var result = await _controller.As("sindico-a", "sindico").GetByBillId("cobranca-joao");

        Assert.Equal(200, StatusOf(result));
    }

    [Fact]
    public async Task Porteiro_nao_le_pagamentos()
    {
        var byBill = await _controller.As("porteiro-1", "porteiro").GetByBillId("cobranca-joao");
        var byId = await _controller.As("porteiro-1", "porteiro").GetById("pagamento-joao");

        Assert.Equal(403, StatusOf(byBill));
        Assert.Equal(403, StatusOf(byId));
        _paymentService.Verify(s => s.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Cobranca_inexistente_ou_pagamento_sem_cobranca_e_404()
    {
        var byBill = await _controller.As("admin-a", "admin").GetByBillId("nao-existe");
        var orphan = await _controller.As("admin-a", "admin").GetById("pagamento-orfao");

        Assert.Equal(404, StatusOf(byBill));
        Assert.Equal(404, StatusOf(orphan));
    }
}
