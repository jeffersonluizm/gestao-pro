using GestaoPro.API.Controllers;
using GestaoPro.API.Services.Demo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Demo;

// Contas e condomínio da demonstração são compartilhados por todos os visitantes:
// ninguém troca a senha, desativa, rebaixa ou apaga (403 com mensagem clara).
public class DemoProtectionTests
{
    private readonly Mock<IUserDirectory> _directory = new();
    private readonly DemoCatalog _catalog = DemoCatalog.FromConfiguration(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Demo:Enabled"] = "true" })
        .Build());

    public DemoProtectionTests()
    {
        _catalog.CondominiumId = CondominioA;
        _directory.Setup(d => d.GetByIdAsync("665f1c2e9b1e8a00000000d1"))
            .ReturnsAsync(new UserResponse { Id = "665f1c2e9b1e8a00000000d1", CondominiumId = CondominioA, Email = "morador@demo.gestaopro.local", Role = UserRole.morador });
        _directory.Setup(d => d.GetByIdAsync("joao"))
            .ReturnsAsync(new UserResponse { Id = "joao", CondominiumId = CondominioA, Email = "joao@teste.com", Role = UserRole.morador });
    }

    // O serviço (MongoDB) não é construído: a recusa acontece antes dele.
    private UsersController Users(string userId, string role) =>
        new UsersController(null!, _directory.Object, NullLogger<UsersController>.Instance, _catalog).As(userId, role);

    private static string? MessageOf(Microsoft.AspNetCore.Mvc.IActionResult result)
    {
        var value = ValueOf<object>(result);
        return value.GetType().GetProperty("message")?.GetValue(value) as string;
    }

    [Fact]
    public async Task Conta_demo_nao_troca_a_propria_senha()
    {
        var result = await Users("665f1c2e9b1e8a00000000d1", "morador").ChangePassword("665f1c2e9b1e8a00000000d1", new ChangePasswordRequest("x", "Senha-Nova-2026"));

        Assert.Equal(403, StatusOf(result));
        Assert.Equal(DemoGuard.ProtectedAccountMessage, MessageOf(result));
    }

    [Fact]
    public async Task Gestao_nao_desativa_nem_redefine_nem_altera_conta_demo()
    {
        var admin = Users("sindico-demo", "sindico");

        var results = new[]
        {
            await admin.Deactivate("665f1c2e9b1e8a00000000d1"),
            await admin.ResetPassword("665f1c2e9b1e8a00000000d1"),
            await admin.UpdatePassword("665f1c2e9b1e8a00000000d1", new ResetPasswordRequest("Senha-Nova-2026")),
            await admin.Update("665f1c2e9b1e8a00000000d1", new UpdateUserRequest(null, null, null, null, null, UserRole.admin, null, false, null)),
        };

        Assert.All(results, result => Assert.Equal(403, StatusOf(result)));
    }

    [Fact]
    public async Task Condominio_da_demonstracao_nao_e_excluido_nem_tem_nome_ou_codigo_trocados()
    {
        var service = new Mock<ICondominiumService>();
        var controller = new CondominiumsController(service.Object, _catalog).As("sindico-demo", "sindico");

        var delete = await controller.Delete(CondominioA);
        var code = await controller.Update(CondominioA, new UpdateCondominiumRequest(null, null, null, null, null, null, null, "Outro-Codigo-2026", null, null));
        var name = await controller.Update(CondominioA, new UpdateCondominiumRequest("Outro nome", null, null, null, null, null, null, null, null, null));
        var rules = await controller.Update(CondominioA, new UpdateCondominiumRequest(null, null, null, null, null, null, null, null, "Novas regras", null));

        Assert.Equal(403, StatusOf(delete));
        Assert.Equal(403, StatusOf(code));
        Assert.Equal(403, StatusOf(name));
        Assert.NotEqual(403, StatusOf(rules));
        service.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Rotas_de_credencial_tambem_protegem_a_conta_demo()
    {
        var auth = new AuthController(null!, _directory.Object, null!, new CondominiumAttemptLimiter(100, TimeSpan.FromMinutes(1)), _catalog)
            .As("sindico-demo", "sindico");

        var reset = await auth.ResetPassword(new AuthResetPasswordRequest("665f1c2e9b1e8a00000000d1", "Senha-Nova-2026"));
        var deactivate = await auth.Deactivate("665f1c2e9b1e8a00000000d1");

        Assert.Equal(403, StatusOf(reset));
        Assert.Equal(403, StatusOf(deactivate));
    }

    [Fact]
    public void Sem_o_modo_demo_nada_e_bloqueado_pela_protecao()
    {
        var off = DemoCatalog.FromConfiguration(new ConfigurationBuilder().Build());

        Assert.False(new DemoGuard(off).IsProtectedEmail("morador@demo.gestaopro.local"));
    }
}
