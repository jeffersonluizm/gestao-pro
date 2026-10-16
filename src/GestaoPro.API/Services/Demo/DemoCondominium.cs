using GestaoPro.API.Models;

namespace GestaoPro.API.Services.Demo;

// Estado original do condomínio de demonstração. O reset devolve tudo o que um visitante pode editar
// (nome, endereço, regras, regulamento) e o código da configuração.
public static class DemoCondominium
{
    public const string Rules = "Silêncio das 22h às 7h. Reservas pelo app ou pelo painel.";
    public const string Regulations = "Regulamento interno de exemplo: áreas comuns até as 23h, animais na coleira nas áreas comuns.";

    public static Address OriginalAddress() => new()
    {
        Street = "Rua das Acácias", Number = "100", Neighborhood = "Centro",
        City = "Belo Horizonte", State = "MG", ZipCode = "30110-000",
    };

    public static void Restore(Condominium condominium, string codeHash)
    {
        condominium.Name = DemoCatalog.CondominiumName;
        condominium.Cnpj = DemoCatalog.CondominiumCnpj;
        condominium.Address = OriginalAddress();
        condominium.Rules = Rules;
        condominium.Regulations = Regulations;
        condominium.DefaultPassword = codeHash;
        condominium.UpdatedAt = DateTime.UtcNow;
    }
}
