using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TwelveDaily.IntegrationTests.Profile;

public class ProfileTests : IntegrationTestBase
{
    [Fact]
    public async Task GetProfile_ShouldReturnUserData()
    {
        using var user = await RegisterAndAuthenticateAsync("profile@example.com");

        var response = await user.Client.GetAsync("/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile!.Email.Should().Be("profile@example.com");
        profile.Timezone.Should().Be("America/Sao_Paulo");
    }

    [Fact]
    public async Task UpdateTimezone_ShouldChange()
    {
        using var user = await RegisterAndAuthenticateAsync("tz@example.com");

        var response = await user.Client.PutAsJsonAsync("/users/me/timezone", new
        {
            timezone = "Europe/Lisbon"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var profile = await user.Client.GetAsync("/users/me");
        var body = await profile.Content.ReadFromJsonAsync<ProfileDto>();
        body!.Timezone.Should().Be("Europe/Lisbon");
    }

    [Fact]
    public async Task UpdatePassword_WithCorrectCurrent_ShouldSucceed()
    {
        using var user = await RegisterAndAuthenticateAsync("passok@example.com");

        var response = await user.Client.PutAsJsonAsync("/users/me/password", new
        {
            currentPassword = "Password123!",
            newPassword = "NewPassword456!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify new password works
        var loginResponse = await Client.PostAsJsonAsync("/auth/login", new
        {
            email = "passok@example.com",
            password = "NewPassword456!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePassword_WithWrongCurrent_ShouldReturn400()
    {
        using var user = await RegisterAndAuthenticateAsync("passfail@example.com");

        var response = await user.Client.PutAsJsonAsync("/users/me/password", new
        {
            currentPassword = "WrongPassword!",
            newPassword = "NewPassword456!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProfile_WithoutAuth_ShouldReturn401()
    {
        var response = await Client.GetAsync("/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterPushToken_ShouldReturn204()
    {
        using var user = await RegisterAndAuthenticateAsync("push@example.com");

        var response = await user.Client.PostAsJsonAsync("/users/push-token", new
        {
            token = "ExponentPushToken[test-device]",
            deviceLabel = "Pixel 8 do Rafael"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SendRemotePushTest_ShouldReturn204()
    {
        using var user = await RegisterAndAuthenticateAsync("push-test@example.com");

        var response = await user.Client.PostAsync("/users/push-test", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SyncActivePushNotification_ShouldReturn204()
    {
        using var user = await RegisterAndAuthenticateAsync("push-sync@example.com");

        var response = await user.Client.PostAsync("/users/push-sync", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

file record ProfileDto(Guid Id, string Email, string Timezone, DateTime CreatedAt);

