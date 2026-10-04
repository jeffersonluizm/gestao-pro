using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GestaoPro.API.Tests.Startup;

// Sem segredos no appsettings.json, a API precisa recusar a partida quando a configuração
// não foi fornecida (appsettings.Local.json ou variáveis de ambiente), em vez de subir quebrada.
public class StartupConfigurationTests : IDisposable
{
    private const string ValidMongo = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500";
    private const string ValidJwtKey = "chave-de-teste-com-mais-de-32-caracteres-0123456789";

    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private Exception? StartApi(string connectionString, string jwtKey)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseContentRoot(_contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", connectionString);
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", jwtKey);
        });

        return Record.Exception(() => factory.CreateClient());
    }

    [Theory]
    [InlineData("", "MongoDbSettings__ConnectionString")]
    [InlineData("   ", "MongoDbSettings__ConnectionString")]
    public void Sem_connection_string_a_API_nao_sobe_e_diz_o_que_falta(string connectionString, string missing)
    {
        var exception = StartApi(connectionString, ValidJwtKey);

        var error = Assert.IsType<InvalidOperationException>(exception?.GetBaseException());
        Assert.Contains(missing, error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("curta-demais")]
    public void Sem_chave_JWT_de_32_caracteres_a_API_nao_sobe_e_diz_o_que_falta(string jwtKey)
    {
        var exception = StartApi(ValidMongo, jwtKey);

        var error = Assert.IsType<InvalidOperationException>(exception?.GetBaseException());
        Assert.Contains("JwtSettings__SecretKey", error.Message);
    }
}
