using GestaoPro.API.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly MongoDBSettings _settings;

    public HealthController(IOptions<MongoDBSettings> settings)
    {
        _settings = settings.Value;
    }

    [HttpGet("mongodb")]
    public async Task<IActionResult> MongoDb()
    {
        if (string.IsNullOrWhiteSpace(_settings.ConnectionString) ||
            _settings.ConnectionString.Contains("usuario:senha", StringComparison.OrdinalIgnoreCase) ||
            _settings.ConnectionString.Contains("cluster.mongodb.net", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                connected = false,
                database = _settings.DatabaseName,
                message = "MongoDB Atlas não configurado. Atualize MongoDbSettings:ConnectionString com a string real."
            });
        }

        try
        {
            var client = new MongoClient(_settings.ConnectionString);
            var database = client.GetDatabase(_settings.DatabaseName);
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

            return Ok(new
            {
                connected = true,
                database = _settings.DatabaseName,
                message = "Conexão com MongoDB realizada com sucesso."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                connected = false,
                database = _settings.DatabaseName,
                message = ex.Message
            });
        }
    }
}
