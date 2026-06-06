namespace TwelveDaily.Application.Interfaces;

public interface IPushNotificationOrchestrator
{
    Task RecomputeUserNotificationsAsync(Guid userId, CancellationToken ct = default);
}

