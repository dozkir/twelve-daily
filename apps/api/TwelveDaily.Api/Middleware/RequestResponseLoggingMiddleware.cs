using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TwelveDaily.Api.Middleware;

public class RequestResponseLoggingMiddleware : IMiddleware
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "currentPassword",
        "newPassword",
        "token",
        "accessToken",
        "refreshToken",
        "authorization",
        "cookie"
    };

    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var requestBody = await ReadRequestBodyAsync(context.Request);
        var originalResponseBody = context.Response.Body;

        await using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await next(context);

            var responseBody = await ReadResponseBodyAsync(context.Response);
            LogExchange(context, requestBody, responseBody);
        }
        finally
        {
            responseBodyStream.Position = 0;
            await responseBodyStream.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;
        }
    }

    private void LogExchange(HttpContext context, string requestBody, string responseBody)
    {
        _logger.LogInformation(
            "HTTP {Method} {Path}{QueryString}\nRequest JSON: {RequestBody}\nResponse {StatusCode} JSON: {ResponseBody}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty,
            requestBody,
            context.Response.StatusCode,
            responseBody);
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!CanReadBody(request.ContentType, request.ContentLength))
            return "<no json body>";

        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return SanitizeBody(body);
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        if (!CanReadBody(response.ContentType, response.Body.Length))
            return "<no json body>";

        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        response.Body.Position = 0;

        return SanitizeBody(body);
    }

    private static bool CanReadBody(string? contentType, long? contentLength)
    {
        if (contentLength is 0)
            return false;

        if (string.IsNullOrWhiteSpace(contentType))
            return true;

        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("text/json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "<empty>";

        try
        {
            var node = JsonNode.Parse(body);
            if (node == null)
                return "<empty>";

            MaskNode(node);
            return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }
        catch (JsonException)
        {
            return body.Length > 4000 ? $"{body[..4000]}... <truncated>" : body;
        }
    }

    private static void MaskNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (property.Value == null)
                    continue;

                if (SensitiveKeys.Contains(property.Key))
                {
                    obj[property.Key] = "***";
                    continue;
                }

                MaskNode(property.Value);
            }

            return;
        }

        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item != null)
                    MaskNode(item);
            }
        }
    }
}


