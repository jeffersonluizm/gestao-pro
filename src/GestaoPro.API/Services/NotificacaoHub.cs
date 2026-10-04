using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace GestaoPro.API.Services;

// Canal só de entrega: quem envia é o servidor (DeliveryService manda para Group(residentId)).
// Não há método invocável pelo cliente; antes, qualquer logado podia disparar NotificarAdmin para
// todos os condomínios (Clients.All) ou NotificarMorador para qualquer morador, e nenhum cliente os usava.
public class NotificacaoHub : Hub
{
    // Cada conexão entra no grupo do próprio usuário (claim NameIdentifier do JWT), o nome usado pelo DeliveryService.
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }
}
