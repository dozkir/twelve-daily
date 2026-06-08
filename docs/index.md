# Twelve Daily — Índice da Documentação

## Visão Geral
Aplicação de rastreamento de hábitos diários focada em rotina pessoal.
- API REST que alimenta clientes **web** e **mobile**
- Notificações push agendadas por hábito/horário
- Atualizações em tempo real via SignalR

---

## Stack Tecnológica

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
| Auth | JWT (Access Token) + Refresh Token |
| Push | Expo Push Notifications |
| Deploy | Fly.io (API + DB) + Azure Static Web Apps (Web) |
| Front-end | Expo (Managed Workflow) + TypeScript |
| Monorepo | Turborepo |
| Geração de tipos | orval (gera tipos TS + hooks TanStack Query a partir do OpenAPI) |

---

## Documentação

| Documento | Conteúdo |
|---|---|
| [summary.md](summary.md) | Resumo do projeto — o que é, como funciona |

### Domínio
| Documento | Conteúdo |
|---|---|
| [domain/entities.md](domain/entities.md) | Entidades, campos, tipos e relações |
| [domain/rules.md](domain/rules.md) | Regras de negócio e separação Plano × Realidade |
| [domain/flows.md](domain/flows.md) | Fluxos do sistema (geração de instâncias, check, etc.) |

### Especificações de Feature
| Documento | Conteúdo |
|---|---|
| [specs/auth.md](specs/auth.md) | Autenticação, JWT, Refresh Token |
| [specs/habit-check-refactor.md](specs/habit-check-refactor.md) | RFC da mudança conceitual: hábitos, checks, timeline, notificações e dashboard |
| [specs/habits.md](specs/habits.md) | Hábitos, schedules, instâncias, navegação por dias |
| [specs/notifications.md](specs/notifications.md) | Push notifications e real-time (SignalR) |
| [specs/google-calendar.md](specs/google-calendar.md) | Integração com Google Calendar |
| [specs/dashboard.md](specs/dashboard.md) | Dashboard semanal |

### Arquitetura
| Documento | Conteúdo |
|---|---|
| [architecture/backend.md](architecture/backend.md) | Clean Architecture, CQRS, estrutura do repositório |
| [architecture/frontend.md](architecture/frontend.md) | Expo, orval, NativeWind, especificação de telas |
| [architecture/testing.md](architecture/testing.md) | TDD, testes unitários e de integração |

### Infraestrutura
| Documento | Conteúdo |
|---|---|
| [infrastructure/hosting.md](infrastructure/hosting.md) | Hospedagem, stack gratuita, banco de dados |
| [infrastructure/cicd.md](infrastructure/cicd.md) | CI/CD com GitHub Actions |
| [infrastructure/containers.md](infrastructure/containers.md) | Docker, docker-compose |

### Desenvolvimento
| Documento | Conteúdo |
|---|---|
| [development.md](development.md) | Guia de setup e execução local |
| [push-notifications-implementation.md](push-notifications-implementation.md) | Passo a passo de implementação das notificações push persistentes |
| [push-notifications-file-breakdown.md](push-notifications-file-breakdown.md) | Breakdown técnico por projeto/arquivo para implementar push notifications |
| [roadmap.md](roadmap.md) | Roadmap de desenvolvimento (fases e checklist) |

