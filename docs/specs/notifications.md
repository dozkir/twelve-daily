# Spec — Notificações

## Push Notifications

### Provider
**Expo Push Notifications** — gerencia FCM (Android) e APNs (iOS) automaticamente.

### Objetivo por plataforma
- **Android**: comportamento de notificação persistente é objetivo principal.
- **iOS**: implementar a experiência mais próxima possível dentro das limitações da plataforma.

### Regra principal
- Apenas **a notificação do próximo hábito** deve ficar visível por vez, por dispositivo.
- A notificação do próximo hábito aparece **15 minutos antes** do `ScheduledStartTime`.
- A notificação possui apenas a ação **`Check`**.
- A ação `Check` **não abre o app**; ela cria o `HabitCheck` de `(HabitId, Date)` diretamente.
- Não existe botão `Dismiss`.
- Se o hábito atual for concluído e o próximo já estiver dentro da janela de 15 minutos, a notificação do próximo hábito deve assumir imediatamente.
- Se o hábito não for concluído, a notificação permanece persistente até `ScheduledEndTime`.
- Após `ScheduledEndTime`, a notificação deixa de ser persistente e o foco passa para o próximo hábito elegível.

### Fluxo

```
1. App registra o dispositivo no Expo → recebe ExponentPushToken
2. App envia o token para a API: POST /users/push-token
3. API armazena o token (entidade PushToken)
4. O recompute determina o próximo hábito elegível a partir de schedules + checks + timezone (sem instâncias)
5. O recompute AUTO-AGENDA (Hangfire) o próximo "acordar" na próxima fronteira de ativação/fim
6. Na ativação (≤ 15 min antes do início), a API envia o payload do próximo hábito para todos os `PushToken` ativos
7. Android mantém a notificação ativa de forma persistente; iOS aplica o melhor comportamento possível
8. Ao receber `Check`, a API cria o `HabitCheck` sem abrir o app
9. Após o check ou após o fim do horário, a API/cliente promovem o próximo hábito elegível
```

### Registro de Push Token

```
POST /users/push-token
Body: { token: "ExponentPushToken[...]", deviceLabel: "iPhone do Rafael" }
Response 204
```

> Um usuário pode ter múltiplos `PushToken` ativos (um por dispositivo).
> Push notifications são enviadas para **todos** os tokens ativos do usuário.
> Ao re-registrar um token já existente, apenas atualiza `UpdatedAt`.
> A regra de “apenas uma notificação visível” vale **por dispositivo/token**, não globalmente para o usuário.

### Conteúdo da Notificação
```json
{
  "to": "ExponentPushToken[...]",
  "title": "Hora do hábito!",
  "body": "{{Habit.Name}} — {{ScheduledStartTime}} até {{ScheduledEndTime}}",
  "data": {
    "habitId": "...",
    "date": "2026-03-27",
    "scheduledStartTime": "...",
    "scheduledEndTime": "...",
    "actionToken": "...",
    "type": "next-habit"
  }
}
```

> O `actionToken` é um JWT curto que carrega `(UserId, HabitId, Date)`, usado pela ação `Check` anônima.

### Ação da Notificação
- A notificação expõe apenas a ação `Check`.
- `Check` cria o `HabitCheck` sem abrir o aplicativo (`POST /habits/{habitId}/check/from-notification`).
- Ao concluir:
  - a notificação atual é removida;
  - se já existir outro hábito dentro da janela de 15 minutos, ele passa a ser a notificação visível.

### Jobs Hangfire
- O recompute do usuário **auto-agenda** o próximo "acordar" na próxima fronteira relevante (ativação de uma ocorrência futura ou fim da ocorrência ativa) — encadeando o ciclo sem jobs por instância
- **No máximo um wake pendente por usuário**: antes de agendar o próximo "acordar", o recompute **cancela o anterior** (o id do job é persistido em `NotificationWake`, chave = `UserId`). Sem essa deduplicação, cada mutação dispararia uma cadeia de wakes independente e auto-perpetuante, e todas as cadeias vivas acordariam juntas na fronteira de ativação → **a mesma notificação chegaria várias vezes** (uma por cadeia). `NotificationWake` é detalhe de infraestrutura (mapeado no `AppDbContext`, fora do `Domain` — o domínio não conhece Hangfire)
- Ao dar/desfazer check, criar/editar/alternar hábito ou alterar schedule, o backend recalcula imediatamente o próximo hábito elegível
- Persistência no PostgreSQL — jobs sobrevivem a reinicializações
- Dashboard exposto em `/hangfire` **apenas no ambiente Development** (filtro de autorização atual libera o acesso). As variáveis `HANGFIRE_USER`/`HANGFIRE_PASSWORD` existem no `.env`/compose mas a proteção por autenticação básica ainda **não está implementada**

### Ciclo de Vida da Notificação
1. O backend identifica o próximo hábito não concluído do usuário.
2. Se faltar 15 minutos ou menos para o início, ele pode ativar imediatamente a notificação desse hábito.
3. Enquanto o horário do hábito não passou, a notificação permanece como a notificação ativa do dispositivo.
4. Se houver `Check`, o hábito é concluído e a próxima notificação elegível assume.
5. Se não houver `Check`, a persistência termina em `ScheduledEndTime`.
6. Após isso, o sistema promove o próximo hábito elegível.

---

## Real-time (SignalR)

> ⚠️ **Planejado — ainda não implementado.** O backend chama `AddSignalR()` em `Program.cs`, mas **nenhum Hub está mapeado** e o cliente ainda não usa `@microsoft/signalr`. A seção abaixo descreve o alvo. Hoje a atualização da UI após um check depende da invalidação das queries do TanStack Query no próprio dispositivo, não de eventos em tempo real entre dispositivos.

### Hub (alvo)
- Endpoint: `/hubs/habits`
- Autenticação: JWT via query string (`?access_token=...`) ou header

### Eventos emitidos pelo servidor

| Evento | Payload | Quando |
|---|---|---|
| `HabitChecked` | `{ habitId, date, checkedAt }` | Ao dar check |
| `HabitUnchecked` | `{ habitId, date }` | Ao desfazer check *(se implementado)* |

### Comportamento no front
- Ao receber `HabitChecked`: atualiza o item correspondente `(habitId, date)` para concluído sem recarregar a tela
- Garante consistência entre múltiplos dispositivos/abas do mesmo usuário

