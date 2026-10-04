using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestaoPro.API.Controllers;

// Health check anônimo: ping no banco com o cliente da aplicação (singleton, sem conexão nova por chamada).
// Na falha, o motivo vai para o log; a resposta pública só diz que o banco está indisponível.
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly IMongoDatabase _database;
    private readonly MongoDBSettings _settings;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IMongoDatabase database, IOptions<MongoDBSettings> settings, ILogger<HealthController> logger)
    {
        _database = database;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpGet("mongodb")]
    public async Task<IActionResult> MongoDb()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: timeout.Token);

            return Ok(new
            {
                connected = true,
                database = _settings.DatabaseName,
                message = "Conexão com MongoDB realizada com sucesso."
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check: MongoDB indisponível.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                connected = false,
                database = _settings.DatabaseName,
                message = "Banco de dados indisponível no momento."
            });
        }
    }
}
