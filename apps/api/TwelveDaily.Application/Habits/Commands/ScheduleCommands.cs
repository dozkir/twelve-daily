using MediatR;
using TwelveDaily.Application.Habits.Commands;

namespace TwelveDaily.Application.Habits.Commands;

public record UpdateHabitSchedulesCommand(
    Guid HabitId,
    Guid UserId,
    List<CreateHabitScheduleDto> Schedules) : IRequest;

public record ToggleHabitScheduleCommand(
    Guid HabitId,
    Guid UserId,
    DayOfWeek DayOfWeek) : IRequest;

