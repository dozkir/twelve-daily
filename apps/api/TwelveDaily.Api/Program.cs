using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TwelveDaily.Api.Swagger;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TwelveDaily.Api.Middleware;
using TwelveDaily.Application.Behaviors;
using TwelveDaily.Infrastructure;
using TwelveDaily.Infrastructure.Data;

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

    // Treat C# non-nullable reference types as required in the schema, so the
    // generated client gets tight types (e.g. string instead of string | null).
    options.SupportNonNullableReferenceTypes();
    options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
});
builder.Services.AddSignalR();

// Atrás da Cloudflare + Caddy: confiar nos cabeçalhos X-Forwarded-* para enxergar
// o esquema (https) e o IP real do cliente. KnownNetworks/Proxies são limpos porque
// o tráfego só chega pelo Caddy interno (a API não fica exposta diretamente).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// CORS: origens de produção vêm de Cors:AllowedOrigins (ex.: Cors__AllowedOrigins__0).
// Sem configuração, cai nas origens locais de desenvolvimento.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy
            .WithOrigins(corsOrigins is { Length: > 0 }
                ? corsOrigins
                : ["http://localhost:8081", "http://127.0.0.1:8081", "http://localhost:19006", "http://127.0.0.1:19006"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Infrastructure (EF Core, Repositories, Services)
builder.Services.AddInfrastructure(builder.Configuration);

// Health check: liveness + readiness do banco (usado pelo docker healthcheck e pós-deploy).
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

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
// Primeiro de tudo: aplicar os X-Forwarded-* antes de qualquer middleware que use
// esquema/IP (logging, auth, geração de links).
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DefaultCors");

// Apply pending EF Core migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandler>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint anônimo de saúde (não exige auth).
app.MapHealthChecks("/health");

// Dashboard do Hangfire:
// - Development: aberto (AllowAll) para conveniência local.
// - Produção: protegido por Basic Auth quando Hangfire:User/Password estão configurados;
//   sem credenciais, o dashboard simplesmente não é exposto (default seguro).
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new AllowAllDashboardAuthorizationFilter()]
    });
}
else
{
    var hangfireUser = app.Configuration["Hangfire:User"];
    var hangfirePassword = app.Configuration["Hangfire:Password"];
    if (!string.IsNullOrWhiteSpace(hangfireUser) && !string.IsNullOrWhiteSpace(hangfirePassword))
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new BasicAuthDashboardAuthorizationFilter(hangfireUser, hangfirePassword)]
        });
    }
}

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program;

internal sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}

/// <summary>Basic Auth para o dashboard do Hangfire em produção.</summary>
internal sealed class BasicAuthDashboardAuthorizationFilter(string user, string password) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        string? header = httpContext.Request.Headers.Authorization;

        if (header is not null && header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encoded = header["Basic ".Length..].Trim();
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var separatorIndex = decoded.IndexOf(':');
                if (separatorIndex > 0)
                {
                    var providedUser = decoded[..separatorIndex];
                    var providedPassword = decoded[(separatorIndex + 1)..];
                    if (providedUser == user && providedPassword == password)
                        return true;
                }
            }
            catch (FormatException)
            {
                // Cabeçalho Basic malformado → trata como não autorizado.
            }
        }

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire\"";
        return false;
    }
}

/// <summary>Health check que confirma conectividade com o banco.</summary>
internal sealed class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
        => await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Database unreachable");
}
