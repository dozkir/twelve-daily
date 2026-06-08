using TwelveDaily.Application.Notifications;

namespace TwelveDaily.Application.Interfaces;

public interface IPushNotificationService
{
    Task SendNextHabitAsync(IReadOnlyList<string> pushTokens, NextHabitPushNotification notification, CancellationToken ct = default);
    Task SendClearNextHabitAsync(IReadOnlyList<string> pushTokens, CancellationToken ct = default);
}

