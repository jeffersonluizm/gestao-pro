using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Bill;

// Cobrança é dado pessoal: morador só vê as próprias, porteiro não vê, administração só do próprio condomínio.
public class BillsControllerAccessTests
{
    private readonly Mock<IBillService> _billService = new();
    private readonly BillsController _controller;

    public BillsControllerAccessTests()
    {
        _controller = new BillsController(_billService.Object);
        _billService.Setup(s => s.GetBillByIdAsync("cobranca-joao"))
            .ReturnsAsync(MakeBill("cobranca-joao", "joao", CondominioA));
        _billService.Setup(s => s.GetBillByIdAsync("cobranca-outro-condominio"))
            .ReturnsAsync(MakeBill("cobranca-outro-condominio", "carlos", CondominioB));
        _billService.Setup(s => s.GetUserBillsAsync("joao")).ReturnsAsync(new[]
        {
            MakeBill("cobranca-joao", "joao", CondominioA),
            MakeBill("cobranca-joao-antiga", "joao", CondominioB),
        });
    }

    [Fact]
    public async Task Morador_le_a_propria_cobranca()
    {
        var result = await _controller.As("joao", "morador").GetById("cobranca-joao");

        Assert.Equal(200, StatusOf(result));
    }

    [Fact]
    public async Task Morador_lendo_cobranca_de_outro_morador_recebe_403()
    {
        var result = await _controller.As("maria", "morador").GetById("cobranca-joao");

        Assert.Equal(403, StatusOf(result));
    }

    [Fact]
    public async Task Admin_de_outro_condominio_recebe_404_como_se_nao_existisse()
    {
        var result = await _controller.As("admin-b", "admin", CondominioB).GetById("cobranca-joao");

        Assert.Equal(404, StatusOf(result));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("sindico")]
    public async Task Administracao_do_condominio_le_cobranca_de_morador(string role)
    {
        var result = await _controller.As("gestor", role).GetById("cobranca-joao");

        Assert.Equal(200, StatusOf(result));
    }

    [Fact]
    public async Task Porteiro_nao_le_cobranca_e_o_banco_nem_e_consultado()
    {
        var byId = await _controller.As("porteiro-1", "porteiro").GetById("cobranca-joao");
        var byUser = await _controller.As("porteiro-1", "porteiro").GetByUserId("porteiro-1");

        Assert.Equal(403, StatusOf(byId));
        Assert.Equal(403, StatusOf(byUser));
        _billService.Verify(s => s.GetBillByIdAsync(It.IsAny<string>()), Times.Never);
        _billService.Verify(s => s.GetUserBillsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Token_sem_condominio_recebe_403()
    {
        var result = await _controller.As("joao", "morador", condominiumId: null).GetById("cobranca-joao");

        Assert.Equal(403, StatusOf(result));
    }

    [Fact]
    public async Task Morador_lista_as_proprias_cobrancas_so_do_condominio_atual()
    {
        var result = await _controller.As("joao", "morador").GetByUserId("joao");

        var bills = ValueOf<IEnumerable<BillModel>>(result);
        Assert.Equal(new[] { "cobranca-joao" }, bills.Select(b => b.Id));
    }

    [Fact]
    public async Task Morador_listando_cobrancas_de_outro_morador_recebe_403()
    {
        var result = await _controller.As("maria", "morador").GetByUserId("joao");

        Assert.Equal(403, StatusOf(result));
        _billService.Verify(s => s.GetUserBillsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Admin_lista_so_as_cobrancas_do_proprio_condominio()
    {
        var result = await _controller.As("admin-b", "admin", CondominioB).GetByUserId("joao");

        var bills = ValueOf<IEnumerable<BillModel>>(result);
        Assert.Equal(new[] { "cobranca-joao-antiga" }, bills.Select(b => b.Id));
    }

    [Fact]
    public async Task Cobranca_inexistente_continua_404()
    {
        var result = await _controller.As("joao", "morador").GetById("nao-existe");

        Assert.Equal(404, StatusOf(result));
    }

    private static BillModel MakeBill(string id, string userId, string condominiumId) => new()
    {
        Id = id,
        UserId = userId,
        CondominiumId = condominiumId,
        Description = "Taxa condominial",
        Amount = 100,
        Barcode = "123",
    };
}
