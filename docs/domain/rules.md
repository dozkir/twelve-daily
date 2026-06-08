# Domínio — Regras de Negócio

> Esta página reflete o modelo de **check** (sem materialização de instâncias). Ver [habit-check-refactor](../specs/habit-check-refactor.md).

## Hábitos
- Sem limite de hábitos por usuário
- Cada hábito pode ter horários **diferentes por dia da semana**, definidos por `HabitSchedule`
- **No máximo um `HabitSchedule` por dia da semana por hábito** — dois horários no mesmo dia = dois hábitos
- `HabitSchedule` pode ser **ativado/desativado por dia** individualmente sem deletar o registro
- `Habit.IsActive = false` **pausa** o hábito: some da timeline de hoje/futuro e não gera notificação. Os dias **passados continuam contando** (existência por `CreatedAt`)

## Rotina sem materialização
- **Não há geração de instâncias.** A rotina de cada dia é reconstruída sob demanda a partir de `Habit` + `HabitSchedule` + `HabitCheck`
- Abrir a timeline **não cria dados** no banco
- Um hábito aparece num dia se: `Habit.CreatedAt` ≤ aquela data **e** existe `HabitSchedule` ativo para o `DayOfWeek` daquele dia
- Dias passados só mostram hábitos que **já existiam** naquela data

## Google Calendar
- **Sincronização adiada** nesta fase do refactor (ver [habit-check-refactor](../specs/habit-check-refactor.md) §10.4). A flag `SyncGoogleCalendar` permanece reservada
- Quando reativada, o modelo-alvo é um **evento recorrente (RRULE)** dirigido pelo schedule, não um evento por dia

---

## Timeline
- A tela de timeline é **somente para visualização e check** de hábitos
- Criação e edição de hábitos acontecem em tela separada

## Check
- Check é um registro (`HabitCheck`) com identidade `(HabitId, Date)` — **um por hábito por dia** (upsert idempotente)
- Marcar grava um snapshot (nome, emoji, horário) para fidelidade histórica
- **Desfazer** check = **deletar** o registro
- **Não é permitido dar check em dias futuros**
- Check em dias passados é permitido (desde que o hábito existisse e estivesse agendado naquele dia)

## Acesso
- Isolamento total: cada usuário acessa **apenas seus próprios dados**
- Sem sistema de roles — todos os usuários têm o mesmo nível de acesso

---

## Separação: Plano × Realidade

| Contexto | Fonte de dados | Check? |
|---|---|---|
| Dias passados | `Habit` + `HabitSchedule` + `HabitCheck` (reconstruído) | ✅ Sim (se o hábito existia naquele dia) |
| Dia atual | `Habit` + `HabitSchedule` + `HabitCheck` (reconstruído) | ✅ Sim |
| Dias futuros | `Habit` + `HabitSchedule` (planejado) | ❌ Não |

---

## Estado de uma ocorrência (hábito + dia)

| Estado | Condição |
|---|---|
| Pendente | sem `HabitCheck` para `(HabitId, Date)` e horário ainda não passou |
| Concluída | existe `HabitCheck` para `(HabitId, Date)` |
| Atrasada | sem `HabitCheck` e o `EndTime` do schedule daquele dia já passou |

