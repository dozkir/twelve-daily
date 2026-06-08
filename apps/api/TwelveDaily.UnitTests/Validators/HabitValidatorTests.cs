using FluentAssertions;
using TwelveDaily.Application.Habits.Validators;
using TwelveDaily.Application.Habits.Commands;

namespace TwelveDaily.UnitTests.Validators;

public class CreateHabitCommandValidatorTests
{
    private readonly CreateHabitCommandValidator _validator = new();

    private static CreateHabitCommand ValidCommand() => new(
        UserId: Guid.NewGuid(),
        Name: "Academia",
        Emoji: "🏋️",
        Description: null,
        SyncGoogleCalendar: false,
        Schedules:
        [
            new CreateHabitScheduleDto(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0), true)
        ]);

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithInvalidName_ShouldFail(string? name)
    {
        var command = ValidCommand() with { Name = name! };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithInvalidEmoji_ShouldFail(string? emoji)
    {
        var command = ValidCommand() with { Emoji = emoji! };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Emoji");
    }

    [Fact]
    public void Validate_WithEmptySchedules_ShouldFail()
    {
        var command = ValidCommand() with { Schedules = [] };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Schedules");
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldFail()
    {
        var command = ValidCommand() with { UserId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithDuplicateDayOfWeek_ShouldFail()
    {
        var command = ValidCommand() with
        {
            Schedules =
            [
                new CreateHabitScheduleDto(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0), true),
                new CreateHabitScheduleDto(DayOfWeek.Monday, new TimeOnly(18, 0), new TimeOnly(19, 0), true)
            ]
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Schedules");
    }
}

public class CreateHabitScheduleDtoValidatorTests
{
    private readonly CreateHabitScheduleDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var dto = new CreateHabitScheduleDto(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0), true);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEndTimeBeforeStartTime_ShouldFail()
    {
        var dto = new CreateHabitScheduleDto(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(7, 0), true);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EndTime");
    }

    [Fact]
    public void Validate_WithEqualStartAndEndTime_ShouldFail()
    {
        var dto = new CreateHabitScheduleDto(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(7, 0), true);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }
}

public class UpdateHabitCommandValidatorTests
{
    private readonly UpdateHabitCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var command = new UpdateHabitCommand(Guid.NewGuid(), Guid.NewGuid(), "Yoga", "🧘", null, false);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithInvalidName_ShouldFail(string? name)
    {
        var command = new UpdateHabitCommand(Guid.NewGuid(), Guid.NewGuid(), name!, "🧘", null, false);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithInvalidEmoji_ShouldFail(string? emoji)
    {
        var command = new UpdateHabitCommand(Guid.NewGuid(), Guid.NewGuid(), "Yoga", emoji!, null, false);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Emoji");
    }
}

public class CheckHabitCommandValidatorTests
{
    private readonly CheckHabitCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var command = new CheckHabitCommand(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyHabitId_ShouldFail()
    {
        var command = new CheckHabitCommand(Guid.NewGuid(), Guid.Empty, DateOnly.FromDateTime(DateTime.UtcNow));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldFail()
    {
        var command = new CheckHabitCommand(Guid.Empty, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}

public class UncheckHabitCommandValidatorTests
{
    private readonly UncheckHabitCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var command = new UncheckHabitCommand(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyHabitId_ShouldFail()
    {
        var command = new UncheckHabitCommand(Guid.NewGuid(), Guid.Empty, DateOnly.FromDateTime(DateTime.UtcNow));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}

public class CheckHabitFromNotificationCommandValidatorTests
{
    private readonly CheckHabitFromNotificationCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var command = new CheckHabitFromNotificationCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), "token");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyHabitId_ShouldFail()
    {
        var command = new CheckHabitFromNotificationCommand(Guid.Empty, DateOnly.FromDateTime(DateTime.UtcNow), "token");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyActionToken_ShouldFail()
    {
        var command = new CheckHabitFromNotificationCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}

