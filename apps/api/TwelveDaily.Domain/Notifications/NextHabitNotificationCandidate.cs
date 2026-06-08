namespace TwelveDaily.Domain.Notifications;

/// <summary>
/// Próxima ocorrência elegível para notificação. Identidade lógica = (HabitId, Date).
/// ScheduledStartTime/EndTime são UTC, derivados do schedule local + timezone do usuário.
/// </summary>
public record NextHabitNotificationCandidate(
    Guid HabitId,
    Guid UserId,
    DateOnly Date,
    string HabitName,
    string HabitEmoji,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime);
