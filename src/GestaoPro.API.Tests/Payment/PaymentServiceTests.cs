namespace GestaoPro.API.Tests.Payment;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock = new();
    private readonly Mock<IBillRepository> _billRepoMock = new();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _service = new PaymentService(_paymentRepoMock.Object, _billRepoMock.Object);
    }

    // ── ProcessPaymentAsync ─────────────────────────────────────

    [Fact]
    public async Task ProcessPaymentAsync_DadosValidos_CriaPaymentEAtualizaBill()
    {
        var dto = MakeDto();
        _paymentRepoMock.Setup(r => r.RegisterPaymentAsync(It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);
        _billRepoMock.Setup(r => r.UpdateStatusAsync(dto.BillId, "paid"))
                     .Returns(Task.CompletedTask);

        var result = await _service.ProcessPaymentAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.BillId, result.BillId);
        Assert.Equal(dto.AmountPaid, result.AmountPaid);
        Assert.Equal(dto.PaymentMethod, result.PaymentMethod);
    }

    [Fact]
    public async Task ProcessPaymentAsync_UsaPaymentDateComUtcNow()
    {
        var dto = MakeDto();
        _paymentRepoMock.Setup(r => r.RegisterPaymentAsync(It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);
        _billRepoMock.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

        var beforeCall = DateTime.UtcNow;
        var result = await _service.ProcessPaymentAsync(dto);
        var afterCall = DateTime.UtcNow;

        Assert.InRange(result.PaymentDate, beforeCall, afterCall);
    }

    [Fact]
    public async Task ProcessPaymentAsync_AtualizaBillStatusParaPaid()
    {
        var dto = MakeDto();
        _paymentRepoMock.Setup(r => r.RegisterPaymentAsync(It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);
        _billRepoMock.Setup(r => r.UpdateStatusAsync(dto.BillId, "paid"))
                     .Returns(Task.CompletedTask);

        await _service.ProcessPaymentAsync(dto);

        _billRepoMock.Verify(r => r.UpdateStatusAsync(dto.BillId, "paid"), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ChamaBillRepositoryUpdateStatus()
    {
        var dto = MakeDto();
        _paymentRepoMock.Setup(r => r.RegisterPaymentAsync(It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);
        _billRepoMock.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

        await _service.ProcessPaymentAsync(dto);

        _billRepoMock.Verify(
            r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_RetornaPaymentComDadosCorretos()
    {
        var dto = MakeDto();
        _paymentRepoMock.Setup(r => r.RegisterPaymentAsync(It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);
        _billRepoMock.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

        var result = await _service.ProcessPaymentAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.BillId, result.BillId);
        Assert.Equal(dto.AmountPaid, result.AmountPaid);
        Assert.Equal(dto.PaymentMethod, result.PaymentMethod);
        Assert.Equal(dto.ReceiptUrl, result.ReceiptUrl);
    }

    // ── UpdateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PaymentNaoEncontrada_RetornaNull()
    {
        _paymentRepoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((PaymentModel?)null);

        var result = await _service.UpdateAsync("id-invalido", new UpdatePaymentDto(null, null, null, null, null));

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaAmountPaid_RetornaPaymentAtualizado()
    {
        var existing = MakePayment();
        var novoAmount = 600m;
        _paymentRepoMock.Setup(r => r.GetByIdAsync("pay1")).ReturnsAsync(existing);
        _paymentRepoMock.Setup(r => r.UpdateAsync("pay1", It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("pay1", new UpdatePaymentDto(null, null, novoAmount, null, null));

        Assert.NotNull(result);
        Assert.Equal(novoAmount, result!.AmountPaid);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaPaymentMethod_RetornaPaymentAtualizado()
    {
        var existing = MakePayment();
        _paymentRepoMock.Setup(r => r.GetByIdAsync("pay1")).ReturnsAsync(existing);
        _paymentRepoMock.Setup(r => r.UpdateAsync("pay1", It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("pay1", new UpdatePaymentDto(null, null, null, "cartao", null));

        Assert.NotNull(result);
        Assert.Equal("cartao", result!.PaymentMethod);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaReceiptUrl_RetornaPaymentAtualizado()
    {
        var existing = MakePayment();
        var novaUrl = "https://example.com/receipt/new";
        _paymentRepoMock.Setup(r => r.GetByIdAsync("pay1")).ReturnsAsync(existing);
        _paymentRepoMock.Setup(r => r.UpdateAsync("pay1", It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("pay1", new UpdatePaymentDto(null, null, null, null, novaUrl));

        Assert.NotNull(result);
        Assert.Equal(novaUrl, result!.ReceiptUrl);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaPaymentDate_RetornaPaymentAtualizado()
    {
        var existing = MakePayment();
        var novaData = DateTime.Today.AddDays(-1);
        _paymentRepoMock.Setup(r => r.GetByIdAsync("pay1")).ReturnsAsync(existing);
        _paymentRepoMock.Setup(r => r.UpdateAsync("pay1", It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("pay1", new UpdatePaymentDto(null, novaData, null, null, null));

        Assert.NotNull(result);
        Assert.Equal(novaData, result!.PaymentDate);
    }

    [Fact]
    public async Task UpdateAsync_ValoresNulos_NaoAlteraAtributos()
    {
        var existing = MakePayment();
        var originalAmount = existing.AmountPaid;
        _paymentRepoMock.Setup(r => r.GetByIdAsync("pay1")).ReturnsAsync(existing);
        _paymentRepoMock.Setup(r => r.UpdateAsync("pay1", It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("pay1", new UpdatePaymentDto(null, null, null, null, null));

        Assert.NotNull(result);
        Assert.Equal(originalAmount, result!.AmountPaid);
    }

    [Fact]
    public async Task UpdateAsync_ChamaRepositoryUpdate()
    {
        var existing = MakePayment();
        _paymentRepoMock.Setup(r => r.GetByIdAsync("pay1")).ReturnsAsync(existing);
        _paymentRepoMock.Setup(r => r.UpdateAsync("pay1", It.IsAny<PaymentModel>()))
                        .Returns(Task.CompletedTask);

        await _service.UpdateAsync("pay1", new UpdatePaymentDto(null, null, 700m, null, null));

        _paymentRepoMock.Verify(r => r.UpdateAsync("pay1", It.IsAny<PaymentModel>()), Times.Once);
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_PaymentEncontrado_RetornaPayment()
    {
        var payment = MakePayment();
        _paymentRepoMock.Setup(r => r.GetByIdAsync("pay1")).ReturnsAsync(payment);

        var result = await _service.GetByIdAsync("pay1");

        Assert.NotNull(result);
        Assert.Equal(payment.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_PaymentNaoEncontrado_RetornaNull()
    {
        _paymentRepoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((PaymentModel?)null);

        var result = await _service.GetByIdAsync("id-invalido");

        Assert.Null(result);
    }

    // ── GetByBillIdAsync ────────────────────────────────────────

    [Fact]
    public async Task GetByBillIdAsync_RetornaListaDePayments()
    {
        var payments = new List<PaymentModel> { MakePayment(), MakePayment() };
        _paymentRepoMock.Setup(r => r.GetByBillIdAsync("bill1")).ReturnsAsync(payments);

        var result = await _service.GetByBillIdAsync("bill1");

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByBillIdAsync_BillSemPayments_RetornaListaVazia()
    {
        var payments = new List<PaymentModel>();
        _paymentRepoMock.Setup(r => r.GetByBillIdAsync("bill1")).ReturnsAsync(payments);

        var result = await _service.GetByBillIdAsync("bill1");

        Assert.Empty(result);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static CreatePaymentDto MakeDto() => new(
        BillId: "bill1",
        AmountPaid: 1200m,
        PaymentMethod: "pix",
        ReceiptUrl: "https://example.com/receipt/12345"
    );

    private static PaymentModel MakePayment() => new()
    {
        Id = "pay1",
        BillId = "bill1",
        AmountPaid = 1200m,
        PaymentMethod = "pix",
        ReceiptUrl = "https://example.com/receipt/12345",
        PaymentDate = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    };
}
