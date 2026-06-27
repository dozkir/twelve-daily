# CLAUDE.md — Frontend (`apps/client`)

Convenções específicas do app Expo (`@twelve-daily/client`). Complementam o
`CLAUDE.md` da raiz; em caso de conflito, a raiz prevalece. Detalhes em
[`docs/architecture/frontend.md`](../../docs/architecture/frontend.md).

## Arquitetura: feature-based ("feature-sliced lite")

```
app/                       ← SÓ roteamento (Expo Router). Telas finas.
  (auth)/  (app)/          ← grupos de rota por estado de login
src/
  api/                     ← cross-cutting de API: query-keys.ts, error.ts
  <feature>/               ← tudo de um domínio (habits, timeline, dashboard, settings, auth, notifications)
    queries.ts             ← hooks de estado de servidor (useQuery/useMutation) + invalidação
    <feature>-form.tsx     ← formulário + schema Zod colocados juntos (quando houver)
    ...
  ui/                      ← componentes burros reutilizáveis (Screen, FormInput, TimeInput)
  theme.ts / date.ts / config.ts
```

## Regras de convenção

- **Telas finas.** Componentes em `app/**` cuidam de UI, navegação e estado **local** (animação, seleção, popover). **Não** colocam `useQuery`/`useMutation` crus, montagem de payload de API nem lógica de invalidação na tela. É o análogo do "controller fino" do backend.
- **Dados moram em hooks por feature** — `src/<feature>/queries.ts` expõe `useXQuery`/`useXMutation`. A tela consome o hook e, se precisar, passa callbacks de UI no próprio `mutate(vars, { onSuccess, onError })` (ex.: `Alert`, `setError`, haptics). Efeitos colaterais de cache (invalidação) ficam **dentro** do hook.
- **Query keys centralizadas** — sempre de `src/api/query-keys.ts` (`habitKeys`, `timelineKeys`, `dashboardKeys`, `profileKeys`). Nunca arrays literais soltos (`["habits"]`) espalhados. Cada feature expõe `all` (prefixo, para invalidar tudo) + helpers por parâmetro. Lembrar que o TanStack faz match por prefixo.
- **Formulários**: React Hook Form + Zod, schema colocado junto do componente do form.
- **Estilo**: `StyleSheet` do React Native + tema central (`src/theme.ts`). **Não** usar NativeWind/Tailwind (foi removido).
- **Sessão/HTTP**: o axios é configurado uma vez em `src/auth/auth-context.tsx` (`configureApiClient`); o token é lido por ref. Não reconfigurar por request.

## Cliente de API (orval)

- `orval.config.ts` está em modo `react-query` e gera, em `packages/api-client/` (**nunca editar à mão**): as **funções tipadas** (`habitsList`, `habitsGetDaily`, `habitChecksCheck`, …) **e** hooks `useX`.
- **Convenção do projeto:** consumimos as **funções tipadas** dentro dos nossos hooks de feature (`src/<feature>/queries.ts`) — assim controlamos query keys e invalidação num só lugar. Não importar os hooks `useX` gerados direto nas telas.
- Tipar parâmetros de mutação com `Parameters<typeof fn>[n]` evita acoplar a nomes de tipos gerados que podem mudar.
- Após mudar DTO/endpoint no backend: `npm run api:generate` (com a API no ar).

## i18n (planejado — ainda não implementado)

As strings de UI hoje estão majoritariamente em inglês, com alguns textos em
português. Há **inconsistência de idioma**: não adicione mais strings hard-coded
soltas. A internacionalização (Português, Inglês, Espanhol) está especificada em
[`docs/specs/i18n.md`](../../docs/specs/i18n.md) — siga aquele padrão de chaves ao
tocar em telas.

## Verificação

```powershell
cd apps/client; npx tsc -p tsconfig.json --noEmit   # typecheck
npx expo lint                                        # lint
```

(O `turbo run typecheck` da raiz pode falhar por falta do campo `packageManager` no
`package.json` — rode o `tsc` direto no workspace enquanto isso não for corrigido.)

## Manutenção da documentação (obrigatório)

Ao mudar a arquitetura do cliente, telas, convenções de dados/estilo ou o contrato
com a API, **revise e atualize na mesma alteração** os documentos relacionados,
para que não fiquem obsoletos nem sejam ignorados:

- [`docs/architecture/frontend.md`](../../docs/architecture/frontend.md) — stack, orval, convenções, telas.
- [`docs/specs/`](../../docs/specs/) — specs de feature afetadas (incl. [`i18n.md`](../../docs/specs/i18n.md)).
- [`docs/CLAUDE.md`](../../docs/CLAUDE.md) e [`docs/index.md`](../../docs/index.md) — referência rápida e índice.
- Este arquivo e o `CLAUDE.md` da raiz, quando a convenção em si mudar.

Se uma doc contradisser o código, corrija a doc — não a deixe divergente em silêncio.
