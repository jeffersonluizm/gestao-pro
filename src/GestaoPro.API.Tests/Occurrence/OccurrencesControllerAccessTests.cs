using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.OccurrenceTests;

public class OccurrencesControllerAccessTests
{
    private readonly Mock<IOccurrenceService> _service = new();
    private readonly OccurrencesController _controller;

    private static readonly OccurrenceResponse DoJoao = Make("ocorrencia-joao", "joao", CondominioA);
    private static readonly OccurrenceResponse DaMaria = Make("ocorrencia-maria", "maria", CondominioA);
    private static readonly OccurrenceResponse DeOutroCondominio = Make("ocorrencia-carlos", "carlos", CondominioB);

    public OccurrencesControllerAccessTests()
    {
        _controller = new OccurrencesController(_service.Object);
        var all = new List<OccurrenceResponse> { DoJoao, DaMaria, DeOutroCondominio };
        _service.Setup(s => s.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => all.FirstOrDefault(o => o.Id == id));
        _service.Setup(s => s.DeleteAsync(It.IsAny<string>())).ReturnsAsync(true);
    }

    // O app mobile mostra "Remover" nas ocorrências do próprio morador (e não manda X-User-Id).
    [Fact]
    public async Task Morador_remove_a_propria_ocorrencia()
    {
        var result = await _controller.As("joao", "morador").Delete("ocorrencia-joao");

        Assert.Equal(204, StatusOf(result));
        _service.Verify(s => s.DeleteAsync("ocorrencia-joao"), Times.Once);
    }

    [Fact]
    public async Task Morador_nao_remove_ocorrencia_de_outro()
    {
        var result = await _controller.As("joao", "morador").Delete("ocorrencia-maria");

        Assert.Equal(403, StatusOf(result));
        _service.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Admin_remove_do_proprio_condominio_e_recebe_404_no_de_outro()
    {
        var own = await _controller.As("admin-a", "admin").Delete("ocorrencia-maria");
        var other = await _controller.As("admin-a", "admin").Delete("ocorrencia-carlos");

        Assert.Equal(204, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
        _service.Verify(s => s.DeleteAsync("ocorrencia-carlos"), Times.Never);
    }

    // ── Leituras ──

    [Fact]
    public async Task Morador_le_a_propria_ocorrencia_e_recebe_403_na_de_outro()
    {
        var own = await _controller.As("joao", "morador").GetById("ocorrencia-joao");
        var other = await _controller.As("joao", "morador").GetById("ocorrencia-maria");

        Assert.Equal(200, StatusOf(own));
        Assert.Equal(403, StatusOf(other));
    }

    [Fact]
    public async Task Admin_de_outro_condominio_recebe_404_na_ocorrencia()
    {
        var result = await _controller.As("admin-b", "admin", CondominioB).GetById("ocorrencia-joao");

        Assert.Equal(404, StatusOf(result));
    }

    [Fact]
    public async Task Lista_por_condominio_morador_ve_as_proprias_admin_todas_e_outro_condominio_e_404()
    {
        _service.Setup(s => s.GetAllByCondominiumAsync(CondominioA)).ReturnsAsync(new List<OccurrenceResponse> { DoJoao, DaMaria });
        _service.Setup(s => s.GetByCondominiumAndStatusAsync(CondominioA, OccurrenceStatus.Open))
            .ReturnsAsync(new List<OccurrenceResponse> { DoJoao, DaMaria });

        var morador = await _controller.As("joao", "morador").GetAllByCondominium(CondominioA);
        var porteiro = await _controller.As("porteiro-1", "porteiro").GetByCondominiumAndStatus(CondominioA, OccurrenceStatus.Open);
        var admin = await _controller.As("admin-a", "admin").GetAllByCondominium(CondominioA);
        var outro = await _controller.As("admin-a", "admin").GetAllByCondominium(CondominioB);

        Assert.Equal(new[] { "ocorrencia-joao" }, Ids(morador));
        Assert.Empty(Ids(porteiro));
        Assert.Equal(new[] { "ocorrencia-joao", "ocorrencia-maria" }, Ids(admin));
        Assert.Equal(404, StatusOf(outro));
    }

    [Fact]
    public async Task Lista_por_usuario_de_outro_e_403_para_morador_e_filtrada_para_admin()
    {
        _service.Setup(s => s.GetByUserAsync("carlos")).ReturnsAsync(new List<OccurrenceResponse> { DeOutroCondominio });

        var morador = await _controller.As("joao", "morador").GetByUser("carlos");
        var admin = await _controller.As("admin-a", "admin").GetByUser("carlos");

        Assert.Equal(403, StatusOf(morador));
        Assert.Empty(Ids(admin));
    }

    // ── Escritas ──

    // Cabeçalhos X-User-Id/X-Condominium-Id não valem mais: quem não administra registra em nome próprio.
    [Fact]
    public async Task Morador_registra_ocorrencia_sempre_em_nome_proprio_e_no_proprio_condominio()
    {
        CreateOccurrenceRequest? sent = null;
        _service.Setup(s => s.CreateAsync(It.IsAny<CreateOccurrenceRequest>()))
            .Callback((CreateOccurrenceRequest request) => sent = request)
            .ReturnsAsync(DoJoao);

        var result = await _controller.As("joao", "morador").Create(
            new CreateOccurrenceRequest(CondominioB, "maria", "Barulho", "Muito barulho", OccurrenceCategory.Complaint, OccurrencePriority.Low));

        Assert.Equal(201, StatusOf(result));
        Assert.Equal("joao", sent!.UserId);
        Assert.Equal(CondominioA, sent.CondominiumId);
    }

    [Fact]
    public async Task Admin_nao_registra_ocorrencia_em_outro_condominio()
    {
        var result = await _controller.As("admin-a", "admin").Create(
            new CreateOccurrenceRequest(CondominioB, "carlos", "Barulho", "Muito barulho", OccurrenceCategory.Complaint, OccurrencePriority.Low));

        Assert.Equal(404, StatusOf(result));
        _service.Verify(s => s.CreateAsync(It.IsAny<CreateOccurrenceRequest>()), Times.Never);
    }

    [Fact]
    public async Task Admin_atualiza_e_muda_status_so_no_proprio_condominio()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdateOccurrenceRequest>())).ReturnsAsync(DaMaria);
        UpdateOccurrenceStatusRequest? sentStatus = null;
        _service.Setup(s => s.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<UpdateOccurrenceStatusRequest>()))
            .Callback((string _, UpdateOccurrenceStatusRequest request) => sentStatus = request)
            .ReturnsAsync(DaMaria);
        var update = new UpdateOccurrenceRequest("Novo título", null, null, null);
        var status = new UpdateOccurrenceStatusRequest(OccurrenceStatus.InProgress, "Em andamento", "qualquer-um");

        var own = await _controller.As("admin-a", "admin").Update("ocorrencia-maria", update);
        var other = await _controller.As("admin-a", "admin").Update("ocorrencia-carlos", update);
        var ownStatus = await _controller.As("admin-a", "admin").UpdateStatus("ocorrencia-maria", status);
        var otherStatus = await _controller.As("admin-a", "admin").UpdateStatus("ocorrencia-carlos", status);

        Assert.Equal(200, StatusOf(own));
        Assert.Equal(404, StatusOf(other));
        Assert.Equal(200, StatusOf(ownStatus));
        Assert.Equal(404, StatusOf(otherStatus));
        Assert.Equal("admin-a", sentStatus!.UpdatedByUserId);
        _service.Verify(s => s.UpdateAsync("ocorrencia-carlos", It.IsAny<UpdateOccurrenceRequest>()), Times.Never);
    }

    private static IEnumerable<string> Ids(Microsoft.AspNetCore.Mvc.IActionResult result) =>
        ValueOf<IEnumerable<OccurrenceResponse>>(result).Select(o => o.Id);

    private static OccurrenceResponse Make(string id, string userId, string condominiumId) => new()
    {
        Id = id,
        UserId = userId,
        CondominiumId = condominiumId,
        Title = "Barulho",
    };
}
