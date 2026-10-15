using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GestaoPro.API.Tests.Startup;

// Teto de tamanho: a demonstração pública roda num Atlas gratuito de 512 MB. Corpo de requisição limitado a 64 KB
// e todo texto de entrada com tamanho máximo, para um script não encher o banco com poucos pedidos.
public class RequestLimitsTests : IDisposable
{
    private readonly string _contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseContentRoot(_contentRoot);
            builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500");
            builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
            builder.UseSetting("JwtSettings:SecretKey", "chave-de-teste-com-mais-de-32-caracteres-0123456789");
            builder.ConfigureTestServices(services =>
            {
                foreach (var hosted in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(hosted);
            });
        });

    [Fact]
    public async Task Corpo_acima_de_64_KB_responde_413_com_mensagem()
    {
        await using var factory = CreateFactory();
        var body = new StringContent("{\"registrationNumber\":\"" + new string('a', 70 * 1024) + "\",\"password\":\"x\"}", Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync("/api/auth/login", body);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Contains("grande", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Corpo_pequeno_passa_pelo_limite()
    {
        await using var factory = CreateFactory();
        var body = new StringContent("{\"registrationNumber\":\"\",\"password\":\"\"}", Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync("/api/auth/login", body);

        Assert.NotEqual(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    // Todo texto que chega pela API (DTOs de entrada e a encomenda, que é recebida direto) tem tamanho máximo.
    public static IEnumerable<object[]> InputTypes() =>
        typeof(LoginRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == "GestaoPro.API.DTOs" && type is { IsClass: true, IsAbstract: false, IsGenericType: false })
            .Where(type => !new[] { "Response", "Summary", "Query", "Result" }.Any(suffix => type.Name.EndsWith(suffix)))
            .Append(typeof(Delivery))
            .Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(InputTypes))]
    public void Todo_texto_de_entrada_tem_tamanho_maximo(Type type)
    {
        var constructorParameters = type.GetConstructors().SelectMany(c => c.GetParameters()).ToList();
        var unbounded = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite || constructorParameters.Any(p => p.Name == property.Name))
            .Where(property => property.PropertyType == typeof(string) || typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
            .Where(property => property.Name != "Id")
            .Where(property => !HasLimit(property) && !constructorParameters.Any(p => p.Name == property.Name && HasLimit(p)))
            .Select(property => property.Name)
            .ToList();

        Assert.True(unbounded.Count == 0, $"{type.Name} sem tamanho máximo em: {string.Join(", ", unbounded)}");
    }

    private static bool HasLimit(ICustomAttributeProvider member) =>
        member.GetCustomAttributes(true).Any(attribute => attribute is StringLengthAttribute or MaxLengthAttribute);

    [Fact]
    public void Titulo_de_aviso_enorme_e_recusado_na_validacao()
    {
        var dto = new CreateAnnouncementDto { CondominiumId = "c1", AuthorId = "a1", Title = new string('x', 5000), Content = "ok", Category = "Geral" };

        var valid = Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), validateAllProperties: true);

        Assert.False(valid);
    }
}
