using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Domain.Interfaces;
using TwelveDaily.Infrastructure.Data;
using TwelveDaily.Infrastructure.Repositories;
using TwelveDaily.Infrastructure.Services;

namespace TwelveDaily.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        var pushNotificationsOptions = new PushNotificationsOptions
        {
            ExpoBaseUrl = configuration["PushNotifications:ExpoBaseUrl"] ?? "https://exp.host/--/",
            ExpoAccessToken = configuration["PushNotifications:ExpoAccessToken"],
            ActivationLeadMinutes = int.TryParse(configuration["PushNotifications:ActivationLeadMinutes"], out var activationLeadMinutes)
                ? activationLeadMinutes
                : 15,
            ActionTokenMaxLifetimeMinutes = int.TryParse(configuration["PushNotifications:ActionTokenMaxLifetimeMinutes"], out var actionTokenMaxLifetimeMinutes)
                ? actionTokenMaxLifetimeMinutes
                : 1440,
            ActionTokenSecret = configuration["PushNotifications:ActionTokenSecret"],
            ActionTokenIssuer = configuration["PushNotifications:ActionTokenIssuer"] ?? "twelve-daily-notifications",
            ActionTokenAudience = configuration["PushNotifications:ActionTokenAudience"] ?? "twelve-daily-clients"
        };

        // EF Core + PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton(Options.Create(pushNotificationsOptions));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHabitRepository, HabitRepository>();
        services.AddScoped<IHabitScheduleRepository, HabitScheduleRepository>();
        services.AddScoped<IHabitCheckRepository, HabitCheckRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPushTokenRepository, PushTokenRepository>();
        services.AddScoped<IGoogleConnectionRepository, GoogleConnectionRepository>();

        // Services
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IPushNotificationActionTokenService, PushNotificationActionTokenService>();
        services.AddScoped<IPushNotificationOrchestrator, PushNotificationOrchestrator>();
        services.AddScoped<PushNotificationJobRunner>();
        services.AddSingleton<IPushNotificationService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PushNotificationsOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ExpoPushNotificationService>>();
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.ExpoBaseUrl, UriKind.Absolute)
            };

            return new ExpoPushNotificationService(httpClient, Options.Create(options), logger);
        });

        return services;
    }
}

