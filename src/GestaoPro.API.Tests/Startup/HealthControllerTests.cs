using GestaoPro.API.Controllers;
using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestaoPro.API.Tests.Startup;

// O health check é anônimo: usa o cliente do MongoDB da aplicação (sem abrir conexão nova a cada chamada)
// e, na falha, não devolve o texto da exceção (que pode trazer host, usuário ou detalhes do driver).
public class HealthControllerTests
{
    private readonly Mock<IMongoDatabase> _database = new();

    private HealthController Controller() => new(
        _database.Object,
        Options.Create(new MongoDBSettings { DatabaseName = "GestaoProStress" }),
        NullLogger<HealthController>.Instance);

    private void PingThrows(Exception exception) =>
        _database.Setup(d => d.RunCommandAsync(It.IsAny<Command<BsonDocument>>(), It.IsAny<ReadPreference>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    private static Dictionary<string, object?> Body(IActionResult result) =>
        JsonBody(Assert.IsAssignableFrom<ObjectResult>(result).Value);

    private static Dictionary<string, object?> JsonBody(object? value) =>
        value!.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(value));

    [Fact]
    public async Task Banco_respondendo_devolve_200_com_o_nome_do_banco()
    {
        _database.Setup(d => d.RunCommandAsync(It.IsAny<Command<BsonDocument>>(), It.IsAny<ReadPreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BsonDocument("ok", 1));

        var result = await Controller().MongoDb();

        Assert.Equal(200, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode ?? 200);
        Assert.Equal(true, Body(result)["connected"]);
        Assert.Equal("GestaoProStress", Body(result)["database"]);
    }

    [Fact]
    public async Task Banco_fora_devolve_503_sem_o_texto_da_excecao()
    {
        PingThrows(new TimeoutException("mongodb+srv://usuario:senha@cluster-secreto"));

        var result = await Controller().MongoDb();

        Assert.Equal(503, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
        var message = Assert.IsType<string>(Body(result)["message"]);
        Assert.DoesNotContain("senha", message);
        Assert.DoesNotContain("cluster-secreto", message);
        Assert.Equal(false, Body(result)["connected"]);
    }
}
