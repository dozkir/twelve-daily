# Twelve Daily — Roadmap de Desenvolvimento

> ⚠️ **Documento histórico.** Este roadmap foi escrito sobre o modelo antigo de `HabitInstance` (geração de instâncias, `startToday`, check por instância). Esse modelo foi **substituído** pelo modelo de **check** `(HabitId, Date)` sem materialização — ver [habit-check-refactor](specs/habit-check-refactor.md). Itens que mencionam "instância", "gerar rotina", `startToday`, `CompletedAt` ou `HabitInstance` refletem o planejamento da época, não o código atual. Para o estado atual do domínio, consulte [domain/](domain/) e [specs/](specs/).

## Fases

```
📄 Documentação → 🔴 Testes Unitários → 🔴 Testes de Integração → 🟢 Back-End → 🎨 Front-End → 🚀 CI/CD
```

> Metodologia TDD: testes são escritos **antes** da implementação.
> Red → Green → Refactor em cada etapa.

---

## Fase 0 — Documentação ✅

- [x] Resumo do projeto (`summary.md`)
- [x] Entidades e relações (`domain/entities.md`)
- [x] Regras de negócio (`domain/rules.md`)
- [x] Fluxos do sistema (`domain/flows.md`)
- [x] Spec de autenticação (`specs/auth.md`)
- [x] Spec de hábitos (`specs/habits.md`)
- [x] Spec de notificações (`specs/notifications.md`)
- [x] Spec de Google Calendar (`specs/google-calendar.md`)
- [x] Spec de dashboard (`specs/dashboard.md`)
- [x] Arquitetura backend (`architecture/backend.md`)
- [x] Arquitetura frontend (`architecture/frontend.md`)
- [x] Arquitetura de testes (`architecture/testing.md`)
- [x] Hospedagem (`infrastructure/hosting.md`)
- [x] CI/CD (`infrastructure/cicd.md`)
- [x] Containers (`infrastructure/containers.md`)
- [x] Guia de desenvolvimento (`development.md`)

---

## Fase 1 — Estrutura do Projeto ✅

Preparar o repositório monorepo antes de escrever qualquer teste.

- [x] Reestruturar repositório para layout monorepo (`apps/api/`, `apps/client/`, `packages/`)
- [x] Criar projetos .NET: `TwelveDaily.Domain`, `TwelveDaily.Application`, `TwelveDaily.Infrastructure`, `TwelveDaily.Api`
- [x] Criar projetos de teste: `TwelveDaily.UnitTests`, `TwelveDaily.IntegrationTests`
- [x] Configurar referências entre projetos (`.slnx`, `.csproj`)
- [x] `docker-compose.yml` (PostgreSQL + API)
- [x] `.env.example`
- [x] Dockerfile da API
- [x] Verificar que `dotnet build` e `dotnet test` passam (sem testes ainda)

---

## Fase 2 — Testes Unitários (RED) ✅ 🔴

Testes escritos — **148 total, 135 falhando (RED), 13 passando**.
A implementação na Fase 4 irá torná-los verdes.

### 2.1 — Domain
- [x] `User` — construtor, validações (email, timezone), UpdateTimezone, UpdatePassword
- [x] `Habit` — construtor, validações (name, emoji), Update, ToggleActive
- [x] `HabitSchedule` — validações (StartTime < EndTime), UpdateTime, ToggleActive
- [x] `HabitInstance` — construtor, `Complete()` (preenche CompletedAt), regra de não completar dia futuro, SetGoogleCalendarEventId
- [x] `RefreshToken` — construtor, `Revoke()`, IsExpired, IsRevoked, IsActive

### 2.2 — Validators (FluentValidation)
- [x] `RegisterCommandValidator` — email formato válido, senha mínima, timezone obrigatório
- [x] `LoginCommandValidator` — email e senha obrigatórios
- [x] `CreateHabitCommandValidator` — name obrigatório, emoji obrigatório, pelo menos 1 schedule, userId válido
- [x] `UpdateHabitCommandValidator` — name obrigatório, emoji obrigatório
- [x] `CreateHabitScheduleDtoValidator` — StartTime < EndTime
- [x] `GenerateInstancesCommandValidator` — userId obrigatório
- [x] `CompleteHabitInstanceCommandValidator` — instanceId e userId obrigatórios

### 2.3 — Handlers (Commands/Queries)
- [x] `RegisterHandler` — cria User + gera tokens, rejeita email duplicado
- [x] `LoginHandler` — valida credenciais, retorna tokens, rejeita email/senha inválidos
- [x] `RefreshTokenHandler` — rotação de tokens, rejeita expirado/inexistente
- [x] `LogoutHandler` — revoga refresh token
- [x] `LogoutAllHandler` — revoga todos os refresh tokens do usuário
- [x] `CreateHabitHandler` — cria Habit + HabitSchedules, gera instância se startToday=true
- [x] `UpdateHabitHandler` — atualiza, rejeita não encontrado, rejeita outro usuário
- [x] `DeleteHabitHandler` — deleta, rejeita outro usuário
- [x] `ToggleHabitHandler` — alterna IsActive
- [x] `CompleteHabitInstanceHandler` — preenche CompletedAt, rejeita não encontrado, rejeita outro usuário
- [x] `GenerateInstancesHandler` — gera instâncias para data passada, rejeita futura/hoje, idempotente
- [x] `UpdateHabitSchedulesHandler` — substitui schedules, rejeita não encontrado/outro usuário
- [x] `ToggleHabitScheduleHandler` — alterna IsActive de schedule, rejeita DayOfWeek inexistente
- [x] `GetDailyHabitsHandler` — retorna D-3 a D+3, tipos past/today/future, future sem instanceId
- [x] `GetHabitDetailHandler` — retorna hábito com schedules, rejeita não encontrado/outro usuário
- [x] `GetHabitsListHandler` — lista hábitos do usuário
- [x] `GetWeeklyDashboardHandler` — métricas da semana (total, concluídos, taxa, dia a dia)
- [x] `GetUserProfileHandler` — retorna perfil, rejeita não encontrado
- [x] `UpdateTimezoneHandler` — atualiza timezone, rejeita não encontrado
- [x] `UpdatePasswordHandler` — valida senha atual, atualiza hash, rejeita senha errada
- [x] `RegisterPushTokenHandler` — upsert por token (add novo, update existente)

---

## Fase 3 — Testes de Integração (RED) ✅

Testes end-to-end com banco real (TestContainers).

### 3.1 — Infraestrutura de teste
- [x] `IntegrationTestBase` — setup TestContainers + WebApplicationFactory
- [x] Helper para criar usuário autenticado (register + extrair token)

### 3.2 — Auth
- [x] Registro com sucesso
- [x] Registro com email duplicado → 409
- [x] Login com credenciais válidas
- [x] Login com senha errada → 401
- [x] Refresh token válido → novos tokens
- [x] Refresh token revogado → 401
- [x] Logout → token revogado
- [x] Logout-all → todos os tokens revogados

### 3.3 — Hábitos
- [x] Criar hábito com schedules
- [x] Criar hábito com `startToday: true` → gera instância do dia
- [x] Listar hábitos do usuário
- [x] Atualizar hábito
- [x] Deletar hábito → cascata em schedules e instâncias
- [x] Toggle de hábito
- [x] Atualizar schedules (upsert)
- [x] Toggle de schedule individual

### 3.4 — Instâncias e Timeline
- [x] GET daily — retorna range D-3 a D+3
- [x] Dias passados sem instâncias → items vazio
- [x] Gerar instâncias de dia passado → 201
- [x] Gerar instâncias de dia futuro → 400
- [x] Gerar instâncias duplicadas → idempotente (lista vazia)
- [x] Check de instância → CompletedAt preenchido
- [x] Check de instância futura → 400
- [x] Dias futuros retornam dados de HabitSchedule (sem id de instância)

### 3.5 — Isolamento
- [x] Usuário A não vê hábitos do Usuário B
- [x] Usuário A não pode dar check em instância do Usuário B → 403
- [x] Usuário A não pode atualizar hábito do Usuário B → 403
- [x] Usuário A não pode deletar hábito do Usuário B → 403

### 3.6 — Dashboard
- [x] Dashboard semanal com dados corretos
- [x] Dashboard sem instâncias → valores zerados

### 3.7 — Perfil
- [x] GET /users/me retorna dados do usuário
- [x] Alterar timezone
- [x] Alterar senha com senha atual correta
- [x] Alterar senha com senha atual errada → 400
- [x] GET /users/me sem auth → 401

---

## Fase 4 — Back-End (GREEN) ✅

Implementar o código para fazer os testes passarem.

### 4.1 — Domain
- [x] Entidades com construtores e métodos de domínio
- [x] Interfaces de repositório

### 4.2 — Infrastructure
- [x] `AppDbContext` + configuração de entidades (EF Core)
- [ ] Migrations iniciais
- [x] Implementação dos repositórios
- [x] `JwtTokenService`
- [x] `PasswordHashService` (bcrypt)

### 4.3 — Application
- [x] Commands, Queries, Handlers (fazer testes unitários passarem)
- [x] Validators (FluentValidation)
- [x] `ValidationBehavior` (pipeline MediatR)

### 4.4 — Api
- [x] `Program.cs` — DI, middlewares, Swagger, SignalR
- [x] Controllers (thin — delegam para MediatR)
- [x] Global exception handler (ValidationException → 400, DomainException → 422, etc.)
- [ ] SignalR Hub (`/hubs/habits`)

### 4.5 — Hangfire
- [ ] Job de geração de instâncias (a cada hora)
- [ ] Job de ativação da notificação do próximo hábito (15 min antes do horário)
- [ ] Job de término da persistência em `ScheduledEndTime`
- [ ] Promoção imediata do próximo hábito ao concluir o atual
- [ ] Reagendamento ao alterar schedules

### 4.6 — Integrações externas
- [ ] Expo Push Notifications (envio de push do próximo hábito)
- [ ] Ação `Check` sem abrir o app
- [ ] Estratégia Android persistente + iOS best-effort
- [ ] Google Calendar (OAuth2 + criação de eventos)

### 4.7 — Refactor
- [x] Todos os testes unitários verdes (148/148) → refatorar código mantendo testes passando

---

## Fase 5 — Front-End ✅ (MVP)

### 5.1 — Setup
- [x] Inicializar projeto Expo em `apps/client/`
- [x] Configurar Turborepo (`turbo.json`, `package.json` raiz)
- [x] Configurar NativeWind (Tailwind)
- [x] Configurar orval (`orval.config.ts`)
- [x] Gerar `packages/api-client/` (cliente manual tipado, orval pronto para gerar quando API rodar)
- [x] Configurar Expo Router (navegação)

### 5.2 — Auth
- [x] Tela de Login
- [x] Tela de Registro (com seleção de timezone)
- [x] Armazenamento seguro de tokens (SecureStore mobile / AsyncStorage web)
- [x] Interceptor Axios para refresh automático (via `onUnauthorized`)

### 5.3 — Timeline (tela principal)
- [x] Layout de timeline vertical com coluna de horários
- [ ] Scroll automático para horário atual
- [ ] Altura proporcional à duração do hábito
- [x] Estados visuais (pendente / concluído / atrasado)
- [x] Interação de check (animação, feedback háptico)
- [ ] Navegação entre dias (swipe / seletor de data)
- [x] Botão "Gerar rotina deste dia" (dias passados)
- [ ] Integração SignalR (atualização em tempo real)

### 5.4 — Gerenciamento de Hábitos
- [x] Tela de listagem de hábitos
- [ ] Tela de criar/editar hábito (nome, emoji, descrição, syncGoogleCalendar)
- [ ] Seletor de dias da semana com toggle
- [ ] Seletor de horário (StartTime / EndTime) por dia
- [ ] Confirmação "Este hábito começa hoje?"

### 5.5 — Dashboard
- [x] Tela de dashboard semanal
- [ ] Gráfico de conclusão por dia (D3)
- [ ] Cards de streak, melhor/pior hábito
- [ ] Seletor de semana

### 5.6 — Configurações
- [ ] Menu hamburger
- [ ] Conexão com Google Calendar (OAuth2)
- [ ] Alterar timezone
- [ ] Alterar senha
- [x] Logout / Logout de todos os dispositivos
- [ ] Seleção de idioma — **i18n (PT/EN/ES)**, ver [specs/i18n.md](specs/i18n.md)

### 5.7 — Notificações
- [ ] Registro de push token (`expo-notifications`)
- [ ] Recebimento da notificação do próximo hábito
- [ ] Exibição persistente no Android até `ScheduledEndTime`
- [ ] Experiência equivalente possível no iOS
- [ ] Ação `Check` sem abrir o app
- [ ] Troca imediata para o próximo hábito quando ele já estiver dentro da janela de 15 minutos
- [ ] Remoção da persistência ao atingir `ScheduledEndTime`

---

## Fase 6 — CI/CD

### 6.1 — Pipeline de PR ✅
- [x] GitHub Actions (`.github/workflows/dotnet.yml`): build + `dotnet test` (unit + integration) em todo push/PR para `master` — job `build` é o status check obrigatório do ruleset
- [x] Verificar que TestContainers funciona no runner GitHub (Docker pré-instalado no `ubuntu-latest`)

### 6.2 — Pipeline de imagens + deploy (merge na `master`) → self-hosted no `onze` ✅
Implementado em `.github/workflows/images.yml`.
- [x] Build da imagem da API → push GitHub Container Registry (ghcr.io)
- [x] Build da Web (`expo export` → nginx) → push ghcr.io
- [x] Deploy no `onze` via **self-hosted runner**: `docker compose pull && up -d` (sem SSH — Cloudflare Tunnel)

### 6.3 — Pipeline Mobile
- [ ] EAS Build (iOS + Android) via tag `v*`
- [ ] Configurar secrets no GitHub (`SSH_HOST`, `SSH_USER`, `SSH_KEY`, `EXPO_TOKEN`, etc.)

### 6.4 — Infraestrutura de produção (`onze`, Debian) 🟡
- [x] Setup do host: Docker + self-hosted runner (usuário `doze`)
- [x] Exposição via **Cloudflare Tunnel** + **Caddy** interno (TLS na borda da Cloudflare — sem port forwarding, sem Let's Encrypt no host)
- [x] Stack do twelve-daily: API + Web + Postgres dedicado (rede interna)
- [x] DNS na Cloudflare (wildcard `*.doze.dev.br` → túnel)
- [x] Verificar deploy end-to-end (web + `/health` por HTTPS)
- [ ] Backups automáticos do Postgres (pg_dump + offsite) — *planejado*
- [ ] Hardening do host (ufw, SSH por chave) — *planejado*
- [ ] Observabilidade (rotação de log no Docker, uptime) — *planejado*

---

## Ordem de Execução Resumida

```
Fase 0  ✅  Documentação completa
Fase 1  ✅  Estrutura do projeto (monorepo, projetos .NET, docker-compose)
Fase 2  ✅  Testes unitários — RED (148 testes, 135 falhando)
Fase 3  ✅  Testes de integração — RED
Fase 4  ✅  Back-end — GREEN (148/148 testes unitários passando)
Fase 5  ✅  Front-end (MVP — setup, auth, timeline, habits list, dashboard, settings)
Fase 6  🟡  CI/CD — PR (build/test) ✅, imagens no GHCR ✅, deploy automático no `onze` ✅; faltam backups/hardening/observabilidade e EAS (mobile)
```

> **Nota pós-MVP:** após a Fase 5, o domínio migrou do modelo de `HabitInstance` para o modelo de **check** (ver [habit-check-refactor](specs/habit-check-refactor.md)) e as **push notifications** (orquestrador + jobs Hangfire + Expo Push) foram implementadas no backend e no cliente. O **SignalR/real-time** segue planejado, ainda não implementado.

