using System.Security.Claims;
using GestaoPro.API.Middleware;
using GestaoPro.API.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GestaoPro.API.Tests.Middleware;

public class ApiAccessMiddlewareTests
{
    private sealed class FixedAdministratorLookup(bool hasAdministrator) : IAdministratorLookup
    {
        public Task<bool> HasAnyAdministratorAsync() => Task.FromResult(hasAdministrator);
    }

    private static async Task<(int StatusCode, bool NextCalled)> InvokeAsync(
        string method, string path, ClaimsPrincipal? user = null, bool hasAdministrator = true)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IAdministratorLookup>(new FixedAdministratorLookup(hasAdministrator))
                .BuildServiceProvider()
        };
        context.Request.Method = method;
        context.Request.Path = path;
        if (user is not null) context.User = user;

        var nextCalled = false;
        var middleware = new ApiAccessMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);
        return (context.Response.StatusCode, nextCalled);
    }

    private static ClaimsPrincipal AuthenticatedUser(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Bearer"));

    [Theory]
    [InlineData("GET", "/api/users")]
    [InlineData("GET", "/api/Users/665f1c2e9b1e8a0012345678")]
    [InlineData("GET", "/api/users/condominium/665f1c2e9b1e8a0012345678")]
    [InlineData("HEAD", "/api/users")]
    [InlineData("GET", "/api/condominiums/665f1c2e9b1e8a0012345678")]
    [InlineData("GET", "/api/visitor")]
    public async Task Leitura_anonima_de_dados_retorna_401_sem_chegar_ao_controller(string method, string path)
    {
        var (statusCode, nextCalled) = await InvokeAsync(method, path);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
        Assert.False(nextCalled);
    }

    [Theory]
    [InlineData("GET", "/api/condominiums")]
    [InlineData("GET", "/api/Condominiums")]
    [InlineData("GET", "/api/health/mongodb")]
    [InlineData("OPTIONS", "/api/users")]
    public async Task Leituras_publicas_continuam_liberadas_sem_login(string method, string path)
    {
        var (_, nextCalled) = await InvokeAsync(method, path);

        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData("morador")]
    [InlineData("porteiro")]
    [InlineData("admin")]
    public async Task Leitura_com_login_continua_liberada_para_qualquer_papel(string role)
    {
        var (_, nextCalled) = await InvokeAsync("GET", "/api/users", AuthenticatedUser(role));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Criar_credencial_sem_login_e_bloqueado_quando_ja_existe_administrador()
    {
        var (statusCode, nextCalled) = await InvokeAsync(
            "POST", "/api/users/665f1c2e9b1e8a0012345678/credentials", hasAdministrator: true);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Criar_credencial_sem_login_e_liberado_enquanto_nao_existe_administrador()
    {
        var (_, nextCalled) = await InvokeAsync(
            "POST", "/api/users/665f1c2e9b1e8a0012345678/credentials", hasAdministrator: false);

        Assert.True(nextCalled);
    }
}
