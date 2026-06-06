namespace TwelveDaily.Infrastructure.Services;

public class PushNotificationsOptions
{
    public const string SectionName = "PushNotifications";

    public string ExpoBaseUrl { get; set; } = "https://exp.host/--/";
    public string? ExpoAccessToken { get; set; }
    public int ActivationLeadMinutes { get; set; } = 15;
    public int ActionTokenMaxLifetimeMinutes { get; set; } = 1440;
    public string? ActionTokenSecret { get; set; }
    public string ActionTokenIssuer { get; set; } = "twelve-daily-notifications";
    public string ActionTokenAudience { get; set; } = "twelve-daily-clients";
}

