using MediatR;
using TwelveDaily.Application.Common;
using TwelveDaily.Application.Habits.Commands;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.Application.Habits.Handlers;

public class UpdateHabitSchedulesHandler : IRequestHandler<UpdateHabitSchedulesCommand>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IHabitScheduleRepository _scheduleRepository;
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator;

    public UpdateHabitSchedulesHandler(
        IHabitRepository habitRepository,
        IHabitScheduleRepository scheduleRepository,
        IPushNotificationOrchestrator pushNotificationOrchestrator)
    {
        _habitRepository = habitRepository;
        _scheduleRepository = scheduleRepository;
        _pushNotificationOrchestrator = pushNotificationOrchestrator;
    }

    public async Task Handle(UpdateHabitSchedulesCommand request, CancellationToken cancellationToken)
    {
        var habit = await _habitRepository.GetOwnedAsync(request.HabitId, request.UserId, cancellationToken);

        await _scheduleRepository.DeleteByHabitIdAsync(habit.Id, cancellationToken);

        var schedules = request.Schedules.Select(s =>
            new HabitSchedule(habit.Id, s.DayOfWeek, s.StartTime, s.EndTime)).ToList();
        await _scheduleRepository.AddRangeAsync(schedules, cancellationToken);
        await _pushNotificationOrchestrator.RecomputeUserNotificationsAsync(habit.UserId, cancellationToken);
    }
}

public class ToggleHabitScheduleHandler : IRequestHandler<ToggleHabitScheduleCommand>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IHabitScheduleRepository _scheduleRepository;
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator;

    public ToggleHabitScheduleHandler(
        IHabitRepository habitRepository,
        IHabitScheduleRepository scheduleRepository,
        IPushNotificationOrchestrator pushNotificationOrchestrator)
    {
        _habitRepository = habitRepository;
        _scheduleRepository = scheduleRepository;
        _pushNotificationOrchestrator = pushNotificationOrchestrator;
    }

    public async Task Handle(ToggleHabitScheduleCommand request, CancellationToken cancellationToken)
    {
        var habit = await _habitRepository.GetOwnedAsync(request.HabitId, request.UserId, cancellationToken);

        var schedules = await _scheduleRepository.GetByHabitIdAsync(habit.Id, cancellationToken);
        var schedule = schedules.FirstOrDefault(s => s.DayOfWeek == request.DayOfWeek);
        if (schedule == null)
            throw new DomainException($"No schedule found for {request.DayOfWeek}.");

        schedule.ToggleActive();
        await _scheduleRepository.UpdateAsync(schedule, cancellationToken);
        await _pushNotificationOrchestrator.RecomputeUserNotificationsAsync(habit.UserId, cancellationToken);
    }
}
