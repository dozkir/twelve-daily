namespace TwelveDaily.Infrastructure.Services;

/// <summary>
/// Detalhe de infraestrutura: o id do job Hangfire de "próximo wake" agendado para um usuário.
/// Existe para garantir <b>no máximo um wake pendente por usuário</b> — antes de agendar o
/// próximo recompute, o anterior é cancelado. Sem isso, cada mutação (criar/editar hábito,
/// schedule, check) enfileira uma cadeia independente e auto-perpetuante de wakes, e na
/// fronteira de ativação todas disparam de uma vez → várias notificações duplicadas.
/// Não é entidade de domínio: o <c>Domain</c> não conhece Hangfire/agendamento.
/// </summary>
public class NotificationWake
{
    public Guid UserId { get; private set; }
    public string JobId { get; private set; } = string.Empty;

    private NotificationWake() { } // EF Core

    public NotificationWake(Guid userId, string jobId)
    {
        UserId = userId;
        JobId = jobId;
    }

    public void SetJob(string jobId) => JobId = jobId;
}
