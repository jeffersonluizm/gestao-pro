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

    // Visitante não cria conta "@demo.gestaopro.local" (que ninguém conseguiria remover) nem condomínio novo
    // (que apareceria na lista pública do cadastro).
    [Fact]
    public async Task Na_demo_nao_se_cria_usuario_do_dominio_demo_nem_condominio_novo()
    {
        var user = await Users("sindico-demo", "sindico").Create(new CreateUserRequest(
            CondominioA, null, null, "Intruso", "intruso@demo.gestaopro.local", "Senha-Forte-2026", "3133330000", "12345678901", UserRole.sindico, null));
        var condominium = await new CondominiumsController(new Mock<ICondominiumService>().Object, _catalog)
            .As("sindico-demo", "sindico")
            .Create(new CreateCondominiumRequest("Novo", "11.111.111/0001-11", "Rua", "1", "Bairro", "Cidade", "MG", "30000-000", "Codigo-Forte-2026", null, null));

        Assert.Equal(400, StatusOf(user));
        Assert.Equal(403, StatusOf(condominium));
    }

    // Nome, telefone e unidade da conta demo também não mudam: ela é a mesma para todos os visitantes.
    [Fact]
    public async Task Conta_demo_nao_tem_nome_telefone_nem_unidade_trocados()
    {
        var admin = Users("sindico-demo", "sindico");

        var name = await admin.Update("665f1c2e9b1e8a00000000d1", new UpdateUserRequest("Outro nome", null, null, null, null, null, null, null, null));
        var phone = await admin.Update("665f1c2e9b1e8a00000000d1", new UpdateUserRequest(null, null, "31999990000", null, null, null, null, null, null));

        Assert.Equal(403, StatusOf(name));
        Assert.Equal(403, StatusOf(phone));
    }

    // Registros de exemplo semeados (áreas, unidade 101, CPFs pré-cadastrados) não são removidos durante a demo.
    [Fact]
    public async Task Registros_semeados_nao_sao_removidos()
    {
        const string area = "665f1c2e9b1e8a0000000a01";
        const string unidade = "665f1c2e9b1e8a0000000101";
        const string preCadastrado = "665f1c2e9b1e8a00000000e1";
        _catalog.SetSeededRecords(new[] { area, unidade, preCadastrado, "665f1c2e9b1e8a00000000d1" });
        _directory.Setup(d => d.GetByIdAsync(preCadastrado))
            .ReturnsAsync(new UserResponse { Id = preCadastrado, CondominiumId = CondominioA, Email = "11111111111@pending.gestaopro.local", Role = UserRole.morador });

        var areas = new Mock<ICommonAreaService>();
        areas.Setup(s => s.GetByIdAsync(area)).ReturnsAsync(new CommonArea { Id = area, CondominiumId = CondominioA, Name = "Salão de festas" });
        var units = new Mock<IUnitService>();
        units.Setup(s => s.GetByIdAsync(unidade)).ReturnsAsync(new UnitResponse(unidade, CondominioA, "101", "A", 1, UnitType.apt, new List<string>(), 0, null, null, DateTime.UtcNow));

        var deleteArea = await new CommonAreasController(areas.Object, _catalog).As("sindico-demo", "sindico").Delete(area);
        var deactivateArea = await new CommonAreasController(areas.Object, _catalog).As("sindico-demo", "sindico")
            .Update(area, new UpdateCommonAreaDto { IsActive = false });
        var deleteUnit = await new UnitsController(units.Object, _directory.Object, _catalog).As("sindico-demo", "sindico").Delete(unidade);
        var deactivateResident = await Users("sindico-demo", "sindico").Deactivate(preCadastrado);

        Assert.Equal(403, StatusOf(deleteArea));
        Assert.Equal(403, StatusOf(deactivateArea));
        Assert.Equal(403, StatusOf(deleteUnit));
        Assert.Equal(403, StatusOf(deactivateResident));
        Assert.Equal(DemoGuard.SeededRecordMessage, MessageOf(deleteArea));
        areas.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
        units.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    // O que o visitante criou continua podendo ser removido.
    [Fact]
    public async Task Area_criada_pelo_visitante_pode_ser_removida()
    {
        const string area = "665f1c2e9b1e8a0000000a99";
        var areas = new Mock<ICommonAreaService>();
        areas.Setup(s => s.GetByIdAsync(area)).ReturnsAsync(new CommonArea { Id = area, CondominiumId = CondominioA, Name = "Sauna" });
        areas.Setup(s => s.DeleteAsync(area)).ReturnsAsync(true);

        var result = await new CommonAreasController(areas.Object, _catalog).As("sindico-demo", "sindico").Delete(area);

        Assert.Equal(204, StatusOf(result));
    }

    [Fact]
    public void Sem_o_modo_demo_nada_e_bloqueado_pela_protecao()
    {
        var off = DemoCatalog.FromConfiguration(new ConfigurationBuilder().Build());

        Assert.False(new DemoGuard(off).IsProtectedEmail("morador@demo.gestaopro.local"));
    }
}
