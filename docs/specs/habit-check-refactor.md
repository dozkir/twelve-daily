# RFC — Evolução do conceito de "instância de hábito"

> Status: decisões da seção 7 fechadas em 2026-06-05 — ver seção 10. Pronto para implementação.
> Objetivo: registrar a nova regra de negócio para timeline, checks, notificações, tela de hábitos e dashboard.

---

## 1. Motivação

Hoje o sistema é centrado em `HabitInstance`: o dia atual gera instâncias, o passado pode gerar instâncias manualmente e o check altera a própria instância.

A mudança proposta inverte essa lógica:

- o **hábito** passa a ser a fonte principal da rotina;
- o **check** passa a ser um registro simples do que foi concluído;
- a timeline e o dashboard passam a ser reconstruídos a partir de:
  - hábito;
  - schedule do hábito;
  - data de criação do hábito;
  - registros de check.

Em outras palavras: a aplicação deixa de depender da existência prévia de uma instância para representar um dia.

---

## 2. Decisão principal

### 2.1. Não deve mais existir geração de instâncias para o dia atual

Fica decidido que:

- o sistema **não gera mais instâncias para hoje**;
- abrir a timeline **não deve mais disparar geração automática** de dados do dia;
- o comportamento `startToday` deixa de representar “gerar instância hoje” e precisará ser revisto na implementação;
- a ideia de “rotina do dia” passa a ser montada dinamicamente a partir dos hábitos elegíveis para aquela data.

### 2.2. Novo papel da entidade hoje chamada `HabitInstance`

A entidade hoje chamada `HabitInstance` deixa de representar uma ocorrência agendada completa.

Ela passa a representar somente o **registro do check**.

Campos conceitualmente necessários nesse novo papel:

- identificador do registro;
- `HabitId`;
- `UserId`;
- `Date` (data local do usuário à qual o check pertence);
- data/hora do check (`CheckedAt` / `CompletedAt`);
- timestamps técnicos de criação/atualização, se ainda fizer sentido.

Campos que deixam de ser parte do conceito principal:

- `ScheduledStartTime`;
- `ScheduledEndTime`;
- qualquer dependência estrutural de a rotina diária existir como linha materializada antes do check.

> Observação: internamente o nome `HabitInstance` pode até ser mantido por um tempo por compatibilidade, mas conceitualmente o dado passa a ser um **check de hábito**. Se o nome atual começar a gerar confusão, uma renomeação futura para algo como `HabitCheck` deve ser considerada.

### 2.3. Desfazer check

Se o usuário desfizer um check:

- o registro correspondente pode ser **deletado**;
- não é necessário manter uma linha “desmarcada”;
- o estado “não concluído” volta a ser derivado da ausência de check.

---

## 3. Regras por tela

## 3.1. Timeline

### Dias futuros

Os dias futuros podem continuar funcionando como hoje em termos conceituais:

- exibem os hábitos planejados com base em `HabitSchedule`;
- não dependem de check;
- não exibem ocorrência materializada prévia.

### Dia atual

O dia atual deve passar a mostrar:

- os **hábitos de hoje**;
- e não mais instâncias geradas para hoje.

O estado visual do item deve ser derivado de:

- horários do `HabitSchedule`;
- existência de check para `(HabitId, Date)`.

### Dias passados

Os dias anteriores devem mostrar os hábitos somente se:

- o hábito já existia naquela data, isto é, `Habit.CreatedAt` é menor ou igual à data em questão.

Além disso, para aparecer no dia em questão, o hábito ainda precisa ser elegível para aquele dia conforme a regra de agenda adotada na implementação.

### Nova fonte de verdade da timeline

A timeline passa a ser reconstruída assim:

| Tipo de dia | Fonte principal | Check vem de |
|---|---|---|
| `past` | hábito + schedule + data de criação | registro de check |
| `today` | hábito + schedule + data de criação | registro de check |
| `future` | hábito + schedule + data de criação | não aplicável |

### Consequência importante

Com isso, a timeline deixa de depender de:

- geração automática diária;
- geração manual de instâncias passadas;
- exclusão de “ocorrência do dia” como mecanismo principal de editar a visualização histórica.

---

## 3.2. Notificações

As notificações deixam de ficar atreladas diretamente a uma instância materializada.

Passam a ficar atreladas ao **hábito agendado para uma data/horário**.

### Regras desejadas

- a notificação deve ser calculada a partir do hábito e do respectivo `HabitSchedule`;
- a notificação **não deve mais aparecer** caso o hábito já possua check para aquela data;
- ao receber um check, o backend deve recalcular imediatamente a próxima notificação elegível;
- ao desfazer um check, o hábito pode voltar a ficar elegível, se ainda estiver dentro das regras de exibição da notificação.

### Implicação técnica

Os payloads e a orquestração de notificação não devem mais depender de um `HabitInstanceId` obrigatório como fonte de verdade do agendamento.

---

## 3.3. Tela de hábitos

### Exclusão de hábito

Ao deletar um hábito, a interface deve exibir uma mensagem esclarecendo que o histórico será apagado junto.

Texto base sugerido:

> Tem certeza? Todos os dados de histórico serão deletados junto. Caso pretenda preservar esses dados, recomenda-se apenas desativar o hábito.

### Hábitos inativos

Os hábitos inativos devem ficar:

- em uma **guia separada**;
- ainda dentro da tela de hábitos.

Isso ajuda a preservar o histórico sem poluir a lista principal de hábitos ativos.

### Tela de edição do hábito

Abaixo do botão de editar/salvar, deve existir uma orientação para evitar distorção histórica ao transformar um hábito em outro.

Texto sugerido:

> Se a intenção for transformar este hábito em outro hábito diferente, o mais indicado é criar um novo hábito e desativar este. Assim o histórico continua fazendo sentido.

---

## 3.4. Dashboard

O dashboard deve considerar os hábitos da seguinte forma para cada data:

1. **Concluídos**
   - hábitos que possuem check naquela data.

2. **Não concluídos, mas contabilizáveis**
   - hábitos que não possuem check naquela data;
   - desde que o hábito tenha `CreatedAt` igual ou anterior àquela data.

### Regra prática

Para um dia entrar no denominador do dashboard, o hábito precisa:

- existir naquela data;
- estar agendado para aquele dia;
- ser elegível segundo as regras de atividade históricas definidas pela implementação.

### Consequência

O dashboard deixa de depender de “quantas instâncias foram geradas” e passa a depender de “quantos hábitos deveriam contar naquele dia”.

---

## 4. Impactos esperados na modelagem

## 4.1. Modelo conceitual alvo

### Habit
Continua sendo a entidade principal da rotina.

### HabitSchedule
Continua definindo:

- dia da semana;
- hora inicial;
- hora final;
- se aquele schedule está ativo.

### Registro de check
Passa a guardar somente o fato de que:

- determinado hábito foi marcado;
- em determinada data;
- em determinado momento.

---

## 4.2. Consultas afetadas

Devem ser revistas pelo menos as consultas e fluxos abaixo:

- timeline diária (`/habits/daily`);
- ação de check e uncheck;
- notificações push;
- check vindo pela notificação;
- dashboard semanal;
- criação de hábito com `startToday`;
- geração manual de instâncias passadas;
- qualquer cálculo que hoje dependa de `ScheduledStartTime` e `ScheduledEndTime` armazenados em instância.

---

## 5. Regras que este documento substitui conceitualmente

Este RFC entra em conflito com a documentação atual que assume:

- geração de instâncias para hoje;
- geração manual de instâncias no passado;
- timeline de hoje baseada em `HabitInstance`;
- notificações baseadas diretamente em instâncias;
- dashboard baseado em instâncias geradas.

Estes documentos **foram revisados** para o modelo de check (implementação concluída):

- ✅ `docs/domain/entities.md`
- ✅ `docs/domain/rules.md`
- ✅ `docs/domain/flows.md`
- ✅ `docs/specs/habits.md`
- ✅ `docs/specs/notifications.md`
- ✅ `docs/specs/dashboard.md`
- ✅ `docs/architecture/frontend.md`
- ✅ `docs/claude.md`, `CLAUDE.md` (raiz), `docs/architecture/backend.md`, `docs/specs/google-calendar.md` (banner de adiamento)

> Ainda **não revisados** (notas de implementação/históricas, baixa prioridade): `docs/push-notifications-implementation.md`, `docs/push-notifications-file-breakdown.md`, `docs/roadmap.md`, `docs/index.md`, `docs/architecture/testing.md`.

---

## 6. Pontos que fazem sentido e estão coerentes

Os direcionamentos acima fazem sentido e são coerentes entre si por alguns motivos:

1. **Simplificam o conceito funcional**
   - o usuário enxerga hábitos planejados e checks realizados, em vez de depender de ocorrências pré-geradas.

2. **Reduzem acoplamento entre leitura e escrita**
   - visualizar a timeline não deveria precisar criar dados no banco.

3. **Tornam o dashboard mais fiel ao que deveria ter sido feito**
   - o denominador passa a vir da rotina esperada naquele dia, e não do que foi previamente materializado.

4. **Melhoram a clareza do histórico**
   - desativar preserva histórico;
   - deletar apaga histórico;
   - editar profundamente um hábito deixa de ser o caminho recomendado.

---

## 7. Principais dúvidas que ainda precisam ser fechadas

A proposta está consistente, mas ainda existem decisões importantes em aberto antes da implementação.

### 7.1. Histórico de alterações do hábito

Hoje o sistema tem `CreatedAt` e `UpdatedAt`, mas não possui versionamento histórico de:

- nome;
- emoji;
- descrição;
- horários antigos;
- dias da semana antigos;
- momento exato de ativação/inativação.

**Pergunta:**
Se um hábito for alterado depois, como a timeline passada e o dashboard devem refletir o passado?

Exemplo:
- hábito era “Academia” às 07:00;
- depois virou “Corrida” às 19:00.

Sem versionamento, o passado pode ser reescrito pelo estado atual do hábito.

### 7.2. Inativação ao longo do tempo

Hoje existe `IsActive`, mas não existe um `DeactivatedAt` explícito.

**Pergunta:**
Se um hábito foi desativado hoje, ele deve continuar contando normalmente no dashboard dos dias anteriores? Provavelmente sim.

Se a resposta for sim, talvez precisemos registrar melhor a linha do tempo de ativação/inativação.

### 7.3. Alterações de schedule e passado

O mesmo problema vale para `HabitSchedule`:

- se o usuário muda o horário de terça de 07:00 para 09:00,
- o passado deve mostrar 07:00 ou 09:00?

Sem histórico de schedule, a timeline passada tende a refletir o estado atual, não o histórico real.

### 7.4. Google Calendar

Hoje a integração com Google Calendar é acionada a partir da criação de instâncias.

**Pergunta:**
Sem instâncias agendadas materializadas, quando exatamente os eventos do calendário serão criados, atualizados ou removidos?

Este ponto não apareceu nos requisitos do ajuste, mas foi impactado diretamente pela mudança conceitual.

### 7.5. Chave do check / da notificação

Se a notificação não ficará mais atrelada a uma instância, precisamos definir qual será a identidade da ocorrência lógica do dia.

Possibilidades conceituais:

- `HabitId + Date`;
- `HabitId + Date + DayOfWeek`;
- `HabitId + Date + faixa de horário`.

Isso precisa ficar claro para evitar ambiguidade em check, uncheck, push e realtime.

### 7.6. API de check

Hoje o check depende de `InstanceId`.

**Pergunta:**
No novo modelo, o endpoint de check será baseado em:

- `HabitId + Date`?
- um novo recurso dedicado de `check`?
- um upsert de registro de conclusão?

### 7.7. Comportamento de dias passados sem versionamento

A regra de “mostrar hábitos antigos se foram criados até aquela data” resolve a existência do hábito, mas não resolve totalmente a fidelidade histórica.

**Pergunta:**
Para a primeira fase, aceitamos reconstrução histórica aproximada com base no estado atual do hábito/schedule?
Ou já vamos exigir histórico fiel desde o início?

### 7.8. Exclusão definitiva vs preservação mínima

Foi decidido que deletar hábito apaga o histórico junto, o que é coerente com o aviso de interface.

Mesmo assim, vale confirmar:

**Pergunta:**
A exclusão será realmente física/lógica com remoção total dos checks, ou haverá alguma política de retenção técnica por auditoria interna?

---

## 8. Recomendação para a próxima etapa

Antes de começar a implementação, vale fechar pelo menos estas decisões:

1. se haverá ou não versionamento histórico de hábito/schedule;
2. como representar tecnicamente o novo registro de check;
3. qual será a chave da ocorrência lógica usada por notificações e check;
4. como o dashboard tratará hábitos desativados ao longo do tempo;
5. como ficará a integração com Google Calendar nesse novo modelo.

Sem essas definições, existe risco de implementar uma timeline coerente no presente, mas inconsistente no histórico.

---

## 9. Resumo executivo

A atualização proposta faz sentido e está bem direcionada.

O núcleo da mudança é este:

- **hábito + schedule** passam a definir o que deveria existir em cada dia;
- **check** passa a registrar apenas o que foi concluído;
- **timeline, notificações e dashboard** deixam de depender da geração prévia de instâncias.

A principal atenção agora não está na ideia central, e sim nas bordas históricas:

- edição de hábito;
- alteração de horários;
- inativação ao longo do tempo;
- integração com Google Calendar;
- identidade técnica da ocorrência lógica usada em check e notificações.

---

## 10. Decisões fechadas (2026-06-05)

As dúvidas da seção 7 foram resolvidas. Estas decisões valem para a primeira fase da implementação.

### 10.1. Histórico (resolve 7.1, 7.3, 7.7) → **Snapshot no check**

- **Não** haverá versionamento de `Habit`/`HabitSchedule` na fase 1.
- Ao criar um check, gravamos no próprio registro um **snapshot**: nome, emoji e a faixa de horário (`StartTime`/`EndTime` locais) do hábito naquele dia.
- **Dias concluídos** são renderizados a partir do snapshot → fiéis ao que o hábito era na época.
- **Dias não concluídos** (passado e futuro) são reconstruídos do estado **atual** de `Habit`/`HabitSchedule` → aproximados.
- Limitação aceita e documentada: após uma edição, um dia com hábito concluído pode exibir nome/horário diferentes de um dia não concluído.

### 10.2. Inativação (resolve 7.2) → **Conta sempre no passado**

- **Não** adicionar `DeactivatedAt`.
- O denominador do dashboard para a data `D` depende só de **existência** (`Habit.CreatedAt` ≤ `D`) + elegibilidade de schedule naquele dia. `IsActive` não remove retroativamente dias passados.
- `IsActive` permanece como pausa do presente/futuro: hábito inativo sai da lista ativa, **não gera notificação** e **não aparece** na timeline de hoje/futuro.

### 10.3. Chave do check e API (resolve 7.5, 7.6) → **HabitId + Date**

- Ocorrência lógica = `(HabitId, Date local)`. **Um check por hábito por dia** (constraint único).
- API: `PUT /habits/{habitId}/check` (body com `date`) para marcar — upsert; `DELETE /habits/{habitId}/check?date=…` para desmarcar.
- O token de ação de push passa a carregar `(UserId, HabitId, Date)` no lugar de `HabitInstanceId`.

### 10.4. Google Calendar (resolve 7.4) → **Adiado**

- Sync **desligado** durante o refactor. Remover a criação de evento do fluxo e o campo `GoogleCalendarEventId` do registro de check.
- `GoogleConnection` permanece; reavaliar depois com modelo de **evento recorrente (RRULE)** dirigido por mudanças de schedule.

### 10.5. Exclusão (resolve 7.8) → **Hard delete com cascade**

- Deletar hábito remove fisicamente o hábito, seus schedules e seus checks. Sem retenção/auditoria.

### 10.6. Renomeação

- `HabitInstance` passa a ser **`HabitCheck`**, com a forma:
  - `Id`, `HabitId`, `UserId` (denormalizado), `Date` (local), `CheckedAt` (UTC);
  - snapshot: `HabitName`, `HabitEmoji`, `StartTime`, `EndTime` (locais).
- Some o conceito antigo de `ScheduledStartTime`/`ScheduledEndTime`/`CompletedAt`/`DeletedAt`/`GoogleCalendarEventId` como fonte de agendamento.

