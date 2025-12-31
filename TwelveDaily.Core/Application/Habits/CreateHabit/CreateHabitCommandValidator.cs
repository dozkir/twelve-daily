using TwelveDaily.Core.Application.Common;

namespace TwelveDaily.Core.Application.Habits.CreateHabit;

public static class CreateHabitCommandValidator
{
    public static ValidationResult Validate(CreateHabitCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Name is required");
        }
        
        return new ValidationResult(errors);
    }
}