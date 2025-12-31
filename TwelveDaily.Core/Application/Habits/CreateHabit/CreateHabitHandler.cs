using TwelveDaily.Core.Application.Common;
using TwelveDaily.Core.Application.Interfaces;
using TwelveDaily.Core.Domains.Habits;

namespace TwelveDaily.Core.Application.Habits.CreateHabit;

public class CreateHabitHandler(IHabitRepository habitRepository, IUserRepository userRepository)
{
    public async Task<ApplicationResult<int>> ExecuteAsync(CreateHabitCommand command)
    {
        // Validations
        var validation = CreateHabitCommandValidator.Validate(command);

        if (!validation.IsValid)
        {
            return ApplicationResult<int>.Fail(validation.Errors);
        }
        
        // Validating if user exists
        var user = await userRepository.GetUserByIdAsync(command.UserId);
        if (user == null)
        {
            return ApplicationResult<int>.Fail(["User doesn't exist"]);
        }
        
        // Creating Schedule instance
        var schedule = WeekSchedule.Create(
            command.Monday,
            command.Tuesday,
            command.Wednesday,
            command.Thursday,
            command.Friday,
            command.Saturday,
            command.Sunday
        );

        // Creating Habit instance
        var habit = Habit.Create(
            command.UserId,
            command.Name,
            command.Description,
            command.Icon,
            schedule
        );

        // Adding to db
        await habitRepository.AddAsync(habit);

        return ApplicationResult<int>.Ok(habit.Id);
    }
}