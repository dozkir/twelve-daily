using MediatR;
using TwelveDaily.Application.Common;
using TwelveDaily.Application.Habits.Commands;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.Application.Habits.Handlers;

public class CheckHabitHandler : IRequestHandler<CheckHabitCommand, HabitCheckResult>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IHabitScheduleRepository _scheduleRepository;
    private readonly IHabitCheckRepository _checkRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTime;
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator;

    public CheckHabitHandler(
        IHabitRepository habitRepository,
        IHabitScheduleRepository scheduleRepository,
        IHabitCheckRepository checkRepository,
        IUserRepository userRepository,
        IDateTimeProvider dateTime,
        IPushNotificationOrchestrator pushNotificationOrchestrator)
    {
        _habitRepository = habitRepository;
        _scheduleRepository = scheduleRepository;
        _checkRepository = checkRepository;
        _userRepository = userRepository;
        _dateTime = dateTime;
        _pushNotificationOrchestrator = pushNotificationOrchestrator;
    }

    public async Task<HabitCheckResult> Handle(CheckHabitCommand request, CancellationToken cancellationToken)
    {
        var habit = await _habitRepository.GetByIdAsync(request.HabitId, cancellationToken);
        if (habit == null)
            throw new DomainException("Habit not found.");
        if (habit.UserId != request.UserId)
            throw new ForbiddenException("Habit does not belong to user.");

        // Upsert idempotente: se já existe check para (hábito, data), retorna o existente.
        var existing = await _checkRepository.GetByHabitAndDateAsync(request.HabitId, request.Date, cancellationToken);
        if (existing != null)
            return new HabitCheckResult(existing.HabitId, existing.Date, existing.CheckedAt);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new DomainException("User not found.");

        var now = _dateTime.UtcNow;
        var localToday = UserClock.LocalToday(user.Timezone, now);

        // Snapshot do horário a partir do schedule ativo do dia (no máximo um por dia da semana).
        var schedules = await _scheduleRepository.GetByHabitIdAsync(request.HabitId, cancellationToken);
        var schedule = schedules.FirstOrDefault(s => s.DayOfWeek == request.Date.DayOfWeek && s.IsActive);
        if (schedule == null)
            throw new DomainException("Habit is not scheduled for this date.");

        var check = new HabitCheck(
            request.HabitId,
            request.UserId,
            request.Date,
            localToday,
            now,
            habit.Name,
            habit.Emoji,
            schedule.StartTime,
            schedule.EndTime);

        await _checkRepository.AddAsync(check, cancellationToken);
        await _pushNotificationOrchestrator.RecomputeUserNotificationsAsync(request.UserId, cancellationToken);

        return new HabitCheckResult(check.HabitId, check.Date, check.CheckedAt);
    }
}

public class UncheckHabitHandler : IRequestHandler<UncheckHabitCommand>
{
    private readonly IHabitCheckRepository _checkRepository;
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator;

    public UncheckHabitHandler(
        IHabitCheckRepository checkRepository,
        IPushNotificationOrchestrator pushNotificationOrchestrator)
    {
        _checkRepository = checkRepository;
        _pushNotificationOrchestrator = pushNotificationOrchestrator;
    }

    public async Task Handle(UncheckHabitCommand request, CancellationToken cancellationToken)
    {
        var check = await _checkRepository.GetByHabitAndDateAsync(request.HabitId, request.Date, cancellationToken);
        if (check == null)
            return; // DELETE idempotente: ausência de check já é o estado "não concluído".
        if (check.UserId != request.UserId)
            throw new ForbiddenException("Check does not belong to user.");

        await _checkRepository.DeleteAsync(check, cancellationToken);
        await _pushNotificationOrchestrator.RecomputeUserNotificationsAsync(request.UserId, cancellationToken);
    }
}

public class CheckHabitFromNotificationHandler : IRequestHandler<CheckHabitFromNotificationCommand, HabitCheckResult>
{
    private readonly IPushNotificationActionTokenService _actionTokenService;
    private readonly IMediator _mediator;

    public CheckHabitFromNotificationHandler(
        IPushNotificationActionTokenService actionTokenService,
        IMediator mediator)
    {
        _actionTokenService = actionTokenService;
        _mediator = mediator;
    }

    public async Task<HabitCheckResult> Handle(CheckHabitFromNotificationCommand request, CancellationToken cancellationToken)
    {
        var payload = _actionTokenService.Validate(request.ActionToken);
        if (payload.HabitId != request.HabitId || payload.Date != request.Date)
            throw new ForbiddenException("Notification action token does not match the requested habit occurrence.");

        return await _mediator.Send(new CheckHabitCommand(payload.UserId, request.HabitId, request.Date), cancellationToken);
    }
}
