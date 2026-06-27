# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Twelve Daily is a daily-habit tracker: a .NET REST API drives Expo web + mobile clients, with scheduled push notifications (SignalR real-time updates are planned but not yet implemented). Detailed docs live in `docs/` (written in Portuguese); start at `docs/index.md`. `docs/claude.md` is a consolidated quick-reference.

## Per-app conventions (nested CLAUDE.md)

Each app carries its own conventions, loaded when you work in that directory:

- **`apps/api/CLAUDE.md`** — backend: Clean Architecture + CQRS, clean-code rules (centralized ownership guard, domain exceptions → HTTP, injectable time), how to build/test via the SDK container.
- **`apps/client/CLAUDE.md`** — frontend: feature-based layout, thin screens, data hooks per feature (`src/<feature>/queries.ts`), centralized query keys, styling, orval usage, i18n.

## Documentation maintenance (always)

Treat the docs as part of the code. When you change architecture, domain rules, contracts or conventions, **review and update the affected docs in the same change** so they don't go stale or get silently ignored — `docs/architecture/*`, `docs/domain/*`, `docs/specs/*`, `docs/CLAUDE.md`, `docs/index.md`, and the relevant `CLAUDE.md`. If a doc contradicts the code, fix the doc (or mark it historical, as `docs/roadmap.md` does) rather than leaving it divergent. (`docs/infrastructure/deployment.md` is a local, gitignored working doc — do not commit it.)

## Monorepo layout

Turborepo at the root coordinates the JS/TS workspaces (`packages/*`); the .NET solution is standalone under `apps/api`.

```
apps/
  api/      ← .NET solution (TwelveDaily.slnx) — Clean Architecture, 6 projects
  client/   ← Expo app (iOS, Android, Web) — @twelve-daily/client
packages/
  api-client/  ← orval-GENERATED TS client + TanStack Query hooks — never edit by hand
docs/       ← architecture, domain, specs, infra (Portuguese)
docker-compose.yml, turbo.json, orval.config.ts, global.json
```

## Commands

Run JS/TS tasks from the repo root (turbo fans out to workspaces); run .NET commands from `apps/api`.

```bash
# Backend (.NET 10, pinned in global.json)
cd apps/api
dotnet build
dotnet test                                   # all tests
dotnet test TwelveDaily.UnitTests             # unit only (no Docker needed)
dotnet test TwelveDaily.IntegrationTests      # needs Docker — TestContainers spins up Postgres
dotnet test --filter "FullyQualifiedName~CreateHabit"   # a single test / class

# EF Core migrations (always target Infrastructure with Api as startup)
dotnet ef migrations add <Name> --project TwelveDaily.Infrastructure --startup-project TwelveDaily.Api
dotnet ef database update     --project TwelveDaily.Infrastructure --startup-project TwelveDaily.Api

# Local run: Postgres + API in containers
docker compose up -d          # API on http://localhost:5000, Postgres on 5432

# Client (from apps/client)
npx expo start                # mobile via Expo Go QR
npx expo start --web          # http://localhost:8081

# Root / turbo
npm run lint                  # turbo run lint   (client: expo lint)
npm run typecheck             # turbo run typecheck
npm run build
npm run api:generate          # regenerate packages/api-client from the running API's OpenAPI
```

## The API → client contract pipeline

This is the seam that ties the two stacks together and is easy to get wrong:

1. C# controllers/DTOs are exposed as OpenAPI/Swagger (Swashbuckle).
2. `orval` (config: `orval.config.ts`) reads the running API's swagger (`http://localhost:5275/swagger/v1/swagger.json` by default, overridable via `ORVAL_OPENAPI_URL`) and generates `packages/api-client/src/generated/`.
3. The client imports typed hooks from `@twelve-daily/api-client`.

**After changing any C# DTO or endpoint, regenerate the client** (`npm run api:generate`) — the API must be running for orval to read the schema. Never hand-edit `packages/api-client/`; it is overwritten.

## Backend architecture (Clean Architecture + CQRS)

Dependencies always point inward toward `Domain`. See `docs/architecture/backend.md`.

```
Api ─► Application ─► Domain
Infrastructure ─► Application ─► Domain
```

- **Domain** — entities, value objects, repository *interfaces*, domain exceptions. Knows nothing about EF, HTTP, or Hangfire.
- **Application** — MediatR Commands/Queries + Handlers, FluentValidation validators, external-service interfaces (`ITokenService`, `IPushNotificationService`, `IGoogleCalendarService`), DTOs.
- **Infrastructure** — EF Core `DbContext` + repository implementations, migrations, JWT/refresh-token, Hangfire jobs, Expo push, Google Calendar OAuth2.
- **Api** — thin controllers that only delegate to MediatR, SignalR hubs, `Program.cs` DI/middleware/Swagger.

Non-negotiable code rules:
- **CQRS is mandatory** — controllers call MediatR, never repositories directly.
- **Validation lives in the MediatR pipeline** (FluentValidation behavior) — handlers do not validate manually.
- **No speculative abstractions** — don't add an interface for something with a single implementation.
- **TDD** — every use case gets at least one integration test; complex domain rules get unit tests.

## Domain model & business rules

The model separates **Plan** (`Habit` + `HabitSchedule`, the recurring intent) from **Reality** (`HabitCheck`, a record that a habit was completed on a date). The daily routine is **not materialized** — timeline and dashboard are reconstructed from `Habit` + `HabitSchedule` + `HabitCheck` on read. See `docs/domain/` and `docs/specs/habit-check-refactor.md`.

- Core entities: `User`, `RefreshToken`, `PushToken`, `Habit`, `HabitSchedule`, `HabitCheck`, `GoogleConnection`.
- `HabitCheck.UserId` is **intentionally denormalized** (avoids a JOIN on per-user queries). Logical occurrence identity = `(HabitId, Date)`, **one check per habit per day** (unique index). Undoing a check **deletes** the row.
- `HabitCheck` stores a **snapshot** (name, emoji, schedule times) at check time for historical fidelity: completed days render from the snapshot; uncompleted days are reconstructed from current state.
- **No instance generation.** A habit appears on a day if `Habit.CreatedAt` ≤ that date and it has an active `HabitSchedule` for that `DayOfWeek` (at most **one schedule per weekday per habit**).
- **Checking a future day is forbidden.** Inactive habits are paused (hidden from today/future) but **still count in the past**. Deleting a habit is a hard delete (cascades schedules + checks).
- Push notifications: the orchestrator computes the next eligible occurrence from schedules + checks + timezone and **self-schedules** the next Hangfire recompute (no per-instance jobs); the action token carries `(UserId, HabitId, Date)`. Google Calendar sync is **deferred** in this phase.
- Timestamps are stored in **UTC**; per-user local time comes from an **IANA timezone ID**. `HabitSchedule` times are local.
- **Total isolation between users**, no roles — user A can never reach user B's data (covered by integration tests).

## Frontend

Expo (managed workflow) + TypeScript, Expo Router for navigation, TanStack Query (via orval) for server state, React Hook Form + Zod for forms, React Native `StyleSheet` + a central theme (`src/theme.ts`) for styling, `expo-notifications`/`@notifee/react-native` for push. Real-time via `@microsoft/signalr` is planned but not yet wired up. See `docs/architecture/frontend.md`.

## Note on the in-progress restructure

The `generalRework` branch is migrating the original single-project `TwelveDaily.Core` / `twelve-daily.sln` into the `apps/api` Clean-Architecture solution above. Work against `apps/api`, `apps/client`, and `packages/`; the old `TwelveDaily.Core`, `TwelveDaily.Test`, and root `twelve-daily.sln` are being removed.
