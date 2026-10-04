using GestaoPro.API.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;

namespace GestaoPro.API.Tests.Startup;

// Id malformado na rota (não é ObjectId) virava 500 (FormatException do driver do MongoDB).
// O filtro comum responde 400 antes de chegar ao controller.
public class MalformedIdFilterTests
{
    private static (ActionExecutingContext Context, bool NextCalled) Run(RouteValueDictionary routeValues)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(routeValues), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: null!);
        new MalformedIdFilter().OnActionExecuting(context);
        return (context, context.Result is null);
    }

    [Theory]
    [InlineData("id", "abc")]
    [InlineData("userId", "nao-e-objectid")]
    [InlineData("condominiumId", "123")]
    [InlineData("residentId", " ")]
    public void Id_malformado_na_rota_responde_400(string key, string value)
    {
        var (context, nextCalled) = Run(new RouteValueDictionary { ["controller"] = "Bills", [key] = value });

        Assert.False(nextCalled);
        Assert.Equal(400, Assert.IsType<BadRequestObjectResult>(context.Result).StatusCode);
    }

    [Fact]
    public void Ids_validos_e_outros_valores_de_rota_seguem()
    {
        var (_, nextCalled) = Run(new RouteValueDictionary
        {
            ["controller"] = "reservations",
            ["action"] = "GetByDate",
            ["id"] = "665f1c2e9b1e8a0012345678",
            ["date"] = "2026-10-04",
            ["status"] = "pendente",
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Filtro_registrado_para_todos_os_controllers()
    {
        var contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseContentRoot(contentRoot);
                builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500");
                builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
                builder.UseSetting("JwtSettings:SecretKey", "chave-de-teste-com-mais-de-32-caracteres-0123456789");
                builder.ConfigureTestServices(services => services.Remove(services.Single(d =>
                    d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                    d.ImplementationType == typeof(VoteIndexInitializer))));
            });

            var filters = factory.Services.GetRequiredService<IOptions<MvcOptions>>().Value.Filters;

            Assert.Contains(filters, filter => filter is TypeFilterAttribute type && type.ImplementationType == typeof(MalformedIdFilter)
                || filter is ServiceFilterAttribute service && service.ServiceType == typeof(MalformedIdFilter)
                || filter is MalformedIdFilter);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }
}
