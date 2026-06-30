# Domínio — Fluxos

> Esta página reflete o modelo de **check** (sem materialização de instâncias). Ver [habit-check-refactor](../specs/habit-check-refactor.md).

## Reconstrução da rotina (timeline / dashboard)

Não há job de geração. A rotina de cada dia é montada **na leitura**:

```
GET /habits/daily?date=D  (janela D-3 .. D+3)
Para cada dia da janela:
  classifica em past / today / future (comparando com o "hoje local" do usuário)
  Para cada hábito do usuário:
    - se existe HabitCheck para (HabitId, dia):
        → item CONCLUÍDO, renderizado a partir do SNAPSHOT do check (nome/emoji/horário)
    - senão (reconstrução pelo estado atual):
        → ignora se Habit.CreatedAt (local) > dia            (ainda não existia)
        → ignora se for hoje/futuro e Habit.IsActive == false (pausado)
        → busca o HabitSchedule ativo para o DayOfWeek do dia (no máximo um)
        → se houver, adiciona item PENDENTE com os horários do schedule
  ordena os itens do dia por horário de início
```

> O dashboard usa a mesma lógica: o denominador de um dia é a quantidade de hábitos que **deveriam** contar (existiam por `CreatedAt` + agendados naquele dia), e o numerador são os que possuem `HabitCheck`. Dias futuros ainda não contam.

---

## Check de um Hábito

```
1. Usuário toca no item na timeline e confirma
2. Front bloqueia dias futuros
3. PUT /habits/{habitId}/check  com { date }   (upsert idempotente)
4. API valida posse do hábito e que a data não é futura
5. API cria HabitCheck (HabitId, Date, CheckedAt) com snapshot do nome/emoji/horário
6. Backend recalcula o próximo hábito elegível e reagenda o próximo "acordar"
7. Front invalida timeline + dashboard e atualiza o item para concluído
```

## Desfazer Check

```
1. Usuário toca em "Undo" no item concluído
2. DELETE /habits/{habitId}/check?date=D   (idempotente)
3. API deleta o HabitCheck de (HabitId, D) — a ausência é o estado "não concluído"
4. Backend recalcula o próximo hábito elegível
```

## Check direto pela Notificação

```
1. Usuário toca em `Check` na notificação do próximo hábito (não abre o app)
2. Cliente/serviço nativo chama POST /habits/{habitId}/check/from-notification { date, actionToken }
3. API valida o actionToken (carrega UserId, HabitId, Date) e confere que bate com a requisição
4. Faz upsert do HabitCheck correspondente
5. Remove a notificação atual e recalcula o próximo hábito elegível
```

---

## Novo Hábito agendado para hoje

```
1. Usuário cria o hábito e configura seus HabitSchedules
2. POST /habits (sem flag startToday — ela não existe mais)
3. Se algum schedule cair no dia de hoje, o hábito JÁ aparece na timeline de hoje
   automaticamente (a rotina é reconstruída na leitura)
```

> Não há mais `startToday` nem materialização: a ocorrência de hoje surge sozinha a partir do schedule.

---

## Refresh de Token

```
1. Access Token expira (15 min)
2. Front detecta resposta 401
3. POST /auth/refresh com { refreshToken }
4. API valida: token existe, não revogado, não expirado
5. API revoga o token antigo (RevokedAt = now)
6. API gera novo Access Token + novo Refresh Token
7. Front armazena os novos tokens e reexecuta a requisição original
```

---

## Alteração de HabitSchedule

```
1. Usuário altera horários ou dias de um hábito
2. PUT /habits/{id}/schedules  (validação: no máximo um schedule por DayOfWeek)
3. API substitui os registros de HabitSchedule
4. Backend recalcula o próximo hábito elegível e reagenda o próximo "acordar"
```

> Não há jobs por instância a cancelar/reagendar — a timeline futura passa a refletir o novo schedule na próxima leitura. Checks passados preservam o snapshot antigo.

---

## Ciclo da Notificação do Próximo Hábito

```
1. RecomputeUserNotificationsAsync calcula a próxima ocorrência elegível a partir de
   schedules ativos + checks de hoje/amanhã + timezone do usuário (sem instâncias)
2. Uma ocorrência é "ativa" quando entra na janela de 15 min antes do início e ainda não terminou
3. Quando faltar ≤ 15 min para o início, a notificação é promovida (persistente no Android)
4. O recompute AUTO-AGENDA (Hangfire) o próximo "acordar" na próxima fronteira relevante
   (ativação de uma ocorrência futura ou fim da ocorrência ativa) — encadeando o ciclo.
   Mantém no máximo UM wake pendente por usuário: cancela o anterior antes de agendar o
   próximo (id persistido em NotificationWake), senão cada mutação criaria uma cadeia
   independente e a mesma notificação chegaria várias vezes
5. Se houver check antes do fim:
   - cria o HabitCheck → a ocorrência some do conjunto elegível
   - remove a notificação atual e promove a próxima, se elegível
6. Se não houver check até o fim:
   - a notificação deixa de ser persistente
   - o foco passa para a próxima ocorrência elegível
```

---

## Google Calendar — adiado

A integração com Google Calendar está **adiada** nesta fase (ver [habit-check-refactor](../specs/habit-check-refactor.md) §10.4). A entidade `GoogleConnection` e a flag `Habit.SyncGoogleCalendar` permanecem, mas nenhum evento é criado.

> Modelo-alvo quando reativada: um **evento recorrente (RRULE)** por hábito/schedule, criado/atualizado quando o schedule muda — não um evento por dia (já que não há mais instâncias materializadas).
