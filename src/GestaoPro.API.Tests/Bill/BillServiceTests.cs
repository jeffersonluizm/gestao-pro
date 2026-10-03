namespace GestaoPro.API.Tests.Bill;

public class BillServiceTests
{
    private readonly Mock<IBillRepository> _repoMock = new();
    private readonly BillService _service;

    public BillServiceTests()
    {
        _service = new BillService(_repoMock.Object);
    }

    // ── CreateBillAsync ─────────────────────────────────────────

    [Fact]
    public async Task CreateBillAsync_DadosValidos_RetornaBill()
    {
        var dto = MakeDto();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<BillModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.CreateBillAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.CondominiumId, result.CondominiumId);
        Assert.Equal(dto.UserId, result.UserId);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.Amount, result.Amount);
        Assert.Equal(dto.DueDate, result.DueDate);
        Assert.Equal("pending", result.Status);
    }

    [Fact]
    public async Task CreateBillAsync_SemBarcode_GeraCodigoAuto()
    {
        var dto = MakeDto();
        dto = new CreateBillDto(
            dto.CondominiumId,
            dto.UserId,
            dto.Description,
            dto.Amount,
            dto.DueDate,
            null // sem barcode
        );
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<BillModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.CreateBillAsync(dto);

        Assert.NotNull(result.Barcode);
        Assert.StartsWith("AUTO-", result.Barcode);
    }

    [Fact]
    public async Task CreateBillAsync_ComBarcode_UsaBarcodeForecido()
    {
        var barcode = "123456789.01 12345.678901 12345.678901 1 12345678901234";
        var dto = MakeDto();
        dto = new CreateBillDto(
            dto.CondominiumId,
            dto.UserId,
            dto.Description,
            dto.Amount,
            dto.DueDate,
            barcode
        );
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<BillModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.CreateBillAsync(dto);

        Assert.Equal(barcode, result.Barcode);
    }

    [Fact]
    public async Task CreateBillAsync_StatusPadraoPending_RetornaBillComStatusPending()
    {
        var dto = MakeDto();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<BillModel>()))
                 .Returns(Task.CompletedTask);

        var result = await _service.CreateBillAsync(dto);

        Assert.Equal("pending", result.Status);
    }

    [Fact]
    public async Task CreateBillAsync_ChamaBillRepositoryCreateAsync()
    {
        var dto = MakeDto();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<BillModel>()))
                 .Returns(Task.CompletedTask);

        await _service.CreateBillAsync(dto);

        _repoMock.Verify(r => r.CreateAsync(It.IsAny<BillModel>()), Times.Once);
    }

    // ── UpdateBillAsync ─────────────────────────────────────────

    [Fact]
    public async Task UpdateBillAsync_BillNaoEncontrada_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((BillModel?)null);

        var result = await _service.UpdateBillAsync("id-invalido", new UpdateBillDto(null, null, null, null, null, null, null));

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateBillAsync_AtualizaDescricao_RetornaBillAtualizada()
    {
        var existing = MakeBill();
        _repoMock.Setup(r => r.GetByIdAsync("bill1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("bill1", It.IsAny<BillModel>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateBillAsync("bill1", new UpdateBillDto(null, null, "Nova Descrição", null, null, null, null));

        Assert.NotNull(result);
        Assert.Equal("Nova Descrição", result!.Description);
    }

    [Fact]
    public async Task UpdateBillAsync_AtualizaAmount_RetornaBillAtualizada()
    {
        var existing = MakeBill();
        var novoAmount = 1500m;
        _repoMock.Setup(r => r.GetByIdAsync("bill1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("bill1", It.IsAny<BillModel>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateBillAsync("bill1", new UpdateBillDto(null, null, null, novoAmount, null, null, null));

        Assert.NotNull(result);
        Assert.Equal(novoAmount, result!.Amount);
    }

    [Fact]
    public async Task UpdateBillAsync_AtualizaStatus_RetornaBillAtualizada()
    {
        var existing = MakeBill();
        _repoMock.Setup(r => r.GetByIdAsync("bill1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("bill1", It.IsAny<BillModel>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateBillAsync("bill1", new UpdateBillDto(null, null, null, null, null, null, "paid"));

        Assert.NotNull(result);
        Assert.Equal("paid", result!.Status);
    }

    [Fact]
    public async Task UpdateBillAsync_AtualizaDueDate_RetornaBillAtualizada()
    {
        var existing = MakeBill();
        var novaDueDate = DateTime.Today.AddDays(30);
        _repoMock.Setup(r => r.GetByIdAsync("bill1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("bill1", It.IsAny<BillModel>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateBillAsync("bill1", new UpdateBillDto(null, null, null, null, novaDueDate, null, null));

        Assert.NotNull(result);
        Assert.Equal(novaDueDate, result!.DueDate);
    }

    [Fact]
    public async Task UpdateBillAsync_ValoresNulos_NaoAlteraAtributos()
    {
        var existing = MakeBill();
        var originalDesc = existing.Description;
        _repoMock.Setup(r => r.GetByIdAsync("bill1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("bill1", It.IsAny<BillModel>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateBillAsync("bill1", new UpdateBillDto(null, null, null, null, null, null, null));

        Assert.NotNull(result);
        Assert.Equal(originalDesc, result!.Description);
    }

    [Fact]
    public async Task UpdateBillAsync_ChamaRepositoryUpdate()
    {
        var existing = MakeBill();
        _repoMock.Setup(r => r.GetByIdAsync("bill1")).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync("bill1", It.IsAny<BillModel>())).Returns(Task.CompletedTask);

        await _service.UpdateBillAsync("bill1", new UpdateBillDto(null, null, "Nova Desc", null, null, null, null));

        _repoMock.Verify(r => r.UpdateAsync("bill1", It.IsAny<BillModel>()), Times.Once);
    }

    // ── GetUserBillsAsync ───────────────────────────────────────

    [Fact]
    public async Task GetUserBillsAsync_RetornaListaDeBills()
    {
        var bills = new List<BillModel> { MakeBill(), MakeBill() };
        _repoMock.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(bills);

        var result = await _service.GetUserBillsAsync("user1");

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count());
    }

    // ── GetBillByIdAsync ────────────────────────────────────────

    [Fact]
    public async Task GetBillByIdAsync_BillEncontrada_RetornaBill()
    {
        var bill = MakeBill();
        _repoMock.Setup(r => r.GetByIdAsync("bill1")).ReturnsAsync(bill);

        var result = await _service.GetBillByIdAsync("bill1");

        Assert.NotNull(result);
        Assert.Equal(bill.Id, result!.Id);
    }

    [Fact]
    public async Task GetBillByIdAsync_BillNaoEncontrada_RetornaNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("id-invalido")).ReturnsAsync((BillModel?)null);

        var result = await _service.GetBillByIdAsync("id-invalido");

        Assert.Null(result);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static CreateBillDto MakeDto() => new(
        CondominiumId: "cond1",
        UserId: "user1",
        Description: "Taxa Condominial 05/2026",
        Amount: 1200m,
        DueDate: DateTime.Today.AddDays(7),
        Barcode: null
    );

    private static BillModel MakeBill() => new()
    {
        Id = "bill1",
        CondominiumId = "cond1",
        UserId = "user1",
        Description = "Taxa Condominial 05/2026",
        Amount = 1200m,
        DueDate = DateTime.Today.AddDays(7),
        Barcode = "AUTO-20260602120000-1234",
        Status = "pending",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
