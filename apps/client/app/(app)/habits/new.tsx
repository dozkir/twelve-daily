import { router } from "expo-router";
import { useState } from "react";

import { getApiErrorMessage } from "@/src/api/error";
import { useCreateHabitMutation } from "@/src/habits/queries";
import { HabitForm, getDefaultHabitFormValues, type HabitFormValues } from "@/src/habits/habit-form";
import { buildHabitSchedulesPayload } from "@/src/habits/habit-form-values";

export default function NewHabitScreen() {
  const [submitError, setSubmitError] = useState<string | null>(null);

  const createMutation = useCreateHabitMutation();

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
          await createMutation.mutateAsync({
            name: values.name,
            emoji: values.emoji,
            description: values.description?.trim() || undefined,
            syncGoogleCalendar: false,
            schedules: buildHabitSchedulesPayload(values)
          });
          router.replace("/(app)/habits");
        } catch (error) {
          setSubmitError(getApiErrorMessage(error));
        }
      }}
      onCancel={() => router.back()}
    />
  );
}


