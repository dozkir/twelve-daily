# Arquitetura — Frontend

## Stack

| Função | Tecnologia |
|---|---|
| Framework | Expo (Managed Workflow) |
| Linguagem | TypeScript |
| Navegação | Expo Router |
| Server state | TanStack Query (gerado via orval) |
| Real-time | `@microsoft/signalr` *(planejado — ainda não adicionado ao cliente)* |
| Formulários | React Hook Form + Zod |
| Estilo | `StyleSheet` do React Native + tema central (`src/theme.ts`) |
| Push Notifications | `expo-notifications` + `@notifee/react-native` + `expo-task-manager` |
| Geração de cliente API | orval |

---

## Geração de Cliente API (orval)

- O .NET expõe schema **OpenAPI/Swagger** automaticamente via Swashbuckle
- O `orval` lê esse schema e gera `packages/api-client/` com:
  - Tipos TypeScript espelhando os DTOs C#
  - Hooks TanStack Query para cada endpoint
- Ao alterar um DTO no C#: rodar `npx orval` na raiz regenera o cliente
- `packages/api-client/` **nunca é editado manualmente**
- Configuração em `orval.config.ts` na raiz do monorepo

```ts
// Exemplo de uso no front — hooks TanStack Query gerados pelo orval
import {
  useHabitsGetDaily,
  useHabitChecksCheck,
  useHabitChecksUncheck,
} from '@twelve-daily/api-client'

const { data: daily } = useHabitsGetDaily({ date: '2026-03-27' })

const check = useHabitChecksCheck()
check.mutate({ habitId, data: { date: '2026-03-27' } })   // upsert do check do dia

const uncheck = useHabitChecksUncheck()
uncheck.mutate({ habitId, params: { date: '2026-03-27' } }) // desfaz
```

> **Nota:** `packages/api-client/src/generated/` já é gerado pelo orval a partir do OpenAPI da API (tipos em `model/` + hooks em `client.ts`, com `customInstance` em `src/http/mutator.ts`). Esse diretório nunca é editado à mão — rode `npm run api:generate` (com a API no ar) após mudar qualquer DTO/endpoint.

---

## Plataformas

- **iOS e Android**: Expo compilado via EAS Build
- **Web**: Expo Web (`npx expo export --platform web`) → arquivos estáticos

---

## Especificação de Telas

### Tela Principal — Timeline do Dia

- Dia atual como tela padrão
- Itens reconstruídos de hábito + schedule + check, ordenados **por horário de início**
- Layout estilo **timeline vertical**: coluna de horários à esquerda, itens à direita
- Scroll automático para o **horário atual** ao abrir a tela
- **Altura de cada item proporcional à duração** (`endTime - startTime` do schedule)
- Interação gamificada ao dar check (animação, feedback háptico); concluído = `checkedAt != null`

#### Estados visuais por item
| Estado | Visual |
|---|---|
| Pendente (antes do horário) | Neutro |
| Concluído | 🟢 Verde |
| Atrasado (após o fim, sem check) | 🔴 Vermelho |

### Navegação entre Dias
- Swipe horizontal ou seletor de data
- Range de **D-3 até D+3** carregado de uma vez (TanStack Query em cache)
- Centralização por horário mantida em todos os dias
- Dias passados e hoje: check/uncheck (a rotina já aparece reconstruída, sem botão de gerar)
- Dias futuros: somente visualização da rotina planejada (`HabitSchedule`) — check desabilitado

> A timeline é **exclusivamente para visualização e check**. Criação e edição de hábitos acontecem na tela de gerenciamento.

### Tela de Gerenciamento de Hábitos
- Guia **Active / Inactive** separando hábitos ativos dos pausados (preserva histórico sem poluir a lista)
- Ao **deletar**: aviso de que o histórico será apagado junto; sugere **desativar** para preservar
- Ao deletar habit → hard delete (hábito + schedules + checks)

### Tela de Edição de Hábito
- Nome, emoji e descrição
- Toggle de sincronização com Google Calendar (`SyncGoogleCalendar`) — *adiado nesta fase*
- Lista dos **7 dias da semana** com toggle por dia (no máximo um horário por dia ⇒ o modelo de dias já impede duplicar)
- Ao ativar um dia: seletor de `StartTime` e `EndTime`
- Abaixo do botão salvar: orientação para **criar um novo hábito e desativar este** ao invés de transformá-lo em outro (preserva a coerência do histórico)
- Não há mais confirmação "começa hoje?" — um schedule de hoje já aparece automaticamente na timeline

### Menu Hamburger (canto superior esquerdo)
1. Dashboard semanal
2. *(demais opções a definir)*

