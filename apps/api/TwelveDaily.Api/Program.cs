using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TwelveDaily.Api.Middleware;
using TwelveDaily.Application.Behaviors;
using TwelveDaily.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Stable operationIds (Controller_Action) so the generated orval client
    // produces clean, collision-free hook names (e.g. useHabitsList).
    options.CustomOperationIds(apiDescription =>
        apiDescription.ActionDescriptor is ControllerActionDescriptor descriptor
            ? $"{descriptor.ControllerName}_{descriptor.ActionName}"
            : null);
});
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevClientCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:8081",
                "http://127.0.0.1:8081",
                "http://localhost:19006",
                "http://127.0.0.1:19006")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Infrastructure (EF Core, Repositories, Services)
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(
        options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default")),
        new PostgreSqlStorageOptions
        {
            PrepareSchemaIfNecessary = true
        }));
builder.Services.AddHangfireServer();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(TwelveDaily.Application.Auth.Commands.RegisterCommand).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(TwelveDaily.Application.Auth.Commands.RegisterCommand).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
            return;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Exception handler middleware
builder.Services.AddTransient<GlobalExceptionHandler>();
builder.Services.AddTransient<RequestResponseLoggingMiddleware>();

var app = builder.Build();

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DevClientCors");

// Apply pending EF Core migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TwelveDaily.Infrastructure.Data.AppDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandler>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
    });
}

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program;

internal sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}

