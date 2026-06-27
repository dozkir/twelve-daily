# Arquitetura — Frontend

> 📌 As **convenções operacionais** (telas finas, hooks por feature, query keys,
> estilo, i18n) vivem em [`apps/client/CLAUDE.md`](../../apps/client/CLAUDE.md).
> Este documento descreve a arquitetura; o `CLAUDE.md` descreve como trabalhar nela.

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

## Arquitetura de pastas (feature-based)

Padrão "feature-sliced lite": roteamento separado da lógica, e cada domínio
agrupado.

```
app/                       ← SÓ roteamento (Expo Router). Telas finas.
  (auth)/  (app)/          ← grupos de rota por estado de login
src/
  api/                     ← cross-cutting de API: query-keys.ts, error.ts
  <feature>/               ← habits, timeline, dashboard, settings, auth, notifications
    queries.ts             ← hooks de estado de servidor (useQuery/useMutation) + invalidação
    <feature>-form.tsx     ← formulário + schema Zod (quando houver)
  ui/                      ← componentes burros reutilizáveis (Screen, FormInput, TimeInput)
  theme.ts / date.ts / config.ts
```

### Padrões/convenções

| Convenção | Onde | Por quê |
|---|---|---|
| **Telas finas** | `app/**` | tela cuida de UI/navegação/estado local; sem `useQuery`/`useMutation` cru — análogo ao "controller fino" do backend |
| **Hooks de dados por feature** | `src/<feature>/queries.ts` | busca + invalidação de cache centralizadas; tela só consome |
| **Query keys centralizadas** | `src/api/query-keys.ts` | fonte única de chaves (`habitKeys`, `timelineKeys`, …); evita typo e invalidação que não atinge o cache |
| **Form + schema colocados** | `src/<feature>/*-form.tsx` | React Hook Form + Zod juntos |
| **Tema central** | `src/theme.ts` + `StyleSheet` | estilo consistente (sem NativeWind) |
| **Context para sessão** | `src/auth/auth-context.tsx` | axios configurado uma vez; token lido por ref |

---

## Geração de Cliente API (orval)

- O .NET expõe schema **OpenAPI/Swagger** automaticamente via Swashbuckle.
- O `orval` (config: `orval.config.ts` na raiz, modo `react-query`) lê esse schema e
  gera `packages/api-client/` com:
  - Tipos TypeScript espelhando os DTOs C# (em `src/generated/model/`)
  - **Funções tipadas** por endpoint (ex.: `habitsList`, `habitsGetDaily`) **e** hooks `useX`
- **Convenção do projeto:** consumimos as **funções tipadas** dentro dos nossos hooks
  de feature (`src/<feature>/queries.ts`), **não** os hooks `useX` gerados direto nas
  telas — assim controlamos query keys e invalidação num lugar só.
- `packages/api-client/` **nunca é editado manualmente** — rode `npm run api:generate`
  (com a API no ar) após mudar qualquer DTO/endpoint.

```ts
// src/timeline/queries.ts — função tipada do orval envolta em hook de feature
import { habitsGetDaily, habitChecksCheck, habitChecksUncheck } from "@twelve-daily/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { dashboardKeys, timelineKeys } from "@/src/api/query-keys";

export const useDailyQuery = (date: string) =>
  useQuery({ queryKey: timelineKeys.byDate(date), queryFn: () => habitsGetDaily({ date }) });

export const useToggleCheckMutation = (date: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ habitId, isDone }: { habitId: string; isDone: boolean }) => {
      if (isDone) await habitChecksUncheck(habitId, { date });
      else await habitChecksCheck(habitId, { date });
    },
    onSuccess: () => Promise.all([
      queryClient.invalidateQueries({ queryKey: timelineKeys.byDate(date) }),
      queryClient.invalidateQueries({ queryKey: dashboardKeys.all })
    ])
  });
};
```

```tsx
// Na tela: consome o hook; efeitos de UI vão nos callbacks de mutate
const timelineQuery = useDailyQuery(date);
const checkMutation = useToggleCheckMutation(date);
checkMutation.mutate({ habitId, isDone }, { onError: (e) => setActionError(getApiErrorMessage(e)) });
```

> **i18n:** as strings de UI ainda têm idioma misto (inglês + alguns textos em
> português). A internacionalização (PT/EN/ES) é uma feature planejada — ver
> [`docs/specs/i18n.md`](../specs/i18n.md).

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

