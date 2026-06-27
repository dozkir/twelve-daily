using MediatR;
using TwelveDaily.Application.Common;
using TwelveDaily.Application.Habits.Commands;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.Application.Habits.Handlers;

public class CreateHabitHandler : IRequestHandler<CreateHabitCommand, Guid>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IHabitScheduleRepository _scheduleRepository;
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator;

    public CreateHabitHandler(
        IHabitRepository habitRepository,
        IHabitScheduleRepository scheduleRepository,
        IPushNotificationOrchestrator pushNotificationOrchestrator)
    {
        _habitRepository = habitRepository;
        _scheduleRepository = scheduleRepository;
        _pushNotificationOrchestrator = pushNotificationOrchestrator;
    }

    public async Task<Guid> Handle(CreateHabitCommand request, CancellationToken cancellationToken)
    {
        var habit = new Habit(request.UserId, request.Name, request.Emoji, request.Description, request.SyncGoogleCalendar);
        await _habitRepository.AddAsync(habit, cancellationToken);

        var schedules = request.Schedules.Select(s =>
            new HabitSchedule(habit.Id, s.DayOfWeek, s.StartTime, s.EndTime)).ToList();
        await _scheduleRepository.AddRangeAsync(schedules, cancellationToken);

        // Sem geração de instâncias: a ocorrência de hoje aparece na timeline a partir do schedule.
        await _pushNotificationOrchestrator.RecomputeUserNotificationsAsync(request.UserId, cancellationToken);

        return habit.Id;
    }
}

public class UpdateHabitHandler : IRequestHandler<UpdateHabitCommand>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator;

    public UpdateHabitHandler(IHabitRepository habitRepository, IPushNotificationOrchestrator pushNotificationOrchestrator)
    {
        _habitRepository = habitRepository;
        _pushNotificationOrchestrator = pushNotificationOrchestrator;
    }

    public async Task Handle(UpdateHabitCommand request, CancellationToken cancellationToken)
    {
        var habit = await _habitRepository.GetOwnedAsync(request.HabitId, request.UserId, cancellationToken);

        habit.Update(request.Name, request.Emoji, request.Description, request.SyncGoogleCalendar);
        await _habitRepository.UpdateAsync(habit, cancellationToken);
        await _pushNotificationOrchestrator.RecomputeUserNotificationsAsync(habit.UserId, cancellationToken);
    }
}

public class DeleteHabitHandler : IRequestHandler<DeleteHabitCommand>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator;

    public DeleteHabitHandler(IHabitRepository habitRepository, IPushNotificationOrchestrator pushNotificationOrchestrator)
    {
        _habitRepository = habitRepository;
        _pushNotificationOrchestrator = pushNotificationOrchestrator;
    }

    public async Task Handle(DeleteHabitCommand request, CancellationToken cancellationToken)
    {
        var habit = await _habitRepository.GetOwnedAsync(request.HabitId, request.UserId, cancellationToken);

        await _habitRepository.DeleteAsync(habit, cancellationToken);
        await _pushNotificationOrchestrator.RecomputeUserNotificationsAsync(habit.UserId, cancellationToken);
    }
}

public class ToggleHabitHandler : IRequestHandler<ToggleHabitCommand>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator;

    public ToggleHabitHandler(IHabitRepository habitRepository, IPushNotificationOrchestrator pushNotificationOrchestrator)
    {
        _habitRepository = habitRepository;
        _pushNotificationOrchestrator = pushNotificationOrchestrator;
    }

    public async Task Handle(ToggleHabitCommand request, CancellationToken cancellationToken)
    {
        var habit = await _habitRepository.GetOwnedAsync(request.HabitId, request.UserId, cancellationToken);

        habit.ToggleActive();
        await _habitRepository.UpdateAsync(habit, cancellationToken);
        await _pushNotificationOrchestrator.RecomputeUserNotificationsAsync(habit.UserId, cancellationToken);
    }
}
