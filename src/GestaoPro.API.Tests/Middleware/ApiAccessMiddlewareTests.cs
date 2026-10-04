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
    [InlineData("GET", "/api/condominiums/")]
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
    [InlineData("GET", "/swagger/index.html")]
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

    // O desvio do middleware vale só para o hub; caminhos que apenas começam com o nome dele seguem a regra comum.
    [Theory]
    [InlineData("POST", "/notificacaoHubX")]
    [InlineData("POST", "/notificacaoHubX/negotiate")]
    [InlineData("POST", "/notificacaoHub-admin/negotiate")]
    public async Task Caminho_parecido_com_o_hub_nao_ganha_o_desvio_do_middleware(string method, string path)
    {
        var (anonymousStatus, anonymousNext) = await InvokeAsync(method, path);
        var (residentStatus, residentNext) = await InvokeAsync(method, path, AuthenticatedUser("morador"));

        Assert.Equal(StatusCodes.Status401Unauthorized, anonymousStatus);
        Assert.False(anonymousNext);
        Assert.Equal(StatusCodes.Status403Forbidden, residentStatus);
        Assert.False(residentNext);
    }

    [Theory]
    [InlineData("POST", "/notificacaoHub/negotiate")]
    [InlineData("GET", "/notificacaoHub")]
    public async Task Hub_continua_com_o_desvio_do_middleware(string method, string path)
    {
        var (_, nextCalled) = await InvokeAsync(method, path, AuthenticatedUser("morador"));

        Assert.True(nextCalled);
    }

    // Ações que os clientes usam fora da administração; o controller confere dono e condomínio.
    [Theory]
    [InlineData("PATCH", "/api/Users/665f1c2e9b1e8a0012345678/change-password", "morador")]
    [InlineData("PATCH", "/api/Users/665f1c2e9b1e8a0012345678/change-password", "porteiro")]
    [InlineData("DELETE", "/api/reservations/665f1c2e9b1e8a0012345678", "morador")]
    [InlineData("DELETE", "/api/Occurrences/665f1c2e9b1e8a0012345678", "morador")]
    [InlineData("POST", "/api/Visitor/check-in/665f1c2e9b1e8a0012345678", "porteiro")]
    [InlineData("POST", "/api/Visitor/check-out/665f1c2e9b1e8a0012345678", "porteiro")]
    public async Task Acoes_do_proprio_papel_chegam_ao_controller(string method, string path, string role)
    {
        var (_, nextCalled) = await InvokeAsync(method, path, AuthenticatedUser(role));

        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData("POST", "/api/Visitor/check-in/665f1c2e9b1e8a0012345678", "morador")]
    [InlineData("POST", "/api/Visitor/check-out/665f1c2e9b1e8a0012345678", "morador")]
    [InlineData("PUT", "/api/Visitor/665f1c2e9b1e8a0012345678", "porteiro")]
    [InlineData("PATCH", "/api/Users/665f1c2e9b1e8a0012345678/password", "morador")]
    [InlineData("PATCH", "/api/Users/665f1c2e9b1e8a0012345678/reset-password", "morador")]
    [InlineData("PUT", "/api/reservations/665f1c2e9b1e8a0012345678", "morador")]
    public async Task Acoes_fora_do_papel_continuam_403(string method, string path, string role)
    {
        var (statusCode, nextCalled) = await InvokeAsync(method, path, AuthenticatedUser(role));

        Assert.Equal(StatusCodes.Status403Forbidden, statusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Trocar_senha_sem_login_continua_401()
    {
        var (statusCode, nextCalled) = await InvokeAsync("PATCH", "/api/Users/665f1c2e9b1e8a0012345678/change-password");

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
        Assert.False(nextCalled);
    }
}
