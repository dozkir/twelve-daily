using MediatR;

namespace TwelveDaily.Application.Habits.Queries;

public record GetDailyHabitsQuery(Guid UserId, DateOnly Date, string UserTimezone) : IRequest<DailyHabitsResult>;

public record DailyHabitsResult(List<DayResult> Days);

public record DayResult(DateOnly Date, string Type, List<DayItemResult> Items);

public record DayItemResult(
    Guid HabitId,
    string Name,
    string Emoji,
    string? Description,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateTime? CheckedAt);

