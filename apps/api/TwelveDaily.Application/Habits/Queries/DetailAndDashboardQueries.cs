using MediatR;

namespace TwelveDaily.Application.Habits.Queries;

public record GetHabitDetailQuery(Guid HabitId, Guid UserId) : IRequest<HabitDetailResult>;

public record HabitDetailResult(
    Guid Id,
    string Name,
    string Emoji,
    string? Description,
    bool IsActive,
    bool SyncGoogleCalendar,
    List<HabitScheduleResult> Schedules);

public record HabitScheduleResult(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

public record GetHabitsListQuery(Guid UserId) : IRequest<List<HabitListItemResult>>;

public record HabitListItemResult(
    Guid Id,
    string Name,
    string Emoji,
    string? Description,
    bool IsActive,
    bool SyncGoogleCalendar);

public record GetWeeklyDashboardQuery(Guid UserId, DateOnly WeekStart) : IRequest<WeeklyDashboardResult>;

public record WeeklyDashboardResult(
    int Total,
    int Completed,
    double CompletionRate,
    List<DayCompletionResult> DayByDay);

public record DayCompletionResult(DateOnly Date, int Total, int Completed);

