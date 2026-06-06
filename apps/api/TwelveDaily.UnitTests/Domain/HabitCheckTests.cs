using FluentAssertions;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.UnitTests.Domain;

public class HabitCheckTests
{
    private readonly Guid _habitId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 4, 5);
    private static readonly DateOnly Today = new(2026, 4, 5);
    private static readonly DateTime CheckedAt = new(2026, 4, 5, 15, 0, 0, DateTimeKind.Utc);

    private HabitCheck CreateValid(DateOnly date, DateOnly today)
        => new(_habitId, _userId, date, today, CheckedAt, "Academia", "🏋️", new TimeOnly(7, 0), new TimeOnly(8, 0));

    [Fact]
    public void Constructor_WithValidData_ShouldCreateCheck()
    {
        var check = CreateValid(Date, Today);

        check.Id.Should().NotBeEmpty();
        check.HabitId.Should().Be(_habitId);
        check.UserId.Should().Be(_userId);
        check.Date.Should().Be(Date);
        check.CheckedAt.Should().Be(CheckedAt);
        check.HabitName.Should().Be("Academia");
        check.HabitEmoji.Should().Be("🏋️");
        check.StartTime.Should().Be(new TimeOnly(7, 0));
        check.EndTime.Should().Be(new TimeOnly(8, 0));
    }

    [Fact]
    public void Constructor_WithFutureDate_ShouldThrow()
    {
        var act = () => CreateValid(Today.AddDays(1), Today);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithPastDate_ShouldBeAllowed()
    {
        var act = () => CreateValid(Today.AddDays(-3), Today);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithEmptyHabitId_ShouldThrow()
    {
        var act = () => new HabitCheck(Guid.Empty, _userId, Date, Today, CheckedAt, "Academia", "🏋️", new TimeOnly(7, 0), new TimeOnly(8, 0));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrow()
    {
        var act = () => new HabitCheck(_habitId, _userId, Date, Today, CheckedAt, " ", "🏋️", new TimeOnly(7, 0), new TimeOnly(8, 0));

        act.Should().Throw<DomainException>();
    }
}
