# Domínio — Entidades

## User
| Campo | Tipo | Descrição |
|---|---|---|
| Id | UUID | Identificador único |
| Email | string | Único, usado no login |
| PasswordHash | string | Senha com hash |
| Timezone | string | IANA Timezone ID (ex: `"America/Sao_Paulo"`) |
| CreatedAt | DateTime (UTC) | |
| UpdatedAt | DateTime (UTC) | |

---

## RefreshToken
| Campo | Tipo | Descrição |
|---|---|---|
| Id | UUID | |
| UserId | UUID | FK → User |
| Token | string | Token opaco gerado aleatoriamente |
| ExpiresAt | DateTime (UTC) | Validade de 7 dias |
| CreatedAt | DateTime (UTC) | |
| RevokedAt | DateTime (UTC) nullable | Preenchido ao revogar |

---

## Habit
| Campo | Tipo | Descrição |
|---|---|---|
| Id | UUID | |
| UserId | UUID | FK → User |
| Name | string | Nome do hábito |
| Emoji | string | Emoji representativo (ex: `"🏋️"`) |
| Description | string nullable | Descrição opcional |
| IsActive | bool | Toggle geral do hábito. Inativo = pausado (sem timeline/notificação no presente/futuro); o passado continua contando |
| SyncGoogleCalendar | bool | Reservado para sincronização com Google Calendar (**adiada** nesta fase — ver [habit-check-refactor](../specs/habit-check-refactor.md) §10.4) |
| CreatedAt | DateTime (UTC) | Define a partir de quando o hábito passa a contar nos dias passados |
| UpdatedAt | DateTime (UTC) | |

---

## HabitSchedule
| Campo | Tipo | Descrição |
|---|---|---|
| Id | UUID | |
| HabitId | UUID | FK → Habit |
| DayOfWeek | enum | Segunda a Domingo |
| StartTime | TimeOnly **(local do usuário)** | Horário de início no fuso do usuário |
| EndTime | TimeOnly **(local do usuário)** | Horário de fim no fuso do usuário |
| IsActive | bool | Toggle do dia — desativa sem deletar o registro |
| CreatedAt | DateTime (UTC) | |
| UpdatedAt | DateTime (UTC) | |

> Um hábito que ocorre segunda e quarta terá **2 registros** de `HabitSchedule` — um por dia da semana.
> **Invariante:** no máximo **um** schedule por dia da semana por hábito. Dois horários no mesmo dia = dois hábitos distintos (imposto na validação).
> `StartTime`/`EndTime` são armazenados no **horário local do usuário**. A conversão para UTC ocorre **na leitura** (timeline, notificações) usando `User.Timezone` — não há mais materialização prévia.

---

## HabitCheck

Registro de que um hábito foi **concluído** numa data. Substitui o antigo `HabitInstance`: a rotina diária não é mais materializada; a timeline/dashboard são reconstruídos de hábito + schedule + checks. Identidade lógica da ocorrência = `(HabitId, Date)` — **um check por hábito por dia** (índice único). Ver [habit-check-refactor](../specs/habit-check-refactor.md).

| Campo | Tipo | Descrição |
|---|---|---|
| Id | UUID | |
| HabitId | UUID | FK → Habit (cascade delete) |
| UserId | UUID | FK → User *(desnormalizado intencionalmente — evita JOIN ao buscar por usuário)* |
| Date | DateOnly **(data local do usuário)** | Data a que o check pertence |
| CheckedAt | DateTime (UTC) | Momento em que foi marcado |
| HabitName | string | **Snapshot** do nome no momento do check (fidelidade histórica) |
| HabitEmoji | string | **Snapshot** do emoji no momento do check |
| StartTime | TimeOnly (local) | **Snapshot** do horário de início do schedule daquele dia |
| EndTime | TimeOnly (local) | **Snapshot** do horário de fim do schedule daquele dia |

> **Desfazer check** = deletar o registro. O estado "não concluído" é a **ausência** de check.
> Dias concluídos renderizam a partir do snapshot; dias não concluídos são reconstruídos do estado atual do hábito/schedule (reconstrução aproximada).

---

## PushToken
| Campo | Tipo | Descrição |
|---|---|---|
| Id | UUID | |
| UserId | UUID | FK → User |
| Token | string | `ExponentPushToken[...]` recebido pelo dispositivo |
| DeviceLabel | string nullable | Identificação opcional do dispositivo (ex: "iPhone do Rafael") |
| CreatedAt | DateTime (UTC) | |
| UpdatedAt | DateTime (UTC) | Atualizado ao re-registrar o mesmo dispositivo |

> Um usuário pode ter múltiplos `PushToken` (um por dispositivo). Push notifications são enviadas para **todos** os tokens ativos do usuário.

---

## GoogleConnection
| Campo | Tipo | Descrição |
|---|---|---|
| Id | UUID | |
| UserId | UUID | FK → User (unique — um por usuário) |
| AccessToken | string | Token OAuth2 do Google (criptografado) |
| RefreshToken | string | Refresh token do Google (criptografado) |
| ExpiresAt | DateTime (UTC) | Expiração do access token |
| CalendarId | string | ID do calendário do Google (default: `"primary"`) |
| CreatedAt | DateTime (UTC) | |
| UpdatedAt | DateTime (UTC) | |

> Credenciais obtidas via fluxo OAuth2 do Google. O usuário conecta a conta Google uma vez na tela de configurações.
> Tokens armazenados **criptografados** no banco — nunca em plain text.

---

## Fuso Horário

- Todos os valores de data/hora são armazenados em **UTC** no banco
- `User.Timezone` contém um **IANA Timezone ID** — trata horário de verão automaticamente
- Offset numérico fixo **não** é suficiente (ex: Brasil não tem horário de verão, mas Portugal tem)
- Conversão no .NET: `TimeZoneInfo.FindSystemTimeZoneById(user.Timezone)`
- Conversão no front: biblioteca `date-fns-tz` ou `Intl.DateTimeFormat`

### Exemplos de IANA IDs
| Localidade | IANA ID |
|---|---|
| Brasil (Brasília) | `America/Sao_Paulo` |
| Portugal | `Europe/Lisbon` |
| EUA (Nova York) | `America/New_York` |

---

## Diagrama de Relações

```
User 1 ──── N RefreshToken
User 1 ──── N PushToken
User 1 ──── 1 GoogleConnection  (opcional)
User 1 ──── N Habit
Habit 1 ──── N HabitSchedule  (um por DayOfWeek)
Habit 1 ──── N HabitCheck     (no máximo um por data)
User 1 ──── N HabitCheck      (desnormalizado)
```

