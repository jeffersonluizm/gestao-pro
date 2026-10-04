using GestaoPro.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Tests.ParkingTests;

using ParkingModel = GestaoPro.API.Models.Parking;

// Número de vaga repetido é conflito de dados, não erro do servidor: 409 com a mensagem do serviço.
public class ParkingControllerTests
{
    private readonly Mock<IParkingRepository> _repoMock = new();
    private readonly ParkingController _controller;

    public ParkingControllerTests()
    {
        _controller = new ParkingController(new ParkingAppService(_repoMock.Object));
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel>
        {
            new() { Id = "vaga-1", Number = "A-01", Type = "carro" },
            new() { Id = "vaga-2", Number = "A-02", Type = "carro" },
        });
        _repoMock.Setup(r => r.GetByIdAsync("vaga-2"))
            .ReturnsAsync(new ParkingModel { Id = "vaga-2", Number = "A-02", Type = "carro" });
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

    private static string? MessageOf(ObjectResult result) =>
        result.Value?.GetType().GetProperty("message")?.GetValue(result.Value) as string;
}
