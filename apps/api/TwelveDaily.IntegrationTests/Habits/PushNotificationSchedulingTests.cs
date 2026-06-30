using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace TwelveDaily.IntegrationTests.Habits;

public class PushNotificationSchedulingTests : IntegrationTestBase
{
    [Fact]
    public async Task RepeatedRecomputes_ShouldKeepAtMostOneScheduledWake()
    {
        using var user = await RegisterAndAuthenticateAsync("wake@example.com");

        // Sem push token o recompute aborta antes de agendar — registramos um para exercitar o agendamento.
        var tokenResponse = await user.Client.PostAsJsonAsync("/users/push-token", new
        {
            token = "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
            deviceLabel = "test-device"
        });
        tokenResponse.EnsureSuccessStatusCode();

        // Hábito com schedule em todos os dias da semana: garante uma ocorrência futura
        // (amanhã, no fuso do usuário) independente da hora em que o teste roda → sempre há
        // uma fronteira futura para agendar um wake.
        var schedules = Enum.GetValues<DayOfWeek>()
            .Select(day => new { dayOfWeek = day.ToString(), startTime = "08:00", endTime = "09:00", isActive = true })
            .ToArray();

        var createResponse = await user.Client.PostAsJsonAsync("/habits", new
        {
            name = "Água",
            emoji = "💧",
            syncGoogleCalendar = false,
            schedules
        });
        createResponse.EnsureSuccessStatusCode();

        // Cada recompute (uma por mutação) deve cancelar o wake anterior antes de agendar o próximo.
        for (var i = 0; i < 10; i++)
        {
            var sync = await user.Client.PostAsync("/users/push-sync", null);
            sync.EnsureSuccessStatusCode();
        }

        var jobClient = Factory.Services.GetRequiredService<RecordingBackgroundJobClient>();

        // Foram muitos recomputes (create + 10 syncs), mas no fim deve sobrar exatamente UM wake.
        // Antes da correção, cada recompute deixava uma cadeia viva → vários wakes simultâneos.
        jobClient.Created.Count.Should().BeGreaterThan(1);
        jobClient.OutstandingCount.Should().Be(1);
    }
}
