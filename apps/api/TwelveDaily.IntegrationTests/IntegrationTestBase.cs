using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TwelveDaily.Infrastructure.Data;

namespace TwelveDaily.IntegrationTests;

public class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("twelvedaily_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected WebApplicationFactory<Program> Factory = null!;
    protected HttpClient Client = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = _dbContainer.GetConnectionString(),
                        ["Jwt:Secret"] = "SuperSecretKeyForTestingThatIsAtLeast32CharactersLong!!",
                        ["Jwt:Issuer"] = "twelve-daily",
                        ["Jwt:Audience"] = "twelve-daily-clients",
                        ["Jwt:ExpiryMinutes"] = "15"
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(_dbContainer.GetConnectionString()));
                });
            });

        // Ensure database schema is created
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Registers a new user and returns an authenticated HttpClient.
    /// </summary>
    protected async Task<AuthenticatedUser> RegisterAndAuthenticateAsync(
        string email = "test@example.com",
        string password = "Password123!",
        string timezone = "America/Sao_Paulo")
    {
        var response = await Client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password,
            timezone
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        var authenticatedClient = Factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.AccessToken);

        return new AuthenticatedUser(authenticatedClient, result);
    }

    /// <summary>
    /// Creates a second authenticated user for isolation tests.
    /// </summary>
    protected Task<AuthenticatedUser> RegisterSecondUserAsync()
    {
        return RegisterAndAuthenticateAsync(
            email: "other@example.com",
            password: "Password123!",
            timezone: "Europe/Lisbon");
    }
}

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public record AuthenticatedUser(HttpClient Client, AuthResponse Auth) : IDisposable
{
    public void Dispose() => Client.Dispose();
}
