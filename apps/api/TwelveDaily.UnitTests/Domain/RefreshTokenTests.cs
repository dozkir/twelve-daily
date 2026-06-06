using FluentAssertions;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.UnitTests.Domain;

public class RefreshTokenTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_ShouldCreateToken()
    {
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var token = new RefreshToken(_userId, "random_token_value", expiresAt);

        token.Id.Should().NotBeEmpty();
        token.UserId.Should().Be(_userId);
        token.Token.Should().Be("random_token_value");
        token.ExpiresAt.Should().Be(expiresAt);
        token.RevokedAt.Should().BeNull();
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_WithInvalidToken_ShouldThrow(string? tokenValue)
    {
        var act = () => new RefreshToken(_userId, tokenValue!, DateTime.UtcNow.AddDays(7));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ShouldThrow()
    {
        var act = () => new RefreshToken(Guid.Empty, "token", DateTime.UtcNow.AddDays(7));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsExpired_WhenNotExpired_ShouldReturnFalse()
    {
        var token = new RefreshToken(_userId, "token", DateTime.UtcNow.AddDays(7));

        token.IsExpired(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpired_ShouldReturnTrue()
    {
        var token = new RefreshToken(_userId, "token", DateTime.UtcNow.AddDays(7));

        token.IsExpired(DateTime.UtcNow.AddDays(8)).Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_WhenNotRevoked_ShouldReturnFalse()
    {
        var token = new RefreshToken(_userId, "token", DateTime.UtcNow.AddDays(7));

        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WhenRevoked_ShouldReturnTrue()
    {
        var token = new RefreshToken(_userId, "token", DateTime.UtcNow.AddDays(7));
        token.Revoke(DateTime.UtcNow);

        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenNotExpiredAndNotRevoked_ShouldReturnTrue()
    {
        var token = new RefreshToken(_userId, "token", DateTime.UtcNow.AddDays(7));

        token.IsActive(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenExpired_ShouldReturnFalse()
    {
        var token = new RefreshToken(_userId, "token", DateTime.UtcNow.AddDays(7));

        token.IsActive(DateTime.UtcNow.AddDays(8)).Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenRevoked_ShouldReturnFalse()
    {
        var token = new RefreshToken(_userId, "token", DateTime.UtcNow.AddDays(7));
        token.Revoke(DateTime.UtcNow);

        token.IsActive(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenNotRevoked_ShouldSetRevokedAt()
    {
        var token = new RefreshToken(_userId, "token", DateTime.UtcNow.AddDays(7));
        var now = DateTime.UtcNow;

        token.Revoke(now);

        token.RevokedAt.Should().Be(now);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldThrow()
    {
        var token = new RefreshToken(_userId, "token", DateTime.UtcNow.AddDays(7));
        token.Revoke(DateTime.UtcNow);

        var act = () => token.Revoke(DateTime.UtcNow);

        act.Should().Throw<DomainException>();
    }
}

