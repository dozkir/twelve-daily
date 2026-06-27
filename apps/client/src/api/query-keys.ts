/**
 * Fonte única das chaves de cache do TanStack Query.
 *
 * Por que centralizar: as telas e os hooks precisam concordar exatamente na chave
 * usada em `useQuery` e em `invalidateQueries`. Strings soltas espalhadas levam a
 * typos e a invalidações que silenciosamente não atingem o cache. Cada feature
 * expõe `all` (prefixo, para invalidar tudo da feature) e um helper por parâmetro.
 *
 * O TanStack faz match por prefixo: invalidar `["daily"]` atinge `["daily", date]`.
 */
export const habitKeys = {
  all: ["habits"] as const,
  detail: (habitId: string) => ["habit", habitId] as const
};

export const timelineKeys = {
  all: ["daily"] as const,
  byDate: (date: string) => ["daily", date] as const
};

export const dashboardKeys = {
  all: ["dashboard"] as const,
  byWeek: (weekStart: string) => ["dashboard", weekStart] as const
};

export const profileKeys = {
  all: ["profile"] as const
};
