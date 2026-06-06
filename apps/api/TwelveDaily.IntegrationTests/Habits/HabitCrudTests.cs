using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TwelveDaily.IntegrationTests.Habits;

public class HabitCrudTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateHabit_WithValidData_ShouldReturn201()
    {
        using var user = await RegisterAndAuthenticateAsync("habits@example.com");

        var response = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Academia",
            emoji = "🏋️",
            description = "Treino de musculação",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = "Monday", startTime = "07:00", endTime = "08:00", isActive = true },
                new { dayOfWeek = "Wednesday", startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateHabit_ScheduledToday_ShouldAppearInTodayTimeline()
    {
        using var user = await RegisterAndAuthenticateAsync("scheduledtoday@example.com", timezone: "UTC");
        var todayDow = DateTime.UtcNow.DayOfWeek.ToString();

        var response = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Yoga",
            emoji = "🧘",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = todayDow, startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Sem geração de instância: o hábito agendado já aparece em "hoje".
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var dailyResponse = await user.Client.GetAsync($"/habits/daily?date={today}");
        dailyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var daily = await dailyResponse.Content.ReadFromJsonAsync<DailyDto>();
        daily!.Days.First(d => d.Type == "today").Items.Should().Contain(i => i.Name == "Yoga");
    }

    [Fact]
    public async Task ListHabits_ShouldReturnUserHabits()
    {
        using var user = await RegisterAndAuthenticateAsync("list@example.com");

        await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Academia",
            emoji = "🏋️",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = "Monday", startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });

        var response = await user.Client.GetAsync("/habits");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var habits = await response.Content.ReadFromJsonAsync<List<HabitListItem>>();
        habits.Should().ContainSingle(h => h.Name == "Academia");
    }

    [Fact]
    public async Task UpdateHabit_ShouldChangeFields()
    {
        using var user = await RegisterAndAuthenticateAsync("update@example.com");

        var createResponse = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Academia",
            emoji = "🏋️",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = "Monday", startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });
        var habitId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateResponse = await user.Client.PutAsJsonAsync($"/habits/{habitId}", new
        {
            name = "Yoga",
            emoji = "🧘",
            description = "Yoga matinal",
            syncGoogleCalendar = true
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteHabit_ShouldRemoveHabit()
    {
        using var user = await RegisterAndAuthenticateAsync("delete@example.com");

        var createResponse = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Temporário",
            emoji = "🗑️",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = "Monday", startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });
        var habitId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var deleteResponse = await user.Client.DeleteAsync($"/habits/{habitId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await user.Client.GetAsync("/habits");
        var habits = await listResponse.Content.ReadFromJsonAsync<List<HabitListItem>>();
        habits.Should().BeEmpty();
    }

    [Fact]
    public async Task ToggleHabit_ShouldAlternateIsActive()
    {
        using var user = await RegisterAndAuthenticateAsync("toggle@example.com");

        var createResponse = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Toggle Test",
            emoji = "🔄",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = "Monday", startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });
        var habitId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var toggleResponse = await user.Client.PatchAsync($"/habits/{habitId}/toggle", null);

        toggleResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await user.Client.GetAsync($"/habits/{habitId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<HabitDetail>();
        detail!.IsActive.Should().BeFalse();
    }
}

// DTOs for deserialization
file record HabitListItem(Guid Id, string Name, string Emoji, string? Description, bool IsActive, bool SyncGoogleCalendar);
file record HabitDetail(Guid Id, string Name, string Emoji, string? Description, bool IsActive, bool SyncGoogleCalendar);
file record DailyDto(List<DayDto> Days);
file record DayDto(string Date, string Type, List<ItemDto> Items);
file record ItemDto(Guid HabitId, string Name);
