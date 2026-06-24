# Implementação — Push Notifications Persistentes

> ⚠️ **Documento histórico (guia de implementação).** A funcionalidade descrita aqui **já foi implementada** no backend (`PushNotificationOrchestrator`, `PushNotificationJobRunner`, `ExpoPushNotificationService`, `PushNotificationActionTokenService`) e no cliente (`apps/client/src/notifications/`). Além disso, foi escrito sobre o modelo antigo de `HabitInstance`: onde o texto cita "instância", `habitInstanceId`, `CompletedAt` ou `POST /habit-instances/{id}/complete-from-notification`, o modelo atual usa um **check** `(HabitId, Date)` e o endpoint real é `POST /habits/{habitId}/check/from-notification`. Não há mais "job de geração de instâncias" — o orquestrador **auto-agenda** o próximo "acordar". Ver [habit-check-refactor](specs/habit-check-refactor.md) e [domain/flows.md](domain/flows.md) para o fluxo vigente.

## Objetivo

Implementar uma experiência de push notification focada no **próximo hábito do usuário**.

### Regra final do produto
- Apenas **uma notificação** deve ficar visível por vez, por dispositivo.
- A notificação visível é sempre a do **próximo hábito elegível**.
- Ela aparece **15 minutos antes** do `ScheduledStartTime`.
- No **Android**, a persistência é objetivo principal.
- No **iOS**, o app implementa a experiência mais próxima possível dentro das limitações da plataforma.
- A notificação possui apenas a ação **`Check`**.
- `Check` conclui a instância **sem abrir o app**.
- A persistência termina em `ScheduledEndTime`.
- Ao concluir um hábito, o próximo pode assumir imediatamente se já estiver dentro da janela de 15 minutos.

---

## Visão geral da arquitetura

### Backend (.NET)
Responsável por:
- armazenar `PushToken`
- determinar o próximo hábito elegível
- agendar jobs com Hangfire
- concluir o hábito ao receber a ação `Check`
- promover o próximo hábito quando necessário
- enviar payloads via Expo Push API

### Cliente Android
Responsável por:
- registrar permissão e token de push
- receber o payload do próximo hábito
- manter a notificação persistente enquanto ela estiver ativa
- executar a ação `Check` sem abrir o app
- atualizar/remover a notificação local quando necessário

### Cliente iOS
Responsável por:
- registrar permissão e token de push
- receber payloads de push
- expor a ação `Check`
- aplicar a melhor experiência possível dentro das limitações da plataforma

---

## Passo 1 — Registro de token no app

### Objetivo
Permitir que cada dispositivo registre seu token de push no backend.

### Cliente
1. Instalar e configurar `expo-notifications`.
2. Solicitar permissão de notificações ao usuário.
3. Obter o Expo Push Token.
4. Montar um `deviceLabel` legível.
5. Enviar para a API:

```http
POST /users/push-token
Content-Type: application/json
Authorization: Bearer <access-token>

{
  "token": "ExponentPushToken[...]",
  "deviceLabel": "Pixel 8 do Rafael"
}
```

### Backend
1. Criar/confirmar entidade `PushToken`.
2. Criar endpoint `POST /users/push-token`.
3. Fazer upsert por token.
4. Permitir múltiplos tokens por usuário.
5. Atualizar `UpdatedAt` quando o token já existir.

---

## Passo 2 — Definir o contrato da notificação

### Objetivo
Padronizar o payload que será enviado ao dispositivo.

### Payload mínimo recomendado
```json
{
  "to": "ExponentPushToken[...]",
  "title": "Hora do hábito!",
  "body": "📚 Ler — 13:00 até 13:20",
  "data": {
    "type": "next-habit",
    "habitInstanceId": "...",
    "habitId": "...",
    "scheduledStartTime": "2026-04-28T16:00:00Z",
    "scheduledEndTime": "2026-04-28T16:20:00Z"
  }
}
```

### Observação importante
Para a ação `Check` funcionar **sem abrir o app**, a implementação deve prever um meio autenticado de concluir a instância.

### Abordagens possíveis
#### Opção recomendada
- gerar um **action token** curto, assinado pelo backend
- incluir esse token no payload da notificação
- ao tocar em `Check`, o app/serviço envia esse token para um endpoint dedicado

#### Alternativa
- reutilizar o access token do usuário se a ação em background tiver acesso confiável a ele

### Recomendação
Documentar e implementar um endpoint dedicado para ação de notificação, por exemplo:

```http
POST /habit-instances/{id}/complete-from-notification
```

com um token assinado e de curta duração.

---

## Passo 3 — Determinar o próximo hábito elegível

### Objetivo
Garantir que só exista uma notificação ativa por vez.

### Regra
O próximo hábito elegível é:
- a `HabitInstance` mais próxima do horário atual
- ainda não concluída
- ainda dentro da janela relevante do produto

### Janela de ativação
A notificação pode ser ativada quando:
- faltarem **15 minutos ou menos** para `ScheduledStartTime`

### Janela de persistência
A notificação permanece ativa até:
- `CompletedAt` ser preenchido
- ou `ScheduledEndTime` ser atingido

---

## Passo 4 — Jobs do Hangfire

### Objetivo
Orquestrar a ativação, remoção e troca da notificação ativa.

### Jobs necessários

#### 1. Job de geração de instâncias
- já previsto no sistema
- continua sendo o ponto de criação das `HabitInstance`

#### 2. Job de ativação do próximo hábito
Executa:
- **15 minutos antes** de `ScheduledStartTime`

Responsabilidades:
- verificar se a instância ainda não foi concluída
- verificar se ela ainda é o próximo hábito elegível
- enviar o payload aos dispositivos do usuário

#### 3. Job de encerramento da persistência
Executa em:
- `ScheduledEndTime`

Responsabilidades:
- remover/rebaixar a persistência da notificação atual
- promover o próximo hábito elegível

#### 4. Job de recomputação imediata
Executa quando:
- o hábito atual é concluído
- um hábito é removido
- um horário é alterado
- um schedule é ativado/desativado

Responsabilidades:
- recalcular o próximo hábito ativo
- ativar o próximo imediatamente se já estiver dentro da janela de 15 minutos

---

## Passo 5 — Android persistente

### Objetivo
Fazer o Android manter o próximo hábito visível até `ScheduledEndTime` ou `Check`.

### Estratégia recomendada
1. Usar Expo Push para entrega do evento ao dispositivo.
2. No Android, transformar esse evento em uma notificação local gerenciada pelo app.
3. Marcar essa notificação como persistente/ongoing se a stack escolhida suportar isso.
4. Atualizar/remover a notificação local quando o estado do hábito mudar.

### Importante
O comportamento persistente forte é uma meta **principalmente Android**.
Se `expo-notifications` sozinho não oferecer o controle necessário para “ongoing notification”, será necessário:
- development build
- ou integração nativa complementar no Android

Essa validação deve acontecer no início da implementação técnica.

---

## Passo 6 — iOS best effort

### Objetivo
Entregar a melhor experiência possível no iPhone, sem prometer paridade total com Android.

### Estratégia
- usar push acionável com ação `Check`
- reapresentar ou atualizar quando possível
- aceitar que a persistência forte pode não existir do mesmo jeito que no Android

### Regra de documentação
Sempre tratar iOS como:
- **best effort dentro das limitações da plataforma**

---

## Passo 7 — Ação `Check` sem abrir o app

### Objetivo
Permitir conclusão direta do hábito pela notificação.

### Fluxo
1. Usuário toca em `Check`.
2. O app/serviço em background recebe a ação.
3. A ação chama o backend.
4. O backend conclui a `HabitInstance`.
5. O backend recalcula o próximo hábito.
6. Se outro hábito já estiver dentro da janela de 15 minutos, a nova notificação assume.

### Requisitos técnicos
- endpoint dedicado ou endpoint atual com autenticação adequada
- tratamento de falha de rede
- idempotência da conclusão
- remoção da notificação atual após sucesso

---

## Passo 8 — Regras de promoção para o próximo hábito

### Quando promover imediatamente
Promover o próximo hábito sem esperar novo job quando:
- o hábito atual recebeu `Check`
- o hábito atual expirou em `ScheduledEndTime`
- o hábito atual foi removido
- o horário/schedule foi alterado

### Condição
O próximo hábito só assume imediatamente se:
- estiver dentro da janela de 15 minutos
- ainda não estiver concluído

---

## Passo 9 — Atualização ao editar hábito

### Ao alterar `HabitSchedule`
É obrigatório:
- cancelar jobs antigos
- recalcular qual é o próximo hábito elegível
- reagendar ativação em 15 minutos antes do novo horário
- reagendar encerramento da persistência em `ScheduledEndTime`

### Ao deletar hábito ou instância
É obrigatório:
- cancelar jobs relacionados
- remover a notificação local correspondente, se existir
- promover o próximo hábito elegível

---

## Passo 10 — Observabilidade e segurança

### Segurança
- action token curto para `Check` em background
- evitar depender exclusivamente do access token interativo
- invalidar tokens comprometidos/expirados

### Observabilidade
Registrar:
- token registrado
- push enviado
- push rejeitado pela Expo
- `Check` recebido via notificação
- promoção do próximo hábito
- remoção de persistência por `ScheduledEndTime`

---

## Passo 11 — Testes recomendados

### Backend
- registrar push token novo
- re-registrar token existente
- selecionar o próximo hábito correto
- agendar job 15 minutos antes
- encerrar persistência em `ScheduledEndTime`
- promover próximo hábito após `Check`
- promover próximo hábito após expiração do atual
- reagendar ao alterar schedules

### Mobile Android
- receber notificação do próximo hábito
- manter notificação persistente até `ScheduledEndTime`
- concluir via `Check` sem abrir app
- trocar imediatamente para o próximo hábito elegível

### Mobile iOS
- receber push acionável
- ação `Check`
- comportamento best effort de reapresentação/atualização

### Cenários de produto
- dois hábitos com menos de 15 minutos entre eles
- hábito concluído faltando poucos minutos para o próximo
- hábito expirado sem check
- múltiplos dispositivos do mesmo usuário

---

## Ordem sugerida de implementação

1. Registro de `PushToken`
2. Endpoint e persistência de tokens
3. Serviço de envio via Expo Push API
4. Contrato do payload `next-habit`
5. Seleção do próximo hábito elegível no backend
6. Jobs de ativação e término no Hangfire
7. Android persistente
8. Ação `Check` em background
9. Promoção imediata do próximo hábito
10. iOS best effort
11. Observabilidade e testes finais

---

## Critério de pronto

A funcionalidade pode ser considerada pronta quando:
- o app registra tokens com sucesso
- apenas o próximo hábito fica visível por dispositivo
- a notificação aparece 15 minutos antes
- no Android ela permanece persistente até `ScheduledEndTime` ou `Check`
- `Check` conclui o hábito sem abrir o app
- o próximo hábito assume imediatamente quando elegível
- ao passar de `ScheduledEndTime`, a persistência termina e o sistema avança para o próximo hábito

