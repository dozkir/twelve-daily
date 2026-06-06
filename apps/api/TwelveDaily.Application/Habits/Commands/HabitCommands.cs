using MediatR;

namespace TwelveDaily.Application.Habits.Commands;

public record CreateHabitCommand(
    Guid UserId,
    string Name,
    string Emoji,
    string? Description,
    bool SyncGoogleCalendar,
    List<CreateHabitScheduleDto> Schedules) : IRequest<Guid>;

public record CreateHabitScheduleDto(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

public record UpdateHabitCommand(
    Guid HabitId,
    Guid UserId,
    string Name,
    string Emoji,
    string? Description,
    bool SyncGoogleCalendar) : IRequest;

public record DeleteHabitCommand(Guid HabitId, Guid UserId) : IRequest;

public record ToggleHabitCommand(Guid HabitId, Guid UserId) : IRequest;

