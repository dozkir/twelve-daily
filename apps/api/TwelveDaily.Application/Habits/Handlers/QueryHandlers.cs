using MediatR;
using TwelveDaily.Application.Common;
using TwelveDaily.Application.Habits.Queries;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.Application.Habits.Handlers;

/// <summary>
/// Reconstrói a rotina de cada dia a partir de hábito + schedule + checks (sem instâncias materializadas).
/// Dias concluídos são renderizados pelo snapshot do check (fidelidade histórica);
/// dias não concluídos são reconstruídos pelo estado atual do hábito/schedule.
/// </summary>
public class GetDailyHabitsHandler : IRequestHandler<GetDailyHabitsQuery, DailyHabitsResult>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IHabitScheduleRepository _scheduleRepository;
    private readonly IHabitCheckRepository _checkRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTime;

    public GetDailyHabitsHandler(
        IHabitRepository habitRepository,
        IHabitScheduleRepository scheduleRepository,
        IHabitCheckRepository checkRepository,
        IUserRepository userRepository,
        IDateTimeProvider dateTime)
    {
        _habitRepository = habitRepository;
        _scheduleRepository = scheduleRepository;
        _checkRepository = checkRepository;
        _userRepository = userRepository;
        _dateTime = dateTime;
    }

    public async Task<DailyHabitsResult> Handle(GetDailyHabitsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        var timezone = user?.Timezone ?? request.UserTimezone;
        var today = UserClock.LocalToday(timezone, _dateTime.UtcNow);

        var startDate = request.Date.AddDays(-3);
        var endDate = request.Date.AddDays(3);

        var habits = await _habitRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var schedulesByHabit = (await _scheduleRepository.GetByUserAsync(request.UserId, cancellationToken))
            .GroupBy(s => s.HabitId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var checks = await _checkRepository.GetByUserAndDateRangeAsync(request.UserId, startDate, endDate, cancellationToken);
        var checksByKey = checks.ToDictionary(c => (c.HabitId, c.Date));

        var days = new List<DayResult>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var type = date < today ? "past" : date == today ? "today" : "future";
            var isPast = type == "past";
            var items = new List<DayItemResult>();

            foreach (var habit in habits)
            {
                if (checksByKey.TryGetValue((habit.Id, date), out var check))
                {
                    // Concluído: renderiza pelo snapshot, independente de edições/inativação posteriores.
                    items.Add(new DayItemResult(
                        habit.Id,
                        check.HabitName,
                        check.HabitEmoji,
                        habit.Description,
                        check.StartTime,
                        check.EndTime,
                        check.CheckedAt));
                    continue;
                }

                // Não concluído: reconstrói pelo estado atual.
                var createdLocalDate = UserClock.ToLocalDate(timezone, habit.CreatedAt);
                if (createdLocalDate > date)
                    continue; // hábito ainda não existia nessa data
                if (!isPast && !habit.IsActive)
                    continue; // hoje/futuro: hábito inativo está pausado

                var schedule = ActiveScheduleForDay(schedulesByHabit, habit.Id, date.DayOfWeek);
                if (schedule == null)
                    continue;

                items.Add(new DayItemResult(
                    habit.Id,
                    habit.Name,
                    habit.Emoji,
                    habit.Description,
                    schedule.StartTime,
                    schedule.EndTime,
                    null));
            }

            days.Add(new DayResult(date, type, items.OrderBy(i => i.StartTime).ToList()));
        }

        return new DailyHabitsResult(days);
    }

    private static HabitSchedule? ActiveScheduleForDay(
        Dictionary<Guid, List<HabitSchedule>> schedulesByHabit, Guid habitId, DayOfWeek dayOfWeek)
    {
        // No máximo um schedule ativo por dia da semana por hábito (invariante de domínio).
        if (!schedulesByHabit.TryGetValue(habitId, out var schedules))
            return null;

        return schedules.FirstOrDefault(s => s.DayOfWeek == dayOfWeek && s.IsActive);
    }
}

public class GetHabitDetailHandler : IRequestHandler<GetHabitDetailQuery, HabitDetailResult>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IHabitScheduleRepository _scheduleRepository;

    public GetHabitDetailHandler(IHabitRepository habitRepository, IHabitScheduleRepository scheduleRepository)
    {
        _habitRepository = habitRepository;
        _scheduleRepository = scheduleRepository;
    }

    public async Task<HabitDetailResult> Handle(GetHabitDetailQuery request, CancellationToken cancellationToken)
    {
        var habit = await _habitRepository.GetByIdAsync(request.HabitId, cancellationToken);
        if (habit == null)
            throw new DomainException("Habit not found.");
        if (habit.UserId != request.UserId)
            throw new ForbiddenException("Habit does not belong to user.");

        var schedules = await _scheduleRepository.GetByHabitIdAsync(habit.Id, cancellationToken);

        return new HabitDetailResult(
            habit.Id,
            habit.Name,
            habit.Emoji,
            habit.Description,
            habit.IsActive,
            habit.SyncGoogleCalendar,
            schedules.Select(s => new HabitScheduleResult(
                s.DayOfWeek, s.StartTime, s.EndTime, s.IsActive)).ToList());
    }
}

public class GetHabitsListHandler : IRequestHandler<GetHabitsListQuery, List<HabitListItemResult>>
{
    private readonly IHabitRepository _habitRepository;

    public GetHabitsListHandler(IHabitRepository habitRepository)
    {
        _habitRepository = habitRepository;
    }

    public async Task<List<HabitListItemResult>> Handle(GetHabitsListQuery request, CancellationToken cancellationToken)
    {
        var habits = await _habitRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return habits.Select(h => new HabitListItemResult(
            h.Id, h.Name, h.Emoji, h.Description, h.IsActive, h.SyncGoogleCalendar)).ToList();
    }
}

/// <summary>
/// Denominador = hábitos que deveriam contar naquele dia (existiam por CreatedAt e estavam agendados),
/// independente do estado de ativação atual. Dias futuros ainda não contam.
/// </summary>
public class GetWeeklyDashboardHandler : IRequestHandler<GetWeeklyDashboardQuery, WeeklyDashboardResult>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IHabitScheduleRepository _scheduleRepository;
    private readonly IHabitCheckRepository _checkRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTime;

    public GetWeeklyDashboardHandler(
        IHabitRepository habitRepository,
        IHabitScheduleRepository scheduleRepository,
        IHabitCheckRepository checkRepository,
        IUserRepository userRepository,
        IDateTimeProvider dateTime)
    {
        _habitRepository = habitRepository;
        _scheduleRepository = scheduleRepository;
        _checkRepository = checkRepository;
        _userRepository = userRepository;
        _dateTime = dateTime;
    }

    public async Task<WeeklyDashboardResult> Handle(GetWeeklyDashboardQuery request, CancellationToken cancellationToken)
    {
        var weekEnd = request.WeekStart.AddDays(6);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        var today = UserClock.LocalToday(user?.Timezone, _dateTime.UtcNow);

        var habits = await _habitRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var schedulesByHabit = (await _scheduleRepository.GetByUserAsync(request.UserId, cancellationToken))
            .GroupBy(s => s.HabitId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var checks = await _checkRepository.GetByUserAndDateRangeAsync(request.UserId, request.WeekStart, weekEnd, cancellationToken);
        var checkedByKey = checks.Select(c => (c.HabitId, c.Date)).ToHashSet();

        var dayByDay = new List<DayCompletionResult>();
        var total = 0;
        var completed = 0;

        for (var date = request.WeekStart; date <= weekEnd; date = date.AddDays(1))
        {
            var dayTotal = 0;
            var dayCompleted = 0;

            if (date <= today)
            {
                foreach (var habit in habits)
                {
                    var createdLocalDate = UserClock.ToLocalDate(user?.Timezone, habit.CreatedAt);
                    if (createdLocalDate > date)
                        continue;

                    var hasSchedule = schedulesByHabit.TryGetValue(habit.Id, out var schedules)
                        && schedules.Any(s => s.DayOfWeek == date.DayOfWeek && s.IsActive);
                    if (!hasSchedule)
                        continue;

                    dayTotal++;
                    if (checkedByKey.Contains((habit.Id, date)))
                        dayCompleted++;
                }
            }

            total += dayTotal;
            completed += dayCompleted;
            dayByDay.Add(new DayCompletionResult(date, dayTotal, dayCompleted));
        }

        var rate = total > 0 ? Math.Round((double)completed / total * 100, 2) : 0;
        return new WeeklyDashboardResult(total, completed, rate, dayByDay);
    }
}
