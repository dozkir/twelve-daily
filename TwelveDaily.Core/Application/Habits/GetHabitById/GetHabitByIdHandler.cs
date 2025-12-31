using TwelveDaily.Core.Application.Common;
using TwelveDaily.Core.Domains.Habits;
using TwelveDaily.Core.Application.Interfaces;

namespace TwelveDaily.Core.Application.Habits.GetHabitById;

public class GetHabitByIdHandler(IHabitRepository habitRepository)
{
    public async Task<ApplicationResult<Habit?>> ExecuteAsync(GetHabitByIdQuery query)
    {
        var habit = await habitRepository.GetHabitByIdAsync(query.habitId);
        return ApplicationResult<Habit?>.Ok(habit);
    }
}