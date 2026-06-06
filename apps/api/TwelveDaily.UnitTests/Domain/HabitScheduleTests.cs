using FluentAssertions;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.UnitTests.Domain;

public class HabitScheduleTests
{
    private readonly Guid _habitId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_ShouldCreateSchedule()
    {
        var start = new TimeOnly(7, 0);
        var end = new TimeOnly(8, 0);

        var schedule = new HabitSchedule(_habitId, DayOfWeek.Monday, start, end);

        schedule.Id.Should().NotBeEmpty();
        schedule.HabitId.Should().Be(_habitId);
        schedule.DayOfWeek.Should().Be(DayOfWeek.Monday);
        schedule.StartTime.Should().Be(start);
        schedule.EndTime.Should().Be(end);
        schedule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEndTimeBeforeStartTime_ShouldThrow()
    {
        var start = new TimeOnly(8, 0);
        var end = new TimeOnly(7, 0);

        var act = () => new HabitSchedule(_habitId, DayOfWeek.Monday, start, end);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithEqualStartAndEndTime_ShouldThrow()
    {
        var time = new TimeOnly(7, 0);

        var act = () => new HabitSchedule(_habitId, DayOfWeek.Monday, time, time);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithEmptyHabitId_ShouldThrow()
    {
        var act = () => new HabitSchedule(Guid.Empty, DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateTime_WithValidTimes_ShouldUpdate()
    {
        var schedule = new HabitSchedule(_habitId, DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0));
        var newStart = new TimeOnly(9, 0);
        var newEnd = new TimeOnly(10, 0);

        schedule.UpdateTime(newStart, newEnd);

        schedule.StartTime.Should().Be(newStart);
        schedule.EndTime.Should().Be(newEnd);
    }

    [Fact]
    public void UpdateTime_WithEndBeforeStart_ShouldThrow()
    {
        var schedule = new HabitSchedule(_habitId, DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0));

        var act = () => schedule.UpdateTime(new TimeOnly(10, 0), new TimeOnly(9, 0));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ToggleActive_WhenActive_ShouldDeactivate()
    {
        var schedule = new HabitSchedule(_habitId, DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0));

        schedule.ToggleActive();

        schedule.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ToggleActive_WhenInactive_ShouldActivate()
    {
        var schedule = new HabitSchedule(_habitId, DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0));
        schedule.ToggleActive();

        schedule.ToggleActive();

        schedule.IsActive.Should().BeTrue();
    }
}

