using Microsoft.AspNetCore.SignalR;

namespace GestaoPro.API.Services;

public class NotificacaoHub : Hub
{
    public async Task NotificarMorador(string residentId, string mensagem)
    {
        await Clients.Group(residentId).SendAsync("NovaNotificacao", mensagem);
    }

    public async Task NotificarAdmin(string mensagem)
    {
        await Clients.All.SendAsync("AdminNotificacao", mensagem);
    }
}
