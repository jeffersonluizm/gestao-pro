using GestaoPro.API.Controllers;
using GestaoPro.API.Tests.Access;
using static GestaoPro.API.Tests.Access.TestSession;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Tests.ParkingTests;

using ParkingModel = GestaoPro.API.Models.Parking;

// Número de vaga repetido é conflito de dados, não erro do servidor: 409 com a mensagem do serviço.
public class ParkingControllerTests
{
    private readonly Mock<IParkingRepository> _repoMock = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly ParkingController _controller;

    public ParkingControllerTests()
    {
        _controller = new ParkingController(new ParkingAppService(_repoMock.Object), _users.Object).As("admin-a", "admin");
        foreach (var (userId, condominiumId) in new[] { ("joao", CondominioA), ("maria", CondominioA), ("carlos", CondominioB) })
            _users.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(new UserResponse { Id = userId, CondominiumId = condominiumId });

        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel>
        {
            new() { Id = "vaga-1", Number = "A-01", Type = "carro", CondominiumId = CondominioA },
            new() { Id = "vaga-2", Number = "A-02", Type = "carro", CondominiumId = CondominioA },
            new() { Id = "vaga-b", Number = "B-09", Type = "carro", CondominiumId = CondominioB },
        });
        _repoMock.Setup(r => r.GetByIdAsync("vaga-2"))
            .ReturnsAsync(new ParkingModel { Id = "vaga-2", Number = "A-02", Type = "carro", CondominiumId = CondominioA });
        _repoMock.Setup(r => r.GetByIdAsync("vaga-b"))
            .ReturnsAsync(new ParkingModel { Id = "vaga-b", Number = "B-09", Type = "carro", CondominiumId = CondominioB });
    }

    [Fact]
    public async Task Create_com_numero_repetido_retorna_409_com_mensagem()
    {
        var result = await _controller.Create(new CreateParkingDto { Number = " a-01 ", Type = "carro" });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Já existe uma vaga cadastrada com esse número.", MessageOf(conflict));
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ParkingModel>()), Times.Never);
    }

    [Fact]
    public async Task Update_com_numero_de_outra_vaga_retorna_409_com_mensagem()
    {
        var result = await _controller.Update("vaga-2", new UpdateParkingDto { Number = "A-01" });

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("Já existe uma vaga cadastrada com esse número.", MessageOf(conflict));
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<ParkingModel>()), Times.Never);
    }

    [Fact]
    public async Task Create_com_numero_novo_continua_retornando_201()
    {
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ParkingModel>()))
            .ReturnsAsync((ParkingModel parking) => { parking.Id = "vaga-3"; return parking; });

        var result = await _controller.Create(new CreateParkingDto { Number = "B-01", Type = "carro" });

        Assert.IsType<CreatedAtActionResult>(result);
    }

    // Vaga é do condomínio: admin/síndico só cadastra e altera no próprio (outro: 404).
    // O painel não manda condominiumId no cadastro: vale o condomínio do token.
    [Fact]
    public async Task Cadastro_sem_condominio_grava_o_do_token_e_com_outro_condominio_e_404()
    {
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ParkingModel>()))
            .ReturnsAsync((ParkingModel parking) => { parking.Id = "vaga-3"; return parking; });

        var own = await _controller.Create(new CreateParkingDto { Number = "C-01", Type = "carro" });
        var other = await _controller.Create(new CreateParkingDto { Number = "C-02", Type = "carro", CondominiumId = CondominioB });

        Assert.IsType<CreatedAtActionResult>(own);
        Assert.Equal(404, StatusOf(other));
        _repoMock.Verify(r => r.CreateAsync(It.Is<ParkingModel>(p => p.Number == "C-01" && p.CondominiumId == CondominioA)), Times.Once);
        _repoMock.Verify(r => r.CreateAsync(It.Is<ParkingModel>(p => p.Number == "C-02")), Times.Never);
    }

    [Fact]
    public async Task Vaga_de_outro_condominio_nao_e_alterada_reservada_nem_removida()
    {
        _controller.Request.Headers["X-User-Id"] = "joao";

        var update = await _controller.Update("vaga-b", new UpdateParkingDto { Type = "moto" });
        var moveAway = await _controller.Update("vaga-2", new UpdateParkingDto { CondominiumId = CondominioB });
        var status = await _controller.UpdateStatus("vaga-b", "occupied");
        var reserve = await _controller.Reserve("vaga-b");
        var delete = await _controller.Delete("vaga-b");

        Assert.Equal(404, StatusOf(update.Result!));
        Assert.Equal(404, StatusOf(moveAway.Result!));
        Assert.Equal(404, StatusOf(status));
        Assert.Equal(404, StatusOf(reserve));
        Assert.Equal(404, StatusOf(delete));
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<ParkingModel>()), Times.Never);
        _repoMock.Verify(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _repoMock.Verify(r => r.ReserveAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    private static string? MessageOf(ObjectResult result) =>
        result.Value?.GetType().GetProperty("message")?.GetValue(result.Value) as string;

    // Reserva em nome de outro (X-User-Id): o cabeçalho é obrigatório (400) e o alvo precisa ser do condomínio (404).
    [Fact]
    public async Task Reserva_exige_morador_alvo_do_proprio_condominio()
    {
        var missing = await _controller.Reserve("vaga-2");
        _controller.Request.Headers["X-User-Id"] = "carlos";
        var otherResident = await _controller.Reserve("vaga-2");
        _controller.Request.Headers["X-User-Id"] = "maria";
        var own = await _controller.Reserve("vaga-2");

        Assert.Equal(400, StatusOf(missing));
        Assert.Equal(404, StatusOf(otherResident));
        Assert.Equal(204, StatusOf(own));
        _repoMock.Verify(r => r.ReserveAsync("vaga-2", "carlos"), Times.Never);
        _repoMock.Verify(r => r.ReserveAsync("vaga-2", "maria"), Times.Once);
    }
}
