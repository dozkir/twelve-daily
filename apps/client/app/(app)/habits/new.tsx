import { habitsCreate } from "@twelve-daily/api-client";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { router } from "expo-router";
import { useState } from "react";

import { getApiErrorMessage } from "@/src/api/error";
import { HabitForm, getDefaultHabitFormValues, type HabitFormValues } from "@/src/habits/habit-form";
import { buildHabitSchedulesPayload } from "@/src/habits/habit-form-values";

export default function NewHabitScreen() {
  const queryClient = useQueryClient();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: async ({ values }: { values: HabitFormValues }) => habitsCreate({
      name: values.name,
      emoji: values.emoji,
      description: values.description?.trim() || undefined,
      syncGoogleCalendar: false,
      schedules: buildHabitSchedulesPayload(values)
    }),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["habits"] }),
        queryClient.invalidateQueries({ queryKey: ["daily"] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard"] })
      ]);
      router.replace("/(app)/habits");
    },
    onError: (error) => {
      setSubmitError(getApiErrorMessage(error));
    }
  });

  return (
    <HabitForm
      title="Create habit"
      subtitle="Add a new routine to your week"
      initialValues={getDefaultHabitFormValues()}
      submitLabel="Create habit"
      submittingLabel="Creating..."
      submitError={submitError}
      isSubmitting={createMutation.isPending}
      onSubmit={async (values) => {
        setSubmitError(null);
        try {
          await createMutation.mutateAsync({ values });
        } catch {
          // Error state is already set via the mutation handler.
        }
      }}
      onCancel={() => router.back()}
    />
  );
}


