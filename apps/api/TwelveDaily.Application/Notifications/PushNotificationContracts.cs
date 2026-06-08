namespace TwelveDaily.Application.Notifications;

public static class PushNotificationTypes
{
    public const string NextHabit = "next-habit";
    public const string ClearNextHabit = "clear-next-habit";
}

public static class PushNotificationActions
{
    public const string CategoryId = "next-habit-actions";
    public const string CheckActionId = "CHECK";
}

public record NextHabitPushNotification(
    Guid UserId,
    Guid HabitId,
    DateOnly Date,
    string HabitName,
    string HabitEmoji,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    string ActionToken,
    string Title,
    string Body);

public record PushNotificationActionTokenPayload(
    Guid UserId,
    Guid HabitId,
    DateOnly Date,
    DateTime ExpiresAtUtc);

