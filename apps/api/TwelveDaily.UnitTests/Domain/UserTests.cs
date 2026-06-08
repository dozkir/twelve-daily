using FluentAssertions;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateUser()
    {
        var user = new User("test@example.com", "hashedpassword", "America/Sao_Paulo");

        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be("test@example.com");
        user.PasswordHash.Should().Be("hashedpassword");
        user.Timezone.Should().Be("America/Sao_Paulo");
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidEmail_ShouldThrow(string? email)
    {
        var act = () => new User(email!, "hash", "America/Sao_Paulo");

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidPasswordHash_ShouldThrow(string? hash)
    {
        var act = () => new User("test@example.com", hash!, "America/Sao_Paulo");

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Invalid/Zone")]
    public void Constructor_WithInvalidTimezone_ShouldThrow(string? timezone)
    {
        var act = () => new User("test@example.com", "hash", timezone!);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithValidIanaTimezone_ShouldAccept()
    {
        var user = new User("test@example.com", "hash", "Europe/Lisbon");

        user.Timezone.Should().Be("Europe/Lisbon");
    }

    [Fact]
    public void UpdateTimezone_WithValidTimezone_ShouldUpdate()
    {
        var user = new User("test@example.com", "hash", "America/Sao_Paulo");

        user.UpdateTimezone("Europe/Lisbon");

        user.Timezone.Should().Be("Europe/Lisbon");
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Invalid/Zone")]
    public void UpdateTimezone_WithInvalidTimezone_ShouldThrow(string? timezone)
    {
        var user = new User("test@example.com", "hash", "America/Sao_Paulo");

        var act = () => user.UpdateTimezone(timezone!);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdatePassword_WithValidHash_ShouldUpdate()
    {
        var user = new User("test@example.com", "oldhash", "America/Sao_Paulo");

        user.UpdatePassword("newhash");

        user.PasswordHash.Should().Be("newhash");
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UpdatePassword_WithInvalidHash_ShouldThrow(string? hash)
    {
        var user = new User("test@example.com", "oldhash", "America/Sao_Paulo");

        var act = () => user.UpdatePassword(hash!);

        act.Should().Throw<DomainException>();
    }
}

