# Breakdown Técnico — Push Notifications por Projeto/Arquivo

> ⚠️ **Documento histórico (planejamento).** Escrito quando a feature ainda não existia e sobre o modelo antigo de `HabitInstance`. A funcionalidade **já foi implementada** — ver o "Estado atual" abaixo e os arquivos reais em `apps/api/TwelveDaily.Infrastructure/Services/` e `apps/client/src/notifications/`. Onde o texto cita "instância"/`HabitInstanceId`, o modelo vigente usa **check** `(HabitId, Date)` e o endpoint `POST /habits/{habitId}/check/from-notification`. Ver [habit-check-refactor](specs/habit-check-refactor.md).

## Objetivo

Mapear, de forma prática, **onde** a funcionalidade de notificações push persistentes deve ser implementada no monorepo e **o que** precisa ser criado, alterado ou validado em cada arquivo.

Este documento complementa:
- `docs/specs/notifications.md`
- `docs/domain/flows.md`
- `docs/push-notifications-implementation.md`

---

## Estado atual resumido

> Atualizado: a feature está implementada. O conteúdo das seções seguintes é o planejamento original e pode citar nomes/arquivos sugeridos que diferem dos finais.

### Backend (implementado)
- `PushToken` + `RegisterPushTokenCommand`/`Handler` + `IPushTokenRepository`/`PushTokenRepository` + `DbSet<PushToken>`
- endpoint `POST /users/push-token` (e `POST /users/push-test`, `POST /users/push-sync`) em `UsersController`
- envio via Expo Push API: `Infrastructure/Services/ExpoPushNotificationService.cs` (`IPushNotificationService`)
- orquestração do próximo hábito: `PushNotificationOrchestrator.cs` (`IPushNotificationOrchestrator`) + `PushNotificationJobRunner.cs`
- token de ação assinado: `PushNotificationActionTokenService.cs` (`IPushNotificationActionTokenService`)
- ação `Check` anônima sem abrir o app: `POST /habits/{habitId}/check/from-notification` (`HabitChecksController`)
- Hangfire configurado em `Program.cs`; o recompute **auto-agenda** o próximo "acordar" (sem job de geração de instâncias)
- recálculo do próximo hábito disparado em check/uncheck, criar/editar/toggle hábito e alterar schedule

### Cliente (implementado)
- `expo-notifications` + `@notifee/react-native` + `expo-task-manager` no `apps/client/package.json`
- registro de token, recepção e ciclo de vida em `apps/client/src/notifications/` (`provider.tsx`, `service.ts`, `background.ts`, `constants.ts`, `types.ts`)

### Ainda pendente / a refinar
- persistência Android refinada (validar limites do `expo-notifications`/`notifee`)
- experiência best-effort no iOS
- observabilidade (receipts da Expo, métricas) e tratamento de tokens inválidos

---

## Visão geral por projeto

| Projeto | Papel na feature |
|---|---|
| `apps/api/TwelveDaily.Api` | expor endpoints, configurar Hangfire e autenticação das ações de notificação |
| `apps/api/TwelveDaily.Application` | regras de negócio, commands, handlers, seleção do próximo hábito, ação `Check` |
| `apps/api/TwelveDaily.Domain` | entidades e contratos (`PushToken`, possivelmente novos contratos de notificação) |
| `apps/api/TwelveDaily.Infrastructure` | EF Core, repositórios, serviço Expo Push, Hangfire jobs |
| `apps/client` | pedir permissão, registrar token, receber push, manter notificação ativa, executar `Check` |
| `packages/api-client` | refletir os novos endpoints após atualização do OpenAPI/orval |

---

## Backend — `apps/api`

## 1) API pública

### `apps/api/TwelveDaily.Api/Controllers/UsersController.cs`
**Status:** já existe, precisa alterar

### O que fazer
- adicionar endpoint:

```http
POST /users/push-token
```

### Responsabilidade
- receber `{ token, deviceLabel }`
- extrair `userId` autenticado
- disparar `RegisterPushTokenCommand`
- retornar `204 NoContent`

### Observação
Este é o ponto mais simples e deve ser implementado primeiro no backend, porque o command e o handler já existem.

---

### `apps/api/TwelveDaily.Api/Program.cs`
**Status:** já existe, precisa alterar

### O que fazer
- registrar Hangfire
- configurar storage persistente do Hangfire no PostgreSQL
- habilitar dashboard do Hangfire protegido
- registrar qualquer serviço novo de push/action token

### Itens esperados
- `AddHangfire(...)`
- `AddHangfireServer()`
- `UseHangfireDashboard(...)`
- configuração de auth básica para o dashboard

### Também pode precisar
- registrar serviço de actions da notificação
- registrar serviços para envio Expo

---

## 2) Application

### `apps/api/TwelveDaily.Application/Users/Commands/UserCommands.cs`
**Status:** já existe, parcialmente pronto

### O que já existe
- `RegisterPushTokenCommand(Guid UserId, string Token, string? DeviceLabel)`

### O que pode precisar
- nenhuma mudança, se o contrato atual for suficiente
- opcionalmente validar melhor o token Expo no futuro

---

### `apps/api/TwelveDaily.Application/Users/Handlers/UserHandlers.cs`
**Status:** já existe, parcialmente pronto

### O que já existe
- `RegisterPushTokenHandler`

### O que revisar
- garantir que o upsert por token está correto
- decidir se reuso do mesmo token por outro usuário deve sobrescrever, rejeitar ou migrar posse

### Recomendação
Documentar explicitamente a regra de ownership do token.

---

### Arquivos novos recomendados em `apps/api/TwelveDaily.Application/Notifications/`
**Status:** criar

### Sugestão de estrutura
- `Commands/NotificationCommands.cs`
- `Handlers/NotificationHandlers.cs`
- `Queries/NotificationQueries.cs` *(se necessário)*
- `Dtos/...` *(se necessário)*
- `Interfaces/IPushNotificationService.cs`
- `Interfaces/INextHabitNotificationPlanner.cs`
- `Interfaces/INotificationActionTokenService.cs`

### Responsabilidade
Concentrar a lógica de:
- determinar o próximo hábito elegível
- montar payload da notificação
- concluir hábito via ação `Check`
- promover a próxima notificação após `Check` ou `ScheduledEndTime`

---

### Novo command sugerido
**Criar em:** `apps/api/TwelveDaily.Application/Notifications/Commands/...`

#### Exemplo de commands
- `ActivateNextHabitNotificationCommand(UserId)`
- `ExpireNextHabitNotificationCommand(UserId, HabitInstanceId)`
- `CompleteHabitInstanceFromNotificationCommand(HabitInstanceId, ActionToken)`
- `RecalculateNextHabitNotificationCommand(UserId)`

### Responsabilidade
Separar as ações do fluxo de notificação do fluxo tradicional da timeline.

---

### `apps/api/TwelveDaily.Application/Habits/Handlers/...`
**Status:** já existe, precisa integrar

### O que fazer
Integrar promoção/reagendamento nos pontos em que o estado muda:
- ao criar instâncias
- ao concluir instância
- ao deletar instância
- ao atualizar schedules
- ao criar hábito com `startToday`

### Objetivo
Toda mudança relevante deve recalcular o “próximo hábito visível”.

---

## 3) Domain

### `apps/api/TwelveDaily.Domain/Entities/PushToken.cs`
**Status:** já existe

### O que revisar
- talvez adicionar `IsActive` no futuro, se quiser desativar tokens inválidos sem apagar registro
- talvez adicionar `LastUsedAt` / `LastDeliveredAt` no futuro

### Neste momento
Não parece obrigatório mudar para começar.

---

### `apps/api/TwelveDaily.Domain/Interfaces/IPushTokenRepository.cs`
**Status:** já existe

### O que pode precisar
- método para desativar/remover token inválido
- método para buscar todos os tokens ativos de um usuário

### Possíveis novos métodos
- `Task RemoveAsync(PushToken pushToken, CancellationToken ct = default)`
- `Task<List<PushToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)`

---

## 4) Infrastructure

### `apps/api/TwelveDaily.Infrastructure/Repositories/PushTokenRepository.cs`
**Status:** já existe

### O que fazer
- adaptar se o contrato `IPushTokenRepository` crescer
- tratar eventual remoção/desativação de token inválido

---

### `apps/api/TwelveDaily.Infrastructure/Data/AppDbContext.cs`
**Status:** já existe

### O que fazer
- manter `PushTokens`
- se houver novos campos em `PushToken`, refletir no mapeamento
- se forem adicionadas entidades de log/agendamento, mapear aqui

---

### Arquivo novo recomendado: `apps/api/TwelveDaily.Infrastructure/Services/ExpoPushNotificationService.cs`
**Status:** criar

### Responsabilidade
- chamar a Expo Push API
- enviar payload para múltiplos tokens do usuário
- tratar respostas e erros
- identificar tokens inválidos

### Contrato sugerido
Implementar `IPushNotificationService`

### Métodos sugeridos
- `SendNextHabitNotificationAsync(...)`
- `SendBatchAsync(...)`
- `HandleReceiptsAsync(...)` *(futuro)*

---

### Arquivos novos recomendados: `apps/api/TwelveDaily.Infrastructure/Jobs/...`
**Status:** criar

### Sugestão de arquivos
- `GenerateHabitInstancesJob.cs`
- `ActivateNextHabitNotificationJob.cs`
- `ExpireNextHabitNotificationJob.cs`
- `RecalculateNextHabitNotificationJob.cs`

### Responsabilidade
Executar os jobs do Hangfire para:
- gerar instâncias
- ativar notificação 15 min antes
- terminar persistência em `ScheduledEndTime`
- recalcular o próximo hábito após mudanças de estado

---

### `apps/api/TwelveDaily.Infrastructure/DependencyInjection.cs`
**Status:** já existe, precisa alterar

### O que fazer
Registrar:
- `IPushNotificationService`
- `INextHabitNotificationPlanner`
- `INotificationActionTokenService`
- jobs/helpers necessários

---

## Cliente mobile — `apps/client`

## 1) Configuração do app

### `apps/client/package.json`
**Status:** já possui `expo-blur` e outras libs, mas não foi confirmado `expo-notifications`

### O que fazer
- adicionar `expo-notifications`
- adicionar dependências auxiliares se a estratégia Android persistente exigir algo extra

### Observação
A feature de persistência Android pode exigir validação de limite do `expo-notifications` puro.

---

### `apps/client/app.json`
**Status:** já existe, precisa alterar

### O que fazer
- configurar plugin de notificações se necessário
- definir metadados Android/iOS relacionados a push
- revisar permissões/canais de notificação no Android

---

### `apps/client/app/_layout.tsx`
**Status:** já existe, precisa alterar

### O que fazer
Centralizar a inicialização global de notificações:
- registrar listeners de notificação recebida
- registrar listeners de ação da notificação
- configurar comportamento foreground
- inicializar canal Android

### Recomendação
Criar um provider/hook dedicado e montá-lo aqui.

---

## 2) Cliente de autenticação e bootstrap

### `apps/client/src/auth/auth-context.tsx`
**Status:** já existe, provável ponto de integração

### O que fazer
Após autenticação/refresh bem-sucedidos:
- registrar push token quando o usuário estiver logado
- evitar registro duplicado desnecessário
- talvez re-registrar token quando mudar

### Atenção
Se a ação `Check` sem abrir o app depender de token assinado pelo backend, este arquivo não precisa resolver tudo; apenas garantir que o dispositivo esteja registrado.

---

## 3) Cliente HTTP

### `apps/client/src/api/client.ts`
**Status:** já existe, precisa alterar

### O que fazer
Adicionar métodos manuais enquanto o OpenAPI/orval não gerar isso automaticamente, por exemplo:
- `registerPushToken(...)`
- `completeHabitInstanceFromNotification(...)` *(se houver endpoint dedicado)*

### Melhor prática
Depois que o endpoint existir na API e o OpenAPI estiver atualizado:
- regenerar `packages/api-client`
- migrar o uso para o cliente gerado

---

### `packages/api-client/*`
**Status:** indireto, precisa atualizar depois da API

### O que fazer
- atualizar schema OpenAPI
- rodar `orval`
- expor os novos métodos/tipos do endpoint de push token e da ação de notificação

---

## 4) Tela de Settings

### `apps/client/app/(app)/settings/index.tsx`
**Status:** já existe, bom ponto de UX

### O que fazer
Opcionalmente exibir:
- estado da permissão de notificação
- botão para reativar permissões / re-registrar token
- explicação breve sobre a feature

### Observação
Não é obrigatório para a primeira entrega, mas ajuda muito em suporte e teste.

---

## 5) Arquivos novos recomendados no cliente

### `apps/client/src/notifications/register-push.ts`
**Criar**

Responsabilidade:
- pedir permissão
- obter Expo Push Token
- enviar token para backend

---

### `apps/client/src/notifications/notification-actions.ts`
**Criar**

Responsabilidade:
- registrar categoria/ação `Check`
- capturar toque na ação
- chamar backend sem abrir o app

---

### `apps/client/src/notifications/notification-lifecycle.ts`
**Criar**

Responsabilidade:
- controlar atualização/substituição da notificação local
- remover a anterior
- promover a próxima quando necessário

---

### `apps/client/src/notifications/android-channel.ts`
**Criar**

Responsabilidade:
- configurar canal Android
- definir prioridade/importância alta
- alinhar comportamento visual da notificação do próximo hábito

---

### `apps/client/src/notifications/use-notifications-bootstrap.ts`
**Criar**

Responsabilidade:
- concentrar bootstrap global do sistema de notificações
- ser usado em `app/_layout.tsx`

---

## Ordem sugerida por implementação real

## Fase A — Base backend
1. `UsersController.cs` → expor `POST /users/push-token`
2. validar fluxo de `RegisterPushTokenCommand`
3. atualizar OpenAPI
4. regenerar `packages/api-client`

## Fase B — Base cliente
5. instalar/configurar `expo-notifications`
6. criar `register-push.ts`
7. integrar bootstrap em `auth-context.tsx` ou `app/_layout.tsx`
8. confirmar que tokens chegam à API

## Fase C — Entrega de push
9. criar `IPushNotificationService`
10. criar `ExpoPushNotificationService.cs`
11. enviar notificação de teste manual para um token salvo

## Fase D — Orquestração Hangfire
12. configurar Hangfire em `Program.cs`
13. criar jobs de ativação e expiração
14. recalcular o próximo hábito elegível
15. integrar reagendamento com alterações de hábito/schedule

## Fase E — Ação `Check`
16. definir estratégia de autenticação da ação
17. criar endpoint/command dedicado
18. implementar ação de notificação no cliente
19. validar conclusão sem abrir o app

## Fase F — Persistência Android + iOS best effort
20. validar se `expo-notifications` atende persistência Android
21. se não atender, decidir extensão nativa/estratégia complementar
22. implementar best effort no iOS

## Fase G — Robustez
23. tratar tokens inválidos
24. logs/observabilidade
25. testes automatizados e testes manuais multi-device

---

## Prioridade recomendada

### Implementar primeiro
- endpoint `POST /users/push-token`
- registro de token no app
- envio manual de push de teste

### Implementar em seguida
- cálculo do próximo hábito elegível
- jobs Hangfire
- ação `Check`

### Implementar por último
- persistência Android refinada
- ajustes iOS
- observabilidade e receipts Expo

---

## Critério de pronto por camada

### Backend
- aceita e armazena tokens
- sabe decidir qual é o próximo hábito
- agenda ativação e expiração
- conclui hábito por ação de notificação

### Cliente
- registra token automaticamente
- recebe a notificação do próximo hábito
- executa `Check` sem abrir o app
- mantém/update/remove a notificação corretamente

### Produto
- só uma notificação ativa por dispositivo
- aparece 15 min antes
- no Android fica persistente até `ScheduledEndTime` ou `Check`
- após `Check` ou expiração, o próximo hábito assume quando elegível

