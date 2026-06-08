using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TwelveDaily.IntegrationTests.Dashboard;

public class DashboardTests : IntegrationTestBase
{
    [Fact]
    public async Task Dashboard_WithCheckedHabit_ShouldReturnMetrics()
    {
        using var user = await RegisterAndAuthenticateAsync("dashboard@example.com", timezone: "UTC");

        var todayDow = DateTime.UtcNow.DayOfWeek.ToString();
        var createResponse = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Dash Test",
            emoji = "📊",
            syncGoogleCalendar = false,
            schedules = new[]
            {
                new { dayOfWeek = todayDow, startTime = "07:00", endTime = "08:00", isActive = true }
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var habitId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await user.Client.PutAsJsonAsync($"/habits/{habitId}/check", new { date = today.ToString("yyyy-MM-dd") });

        var response = await user.Client.GetAsync($"/dashboard/weekly?weekStart={today:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardDto>();
        dashboard!.Total.Should().BeGreaterThanOrEqualTo(1);
        dashboard.Completed.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Dashboard_WithNoHabits_ShouldReturnZeros()
    {
        using var user = await RegisterAndAuthenticateAsync("dashempty@example.com", timezone: "UTC");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await user.Client.GetAsync($"/dashboard/weekly?weekStart={today:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardDto>();
        dashboard!.Total.Should().Be(0);
        dashboard.Completed.Should().Be(0);
        dashboard.CompletionRate.Should().Be(0);
    }
}

file record DashboardDto(int Total, int Completed, double CompletionRate);
