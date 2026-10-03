namespace GestaoPro.API.Services;

public class SmsService
{
    public Task EnviarAsync(string numero, string mensagem)
    {
        Console.WriteLine($"[SMS] Para: {numero} | Mensagem: {mensagem}");
        return Task.CompletedTask;
    }
}
