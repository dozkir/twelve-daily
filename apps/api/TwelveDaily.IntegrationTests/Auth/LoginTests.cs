using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TwelveDaily.IntegrationTests.Auth;

public class LoginTests : IntegrationTestBase
{
    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200AndTokens()
    {
        await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "login@example.com",
            password = "Password123!",
            timezone = "America/Sao_Paulo"
        });

        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            email = "login@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        await Client.PostAsJsonAsync("/auth/register", new
        {
            email = "loginwrong@example.com",
            password = "Password123!",
            timezone = "America/Sao_Paulo"
        });

        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            email = "loginwrong@example.com",
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldReturn401()
    {
        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            email = "nobody@example.com",
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

