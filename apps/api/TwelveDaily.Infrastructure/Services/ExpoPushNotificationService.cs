using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Application.Notifications;

namespace TwelveDaily.Infrastructure.Services;

public class ExpoPushNotificationService : IPushNotificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly PushNotificationsOptions _options;
    private readonly ILogger<ExpoPushNotificationService> _logger;

    public ExpoPushNotificationService(
        HttpClient httpClient,
        IOptions<PushNotificationsOptions> options,
        ILogger<ExpoPushNotificationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.ExpoAccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ExpoAccessToken);
        }
    }

    public Task SendNextHabitAsync(
        IReadOnlyList<string> pushTokens,
        NextHabitPushNotification notification,
        CancellationToken ct = default)
    {
        var messages = pushTokens
            .Distinct(StringComparer.Ordinal)
            .Select(token => new ExpoPushMessage(
                To: token,
                Title: notification.Title,
                Body: notification.Body,
                Sound: "default",
                Priority: "high",
                CategoryId: PushNotificationActions.CategoryId,
                Data: new Dictionary<string, object?>
                {
                    ["type"] = PushNotificationTypes.NextHabit,
                    ["habitId"] = notification.HabitId,
                    ["date"] = notification.Date.ToString("yyyy-MM-dd"),
                    ["habitName"] = notification.HabitName,
                    ["habitEmoji"] = notification.HabitEmoji,
                    ["scheduledStartTime"] = notification.ScheduledStartTime,
                    ["scheduledEndTime"] = notification.ScheduledEndTime,
                    ["actionToken"] = notification.ActionToken
                }))
            .ToList();

        return SendBatchAsync(messages, $"next habit {notification.HabitId} {notification.Date:yyyy-MM-dd}", ct);
    }

    public Task SendClearNextHabitAsync(IReadOnlyList<string> pushTokens, CancellationToken ct = default)
    {
        var messages = pushTokens
            .Distinct(StringComparer.Ordinal)
            .Select(token => new ExpoPushMessage(
                To: token,
                Title: null,
                Body: null,
                Sound: null,
                Priority: "high",
                CategoryId: null,
                Data: new Dictionary<string, object?>
                {
                    ["type"] = PushNotificationTypes.ClearNextHabit
                }))
            .ToList();

        return SendBatchAsync(messages, "clear-next-habit", ct);
    }

    private async Task SendBatchAsync(List<ExpoPushMessage> messages, string description, CancellationToken ct)
    {
        if (messages.Count == 0)
            return;

        var response = await _httpClient.PostAsJsonAsync("api/v2/push/send", messages, JsonOptions, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Expo push send failed for {Description}. Status: {StatusCode}. Response: {ResponseBody}",
                description,
                (int)response.StatusCode,
                body);
            return;
        }

        _logger.LogInformation("Expo push sent for {Description}. Response: {ResponseBody}", description, body);
    }

    private sealed record ExpoPushMessage(
        string To,
        string? Title,
        string? Body,
        string? Sound,
        string Priority,
        string? CategoryId,
        Dictionary<string, object?> Data);
}

