using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TwelveDaily.IntegrationTests.Auth;

public class TokenTests : IntegrationTestBase
{
    [Fact]
    public async Task Refresh_WithValidToken_ShouldReturnNewTokens()
    {
        using var user = await RegisterAndAuthenticateAsync("refresh@example.com");

        var response = await Client.PostAsJsonAsync("/auth/refresh", new
        {
            refreshToken = user.Auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBe(user.Auth.AccessToken);
        body.RefreshToken.Should().NotBe(user.Auth.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ShouldReturn401()
    {
        using var user = await RegisterAndAuthenticateAsync("revoked@example.com");

        // Logout revokes the token
        await user.Client.PostAsJsonAsync("/auth/logout", new
        {
            refreshToken = user.Auth.RefreshToken
        });

        // Try to use revoked token
        var response = await Client.PostAsJsonAsync("/auth/refresh", new
        {
            refreshToken = user.Auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithNonExistentToken_ShouldReturn401()
    {
        var response = await Client.PostAsJsonAsync("/auth/refresh", new
        {
            refreshToken = "nonexistent_token_value"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken()
    {
        using var user = await RegisterAndAuthenticateAsync("logout@example.com");

        var response = await user.Client.PostAsJsonAsync("/auth/logout", new
        {
            refreshToken = user.Auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LogoutAll_ShouldRevokeAllTokens()
    {
        using var user = await RegisterAndAuthenticateAsync("logoutall@example.com");

        // Login a second time to create another refresh token
        await Client.PostAsJsonAsync("/auth/login", new
        {
            email = "logoutall@example.com",
            password = "Password123!"
        });

        var response = await user.Client.PostAsync("/auth/logout-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Both refresh tokens should now be revoked
        var refreshResponse = await Client.PostAsJsonAsync("/auth/refresh", new
        {
            refreshToken = user.Auth.RefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

