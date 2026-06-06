using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Application.Notifications;
using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.Infrastructure.Services;

public class PushNotificationActionTokenService : IPushNotificationActionTokenService
{
    private readonly PushNotificationsOptions _options;
    private readonly string _secret;

    public PushNotificationActionTokenService(IOptions<PushNotificationsOptions> options, IConfiguration configuration)
    {
        _options = options.Value;
        _secret = string.IsNullOrWhiteSpace(_options.ActionTokenSecret)
            ? configuration["Jwt:Secret"] ?? string.Empty
            : _options.ActionTokenSecret;
    }

    public string GenerateToken(Guid userId, Guid habitId, DateOnly date, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(_secret))
            throw new DomainException("Push notification action token secret is not configured.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("habitId", habitId.ToString()),
            new Claim("date", date.ToString("yyyy-MM-dd")),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.ActionTokenIssuer,
            audience: _options.ActionTokenAudience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public PushNotificationActionTokenPayload Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(_secret))
            throw new DomainException("Push notification action token secret is not configured.");

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _options.ActionTokenIssuer,
                ValidAudience = _options.ActionTokenAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new DomainException("Notification action token is missing user information."));
            var habitId = Guid.Parse(principal.FindFirst("habitId")?.Value
                ?? throw new DomainException("Notification action token is missing habit information."));
            var date = DateOnly.ParseExact(principal.FindFirst("date")?.Value
                ?? throw new DomainException("Notification action token is missing date information."), "yyyy-MM-dd");
            var expiresAtUtc = ((JwtSecurityToken)validatedToken).ValidTo;

            return new PushNotificationActionTokenPayload(userId, habitId, date, expiresAtUtc);
        }
        catch (SecurityTokenException)
        {
            throw new DomainException("Invalid or expired notification action token.");
        }
        catch (ArgumentException)
        {
            throw new DomainException("Invalid notification action token payload.");
        }
    }
}


