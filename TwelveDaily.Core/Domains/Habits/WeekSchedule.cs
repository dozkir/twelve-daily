namespace TwelveDaily.Core.Domains.Habits;

public class WeekSchedule
{
    public TimeOnly? Monday { get; }
    public TimeOnly? Tuesday { get;}
    public TimeOnly? Wednesday { get; }
    public TimeOnly? Thursday { get; }
    public TimeOnly? Friday { get; }
    public TimeOnly? Saturday { get; }
    public TimeOnly? Sunday { get; }

    public bool HasAnyDayDefined => 
        Monday != null ||
        Tuesday != null ||
        Wednesday != null ||
        Thursday != null ||
        Friday != null ||
        Saturday != null ||
        Sunday != null;
    
    protected WeekSchedule(){} // EF

    private WeekSchedule(
        TimeOnly? monday,
        TimeOnly? tuesday,
        TimeOnly? wednesday,
        TimeOnly? thursday,
        TimeOnly? friday,
        TimeOnly? saturday,
        TimeOnly? sunday
    )
    {
        Monday = monday;
        Tuesday = tuesday;
        Wednesday = wednesday;
        Thursday = thursday;
        Friday = friday;
        Saturday = saturday;
        Sunday = sunday;
    }

    public static WeekSchedule Create(
        TimeOnly? monday,
        TimeOnly? tuesday,
        TimeOnly? wednesday,
        TimeOnly? thursday,
        TimeOnly? friday,
        TimeOnly? saturday,
        TimeOnly? sunday
    )
    {
        return new WeekSchedule(monday, tuesday, wednesday, thursday, friday, saturday, sunday);
    }
        
}