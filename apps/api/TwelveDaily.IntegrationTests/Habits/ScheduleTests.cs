using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TwelveDaily.IntegrationTests.Habits;

public class ScheduleTests : IntegrationTestBase
{
    [Fact]
    public async Task UpdateSchedules_ShouldReplaceAllSchedules()
    {
        using var user = await RegisterAndAuthenticateAsync("schedules@example.com");

        var createResponse = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Academia",
            emoji = "🏋️",
            syncGoogleCalendar = false,
            startToday = false,
            schedules = new[]
            {
                new { dayOfWeek = "Monday", startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });
        var habitId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var updateResponse = await user.Client.PutAsJsonAsync($"/habits/{habitId}/schedules", new
        {
            schedules = new[]
            {
                new { dayOfWeek = "Tuesday", startTime = "09:00", endTime = "10:00", isActive = true },
                new { dayOfWeek = "Thursday", startTime = "09:00", endTime = "10:00", isActive = true }
            }
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await user.Client.GetAsync($"/habits/{habitId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<HabitWithSchedules>();
        detail!.Schedules.Should().HaveCount(2);
    }

    [Fact]
    public async Task ToggleSchedule_ShouldAlternateIsActive()
    {
        using var user = await RegisterAndAuthenticateAsync("toggleschedule@example.com");

        var createResponse = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Leitura",
            emoji = "📖",
            syncGoogleCalendar = false,
            startToday = false,
            schedules = new[]
            {
                new { dayOfWeek = "Monday", startTime = "20:00", endTime = "21:00", isActive = true }
            }
        });
        var habitId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var toggleResponse = await user.Client.PatchAsync($"/habits/{habitId}/schedules/Monday/toggle", null);

        toggleResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

file record ScheduleItem(string DayOfWeek, string StartTime, string EndTime, bool IsActive);
file record HabitWithSchedules(Guid Id, string Name, List<ScheduleItem> Schedules);

