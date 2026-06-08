# Spec — Hábitos

## Navegação por Dias

A API retorna um range de **D-3 até D+3** em torno da data solicitada em uma única chamada.

```
GET /habits/daily?date=2026-03-27

Response 200:
{
  days: [
    {
      date: "2026-03-24",
      type: "past",        // "past" | "today" | "future"
      items: [...]
    },
    ...
    {
      date: "2026-03-27",
      type: "today",
      items: [...]
    },
    ...
    {
      date: "2026-03-30",
      type: "future",
      items: [...]
    }
  ]
}
```

### Fonte de dados por tipo de dia

Todos os dias são **reconstruídos** de `Habit` + `HabitSchedule` + `HabitCheck` (não há instância materializada). Cada item traz: `habitId, name, emoji, description, startTime, endTime, checkedAt`. `checkedAt != null` = concluído (renderizado do snapshot do check).

| Tipo | Fonte | Check? |
|---|---|---|
| `past` | hábito + schedule + check (se o hábito existia naquele dia) | ✅ |
| `today` | hábito + schedule + check | ✅ |
| `future` | hábito + schedule (planejado) | ❌ |

> `startTime`/`endTime` são `TimeOnly` locais (ex.: `"07:00:00"`). Não há mais `instanceId`.

---

## Check de Hábito

Identidade da ocorrência = `(habitId, date)`. Um check por hábito por dia.

```
PUT /habits/{habitId}/check          Body: { "date": "2026-03-27" }   → upsert idempotente
Response 200: { habitId, date, checkedAt }

DELETE /habits/{habitId}/check?date=2026-03-27   → desmarca (idempotente)
Response 204

POST /habits/{habitId}/check/from-notification   Body: { "date", "actionToken" }   [AllowAnonymous]
Response 200: { habitId, date, checkedAt }

Response 400: hábito não encontrado, data futura, ou hábito não agendado nessa data
Response 403: hábito pertence a outro usuário (ou actionToken não confere)
```

> Não existe mais geração manual de instâncias para dias passados — a rotina do dia já é reconstruída na leitura.

---

## CRUD de Hábitos

```
GET    /habits              → lista todos os hábitos do usuário
GET    /habits/{id}         → detalhe de um hábito com seus schedules
POST   /habits              → cria hábito
PUT    /habits/{id}         → atualiza nome/emoji/descrição
DELETE /habits/{id}         → remove hábito e seus schedules/checks (hard delete, cascade)
PATCH  /habits/{id}/toggle  → alterna Habit.IsActive
```

### Body do POST /habits
```json
{
  "name": "Academia",
  "emoji": "🏋️",
  "description": "Treino de musculação",
  "syncGoogleCalendar": true,
  "schedules": [
    { "dayOfWeek": "Monday",    "startTime": "07:00", "endTime": "08:00", "isActive": true },
    { "dayOfWeek": "Wednesday", "startTime": "07:00", "endTime": "08:00", "isActive": true }
  ]
}
```

> `schedules` é obrigatório na criação. **No máximo um schedule por `DayOfWeek`** (validado). Não há mais `startToday`: se um schedule cair hoje, o hábito já aparece na timeline de hoje automaticamente.

### Body do PUT /habits/{id}
```json
{
  "name": "Academia",
  "emoji": "🏋️",
  "description": "Treino de musculação",
  "syncGoogleCalendar": true
}
```

## CRUD de HabitSchedules

```
GET    /habits/{id}/schedules                         → lista schedules do hábito
PUT    /habits/{id}/schedules                         → substitui todos os schedules (upsert por DayOfWeek)
PATCH  /habits/{id}/schedules/{dayOfWeek}/toggle      → alterna HabitSchedule.IsActive
```

### Body do PUT /habits/{id}/schedules
```json
{
  "schedules": [
    { "dayOfWeek": "Monday",    "startTime": "07:00", "endTime": "07:30", "isActive": true },
    { "dayOfWeek": "Wednesday", "startTime": "07:00", "endTime": "07:30", "isActive": true }
  ]
}
```


