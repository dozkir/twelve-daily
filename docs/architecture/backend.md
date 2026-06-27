# Arquitetura — Backend

> 📌 As **convenções operacionais e de clean code** deste backend (o que fazer ao
> adicionar um caso de uso, como buildar/testar, padrões de nomenclatura) vivem em
> [`apps/api/CLAUDE.md`](../../apps/api/CLAUDE.md). Este documento descreve a
> arquitetura; o `CLAUDE.md` descreve como trabalhar dentro dela.

## Estilo Arquitetural
- **Clean Architecture** — dependências apontam sempre para o centro (Domain)
- **CQRS** com MediatR — Commands, Queries e Handlers
- **Validação centralizada** via FluentValidation no pipeline do MediatR
- Sem Event Sourcing
- Sem abstrações desnecessárias

### Padrões de projeto (design patterns) em uso

| Padrão | Onde no código | Papel |
|---|---|---|
| **Clean Architecture / Ports & Adapters** | separação `Domain`/`Application`/`Infrastructure`/`Api` | regra de negócio isolada de framework e banco |
| **CQRS** (Mediator) | `Application/*/Commands`, `Queries`, `Handlers` (MediatR) | separa intenção da execução; controller fino |
| **Pipeline / Decorator** | `Application/Behaviors/ValidationBehavior.cs` | validação como cross-cutting, antes do handler |
| **Repository** | `Domain/Interfaces` (contrato) + `Infrastructure/Repositories` (EF) | domínio define contrato; EF é detalhe |
| **Domain Model rico** | `Domain/Entities` | invariantes no construtor/métodos, setters privados |
| **Extension method** (helper de acesso) | `Application/Common/HabitRepositoryExtensions.GetOwnedAsync` | centraliza o guard "existe + pertence ao usuário" |
| **Options** | `Infrastructure/Services/PushNotificationsOptions.cs` | configuração tipada |

## Direção das Dependências

```
TwelveDaily.Api
    └── TwelveDaily.Application
            └── TwelveDaily.Domain

TwelveDaily.Infrastructure
    └── TwelveDaily.Application
            └── TwelveDaily.Domain
```

> `Domain` não conhece banco, HTTP ou Hangfire.
> `Application` não conhece PostgreSQL, EF Core ou qualquer tecnologia concreta.
> `Infrastructure` implementa as interfaces definidas em `Application`.

---

## Projetos .NET

### TwelveDaily.Domain
- Entidades (`User`, `Habit`, `HabitSchedule`, `HabitCheck`, `RefreshToken`)
- Value Objects (se necessário)
- Interfaces de repositório *(contratos, sem implementação)*
- Exceções de domínio

### TwelveDaily.Application
- Commands e Queries (MediatR)
- Handlers
- Validators (FluentValidation)
- Interfaces de serviços externos (ex: `IPushNotificationService`, `ITokenService`, `IGoogleCalendarService`)
- DTOs de entrada e saída

### TwelveDaily.Infrastructure
- Implementações de repositório (EF Core)
- `AppDbContext`
- Migrations
- Implementação JWT + Refresh Token
- Implementação Hangfire (jobs, scheduling)
- Implementação Expo Push Notifications (orquestrador + job runner + action token)
- Implementação Google Calendar API (OAuth2) — **adiada**: existe apenas a interface `IGoogleCalendarService` em `Application`, sem implementação concreta ainda (ver [habit-check-refactor](../specs/habit-check-refactor.md) §10.4)

### TwelveDaily.Api
- Controllers (thin — apenas delegam para MediatR)
- SignalR Hubs *(planejado — `AddSignalR()` já está em `Program.cs`, mas nenhum Hub foi mapeado ainda; quando criado, fica na Api — é parte da interface de entrada, não de infraestrutura)*
- `Program.cs` (configuração de DI, middlewares, Swagger)
- `appsettings.json`

### TwelveDaily.UnitTests
- Testes de Domain
- Testes de Handlers
- Testes de Validators

### TwelveDaily.IntegrationTests
- Testes de endpoints via `WebApplicationFactory`
- Banco real via TestContainers

---

## Estrutura do Repositório (Monorepo)

```
/ (raiz)
  turbo.json
  package.json
  docker-compose.yml
  .github/
    workflows/
  apps/
    client/                   ← Expo (iOS, Android, Web)
    api/                      ← solução .NET
      TwelveDaily.slnx
      TwelveDaily.Api/
      TwelveDaily.Application/
      TwelveDaily.Domain/
      TwelveDaily.Infrastructure/
      TwelveDaily.UnitTests/
      TwelveDaily.IntegrationTests/
  packages/
    api-client/               ← gerado pelo orval
```

---

## Regras de Código
- CQRS obrigatório — nenhum Controller chama repositório diretamente
- Validação via FluentValidation no pipeline do MediatR — nenhum Handler valida manualmente
- Evitar abstrações desnecessárias — não criar interfaces para coisas que só terão uma implementação
- TDD — testes escritos antes da implementação

### Convenções de clean code

- **Isolamento entre usuários em um só lugar** — carregar um hábito do usuário usa
  `IHabitRepository.GetOwnedAsync(habitId, userId, ct)` (extensão em
  `Application/Common`). O guard `GetByIdAsync` + checagem de `null` + checagem de
  `UserId` **não** é reescrito por handler (era duplicado em 7 handlers).
- **Exceções de domínio carregam a semântica HTTP** — `DomainException` (404/422),
  `ForbiddenException : DomainException` (403), `ConflictException` (409),
  `UnauthorizedException` (401). O mapeamento para status fica no
  `Api/Middleware/GlobalExceptionHandler.cs`; handlers apenas lançam.
- **Tempo injetável** — handlers dependem de `IDateTimeProvider`, não de
  `DateTime.UtcNow` direto (testes determinísticos).
- **Sem parâmetro morto** — não trafegar valores que o handler ignora; o fuso, por
  exemplo, vem de `User.Timezone` via `UserClock`, não de input do cliente.
- **Nomenclatura** — `:<Caso>Command`/`<Caso>Query` → `<Caso>Handler` → `<Caso>Result`.

