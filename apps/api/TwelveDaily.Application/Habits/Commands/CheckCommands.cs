using MediatR;

namespace TwelveDaily.Application.Habits.Commands;

// Ocorrência lógica = (HabitId, Date). Um check por hábito por dia (upsert/delete).

public record CheckHabitCommand(Guid UserId, Guid HabitId, DateOnly Date) : IRequest<HabitCheckResult>;

public record UncheckHabitCommand(Guid UserId, Guid HabitId, DateOnly Date) : IRequest;

public record CheckHabitFromNotificationCommand(Guid HabitId, DateOnly Date, string ActionToken) : IRequest<HabitCheckResult>;

public record HabitCheckResult(Guid HabitId, DateOnly Date, DateTime CheckedAt);
