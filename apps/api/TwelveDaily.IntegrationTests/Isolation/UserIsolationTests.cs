using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TwelveDaily.IntegrationTests.Isolation;

public class UserIsolationTests : IntegrationTestBase
{
    [Fact]
    public async Task UserA_ShouldNotSeeHabitsOfUserB()
    {
        using var userA = await RegisterAndAuthenticateAsync("usera@example.com");
        using var userB = await RegisterSecondUserAsync();

        await userA.Client.PostAsJsonAsync("/habits", new
        {
            name = "Hábito do A",
            emoji = "🅰️",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = "Monday", startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });

        var response = await userB.Client.GetAsync("/habits");
        var habits = await response.Content.ReadFromJsonAsync<List<HabitDto>>();
        habits.Should().BeEmpty();
    }

    [Fact]
    public async Task UserA_ShouldNotCheckHabitOfUserB()
    {
        using var userA = await RegisterAndAuthenticateAsync("checka@example.com", timezone: "UTC");
        using var userB = await RegisterSecondUserAsync();

        var todayDow = DateTime.UtcNow.DayOfWeek.ToString();
        var createResponse = await userA.Client.PostAsJsonAsync("/habits", new
        {
            name = "Hábito Privado",
            emoji = "🔒",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = todayDow, startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });
        var habitId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await userB.Client.PutAsJsonAsync($"/habits/{habitId}/check", new { date = today });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserA_ShouldNotUpdateHabitOfUserB()
    {
        using var userA = await RegisterAndAuthenticateAsync("updatea@example.com");
        using var userB = await RegisterSecondUserAsync();

        var createResponse = await userA.Client.PostAsJsonAsync("/habits", new
        {
            name = "Hábito do A",
            emoji = "🅰️",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = "Monday", startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });
        var habitId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await userB.Client.PutAsJsonAsync($"/habits/{habitId}", new
        {
            name = "Hackeado",
            emoji = "💀",
            syncGoogleCalendar = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UserA_ShouldNotDeleteHabitOfUserB()
    {
        using var userA = await RegisterAndAuthenticateAsync("deletea@example.com");
        using var userB = await RegisterSecondUserAsync();

        var createResponse = await userA.Client.PostAsJsonAsync("/habits", new
        {
            name = "Não delete",
            emoji = "🛡️",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = "Monday", startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });
        var habitId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await userB.Client.DeleteAsync($"/habits/{habitId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

file record HabitDto(Guid Id, string Name);
