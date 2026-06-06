using FluentAssertions;
using MediatR;
using NSubstitute;
using TwelveDaily.Application.Habits.Commands;
using TwelveDaily.Application.Habits.Handlers;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Application.Notifications;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.UnitTests.Handlers;

public class CheckHabitHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly IHabitScheduleRepository _scheduleRepository = Substitute.For<IHabitScheduleRepository>();
    private readonly IHabitCheckRepository _checkRepository = Substitute.For<IHabitCheckRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly IPushNotificationOrchestrator _orchestrator = Substitute.For<IPushNotificationOrchestrator>();
    private readonly CheckHabitHandler _handler;

    private static readonly DateOnly Date = new(2026, 4, 5); // Sunday

    public CheckHabitHandlerTests()
    {
        _dateTime.UtcNow.Returns(new DateTime(2026, 4, 5, 15, 0, 0, DateTimeKind.Utc));
        _handler = new CheckHabitHandler(
            _habitRepository, _scheduleRepository, _checkRepository, _userRepository, _dateTime, _orchestrator);
    }

    private (Guid userId, Habit habit) ArrangeHabitWithSchedule()
    {
        var userId = Guid.NewGuid();
        var habit = new Habit(userId, "Academia", "🏋️", null, false);
        var schedule = new HabitSchedule(habit.Id, Date.DayOfWeek, new TimeOnly(7, 0), new TimeOnly(8, 0));

        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);
        _scheduleRepository.GetByHabitIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns([schedule]);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new User("test@example.com", "hash", "America/Sao_Paulo"));
        _checkRepository.GetByHabitAndDateAsync(habit.Id, Date, Arg.Any<CancellationToken>()).Returns((HabitCheck?)null);

        return (userId, habit);
    }

    [Fact]
    public async Task Handle_WithValidHabit_ShouldCreateCheckAndRecompute()
    {
        var (userId, habit) = ArrangeHabitWithSchedule();

        var result = await _handler.Handle(new CheckHabitCommand(userId, habit.Id, Date), CancellationToken.None);

        result.HabitId.Should().Be(habit.Id);
        result.Date.Should().Be(Date);
        await _checkRepository.Received(1).AddAsync(
            Arg.Is<HabitCheck>(c => c.HabitId == habit.Id && c.Date == Date && c.HabitName == "Academia"),
            Arg.Any<CancellationToken>());
        await _orchestrator.Received(1).RecomputeUserNotificationsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyChecked_ShouldStayIdempotent()
    {
        var (userId, habit) = ArrangeHabitWithSchedule();
        var existing = new HabitCheck(
            habit.Id, userId, Date, Date,
            new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc),
            "Academia", "🏋️", new TimeOnly(7, 0), new TimeOnly(8, 0));
        _checkRepository.GetByHabitAndDateAsync(habit.Id, Date, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(new CheckHabitCommand(userId, habit.Id, Date), CancellationToken.None);

        result.CheckedAt.Should().Be(existing.CheckedAt);
        await _checkRepository.DidNotReceive().AddAsync(Arg.Any<HabitCheck>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentHabit_ShouldThrow()
    {
        _habitRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Habit?)null);

        var act = () => _handler.Handle(new CheckHabitCommand(Guid.NewGuid(), Guid.NewGuid(), Date), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithDifferentUser_ShouldThrowForbidden()
    {
        var habit = new Habit(Guid.NewGuid(), "Academia", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        var act = () => _handler.Handle(new CheckHabitCommand(Guid.NewGuid(), habit.Id, Date), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenNotScheduledForDate_ShouldThrow()
    {
        var (userId, habit) = ArrangeHabitWithSchedule();
        _scheduleRepository.GetByHabitIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns([]); // sem schedule no dia

        var act = () => _handler.Handle(new CheckHabitCommand(userId, habit.Id, Date), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class UncheckHabitHandlerTests
{
    private readonly IHabitCheckRepository _checkRepository = Substitute.For<IHabitCheckRepository>();
    private readonly IPushNotificationOrchestrator _orchestrator = Substitute.For<IPushNotificationOrchestrator>();
    private readonly UncheckHabitHandler _handler;

    private static readonly DateOnly Date = new(2026, 4, 5);

    public UncheckHabitHandlerTests()
    {
        _handler = new UncheckHabitHandler(_checkRepository, _orchestrator);
    }

    [Fact]
    public async Task Handle_WithExistingCheck_ShouldDeleteAndRecompute()
    {
        var userId = Guid.NewGuid();
        var habitId = Guid.NewGuid();
        var check = new HabitCheck(
            habitId, userId, Date, Date,
            new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc),
            "Academia", "🏋️", new TimeOnly(7, 0), new TimeOnly(8, 0));
        _checkRepository.GetByHabitAndDateAsync(habitId, Date, Arg.Any<CancellationToken>()).Returns(check);

        await _handler.Handle(new UncheckHabitCommand(userId, habitId, Date), CancellationToken.None);

        await _checkRepository.Received(1).DeleteAsync(check, Arg.Any<CancellationToken>());
        await _orchestrator.Received(1).RecomputeUserNotificationsAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutCheck_ShouldBeIdempotent()
    {
        _checkRepository.GetByHabitAndDateAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((HabitCheck?)null);

        await _handler.Handle(new UncheckHabitCommand(Guid.NewGuid(), Guid.NewGuid(), Date), CancellationToken.None);

        await _checkRepository.DidNotReceive().DeleteAsync(Arg.Any<HabitCheck>(), Arg.Any<CancellationToken>());
        await _orchestrator.DidNotReceive().RecomputeUserNotificationsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDifferentUser_ShouldThrowForbidden()
    {
        var habitId = Guid.NewGuid();
        var check = new HabitCheck(
            habitId, Guid.NewGuid(), Date, Date,
            new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc),
            "Academia", "🏋️", new TimeOnly(7, 0), new TimeOnly(8, 0));
        _checkRepository.GetByHabitAndDateAsync(habitId, Date, Arg.Any<CancellationToken>()).Returns(check);

        var act = () => _handler.Handle(new UncheckHabitCommand(Guid.NewGuid(), habitId, Date), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}

public class CheckHabitFromNotificationHandlerTests
{
    private readonly IPushNotificationActionTokenService _actionTokenService = Substitute.For<IPushNotificationActionTokenService>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CheckHabitFromNotificationHandler _handler;

    private static readonly DateOnly Date = new(2026, 4, 5);

    public CheckHabitFromNotificationHandlerTests()
    {
        _handler = new CheckHabitFromNotificationHandler(_actionTokenService, _mediator);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldDelegateToCheckCommand()
    {
        var userId = Guid.NewGuid();
        var habitId = Guid.NewGuid();
        var expected = new HabitCheckResult(habitId, Date, new DateTime(2026, 4, 5, 15, 0, 0, DateTimeKind.Utc));
        _actionTokenService.Validate("token").Returns(
            new PushNotificationActionTokenPayload(userId, habitId, Date, new DateTime(2026, 4, 5, 16, 0, 0, DateTimeKind.Utc)));
        _mediator.Send(Arg.Any<CheckHabitCommand>(), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _handler.Handle(new CheckHabitFromNotificationCommand(habitId, Date, "token"), CancellationToken.None);

        result.Should().Be(expected);
        await _mediator.Received(1).Send(
            Arg.Is<CheckHabitCommand>(c => c.UserId == userId && c.HabitId == habitId && c.Date == Date),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMismatchedToken_ShouldThrowForbidden()
    {
        _actionTokenService.Validate("token").Returns(
            new PushNotificationActionTokenPayload(Guid.NewGuid(), Guid.NewGuid(), Date, new DateTime(2026, 4, 5, 16, 0, 0, DateTimeKind.Utc)));

        var act = () => _handler.Handle(new CheckHabitFromNotificationCommand(Guid.NewGuid(), Date, "token"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
