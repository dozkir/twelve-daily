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

---

## Testes do Front-End (`apps/client`) — *planejado, ainda não implementado*

> **Estado atual:** o cliente Expo **não tem nenhum test runner**. Lógica de auth,
> hooks de dados e schemas Zod só são validados por `tsc` (typecheck) — não há teste
> de comportamento. A verificação hoje é `npx tsc --noEmit` + `expo lint`
> (ver [`apps/client/CLAUDE.md`](../../apps/client/CLAUDE.md)).

### Infra proposta
- **Runner:** [`jest-expo`](https://docs.expo.dev/develop/unit-testing/) (preset oficial do Expo) — alinha o Jest ao transform/resolução do RN/Expo.
- **DOM/componentes:** `@testing-library/react-native` para hooks e telas.
- **Rede:** mockar o axios do `@twelve-daily/api-client` (ex.: `axios-mock-adapter` ou `msw`) — testar o contrato, não a API real.
- **Storage nativo:** mockar `expo-secure-store` e `@react-native-async-storage/async-storage`.
- Script `test` no `package.json` do workspace + entrada no `turbo.json` (`turbo run test`).

### O que priorizar (maior risco / menor cobertura hoje)
| Alvo | Por quê |
|---|---|
| **Refresh-and-retry no 401** (`packages/api-client/src/http/mutator.ts` + `auth-context.tsx`) | Lógica de sessão com concorrência (single-flight) e rotação de refresh token; regressão aqui derruba o usuário pro login. Cobrir: 401 → renova → re-tenta; refresh falho → `onUnauthorized`; N requisições concorrentes → **um** único refresh; não re-tentar rotas `/auth/*`. |
| **Schemas Zod dos forms** (`src/habits/habit-form-values.ts`) | Regras de validação puras, fáceis de testar e fáceis de quebrar. |
| **Hooks de dados por feature** (`src/<feature>/queries.ts`) | Garantir query keys corretas e invalidação de cache esperada, com o api-client mockado. |

> Telas (`app/**`) são finas por convenção — teste de UI é baixa prioridade; o valor
> está na lógica dos hooks/contexto e nos schemas.

