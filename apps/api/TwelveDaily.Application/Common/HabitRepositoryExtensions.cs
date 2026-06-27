using TwelveDaily.Domain.Entities;
using TwelveDaily.Domain.Exceptions;
using TwelveDaily.Domain.Interfaces;

namespace TwelveDaily.Application.Common;

/// <summary>
/// Helpers de acesso a hábitos compartilhados pelos handlers.
/// Centraliza o guard "existe + pertence ao usuário" em um único lugar (DRY),
/// mantendo a regra de isolamento entre usuários consistente em todos os casos de uso.
/// </summary>
public static class HabitRepositoryExtensions
{
    /// <summary>
    /// Carrega o hábito garantindo que ele existe e pertence ao usuário.
    /// Lança <see cref="DomainException"/> quando não existe e
    /// <see cref="ForbiddenException"/> quando pertence a outro usuário.
    /// </summary>
    public static async Task<Habit> GetOwnedAsync(
        this IHabitRepository repository,
        Guid habitId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var habit = await repository.GetByIdAsync(habitId, cancellationToken);
        if (habit is null)
            throw new DomainException("Habit not found.");
        if (habit.UserId != userId)
            throw new ForbiddenException("Habit does not belong to user.");

        return habit;
    }
}
