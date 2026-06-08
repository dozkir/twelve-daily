using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TwelveDaily.IntegrationTests.Habits;

public class TimelineTests : IntegrationTestBase
{
    [Fact]
    public async Task GetDaily_ShouldReturn7Days()
    {
        using var user = await RegisterAndAuthenticateAsync("timeline@example.com", timezone: "UTC");
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await user.Client.GetAsync($"/habits/daily?date={today}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DailyResponse>();
        body!.Days.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetDaily_TodayShouldHaveTypeToday()
    {
        using var user = await RegisterAndAuthenticateAsync("todaytype@example.com", timezone: "UTC");
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await user.Client.GetAsync($"/habits/daily?date={today}");

        var body = await response.Content.ReadFromJsonAsync<DailyResponse>();
        body!.Days.Should().Contain(d => d.Type == "today" && d.Date == today);
    }

    [Fact]
    public async Task GetDaily_PastDaysForNewHabit_ShouldHaveEmptyItems()
    {
        // Hábito criado agora não aparece em dias passados (CreatedAt > data) e nem tem check.
        using var user = await RegisterAndAuthenticateAsync("pastempty@example.com", timezone: "UTC");
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await user.Client.GetAsync($"/habits/daily?date={today}");

        var body = await response.Content.ReadFromJsonAsync<DailyResponse>();
        body!.Days.Where(d => d.Type == "past").Should().OnlyContain(d => d.Items.Count == 0);
    }

    [Fact]
    public async Task GetDaily_FutureDays_ShouldShowScheduledHabitWithoutCheck()
    {
        using var user = await RegisterAndAuthenticateAsync("futureschedule@example.com", timezone: "UTC");

        var tomorrow = DateTime.UtcNow.AddDays(1).DayOfWeek.ToString();
        await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Futuro",
            emoji = "🔮",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = tomorrow, startTime = "10:00", endTime = "11:00", isActive = true }
            }
        });

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await user.Client.GetAsync($"/habits/daily?date={today}");

        var body = await response.Content.ReadFromJsonAsync<DailyResponse>();
        body!.Days.Where(d => d.Type == "future").SelectMany(d => d.Items)
            .Where(i => i.Name == "Futuro")
            .Should().OnlyContain(i => i.CheckedAt == null);
    }

    [Fact]
    public async Task GetDaily_TodayScheduledHabit_ShouldShowScheduleLocalTimes()
    {
        using var user = await RegisterAndAuthenticateAsync("timeline-timezone@example.com", timezone: "UTC");

        var todayDow = DateTime.UtcNow.DayOfWeek.ToString();
        var createResponse = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Almoço local",
            emoji = "🍽️",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = todayDow, startTime = "13:00", endTime = "14:00", isActive = true }
            }
        });
        createResponse.EnsureSuccessStatusCode();

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await user.Client.GetAsync($"/habits/daily?date={today}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DailyResponse>();
        var item = body!.Days.First(d => d.Type == "today").Items.First(i => i.Name == "Almoço local");
        item.StartTime.Should().StartWith("13:00");
        item.EndTime.Should().StartWith("14:00");
    }

    [Fact]
    public async Task GetDaily_TodayScheduledHabit_ShouldAppearWithoutGeneration()
    {
        using var user = await RegisterAndAuthenticateAsync("timeline-auto@example.com", timezone: "UTC");

        var todayDow = DateTime.UtcNow.DayOfWeek.ToString();
        var createResponse = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Aparece sozinho",
            emoji = "🔄",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = todayDow, startTime = "09:00", endTime = "10:00", isActive = true }
            }
        });
        createResponse.EnsureSuccessStatusCode();

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await user.Client.GetAsync($"/habits/daily?date={today}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DailyResponse>();
        var item = body!.Days.First(d => d.Type == "today").Items.First(i => i.Name == "Aparece sozinho");
        item.HabitId.Should().NotBeEmpty();
        item.CheckedAt.Should().BeNull();
    }
}

file record DailyResponse(List<DayDto> Days);
file record DayDto(string Date, string Type, List<DayItemDto> Items);
file record DayItemDto(Guid HabitId, string Name, string Emoji, string? StartTime = null, string? EndTime = null, DateTime? CheckedAt = null);
