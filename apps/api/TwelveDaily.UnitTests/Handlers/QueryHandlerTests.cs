using FluentAssertions;
using NSubstitute;
using TwelveDaily.Application.Habits.Handlers;
using TwelveDaily.Application.Habits.Queries;
using TwelveDaily.Application.Interfaces;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.UnitTests.Handlers;

public class GetDailyHabitsHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly IHabitScheduleRepository _scheduleRepository = Substitute.For<IHabitScheduleRepository>();
    private readonly IHabitCheckRepository _checkRepository = Substitute.For<IHabitCheckRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly GetDailyHabitsHandler _handler;

    public GetDailyHabitsHandlerTests()
    {
        _dateTime.UtcNow.Returns(new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc));
        _dateTime.TodayUtc.Returns(new DateOnly(2026, 4, 6)); // Monday
        _habitRepository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _scheduleRepository.GetByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _checkRepository.GetByUserAndDateRangeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new User("test@example.com", "hash", "America/Sao_Paulo"));
        _handler = new GetDailyHabitsHandler(_habitRepository, _scheduleRepository, _checkRepository, _userRepository, _dateTime);
    }

    [Fact]
    public async Task Handle_ShouldReturn7Days()
    {
        var query = new GetDailyHabitsQuery(Guid.NewGuid(), new DateOnly(2026, 4, 6), "America/Sao_Paulo");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Days.Should().HaveCount(7); // D-3 to D+3
    }

    [Fact]
    public async Task Handle_ShouldLabelDayTypes()
    {
        var query = new GetDailyHabitsQuery(Guid.NewGuid(), new DateOnly(2026, 4, 6), "America/Sao_Paulo");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Days.Should().Contain(d => d.Date == new DateOnly(2026, 4, 6) && d.Type == "today");
        result.Days.Where(d => d.Date < new DateOnly(2026, 4, 6)).Should().OnlyContain(d => d.Type == "past");
        result.Days.Where(d => d.Date > new DateOnly(2026, 4, 6)).Should().OnlyContain(d => d.Type == "future");
    }

    [Fact]
    public async Task Handle_ShouldUseUserTimezoneToDetermineToday()
    {
        var userId = Guid.NewGuid();
        _dateTime.UtcNow.Returns(new DateTime(2026, 4, 6, 2, 0, 0, DateTimeKind.Utc));
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new User("timezone@example.com", "hash", "America/Los_Angeles"));

        var result = await _handler.Handle(new GetDailyHabitsQuery(userId, new DateOnly(2026, 4, 5), "UTC"), CancellationToken.None);

        // 2026-04-06 02:00 UTC is still 2026-04-05 in Los Angeles
        result.Days.Should().Contain(d => d.Date == new DateOnly(2026, 4, 5) && d.Type == "today");
    }

    [Fact]
    public async Task Handle_ScheduledHabit_ShouldAppearOnTodayWithoutCheck()
    {
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _dateTime.UtcNow.Returns(DateTime.UtcNow);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new User("u@example.com", "hash", "UTC"));

        var habit = new Habit(userId, "Academia", "🏋️", null, false);
        var schedule = new HabitSchedule(habit.Id, today.DayOfWeek, new TimeOnly(7, 0), new TimeOnly(8, 0));
        _habitRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns([habit]);
        _scheduleRepository.GetByUserAsync(userId, Arg.Any<CancellationToken>()).Returns([schedule]);

        var result = await _handler.Handle(new GetDailyHabitsQuery(userId, today, "UTC"), CancellationToken.None);

        var todayItems = result.Days.First(d => d.Date == today).Items;
        todayItems.Should().ContainSingle(i => i.HabitId == habit.Id && i.CheckedAt == null);
    }

    [Fact]
    public async Task Handle_CheckedHabit_ShouldRenderFromSnapshot()
    {
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _dateTime.UtcNow.Returns(DateTime.UtcNow);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new User("u@example.com", "hash", "UTC"));

        // hábito mudou de nome depois do check
        var habit = new Habit(userId, "Novo Nome", "🆕", null, false);
        var schedule = new HabitSchedule(habit.Id, today.DayOfWeek, new TimeOnly(9, 0), new TimeOnly(10, 0));
        _habitRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns([habit]);
        _scheduleRepository.GetByUserAsync(userId, Arg.Any<CancellationToken>()).Returns([schedule]);

        var checkedAt = DateTime.UtcNow;
        var check = new HabitCheck(habit.Id, userId, today, today, checkedAt, "Nome Antigo", "🏋️", new TimeOnly(7, 0), new TimeOnly(8, 0));
        _checkRepository.GetByUserAndDateRangeAsync(userId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([check]);

        var result = await _handler.Handle(new GetDailyHabitsQuery(userId, today, "UTC"), CancellationToken.None);

        var item = result.Days.First(d => d.Date == today).Items.Single(i => i.HabitId == habit.Id);
        item.Name.Should().Be("Nome Antigo");          // snapshot, não o nome atual
        item.StartTime.Should().Be(new TimeOnly(7, 0)); // horário do snapshot
        item.CheckedAt.Should().Be(checkedAt);
    }
}

public class GetHabitDetailHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly IHabitScheduleRepository _scheduleRepository = Substitute.For<IHabitScheduleRepository>();
    private readonly GetHabitDetailHandler _handler;

    public GetHabitDetailHandlerTests()
    {
        _handler = new GetHabitDetailHandler(_habitRepository, _scheduleRepository);
    }

    [Fact]
    public async Task Handle_WithExistingHabit_ShouldReturnDetailWithSchedules()
    {
        var userId = Guid.NewGuid();
        var habit = new Habit(userId, "Academia", "🏋️", "Treino", false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        var schedules = new List<HabitSchedule>
        {
            new(habit.Id, DayOfWeek.Monday, new TimeOnly(7, 0), new TimeOnly(8, 0)),
            new(habit.Id, DayOfWeek.Wednesday, new TimeOnly(7, 0), new TimeOnly(8, 0))
        };
        _scheduleRepository.GetByHabitIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(schedules);

        var result = await _handler.Handle(new GetHabitDetailQuery(habit.Id, userId), CancellationToken.None);

        result.Name.Should().Be("Academia");
        result.Emoji.Should().Be("🏋️");
        result.Schedules.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNonExistentHabit_ShouldThrow()
    {
        _habitRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Habit?)null);

        var act = () => _handler.Handle(new GetHabitDetailQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WithDifferentUserId_ShouldThrow()
    {
        var habit = new Habit(Guid.NewGuid(), "Academia", "🏋️", null, false);
        _habitRepository.GetByIdAsync(habit.Id, Arg.Any<CancellationToken>()).Returns(habit);

        var act = () => _handler.Handle(new GetHabitDetailQuery(habit.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class GetHabitsListHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly GetHabitsListHandler _handler;

    public GetHabitsListHandlerTests()
    {
        _handler = new GetHabitsListHandler(_habitRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserHabits()
    {
        var userId = Guid.NewGuid();
        var habits = new List<Habit>
        {
            new(userId, "Academia", "🏋️", null, false),
            new(userId, "Leitura", "📖", null, false)
        };
        _habitRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(habits);

        var result = await _handler.Handle(new GetHabitsListQuery(userId), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNoHabits_ShouldReturnEmptyList()
    {
        var userId = Guid.NewGuid();
        _habitRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<Habit>());

        var result = await _handler.Handle(new GetHabitsListQuery(userId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class GetWeeklyDashboardHandlerTests
{
    private readonly IHabitRepository _habitRepository = Substitute.For<IHabitRepository>();
    private readonly IHabitScheduleRepository _scheduleRepository = Substitute.For<IHabitScheduleRepository>();
    private readonly IHabitCheckRepository _checkRepository = Substitute.For<IHabitCheckRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly GetWeeklyDashboardHandler _handler;

    public GetWeeklyDashboardHandlerTests()
    {
        _dateTime.UtcNow.Returns(DateTime.UtcNow);
        _habitRepository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _scheduleRepository.GetByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _checkRepository.GetByUserAndDateRangeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _userRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new User("u@example.com", "hash", "UTC"));
        _handler = new GetWeeklyDashboardHandler(
            _habitRepository, _scheduleRepository, _checkRepository, _userRepository, _dateTime);
    }

    [Fact]
    public async Task Handle_WithScheduledAndCheckedHabit_ShouldCountIt()
    {
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var habit = new Habit(userId, "Academia", "🏋️", null, false);
        var schedule = new HabitSchedule(habit.Id, today.DayOfWeek, new TimeOnly(7, 0), new TimeOnly(8, 0));
        _habitRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns([habit]);
        _scheduleRepository.GetByUserAsync(userId, Arg.Any<CancellationToken>()).Returns([schedule]);
        var check = new HabitCheck(habit.Id, userId, today, today, DateTime.UtcNow, "Academia", "🏋️", new TimeOnly(7, 0), new TimeOnly(8, 0));
        _checkRepository.GetByUserAndDateRangeAsync(userId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([check]);

        var result = await _handler.Handle(new GetWeeklyDashboardQuery(userId, today), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Completed.Should().Be(1);
        result.CompletionRate.Should().Be(100);
    }

    [Fact]
    public async Task Handle_WithNoHabits_ShouldReturnZeros()
    {
        var userId = Guid.NewGuid();
        var weekStart = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await _handler.Handle(new GetWeeklyDashboardQuery(userId, weekStart), CancellationToken.None);

        result.Total.Should().Be(0);
        result.Completed.Should().Be(0);
        result.CompletionRate.Should().Be(0);
        result.DayByDay.Should().HaveCount(7);
    }
}
