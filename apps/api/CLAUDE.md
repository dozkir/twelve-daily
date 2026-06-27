# CLAUDE.md — Backend (`apps/api`)

Convenções específicas da solução .NET (`TwelveDaily.slnx`). Complementam o
`CLAUDE.md` da raiz; em caso de conflito, a raiz prevalece. Detalhes de arquitetura
em [`docs/architecture/backend.md`](../../docs/architecture/backend.md).

## Padrões de projeto em uso (e por quê)

| Padrão | Onde | Por que existe |
|---|---|---|
| **Clean Architecture** | 4 projetos (`Domain` ← `Application` ← `Infrastructure`/`Api`) | Regra de negócio independente de framework/banco; dependências apontam pra dentro |
| **CQRS** (MediatR) | `Application/*/Commands`, `Queries`, `Handlers` | Separa intenção (comando/consulta) da execução; controller fica fino |
| **Pipeline Behavior** | `Application/Behaviors/ValidationBehavior.cs` | Validação como cross-cutting concern — roda antes de todo handler |
| **Repository** | `Domain/Interfaces` (contrato) + `Infrastructure/Repositories` (EF) | Domínio define o contrato; EF é detalhe substituível |
| **Domain Model rico** | `Domain/Entities` | Invariantes no construtor/métodos (ex.: `Habit.cs`), setters privados |
| **Options** | `Infrastructure/Services/PushNotificationsOptions.cs` | Configuração tipada |

## Regras inegociáveis (já valem hoje)

- **CQRS obrigatório** — controllers só chamam `_mediator.Send(...)`; nunca tocam repositório.
- **Validação só no pipeline** — `FluentValidation` + `ValidationBehavior`. Handlers **não** validam formato de entrada manualmente.
- **Domain puro** — sem `using` de EF Core, Npgsql, ASP.NET ou Hangfire em `Domain`/`Application` (verificável por `grep`).
- **Sem abstração especulativa** — não criar interface para algo com uma única implementação.
- **TDD** — todo caso de uso ganha ≥1 teste de integração; regra de domínio complexa ganha teste unitário. Mensagens de exceção e contratos cobertos por teste.

## Convenções de clean code (derivadas do código atual)

- **Guard de propriedade centralizado** — para "carregar hábito + checar existência + checar dono", use a extensão `IHabitRepository.GetOwnedAsync(habitId, userId, ct)` (`Application/Common/HabitRepositoryExtensions.cs`). **Não** reescreva o trio `GetByIdAsync` + `null` + `UserId !=` nos handlers — isso é o isolamento entre usuários e mora num lugar só.
- **Exceções de domínio** comunicam intenção HTTP: `DomainException` (404/422), `ForbiddenException : DomainException` (403), `ConflictException` (409), `UnauthorizedException` (401). O `GlobalExceptionHandler` faz o mapeamento — handlers só lançam.
- **Sem parâmetro morto/enganoso** — não passe valores que o handler ignora. Ex.: o fuso vem de `User.Timezone` (resolvido por `UserClock`), não de input do cliente.
- **Tempo é injetável** — use `IDateTimeProvider`, nunca `DateTime.UtcNow` direto em handler (permite teste determinístico).
- **Nomes**: handlers `<Caso>Handler`; comandos/queries `<Caso>Command`/`<Caso>Query`; resultados `<Caso>Result`. Um caso de uso por par command/handler.
- **Validadores consistentes** — comandos que carregam `UserId`/`HabitId` validam `.NotEmpty()`.

## Fluxo ao adicionar/alterar um caso de uso

1. Command/Query (record `: IRequest<...>`) em `Application/<feature>/Commands|Queries`.
2. Validator (se houver entrada a validar) em `Validators` — registrado automaticamente.
3. Handler em `Handlers` — orquestra domínio + repositórios; usa `GetOwnedAsync` quando precisar de um hábito do usuário.
4. Endpoint fino no controller, delegando ao MediatR.
5. Teste (unitário do handler/validator e/ou integração do endpoint) **antes** de fechar.
6. Se mudou DTO/endpoint: regenerar o api-client (`npm run api:generate`, com a API no ar).

## Build & teste (host sem o SDK 10 fixado)

O host não tem o .NET 10 do `global.json`. Compile/teste num container do SDK (PowerShell):

```powershell
docker run --rm -v "<repo>:/src" -w /src/apps/api mcr.microsoft.com/dotnet/sdk:10.0.102 `
  sh -c "dotnet build TwelveDaily.slnx -c Release && dotnet test TwelveDaily.UnitTests -c Release --no-build"
```

Integração precisa do socket do Docker (TestContainers) — ver memória do projeto.

## Manutenção da documentação (obrigatório)

Ao mudar arquitetura, regras de domínio, contratos ou convenções deste backend,
**revise e atualize na mesma alteração** os documentos relacionados, para que não
fiquem obsoletos nem sejam ignorados:

- [`docs/architecture/backend.md`](../../docs/architecture/backend.md) — estilo, camadas, regras de código.
- [`docs/architecture/testing.md`](../../docs/architecture/testing.md) — estratégia de testes.
- [`docs/domain/`](../../docs/domain/) e [`docs/specs/`](../../docs/specs/) — entidades, regras e specs afetadas.
- [`docs/CLAUDE.md`](../../docs/CLAUDE.md) e [`docs/index.md`](../../docs/index.md) — referência rápida e índice.
- Este arquivo e o `CLAUDE.md` da raiz, quando a convenção em si mudar.

Se uma doc contradisser o código, corrija a doc (ou marque-a como histórica, como
em `roadmap.md`) — não a deixe divergente em silêncio.
