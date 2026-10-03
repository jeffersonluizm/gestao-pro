namespace GestaoPro.API.Services;

public class EmailService
{
    public Task EnviarAsync(string destino, string assunto, string corpo)
    {
        Console.WriteLine($"[EMAIL] Para: {destino} | Assunto: {assunto}");
        return Task.CompletedTask;
    }
}
