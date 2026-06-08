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

### Exemplo de estrutura
```
TwelveDaily.UnitTests/
  Domain/
    HabitTests.cs
    HabitInstanceTests.cs
  Application/
    Habits/
      CreateHabitHandlerTests.cs
      CompleteHabitInstanceHandlerTests.cs
    Auth/
      LoginHandlerTests.cs
  Validators/
    CreateHabitValidatorTests.cs
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
- Check de instâncias (incluindo validações de data futura)
- Criação manual de instâncias em dias passados
- Isolamento entre usuários (usuário A não acessa dados do usuário B)

### Exemplo de estrutura
```
TwelveDaily.IntegrationTests/
  Auth/
    RegisterTests.cs
    LoginTests.cs
    RefreshTokenTests.cs
  Habits/
    CreateHabitTests.cs
    GetDailyHabitsTests.cs
    CompleteHabitInstanceTests.cs
  Infrastructure/
    IntegrationTestBase.cs   ← setup do TestContainers + WebApplicationFactory
```

---

## Cobertura
- Não há meta numérica de cobertura — foco em **comportamento**, não em percentual
- Todo caso de uso deve ter ao menos um teste de integração
- Regras de domínio complexas devem ter testes unitários dedicados

