import { habitChecksCheck, habitChecksUncheck, habitsGetDaily } from "@twelve-daily/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { dashboardKeys, timelineKeys } from "@/src/api/query-keys";

/**
 * Estado de servidor da timeline (rotina reconstruída por dia).
 * A tela cuida de animações/UX (haptics, popover, scroll); aqui ficam a busca
 * do dia e o check/uncheck idempotente com a invalidação de cache correspondente.
 */
export const useDailyQuery = (date: string) =>
  useQuery({
    queryKey: timelineKeys.byDate(date),
    queryFn: () => habitsGetDaily({ date })
  });

export const useToggleCheckMutation = (date: string) => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ habitId, isDone }: { habitId: string; isDone: boolean }) => {
      if (isDone) {
        await habitChecksUncheck(habitId, { date });
      } else {
        await habitChecksCheck(habitId, { date });
      }
    },
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: timelineKeys.byDate(date) }),
        queryClient.invalidateQueries({ queryKey: dashboardKeys.all })
      ])
  });
};
