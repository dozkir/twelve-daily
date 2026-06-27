using FluentValidation;
using TwelveDaily.Application.Habits.Commands;

namespace TwelveDaily.Application.Habits.Validators;

public class CreateHabitCommandValidator : AbstractValidator<CreateHabitCommand>
{
    public CreateHabitCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().NotNull()
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("'Name' must not be empty.");
        RuleFor(x => x.Emoji).NotEmpty();
        RuleFor(x => x.Schedules).NotEmpty()
            .Must(ScheduleRules.HaveDistinctDays).WithMessage(ScheduleRules.DistinctDaysMessage);
        RuleForEach(x => x.Schedules).SetValidator(new CreateHabitScheduleDtoValidator());
    }
}

public class UpdateHabitSchedulesCommandValidator : AbstractValidator<UpdateHabitSchedulesCommand>
{
    public UpdateHabitSchedulesCommandValidator()
    {
        RuleFor(x => x.HabitId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Schedules).NotEmpty()
            .Must(ScheduleRules.HaveDistinctDays).WithMessage(ScheduleRules.DistinctDaysMessage);
        RuleForEach(x => x.Schedules).SetValidator(new CreateHabitScheduleDtoValidator());
    }
}

internal static class ScheduleRules
{
    // Um hábito tem no máximo um horário por dia da semana; dois horários no mesmo dia = dois hábitos.
    public const string DistinctDaysMessage = "A habit cannot have more than one schedule on the same day of week.";

    public static bool HaveDistinctDays(List<CreateHabitScheduleDto> schedules)
        => schedules == null || schedules.GroupBy(s => s.DayOfWeek).All(g => g.Count() == 1);
}

public class UpdateHabitCommandValidator : AbstractValidator<UpdateHabitCommand>
{
    public UpdateHabitCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Emoji).NotEmpty();
    }
}

public class CreateHabitScheduleDtoValidator : AbstractValidator<CreateHabitScheduleDto>
{
    public CreateHabitScheduleDtoValidator()
    {
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime);
    }
}

public class CheckHabitCommandValidator : AbstractValidator<CheckHabitCommand>
{
    public CheckHabitCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.HabitId).NotEmpty();
    }
}

public class UncheckHabitCommandValidator : AbstractValidator<UncheckHabitCommand>
{
    public UncheckHabitCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.HabitId).NotEmpty();
    }
}

public class CheckHabitFromNotificationCommandValidator : AbstractValidator<CheckHabitFromNotificationCommand>
{
    public CheckHabitFromNotificationCommandValidator()
    {
        RuleFor(x => x.HabitId).NotEmpty();
        RuleFor(x => x.ActionToken).NotEmpty();
    }
}
