using TwelveDaily.Application.Notifications;

namespace TwelveDaily.Application.Interfaces;

public interface IPushNotificationActionTokenService
{
    string GenerateToken(Guid userId, Guid habitId, DateOnly date, DateTime expiresAtUtc);
    PushNotificationActionTokenPayload Validate(string token);
}

