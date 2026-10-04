using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace GestaoPro.API.Tests.Startup;

// O hub só entrega notificações do servidor (DeliveryService envia para Group(residentId)).
public class NotificacaoHubTests
{
    [Fact]
    public async Task Ao_conectar_a_conexao_entra_no_grupo_do_proprio_usuario()
    {
        var groups = new Mock<IGroupManager>();
        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.ConnectionId).Returns("conexao-1");
        context.SetupGet(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "morador-1"),
            new Claim(ClaimTypes.Role, "morador"),
        }, "Bearer")));
        var hub = new NotificacaoHub { Context = context.Object, Groups = groups.Object };

        await hub.OnConnectedAsync();

        groups.Verify(g => g.AddToGroupAsync("conexao-1", "morador-1", It.IsAny<CancellationToken>()), Times.Once);
        groups.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Conexao_sem_usuario_no_token_nao_entra_em_grupo()
    {
        var groups = new Mock<IGroupManager>();
        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.ConnectionId).Returns("conexao-1");
        context.SetupGet(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        var hub = new NotificacaoHub { Context = context.Object, Groups = groups.Object };

        await hub.OnConnectedAsync();

        groups.VerifyNoOtherCalls();
    }

    // Qualquer logado podia chamar NotificarAdmin (Clients.All, todos os condomínios) e NotificarMorador.
    // Nenhum cliente (web ou mobile) usa esses métodos: o hub não expõe método invocável.
    [Fact]
    public void Hub_nao_expoe_metodos_que_o_cliente_possa_invocar()
    {
        var invocable = typeof(NotificacaoHub)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => !method.IsSpecialName && method.GetBaseDefinition().DeclaringType == typeof(NotificacaoHub))
            .Select(method => method.Name);

        Assert.Empty(invocable);
    }
}
