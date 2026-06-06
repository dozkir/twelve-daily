using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TwelveDaily.IntegrationTests.Auth;

public class RegisterTests : IntegrationTestBase
{
    [Fact]
    public async Task Register_WithValidData_ShouldReturn201AndTokens()
    {
        var response = await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "newuser@example.com",
            password = "Password123!",
            timezone = "America/Sao_Paulo"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.AccessTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
        body.RefreshTokenExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn409()
    {
        await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "duplicate@example.com",
            password = "Password123!",
            timezone = "America/Sao_Paulo"
        });

        var response = await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "duplicate@example.com",
            password = "Password123!",
            timezone = "America/Sao_Paulo"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturn400()
    {
        var response = await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "not-an-email",
            password = "Password123!",
            timezone = "America/Sao_Paulo"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithShortPassword_ShouldReturn400()
    {
        var response = await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "user@example.com",
            password = "short",
            timezone = "America/Sao_Paulo"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

