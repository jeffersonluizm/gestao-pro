using System.Security.Claims;
using GestaoPro.API.Services.Access;

namespace GestaoPro.API.Tests.Access;

public class ResourceAccessTests
{
    private const string CondominioA = "condominio-a";
    private const string CondominioB = "condominio-b";

    private static Caller Morador(string userId = "morador-1", string? condominiumId = CondominioA) =>
        new(userId, "morador", condominiumId);

    private static Caller Admin(string? condominiumId = CondominioA) =>
        new("admin-1", "admin", condominiumId);

    [Fact]
    public void Morador_le_o_proprio_recurso()
    {
        var decision = ResourceAccess.Decide(Morador(), ownerId: "morador-1", condominiumId: CondominioA, condominiumWide: false);

        Assert.Equal(AccessDecision.Allowed, decision);
    }

    [Fact]
    public void Morador_nao_le_recurso_de_outro_morador_do_mesmo_condominio()
    {
        var decision = ResourceAccess.Decide(Morador(), ownerId: "morador-2", condominiumId: CondominioA, condominiumWide: false);

        Assert.Equal(AccessDecision.Forbidden, decision);
    }

    [Fact]
    public void Recurso_de_outro_condominio_e_tratado_como_inexistente()
    {
        Assert.Equal(AccessDecision.NotFound,
            ResourceAccess.Decide(Morador(), ownerId: "morador-2", condominiumId: CondominioB, condominiumWide: false));
        Assert.Equal(AccessDecision.NotFound,
            ResourceAccess.Decide(Admin(), ownerId: "morador-2", condominiumId: CondominioB, condominiumWide: true));
    }

    [Fact]
    public void Administracao_le_recurso_de_qualquer_morador_do_proprio_condominio()
    {
        var decision = ResourceAccess.Decide(Admin(), ownerId: "morador-2", condominiumId: CondominioA, condominiumWide: true);

        Assert.Equal(AccessDecision.Allowed, decision);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Token_sem_condominio_e_sessao_invalida_mesmo_para_o_dono(string? condominiumId)
    {
        Assert.Equal(AccessDecision.InvalidSession,
            ResourceAccess.Decide(Morador(condominiumId: condominiumId), "morador-1", CondominioA, condominiumWide: false));
        Assert.Equal(AccessDecision.InvalidSession,
            ResourceAccess.Decide(Admin(condominiumId), "morador-2", CondominioA, condominiumWide: true));
    }

    [Fact]
    public void Recurso_legado_sem_condominio_fica_so_com_o_dono()
    {
        Assert.Equal(AccessDecision.Allowed,
            ResourceAccess.Decide(Morador(), "morador-1", condominiumId: null, condominiumWide: false));
        Assert.Equal(AccessDecision.Forbidden,
            ResourceAccess.Decide(Admin(), "morador-1", condominiumId: null, condominiumWide: true));
    }

    [Fact]
    public void Caller_le_usuario_papel_e_condominio_das_claims_do_token()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "usuario-1"),
            new Claim(ClaimTypes.Role, "sindico"),
            new Claim(Caller.CondominiumIdClaim, CondominioA),
        }, "Bearer"));

        var caller = Caller.From(principal);

        Assert.Equal("usuario-1", caller.UserId);
        Assert.Equal(CondominioA, caller.CondominiumId);
        Assert.True(caller.IsAdministrator);
        Assert.False(caller.IsDoorman);
    }

    [Theory]
    [InlineData("porteiro", false, true)]
    [InlineData("morador", false, false)]
    [InlineData("admin", true, false)]
    public void Caller_identifica_os_papeis(string role, bool isAdministrator, bool isDoorman)
    {
        var caller = new Caller("u", role, CondominioA);

        Assert.Equal(isAdministrator, caller.IsAdministrator);
        Assert.Equal(isDoorman, caller.IsDoorman);
    }

    // Escritas: só a equipe (administração; porteiro onde o fluxo dele permite) e só no próprio condomínio.
    [Fact]
    public void Equipe_altera_recurso_do_proprio_condominio()
    {
        Assert.Equal(AccessDecision.Allowed, ResourceAccess.DecideManage(Admin(), CondominioA, staff: true));
    }

    [Fact]
    public void Equipe_recebe_404_ao_alterar_recurso_de_outro_condominio_ou_sem_condominio()
    {
        Assert.Equal(AccessDecision.NotFound, ResourceAccess.DecideManage(Admin(), CondominioB, staff: true));
        Assert.Equal(AccessDecision.NotFound, ResourceAccess.DecideManage(Admin(), null, staff: true));
        Assert.Equal(AccessDecision.NotFound, ResourceAccess.DecideManage(Admin(), "", staff: true));
    }

    [Fact]
    public void Quem_nao_e_da_equipe_recebe_403_e_token_sem_condominio_e_sessao_invalida()
    {
        Assert.Equal(AccessDecision.Forbidden, ResourceAccess.DecideManage(Morador(), CondominioA, staff: false));
        Assert.Equal(AccessDecision.InvalidSession, ResourceAccess.DecideManage(Admin(condominiumId: null), CondominioA, staff: true));
    }
}
