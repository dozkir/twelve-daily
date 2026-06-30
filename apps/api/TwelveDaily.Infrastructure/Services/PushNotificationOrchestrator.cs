using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwelveDaily.Application.Common;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Application.Notifications;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.Infrastructure.Services;

public class PushNotificationOrchestrator : IPushNotificationOrchestrator
{
    private readonly IPushTokenRepository _pushTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHabitRepository _habitRepository;
    private readonly IHabitScheduleRepository _scheduleRepository;
    private readonly IHabitCheckRepository _checkRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IPushNotificationActionTokenService _actionTokenService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly NotificationWakeStore _wakeStore;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PushNotificationsOptions _options;
    private readonly ILogger<PushNotificationOrchestrator> _logger;

    public PushNotificationOrchestrator(
        IPushTokenRepository pushTokenRepository,
        IUserRepository userRepository,
        IHabitRepository habitRepository,
        IHabitScheduleRepository scheduleRepository,
        IHabitCheckRepository checkRepository,
        IPushNotificationService pushNotificationService,
        IPushNotificationActionTokenService actionTokenService,
        IBackgroundJobClient backgroundJobClient,
        NotificationWakeStore wakeStore,
        IDateTimeProvider dateTimeProvider,
        IOptions<PushNotificationsOptions> options,
        ILogger<PushNotificationOrchestrator> logger)
    {
        _pushTokenRepository = pushTokenRepository;
        _userRepository = userRepository;
        _habitRepository = habitRepository;
        _scheduleRepository = scheduleRepository;
        _checkRepository = checkRepository;
        _pushNotificationService = pushNotificationService;
        _actionTokenService = actionTokenService;
        _backgroundJobClient = backgroundJobClient;
        _wakeStore = wakeStore;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        _logger = logger;
    }

    private readonly record struct Occurrence(
        Guid HabitId,
        DateOnly Date,
        string HabitName,
        string HabitEmoji,
        DateTime StartUtc,
        DateTime EndUtc);

    public async Task RecomputeUserNotificationsAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = (await _pushTokenRepository.GetByUserIdAsync(userId, ct))
            .Select(token => token.Token)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (tokens.Count == 0)
        {
            _logger.LogInformation("Skipping push recompute for user {UserId} because no push tokens are registered.", userId);
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        var user = await _userRepository.GetByIdAsync(userId, ct);
        var tz = UserClock.ResolveTimeZone(user?.Timezone);

        var occurrences = await ComputeUpcomingOccurrencesAsync(userId, tz, now, ct);

        var activationLead = TimeSpan.FromMinutes(_options.ActivationLeadMinutes);
        var next = occurrences
            .Where(o => o.StartUtc <= now.Add(activationLead))
            .OrderBy(o => o.StartUtc)
            .ThenBy(o => o.EndUtc)
            .Select(o => (Occurrence?)o)
            .FirstOrDefault();

        if (next == null)
        {
            _logger.LogInformation("No eligible habit notification for user {UserId}; clearing active notification.", userId);
            await _pushNotificationService.SendClearNextHabitAsync(tokens, ct);
        }
        else
        {
            var occ = next.Value;
            var title = BuildTitle(occ.StartUtc, occ.EndUtc, tz);
            var body = BuildBody(occ.HabitEmoji, occ.HabitName);
            var actionTokenExpiresAt = Min(occ.EndUtc, now.AddMinutes(_options.ActionTokenMaxLifetimeMinutes));
            var actionToken = _actionTokenService.GenerateToken(userId, occ.HabitId, occ.Date, actionTokenExpiresAt);

            _logger.LogInformation(
                "Promoting habit {HabitId} on {Date} as active notification for user {UserId}.",
                occ.HabitId, occ.Date, userId);

            await _pushNotificationService.SendNextHabitAsync(tokens, new NextHabitPushNotification(
                userId,
                occ.HabitId,
                occ.Date,
                occ.HabitName,
                occ.HabitEmoji,
                occ.StartUtc,
                occ.EndUtc,
                actionToken,
                title,
                body), ct);
        }

        await ScheduleNextWakeAsync(userId, now, occurrences, activationLead, ct);
    }

    /// <summary>
    /// Ocorrências de hoje e amanhã (local) que ainda não terminaram e ainda não têm check.
    /// Derivadas de schedules ativos de hábitos ativos + timezone do usuário.
    /// </summary>
    private async Task<List<Occurrence>> ComputeUpcomingOccurrencesAsync(
        Guid userId, TimeZoneInfo tz, DateTime now, CancellationToken ct)
    {
        var habits = (await _habitRepository.GetByUserIdAsync(userId, ct)).ToDictionary(h => h.Id);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, tz));

        var occurrences = new List<Occurrence>();

        foreach (var date in new[] { today, today.AddDays(1) })
        {
            var checkedHabitIds = await _checkRepository.GetCheckedHabitIdsAsync(userId, date, ct);
            var schedules = await _scheduleRepository.GetActiveByUserAndDayAsync(userId, date.DayOfWeek, ct);

            foreach (var schedule in schedules)
            {
                if (checkedHabitIds.Contains(schedule.HabitId))
                    continue;
                if (!habits.TryGetValue(schedule.HabitId, out var habit))
                    continue;

                var startUtc = UserClock.ToUtc(date, schedule.StartTime, tz);
                var endUtc = UserClock.ToUtc(date, schedule.EndTime, tz);
                if (endUtc <= now)
                    continue;

                occurrences.Add(new Occurrence(habit.Id, date, habit.Name, habit.Emoji, startUtc, endUtc));
            }
        }

        return occurrences;
    }

    /// <summary>
    /// Agenda o próximo recompute na fronteira mais próxima em que o conjunto elegível muda
    /// (ativação de uma ocorrência futura ou fim da ocorrência ativa). Encadeia o ciclo.
    /// Mantém <b>no máximo um wake pendente por usuário</b>: cancela o anterior antes de agendar
    /// o próximo. Sem isso, cada recompute (uma por mutação de hábito/schedule/check) criaria uma
    /// cadeia independente, e todas disparariam juntas na fronteira → notificações duplicadas.
    /// </summary>
    private async Task ScheduleNextWakeAsync(
        Guid userId, DateTime now, List<Occurrence> occurrences, TimeSpan activationLead, CancellationToken ct)
    {
        var boundaries = new List<DateTime>();
        foreach (var occ in occurrences)
        {
            var activationAt = occ.StartUtc - activationLead;
            if (activationAt > now)
                boundaries.Add(activationAt);
            if (occ.EndUtc > now)
                boundaries.Add(occ.EndUtc);
        }

        var previousJobId = await _wakeStore.GetJobIdAsync(userId, ct);

        if (boundaries.Count == 0)
        {
            if (previousJobId != null)
            {
                _backgroundJobClient.Delete(previousJobId);
                await _wakeStore.ClearAsync(userId, ct);
            }
            return;
        }

        var nextWake = boundaries.Min();
        var delay = nextWake - now;
        if (delay <= TimeSpan.Zero)
            return;

        if (previousJobId != null)
            _backgroundJobClient.Delete(previousJobId);

        var jobId = _backgroundJobClient.Schedule<PushNotificationJobRunner>(
            runner => runner.RecomputeUserNotificationsAsync(userId, CancellationToken.None), delay);

        await _wakeStore.SetJobIdAsync(userId, jobId, ct);
    }

    private static string BuildTitle(DateTime startUtc, DateTime endUtc, TimeZoneInfo tz)
    {
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc), tz);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(endUtc, DateTimeKind.Utc), tz);
        return $"{localStart:HH:mm} - {localEnd:HH:mm}";
    }

    private static string BuildBody(string emoji, string habitName)
        => $"{emoji} {habitName}";

    private static DateTime Min(DateTime left, DateTime right)
        => left <= right ? left : right;
}
