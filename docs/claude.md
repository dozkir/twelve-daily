# Twelve Daily — Referência Rápida

> Documento de referência consolidada. Para detalhes, consulte os documentos em `docs/`.
> Índice completo: [docs/index.md](index.md)

---

## Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 10 / C# 13 |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Banco | PostgreSQL (Docker local / Fly.io Postgres em produção) |
| CQRS | MediatR |
| Validação | FluentValidation + MediatR Pipeline Behavior |
| Jobs | Hangfire + Hangfire.PostgreSql |
| Real-time | SignalR |
| Auth | JWT 15min + Refresh Token 7 dias |
| Push | Expo Push Notifications |
| Integração | Google Calendar API (OAuth2) |
| Deploy | Fly.io (API + DB) + Azure Static Web Apps (Web) |
| Front-end | Expo (Managed Workflow) + TypeScript |
| Monorepo | Turborepo |
| Geração de tipos | orval (OpenAPI → tipos TS + hooks TanStack Query) |

---

## Entidades

- **User** — Id, Email, PasswordHash, Timezone (IANA), CreatedAt, UpdatedAt
- **RefreshToken** — Id, UserId, Token, ExpiresAt, CreatedAt, RevokedAt
- **PushToken** — Id, UserId, Token, DeviceLabel?, CreatedAt, UpdatedAt
- **Habit** — Id, UserId, Name, Emoji, Description?, IsActive, SyncGoogleCalendar, CreatedAt, UpdatedAt
- **HabitSchedule** — Id, HabitId, DayOfWeek, StartTime (local), EndTime (local), IsActive *(no máx. 1 por dia da semana por hábito)*
- **HabitCheck** — Id, HabitId, UserId*, Date (local), CheckedAt (UTC), + snapshot: HabitName, HabitEmoji, StartTime, EndTime *(1 por hábito por dia)*
- **GoogleConnection** — Id, UserId, AccessToken (enc), RefreshToken (enc), ExpiresAt, CalendarId

*UserId desnormalizado intencionalmente — evita JOIN ao buscar por usuário.

---

## Regras Essenciais

- Sem limite de hábitos por usuário
- **Sem geração de instâncias** — timeline/dashboard reconstruídos de hábito + schedule + check na leitura
- Check = `HabitCheck (HabitId, Date)`, upsert idempotente; desfazer = deletar
- **Check proibido em dias futuros**; dias passados só contam se o hábito já existia (`CreatedAt`)
- Hábito inativo = pausado (some de hoje/futuro); passado continua contando
- Deletar hábito = hard delete com cascade (hábito + schedules + checks)
- UTC no banco — fuso via IANA Timezone ID por usuário
- Isolamento total entre usuários, sem roles
- Google Calendar **adiado** nesta fase

---

## Estrutura do Repositório

```
/ (raiz)
  global.json / docker-compose.yml / .env.example
  apps/
    api/                  ← solução .NET
      TwelveDaily.slnx
      TwelveDaily.Api/
      TwelveDaily.Application/
      TwelveDaily.Domain/
      TwelveDaily.Infrastructure/
      TwelveDaily.UnitTests/
      TwelveDaily.IntegrationTests/
      Dockerfile
    client/               ← Expo (iOS, Android, Web)
  packages/
    api-client/           ← gerado pelo orval (nunca editar manualmente)
```

---

## Regras de Código

- CQRS obrigatório
- Validação via FluentValidation no pipeline do MediatR
- Evitar abstrações desnecessárias
- TDD
- Não assumir acesso root
