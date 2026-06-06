import type { HabitDetail } from "@twelve-daily/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { router, useLocalSearchParams } from "expo-router";
import { useMemo, useState } from "react";
import { ActivityIndicator, StyleSheet, Text, TouchableOpacity, View } from "react-native";

import { useAuth } from "@/src/auth/auth-context";
import { makeAuthedClient } from "@/src/api/client";
import { getApiErrorMessage } from "@/src/api/error";
import { HabitForm, type HabitFormValues } from "@/src/habits/habit-form";
import { buildHabitFormInitialValues, buildHabitSchedulesPayload } from "@/src/habits/habit-form-values";
import { colors } from "@/src/theme";
import { Screen } from "@/src/ui/screen";

export default function EditHabitScreen() {
  const { id } = useLocalSearchParams<{ id?: string | string[] }>();
  const habitId = typeof id === "string" ? id : id?.[0] ?? "";
  const { accessToken, logout } = useAuth();
  const client = useMemo(() => makeAuthedClient(accessToken, logout), [accessToken, logout]);
  const queryClient = useQueryClient();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const habitQuery = useQuery({
    queryKey: ["habit", habitId],
    queryFn: () => client.getHabitDetail(habitId),
    enabled: habitId.length > 0
  });

  const updateMutation = useMutation({
    mutationFn: async ({ values, detail }: { values: HabitFormValues; detail: HabitDetail }) => {
      await Promise.all([
        client.updateHabit(habitId, {
          name: values.name,
          emoji: values.emoji,
          description: values.description?.trim() || undefined,
          syncGoogleCalendar: detail.syncGoogleCalendar
        }),
        client.updateHabitSchedules(habitId, {
          schedules: buildHabitSchedulesPayload(values)
        })
      ]);
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["habits"] }),
        queryClient.invalidateQueries({ queryKey: ["habit", habitId] }),
        queryClient.invalidateQueries({ queryKey: ["daily"] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard"] })
      ]);
      router.replace("/(app)/habits");
    },
    onError: (error) => {
      setSubmitError(getApiErrorMessage(error));
    }
  });

  const initialValues = useMemo(
    () => habitQuery.data ? buildHabitFormInitialValues(habitQuery.data) : undefined,
    [habitQuery.data]
  );

  if (!habitId) {
    return (
      <Screen title="Edit habit" subtitle="Invalid habit">
        <View style={styles.feedbackContainer}>
          <Text style={styles.feedbackText}>Could not identify the selected habit.</Text>
          <TouchableOpacity style={styles.secondaryButton} onPress={() => router.replace("/(app)/habits")}>
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
          <TouchableOpacity style={styles.secondaryButton} onPress={() => router.replace("/(app)/habits")}>
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
          await updateMutation.mutateAsync({ values, detail: habitQuery.data });
        } catch {
          // Error state is already set via the mutation handler.
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


