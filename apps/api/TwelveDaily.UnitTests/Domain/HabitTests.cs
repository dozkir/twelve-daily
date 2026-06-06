using FluentAssertions;
using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.UnitTests.Domain;

public class HabitTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_ShouldCreateHabit()
    {
        var habit = new Habit(_userId, "Academia", "🏋️", "Treino de musculação", false);

        habit.Id.Should().NotBeEmpty();
        habit.UserId.Should().Be(_userId);
        habit.Name.Should().Be("Academia");
        habit.Emoji.Should().Be("🏋️");
        habit.Description.Should().Be("Treino de musculação");
        habit.IsActive.Should().BeTrue();
        habit.SyncGoogleCalendar.Should().BeFalse();
        habit.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithNullDescription_ShouldAccept()
    {
        var habit = new Habit(_userId, "Leitura", "📖", null, false);

        habit.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ShouldThrow(string? name)
    {
        var act = () => new Habit(_userId, name!, "🏋️", null, false);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidEmoji_ShouldThrow(string? emoji)
    {
        var act = () => new Habit(_userId, "Academia", emoji!, null, false);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ShouldThrow()
    {
        var act = () => new Habit(Guid.Empty, "Academia", "🏋️", null, false);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateFields()
    {
        var habit = new Habit(_userId, "Academia", "🏋️", null, false);

        habit.Update("Yoga", "🧘", "Yoga matinal", true);

        habit.Name.Should().Be("Yoga");
        habit.Emoji.Should().Be("🧘");
        habit.Description.Should().Be("Yoga matinal");
        habit.SyncGoogleCalendar.Should().BeTrue();
        habit.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Update_WithInvalidName_ShouldThrow(string? name)
    {
        var habit = new Habit(_userId, "Academia", "🏋️", null, false);

        var act = () => habit.Update(name!, "🧘", null, false);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ToggleActive_WhenActive_ShouldDeactivate()
    {
        var habit = new Habit(_userId, "Academia", "🏋️", null, false);
        habit.IsActive.Should().BeTrue();

        habit.ToggleActive();

        habit.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ToggleActive_WhenInactive_ShouldActivate()
    {
        var habit = new Habit(_userId, "Academia", "🏋️", null, false);
        habit.ToggleActive(); // deactivate

        habit.ToggleActive(); // reactivate

        habit.IsActive.Should().BeTrue();
    }
}

