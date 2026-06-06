# Arquitetura — Backend

## Estilo Arquitetural
- **Clean Architecture** — dependências apontam sempre para o centro (Domain)
- **CQRS** com MediatR — Commands, Queries e Handlers
- **Validação centralizada** via FluentValidation no pipeline do MediatR
- Sem Event Sourcing
- Sem abstrações desnecessárias

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
- Implementação Expo Push Notifications
- Implementação Google Calendar API (OAuth2 + criação de eventos)

### TwelveDaily.Api
- Controllers (thin — apenas delegam para MediatR)
- SignalR Hubs *(o Hub fica na Api — é parte da interface de entrada, não de infraestrutura)*
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

