using FluentAssertions;
using NSubstitute;
using TwelveDaily.Application.Habits.Commands;
using TwelveDaily.Application.Habits.Handlers;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.UnitTests.Handlers;

public class CreateHabitHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly IHabitScheduleRepository _scheduleRepository = Substitute.For<IHabitScheduleRepository>();
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator = Substitute.For<IPushNotificationOrchestrator>();
    private readonly CreateHabitHandler _handler;

    public CreateHabitHandlerTests()
    {
        _handler = new CreateHabitHandler(_habitRepository, _scheduleRepository, _pushNotificationOrchestrator);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateHabitAndSchedules()
    {
        var command = new CreateHabitCommand(
            Guid.NewGuid(), "Academia", "🏋️", null, false,
            [new CreateHabitScheduleDto(DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0), true)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _habitRepository.Received(1).AddAsync(Arg.Any<Habit>(), Arg.Any<CancellationToken>());
        await _scheduleRepository.Received(1).AddRangeAsync(Arg.Any<IEnumerable<HabitSchedule>>(), Arg.Any<CancellationToken>());
        await _pushNotificationOrchestrator.Received(1).RecomputeUserNotificationsAsync(command.UserId, Arg.Any<CancellationToken>());
    }
}

public class UpdateHabitHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator = Substitute.For<IPushNotificationOrchestrator>();
    private readonly UpdateHabitHandler _handler;

    public UpdateHabitHandlerTests()
    {
        _handler = new UpdateHabitHandler(_habitRepository, _pushNotificationOrchestrator);
    }

    [Fact]
    public async Task Handle_WithExistingHabit_ShouldUpdate()
    {
        var userId = Guid.NewGuid();
        var habit = new Habit(userId, "Old Name", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        var command = new UpdateHabitCommand(habit.Id, userId, "New Name", "🧘", "New desc", true);

        await _handler.Handle(command, CancellationToken.None);

        await _habitRepository.Received(1).UpdateAsync(Arg.Is<Habit>(h => h.Name == "New Name"), Arg.Any<CancellationToken>());
        await _pushNotificationOrchestrator.Received(1).RecomputeUserNotificationsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentHabit_ShouldThrow()
    {
        _habitRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Habit?)null);

        var command = new UpdateHabitCommand(Guid.NewGuid(), Guid.NewGuid(), "Name", "🏋️", null, false);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithDifferentUserId_ShouldThrow()
    {
        var habit = new Habit(Guid.NewGuid(), "Name", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        var command = new UpdateHabitCommand(habit.Id, Guid.NewGuid(), "New Name", "🧘", null, false);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class ToggleHabitHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator = Substitute.For<IPushNotificationOrchestrator>();
    private readonly ToggleHabitHandler _handler;

    public ToggleHabitHandlerTests()
    {
        _handler = new ToggleHabitHandler(_habitRepository, _pushNotificationOrchestrator);
    }

    [Fact]
    public async Task Handle_WithExistingHabit_ShouldToggle()
    {
        var userId = Guid.NewGuid();
        var habit = new Habit(userId, "Academia", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        await _handler.Handle(new ToggleHabitCommand(habit.Id, userId), CancellationToken.None);

        await _habitRepository.Received(1).UpdateAsync(Arg.Any<Habit>(), Arg.Any<CancellationToken>());
        await _pushNotificationOrchestrator.Received(1).RecomputeUserNotificationsAsync(userId, Arg.Any<CancellationToken>());
    }
}

public class DeleteHabitHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly IPushNotificationOrchestrator _pushNotificationOrchestrator = Substitute.For<IPushNotificationOrchestrator>();
    private readonly DeleteHabitHandler _handler;

    public DeleteHabitHandlerTests()
    {
        _handler = new DeleteHabitHandler(_habitRepository, _pushNotificationOrchestrator);
    }

    [Fact]
    public async Task Handle_WithExistingHabit_ShouldDelete()
    {
        var userId = Guid.NewGuid();
        var habit = new Habit(userId, "Academia", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        await _handler.Handle(new DeleteHabitCommand(habit.Id, userId), CancellationToken.None);

        await _habitRepository.Received(1).DeleteAsync(Arg.Any<Habit>(), Arg.Any<CancellationToken>());
        await _pushNotificationOrchestrator.Received(1).RecomputeUserNotificationsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDifferentUserId_ShouldThrow()
    {
        var habit = new Habit(Guid.NewGuid(), "Academia", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        var act = () => _handler.Handle(new DeleteHabitCommand(habit.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

