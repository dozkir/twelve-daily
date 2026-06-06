using FluentAssertions;
using TwelveDaily.Application.Auth.Validators;
using TwelveDaily.Application.Auth.Commands;

namespace TwelveDaily.UnitTests.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var command = new RegisterCommand("test@example.com", "Password123!", "America/Sao_Paulo");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("@missing-local.com")]
    public void Validate_WithInvalidEmail_ShouldFail(string? email)
    {
        var command = new RegisterCommand(email!, "Password123!", "America/Sao_Paulo");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    public void Validate_WithInvalidPassword_ShouldFail(string? password)
    {
        var command = new RegisterCommand("test@example.com", password!, "America/Sao_Paulo");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithInvalidTimezone_ShouldFail(string? timezone)
    {
        var command = new RegisterCommand("test@example.com", "Password123!", timezone!);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Timezone");
    }
}

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var command = new LoginCommand("test@example.com", "Password123!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithEmptyEmail_ShouldFail(string? email)
    {
        var command = new LoginCommand(email!, "Password123!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithEmptyPassword_ShouldFail(string? password)
    {
        var command = new LoginCommand("test@example.com", password!);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}

