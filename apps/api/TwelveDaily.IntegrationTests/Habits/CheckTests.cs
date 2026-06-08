using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TwelveDaily.Application.Interfaces;

namespace TwelveDaily.IntegrationTests.Habits;

public class CheckTests : IntegrationTestBase
{
    private async Task<(AuthenticatedUser user, Guid habitId, string today)> CreateHabitForTodayAsync(string email)
    {
        var user = await RegisterAndAuthenticateAsync(email, timezone: "UTC");
        var todayDow = DateTime.UtcNow.DayOfWeek.ToString();
        var createResponse = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Hábito",
            emoji = "✅",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = todayDow, startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var habitId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        return (user, habitId, today);
    }

    [Fact]
    public async Task Check_ShouldMarkHabitAsDone()
    {
        var (user, habitId, today) = await CreateHabitForTodayAsync("check@example.com");

        var response = await user.Client.PutAsJsonAsync($"/habits/{habitId}/check", new { date = today });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CheckResult>();
        result!.HabitId.Should().Be(habitId);

        // A timeline reflete o check.
        (await GetTodayCheckedAtAsync(user, today, habitId)).Should().HaveValue();

        user.Dispose();
    }

    [Fact]
    public async Task Check_ShouldBeIdempotent()
    {
        var (user, habitId, today) = await CreateHabitForTodayAsync("checkidem@example.com");

        var first = await user.Client.PutAsJsonAsync($"/habits/{habitId}/check", new { date = today });
        var second = await user.Client.PutAsJsonAsync($"/habits/{habitId}/check", new { date = today });

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstResult = await first.Content.ReadFromJsonAsync<CheckResult>();
        var secondResult = await second.Content.ReadFromJsonAsync<CheckResult>();
        // mesmo registro (tolerância p/ precisão de timestamp do Postgres vs ticks do .NET)
        secondResult!.CheckedAt.Should().BeCloseTo(firstResult!.CheckedAt, TimeSpan.FromMilliseconds(1));

        user.Dispose();
    }

    [Fact]
    public async Task Check_FutureDate_ShouldReturn400()
    {
        var (user, habitId, _) = await CreateHabitForTodayAsync("checkfuture@example.com");

        // mesmo dia da semana, 7 dias à frente
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd");
        var response = await user.Client.PutAsJsonAsync($"/habits/{habitId}/check", new { date = future });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        user.Dispose();
    }

    [Fact]
    public async Task Check_NonExistentHabit_ShouldReturn400()
    {
        var user = await RegisterAndAuthenticateAsync("checkmissing@example.com", timezone: "UTC");
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await user.Client.PutAsJsonAsync($"/habits/{Guid.NewGuid()}/check", new { date = today });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        user.Dispose();
    }

    [Fact]
    public async Task Check_FromAnotherUser_ShouldReturn403()
    {
        var (owner, habitId, today) = await CreateHabitForTodayAsync("checkowner@example.com");
        var other = await RegisterAndAuthenticateAsync("checkother@example.com", timezone: "UTC");

        var response = await other.Client.PutAsJsonAsync($"/habits/{habitId}/check", new { date = today });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        owner.Dispose();
        other.Dispose();
    }

    [Fact]
    public async Task Uncheck_ShouldClearCheck()
    {
        var (user, habitId, today) = await CreateHabitForTodayAsync("uncheck@example.com");

        await user.Client.PutAsJsonAsync($"/habits/{habitId}/check", new { date = today });

        var response = await user.Client.DeleteAsync($"/habits/{habitId}/check?date={today}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetTodayCheckedAtAsync(user, today, habitId)).Should().BeNull();

        user.Dispose();
    }

    [Fact]
    public async Task Uncheck_WithoutExistingCheck_ShouldBeIdempotent()
    {
        var (user, habitId, today) = await CreateHabitForTodayAsync("uncheckidem@example.com");

        var response = await user.Client.DeleteAsync($"/habits/{habitId}/check?date={today}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        user.Dispose();
    }

    [Fact]
    public async Task CheckFromNotification_ShouldAllowAnonymousWithValidToken()
    {
        var (user, habitId, today) = await CreateHabitForTodayAsync("checknotif@example.com");

        var profile = await (await user.Client.GetAsync("/users/me"))
            .Content.ReadFromJsonAsync<NotificationProfileDto>();

        using var scope = Factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IPushNotificationActionTokenService>();
        var actionToken = tokenService.GenerateToken(
            profile!.Id,
            habitId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTime.UtcNow.AddMinutes(30));

        // Client sem autenticação
        var response = await Client.PostAsJsonAsync(
            $"/habits/{habitId}/check/from-notification",
            new { date = today, actionToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetTodayCheckedAtAsync(user, today, habitId)).Should().HaveValue();

        user.Dispose();
    }

    private async Task<DateTime?> GetTodayCheckedAtAsync(AuthenticatedUser user, string date, Guid habitId)
    {
        var response = await user.Client.GetAsync($"/habits/daily?date={date}");
        response.EnsureSuccessStatusCode();
        var daily = (await response.Content.ReadFromJsonAsync<DailyDto>())!;
        return daily.Days.First(d => d.Type == "today").Items.Single(i => i.HabitId == habitId).CheckedAt;
    }
}

file record CheckResult(Guid HabitId, DateOnly Date, DateTime CheckedAt);
file record DailyDto(List<DayDto> Days);
file record DayDto(string Date, string Type, List<ItemDto> Items);
file record ItemDto(Guid HabitId, string Name, DateTime? CheckedAt = null);
file record NotificationProfileDto(Guid Id, string Email, string Timezone, DateTime CreatedAt);
