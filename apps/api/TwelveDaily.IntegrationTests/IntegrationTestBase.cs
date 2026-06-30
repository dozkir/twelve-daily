using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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

                    // Hangfire keeps static state (GlobalConfiguration / JobStorage and the
                    // ILoggerFactory it captures). xUnit runs test classes in parallel, each
                    // with its own WebApplicationFactory<Program>; when a sibling host is
                    // disposed it disposes a LoggerFactory that Hangfire still references, so
                    // constructing the real IBackgroundJobClient throws ObjectDisposedException.
                    // We don't run the background server, but we do assert on scheduling, so swap
                    // the client for a recording fake (registered by its concrete type too, so
                    // tests can resolve it and inspect created/deleted jobs) and drop the server.
                    services.RemoveAll<IBackgroundJobClient>();
                    services.AddSingleton<RecordingBackgroundJobClient>();
                    services.AddSingleton<IBackgroundJobClient>(sp =>
                        sp.GetRequiredService<RecordingBackgroundJobClient>());

                    foreach (var hosted in services
                        .Where(d => d.ServiceType == typeof(IHostedService)
                            && d.ImplementationType?.Namespace?.StartsWith("Hangfire", StringComparison.Ordinal) == true)
                        .ToList())
                    {
                        services.Remove(hosted);
                    }
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

/// <summary>
/// Recording fake for IBackgroundJobClient used in tests, so handlers can depend on it without
/// building Hangfire's real client (which touches process-wide static state shared across the
/// parallel test hosts). No job is ever executed — there is no background server — but every
/// Create returns a unique id and every Delete is captured, so a test can assert how many wakes
/// are still outstanding (Created minus Deleted).
/// </summary>
public sealed class RecordingBackgroundJobClient : IBackgroundJobClient
{
    private int _counter;
    private readonly ConcurrentBag<string> _created = new();
    private readonly ConcurrentBag<string> _deleted = new();

    public IReadOnlyCollection<string> Created => _created;
    public IReadOnlyCollection<string> Deleted => _deleted;

    /// <summary>Wakes ainda agendados: criados menos cancelados.</summary>
    public int OutstandingCount => _created.Count - _deleted.Count;

    public string Create(Job job, IState state)
    {
        var jobId = Interlocked.Increment(ref _counter).ToString();
        _created.Add(jobId);
        return jobId;
    }

    public bool ChangeState(string jobId, IState state, string expectedState)
    {
        // BackgroundJobClientExtensions.Delete(jobId) chega aqui com um DeletedState.
        if (state is DeletedState)
            _deleted.Add(jobId);
        return true;
    }
}
