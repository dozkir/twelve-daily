import {
  habitsCreate,
  habitsDelete,
  habitsGetDetail,
  habitsList,
  habitsToggle,
  habitsUpdate,
  habitsUpdateSchedules
} from "@twelve-daily/api-client";
import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";

import { dashboardKeys, habitKeys, timelineKeys } from "@/src/api/query-keys";

/**
 * Estado de servidor da feature "hábitos".
 *
 * As telas consomem estes hooks e ficam responsáveis apenas por UI/navegação:
 * a busca, a normalização de chaves e a invalidação de cache vivem aqui. É o
 * equivalente, no front, ao "controller fino" do backend (a lógica não mora na tela).
 */

// Criar/editar/alternar/excluir um hábito afeta a lista, a timeline e o dashboard.
const invalidateHabitViews = (queryClient: QueryClient) =>
  Promise.all([
    queryClient.invalidateQueries({ queryKey: habitKeys.all }),
    queryClient.invalidateQueries({ queryKey: timelineKeys.all }),
    queryClient.invalidateQueries({ queryKey: dashboardKeys.all })
  ]);

export const useHabitsQuery = () =>
  useQuery({
    queryKey: habitKeys.all,
    queryFn: () => habitsList()
  });

export const useHabitDetailQuery = (habitId: string) =>
  useQuery({
    queryKey: habitKeys.detail(habitId),
    queryFn: () => habitsGetDetail(habitId),
    enabled: habitId.length > 0
  });

export const useCreateHabitMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: Parameters<typeof habitsCreate>[0]) => habitsCreate(payload),
    onSuccess: () => invalidateHabitViews(queryClient)
  });
};

export const useUpdateHabitMutation = (habitId: string) => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: {
      habit: Parameters<typeof habitsUpdate>[1];
      schedules: Parameters<typeof habitsUpdateSchedules>[1];
    }) =>
      Promise.all([
        habitsUpdate(habitId, payload.habit),
        habitsUpdateSchedules(habitId, payload.schedules)
      ]),
    onSuccess: () =>
      Promise.all([
        invalidateHabitViews(queryClient),
        queryClient.invalidateQueries({ queryKey: habitKeys.detail(habitId) })
      ])
  });
};

export const useToggleHabitMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (habitId: string) => habitsToggle(habitId),
    onSuccess: () => invalidateHabitViews(queryClient)
  });
};

export const useDeleteHabitMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (habitId: string) => habitsDelete(habitId),
    onSuccess: () => invalidateHabitViews(queryClient)
  });
};
