using TwelveDaily.Core.Application.Common;
using TwelveDaily.Core.Application.Interfaces;
using TwelveDaily.Core.Domains.Habits;

namespace TwelveDaily.Core.Application.Habits.GetAllUserHabits;

public class GetAllUserHabitsHandler(IHabitRepository habitRepository)
{
    public async Task<ApplicationResult<IReadOnlyList<Habit?>>> ExecuteAsync(GetAllUserHabitsQuery query)
    {
        // TODO: Validate if user exists
        
        // if (!await _userRepository.ExistsAsync(query.UserId))
        // {
        //     return Result<IReadOnlyList<HabitDto>>
        //         .Failure("User not found");
        // }
        
        var habits = await habitRepository.GetAllUserHabitsAsync(query.UserId);
        
        return ApplicationResult<IReadOnlyList<Habit?>>.Ok(habits);
    }
}