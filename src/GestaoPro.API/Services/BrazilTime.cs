namespace GestaoPro.API.Services;

// Horário de Brasília para regras de "hoje" (o servidor hospedado roda em UTC).
public static class BrazilTime
{
    private static readonly TimeZoneInfo? Zone = FindZone();

    public static DateTime Now(TimeProvider? clock = null)
    {
        var utc = (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        return Zone is null ? utc.AddHours(-3) : TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
    }

    public static DateOnly Today(TimeProvider? clock = null) => DateOnly.FromDateTime(Now(clock));

    private static TimeZoneInfo? FindZone()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return null;
    }
}
