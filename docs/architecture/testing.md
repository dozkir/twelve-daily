# Arquitetura — Testes

## Metodologia
**TDD (Test-Driven Development)** — testes escritos antes da implementação.

```
Red   → escrever teste que falha
Green → implementar o mínimo para passar
Refactor → melhorar o código mantendo os testes verdes
```

---

## Testes Unitários (`TwelveDaily.UnitTests`)

Testam lógica isolada, sem dependências externas (banco, HTTP, clock).

### O que testar
| Camada | O que |
|---|---|
| Domain | Construtores, regras de criação, exceções de domínio |
| Handlers | Lógica de Commands/Queries com repositórios mockados |
| Validators | Regras de validação do FluentValidation |

### Estrutura atual
```
TwelveDaily.UnitTests/
  Domain/
    UserTests.cs
    HabitTests.cs
    HabitScheduleTests.cs
    HabitCheckTests.cs
    RefreshTokenTests.cs
  Handlers/
    AuthHandlerTests.cs
    HabitHandlerTests.cs
    ScheduleHandlerTests.cs
    CheckHandlerTests.cs
    QueryHandlerTests.cs
    UserHandlerTests.cs
  Validators/
    AuthValidatorTests.cs
    HabitValidatorTests.cs
```

---

## Testes de Integração (`TwelveDaily.IntegrationTests`)

Testam o endpoint completo — do HTTP até o banco de dados real.

### Infraestrutura
- **TestContainers** (`Testcontainers.PostgreSql`) — sobe container PostgreSQL programaticamente
- **`WebApplicationFactory`** — sobe a API em memória com o banco real
- Container PostgreSQL criado uma vez por suite e descartado ao final
- Sem docker-compose externo — funciona localmente e no GitHub Actions (que tem Docker)

### O que testar
- Fluxo completo de autenticação (register → login → refresh → logout)
- CRUD de hábitos e schedules
- Check/uncheck de hábitos `(HabitId, Date)` — incluindo a proibição de data futura
- Timeline reconstruída (range D-3 a D+3, tipos past/today/future)
- Isolamento entre usuários (usuário A não acessa dados do usuário B)

### Estrutura atual
```
TwelveDaily.IntegrationTests/
  IntegrationTestBase.cs       ← setup do TestContainers + WebApplicationFactory
  Auth/
    RegisterTests.cs
    LoginTests.cs
    TokenTests.cs
  Habits/
    HabitCrudTests.cs
    ScheduleTests.cs
    CheckTests.cs
    TimelineTests.cs
  Dashboard/
    DashboardTests.cs
  Isolation/
    UserIsolationTests.cs
  Profile/
    ProfileTests.cs
```

---

## Cobertura
- Não há meta numérica de cobertura — foco em **comportamento**, não em percentual
- Todo caso de uso deve ter ao menos um teste de integração
- Regras de domínio complexas devem ter testes unitários dedicados

