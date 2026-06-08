namespace TwelveDaily.Application.Common;

/// <summary>
/// Conversões entre o horário local do usuário (IANA timezone) e UTC.
/// Timezone inválido/ausente cai em UTC.
/// </summary>
public static class UserClock
{
    public static TimeZoneInfo ResolveTimeZone(string? timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    public static DateOnly ToLocalDate(string? timezone, DateTime utc)
    {
        var tz = ResolveTimeZone(timezone);
        var normalizedUtc = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, tz));
    }

    public static DateOnly LocalToday(string? timezone, DateTime utcNow)
        => ToLocalDate(timezone, utcNow);

    public static DateTime ToUtc(DateOnly date, TimeOnly localTime, TimeZoneInfo tz)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(localTime), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }
}
