using TwelveDaily.Application.Interfaces;

namespace TwelveDaily.Infrastructure.Services;

public class PushNotificationJobRunner
{
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator;

    public PushNotificationJobRunner(IPushNotificationOrchestrator pushNotificationOrchestrator)
    {
        _pushNotificationOrchestrator = pushNotificationOrchestrator;
    }

    public Task RecomputeUserNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
        => _pushNotificationOrchestrator.RecomputeUserNotificationsAsync(userId, cancellationToken);
}

