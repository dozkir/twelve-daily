namespace TwelveDaily.Core.Application.Habits.CreateHabit;

public sealed record CreateHabitCommand(
    int UserId,
    string Name,
    string? Description, 
    string? Icon,
    TimeOnly? Monday,
    TimeOnly? Tuesday,
    TimeOnly? Wednesday,
    TimeOnly? Thursday,
    TimeOnly? Friday,
    TimeOnly? Saturday,
    TimeOnly? Sunday
);