using FluentAssertions;
using NSubstitute;
using TwelveDaily.Application.Habits.Commands;
using TwelveDaily.Application.Habits.Handlers;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.UnitTests.Handlers;

public class UpdateHabitSchedulesHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly IHabitScheduleRepository _scheduleRepository = Substitute.For<IHabitScheduleRepository>();
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator = Substitute.For<IPushNotificationOrchestrator>();
    private readonly UpdateHabitSchedulesHandler _handler;

    public UpdateHabitSchedulesHandlerTests()
    {
        _handler = new UpdateHabitSchedulesHandler(_habitRepository, _scheduleRepository, _pushNotificationOrchestrator);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldReplaceSchedules()
    {
        var userId = Guid.NewGuid();
        var habit = new Habit(userId, "Academia", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        var command = new UpdateHabitSchedulesCommand(habit.Id, userId,
        [
            new CreateHabitScheduleDto(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0), true),
            new CreateHabitScheduleDto(DayOfWeek.Friday, new TimeOnly(9, 0), new TimeOnly(10, 0), true)
        ]);

        await _handler.Handle(command, CancellationToken.None);

        await _scheduleRepository.Received(1).DeleteByHabitIdAsync(habit.Id, Arg.Any<CancellationToken>());
        await _scheduleRepository.Received(1).AddRangeAsync(Arg.Any<IEnumerable<HabitSchedule>>(), Arg.Any<CancellationToken>());
        await _pushNotificationOrchestrator.Received(1).RecomputeUserNotificationsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentHabit_ShouldThrow()
    {
        _habitRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Habit?)null);

        var command = new UpdateHabitSchedulesCommand(Guid.NewGuid(), Guid.NewGuid(),
            [new CreateHabitScheduleDto(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0), true)]);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithDifferentUserId_ShouldThrow()
    {
        var habit = new Habit(Guid.NewGuid(), "Academia", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        var command = new UpdateHabitSchedulesCommand(habit.Id, Guid.NewGuid(),
            [new CreateHabitScheduleDto(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0), true)]);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class ToggleHabitScheduleHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly IHabitScheduleRepository _scheduleRepository = Substitute.For<IHabitScheduleRepository>();
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator = Substitute.For<IPushNotificationOrchestrator>();
    private readonly ToggleHabitScheduleHandler _handler;

    public ToggleHabitScheduleHandlerTests()
    {
        _handler = new ToggleHabitScheduleHandler(_habitRepository, _scheduleRepository, _pushNotificationOrchestrator);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldToggleSchedule()
    {
        var userId = Guid.NewGuid();
        var habit = new Habit(userId, "Academia", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        var schedule = new HabitSchedule(habit.Id, DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0));
        _scheduleRepository.GetByHabitIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns([schedule]);

        await _handler.Handle(new ToggleHabitScheduleCommand(habit.Id, userId, DayOfWeek.Monday), CancellationToken.None);

        await _scheduleRepository.Received(1).UpdateAsync(Arg.Any<HabitSchedule>(), Arg.Any<CancellationToken>());
        await _pushNotificationOrchestrator.Received(1).RecomputeUserNotificationsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentScheduleForDay_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        var habit = new Habit(userId, "Academia", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);
        _scheduleRepository.GetByHabitIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns([]);

        var act = () => _handler.Handle(new ToggleHabitScheduleCommand(habit.Id, userId, DayOfWeek.Monday), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

