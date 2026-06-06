using System.Text.Json;
using FluentValidation;
using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.Api.Middleware;

public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ValidationException validationEx => (400, FormatValidationErrors(validationEx)),
            ConflictException ex => (409, ex.Message),
            UnauthorizedException ex => (401, ex.Message),
            ForbiddenException ex => (403, ex.Message),
            DomainException ex => (400, ex.Message),
            _ => (500, "An unexpected error occurred.")
        };

        if (statusCode == 500)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(response);
    }

    private static string FormatValidationErrors(ValidationException ex)
    {
        var errors = ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
        return string.Join("; ", errors);
    }
}

