namespace TwelveDaily.Core.Domains.Habits;

public class Habit
{
    public int Id  { get; set; }
    public string Name  { get; set; }
    public string? Description  { get; set; }
    public bool Enabled { get; set; }
    public WeekSchedule WeekSchedule { get; set; }
    public string? Icon { get; set; }
    public DateTime CreatedAt { get; }
    public DateTime ModifiedAt { get; }
    public int UserId  { get; set; }

    protected Habit() { } // EF

    private Habit(int userId, string name, string? description, string? icon, WeekSchedule weekSchedule)
    {
        UserId = userId;
        Name = name;
        Description = description;
        Icon = icon;
        WeekSchedule = weekSchedule;
        Enabled = true;
        CreatedAt = DateTime.Now;
        ModifiedAt = CreatedAt;
    }

    public static Habit Create(int userId, string name, string? description, string? icon, WeekSchedule weekSchedule)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exception("Name is required");
        }
        
        var newHabit = new Habit(
            userId,
            name,
            description,
            icon,
            weekSchedule
        );

        return newHabit;
    }
}