import { dashboardGetWeekly } from "@twelve-daily/api-client";
import { useQuery } from "@tanstack/react-query";

import { dashboardKeys } from "@/src/api/query-keys";

/** Estado de servidor do dashboard semanal. */
export const useWeeklyDashboardQuery = (weekStart: string) =>
  useQuery({
    queryKey: dashboardKeys.byWeek(weekStart),
    queryFn: () => dashboardGetWeekly({ weekStart })
  });
