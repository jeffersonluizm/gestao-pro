using Microsoft.AspNetCore.Mvc;

namespace GestaoPro.API.Services.Demo;

// Proteção do modo demonstração: as contas e o condomínio demo são compartilhados por todos os visitantes,
// então não podem ter senha trocada, ser desativados, rebaixados ou excluídos. Sem a flag, não bloqueia nada.
public sealed class DemoGuard
{
    public const string ProtectedAccountMessage =
        "Esta é uma conta de demonstração compartilhada: senha, perfil e acesso não podem ser alterados.";
    public const string ProtectedCondominiumMessage =
        "Este é o condomínio de demonstração: ele não pode ser excluído nem ter o nome ou o código trocados.";

    private readonly DemoCatalog _catalog;

    public DemoGuard(DemoCatalog? catalog)
    {
        _catalog = catalog ?? DemoCatalog.Disabled;
    }

    public bool IsProtectedEmail(string? email) => _catalog.IsDemoEmail(email);

    public bool IsProtectedCondominium(string? condominiumId) => _catalog.IsDemoCondominium(condominiumId);

    public static ObjectResult Forbidden(string message) =>
        new(new { message }) { StatusCode = StatusCodes.Status403Forbidden };
}
