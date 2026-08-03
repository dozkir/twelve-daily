import { router, useLocalSearchParams } from "expo-router";
import { useMemo, useState } from "react";
import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from "react-native";

import { getApiErrorMessage } from "@/src/api/error";
import { useHabitDetailQuery, useUpdateHabitMutation } from "@/src/habits/queries";
import { HabitForm } from "@/src/habits/habit-form";
import { buildHabitFormInitialValues, buildHabitSchedulesPayload } from "@/src/habits/habit-form-values";
import { colors } from "@/src/theme";
import { useGuardedNavigation } from "@/src/ui/press-guard";
import { Screen } from "@/src/ui/screen";

export default function EditHabitScreen() {
  const { id } = useLocalSearchParams<{ id?: string | string[] }>();
  const habitId = typeof id === "string" ? id : id?.[0] ?? "";
  const [submitError, setSubmitError] = useState<string | null>(null);

  const habitQuery = useHabitDetailQuery(habitId);
  const updateMutation = useUpdateHabitMutation(habitId);

  const initialValues = useMemo(
    () => habitQuery.data ? buildHabitFormInitialValues(habitQuery.data) : undefined,
    [habitQuery.data]
  );

  const backToHabits = useGuardedNavigation(() => router.replace("/(app)/habits"));

  if (!habitId) {
    return (
      <Screen title="Edit habit" subtitle="Invalid habit">
        <View style={styles.feedbackContainer}>
          <Text style={styles.feedbackText}>Could not identify the selected habit.</Text>
          <TouchableOpacity style={styles.secondaryButton} onPress={backToHabits}>
            <Text style={styles.secondaryButtonText}>Back to habits</Text>
          </TouchableOpacity>
        </View>
      </Screen>
    );
  }

  if (habitQuery.isLoading || !initialValues) {
    return (
      <Screen title="Edit habit" subtitle="Loading habit details">
        <View style={styles.feedbackContainer}>
          <ActivityIndicator color={colors.accentSoft} />
        </View>
      </Screen>
    );
  }

  if (habitQuery.isError || !habitQuery.data) {
    return (
      <Screen title="Edit habit" subtitle="Unable to load habit">
        <View style={styles.feedbackContainer}>
          <Text style={styles.feedbackText}>{getApiErrorMessage(habitQuery.error)}</Text>
          <TouchableOpacity style={styles.secondaryButton} onPress={backToHabits}>
            <Text style={styles.secondaryButtonText}>Back to habits</Text>
          </TouchableOpacity>
        </View>
      </Screen>
    );
  }

  return (
    <HabitForm
      title="Edit habit"
      subtitle="Update your routine"
      initialValues={initialValues}
      submitLabel="Save changes"
      submittingLabel="Saving..."
      submitError={submitError}
      isSubmitting={updateMutation.isPending}
      footerNote="If you want to turn this into a different habit, it's better to create a new one and deactivate this. That way your history stays meaningful."
      onSubmit={async (values) => {
        setSubmitError(null);
        try {
          await updateMutation.mutateAsync({
            habit: {
              name: values.name,
              emoji: values.emoji,
              description: values.description?.trim() || undefined,
              syncGoogleCalendar: habitQuery.data.syncGoogleCalendar
            },
            schedules: { schedules: buildHabitSchedulesPayload(values) }
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

const styles = StyleSheet.create({
  feedbackContainer: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    gap: 16
  },
  feedbackText: {
    textAlign: "center",
    color: colors.textSecondary
  },
  secondaryButton: {
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingHorizontal: 16,
    paddingVertical: 12
  },
  secondaryButtonText: {
    textAlign: "center",
    fontWeight: "600",
    color: colors.textPrimary
  }
});


