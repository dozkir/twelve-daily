using TwelveDaily.Domain.Exceptions;

namespace TwelveDaily.Domain.Entities;

/// <summary>
/// Registro de que um hábito foi concluído em uma data específica.
/// Identidade lógica da ocorrência = (HabitId, Date). Um check por hábito por dia.
/// Guarda um snapshot do hábito/horário no momento do check para fidelidade histórica.
/// </summary>
public class HabitCheck
{
    public Guid Id { get; private set; }
    public Guid HabitId { get; private set; }
    public Guid UserId { get; private set; } // denormalizado — evita JOIN nas consultas por usuário
    public DateOnly Date { get; private set; } // data local do usuário a que o check pertence
    public DateTime CheckedAt { get; private set; } // UTC

    // Snapshot do hábito/horário no momento do check (fidelidade histórica)
    public string HabitName { get; private set; } = string.Empty;
    public string HabitEmoji { get; private set; } = string.Empty;
    public TimeOnly StartTime { get; private set; } // local
    public TimeOnly EndTime { get; private set; }   // local

    private HabitCheck() { } // EF Core

    public HabitCheck(
        Guid habitId,
        Guid userId,
        DateOnly date,
        DateOnly localToday,
        DateTime checkedAtUtc,
        string habitName,
        string habitEmoji,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        if (habitId == Guid.Empty)
            throw new DomainException("HabitId is required.");
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");
        if (date > localToday)
            throw new DomainException("Cannot check a habit for a future date.");
        if (string.IsNullOrWhiteSpace(habitName))
            throw new DomainException("HabitName is required.");
        if (string.IsNullOrWhiteSpace(habitEmoji))
            throw new DomainException("HabitEmoji is required.");

        Id = Guid.NewGuid();
        HabitId = habitId;
        UserId = userId;
        Date = date;
        CheckedAt = checkedAtUtc;
        HabitName = habitName;
        HabitEmoji = habitEmoji;
        StartTime = startTime;
        EndTime = endTime;
    }
}
