import { router } from "expo-router";
import { useMemo, useState } from "react";
import { ActivityIndicator, Alert, FlatList, StyleSheet, Text, TouchableOpacity, View } from "react-native";

import { getApiErrorMessage } from "@/src/api/error";
import { useDeleteHabitMutation, useHabitsQuery, useToggleHabitMutation } from "@/src/habits/queries";
import { colors } from "@/src/theme";
import { confirmAsync } from "@/src/ui/confirm";
import { useGuardedNavigation, useGuardedPress } from "@/src/ui/press-guard";
import { Screen } from "@/src/ui/screen";

export default function HabitsScreen() {
  const [tab, setTab] = useState<"active" | "inactive">("active");

  const habitsQuery = useHabitsQuery();

  const visibleHabits = useMemo(
    () => (habitsQuery.data ?? []).filter((habit) => (tab === "active" ? habit.isActive : !habit.isActive)),
    [habitsQuery.data, tab]
  );

  const toggleHabitMutation = useToggleHabitMutation();
  const deleteHabitMutation = useDeleteHabitMutation();

  const createHabit = useGuardedNavigation(() => router.push("/(app)/habits/new"));
  const openHabit = useGuardedNavigation((id: string) =>
    router.push({ pathname: "/(app)/habits/[id]", params: { id } })
  );

  const { onPress: toggleHabit, isRunning: isTogglingHabit } = useGuardedPress(
    async (habitId: string) => {
      try {
        await toggleHabitMutation.mutateAsync(habitId);
      } catch (error) {
        Alert.alert("Unable to update habit", getApiErrorMessage(error));
      }
    }
  );

  // A confirmação é aguardada dentro da trava de propósito: sem ela, o segundo
  // toque abriria um segundo diálogo (a mutation ainda nem começou, então
  // `isPending` continua falso enquanto o diálogo está aberto).
  const { onPress: deleteHabit, isRunning: isDeletingHabit } = useGuardedPress(
    async (habitId: string, habitName: string) => {
      const confirmed = await confirmAsync({
        title: "Delete habit",
        message: `Are you sure? All history for ${habitName} will be deleted too. If you want to keep this data, deactivate the habit instead.`,
        confirmText: "Delete",
        destructive: true
      });

      if (!confirmed) {
        return;
      }

      try {
        await deleteHabitMutation.mutateAsync(habitId);
      } catch (error) {
        Alert.alert("Unable to delete habit", getApiErrorMessage(error));
      }
    }
  );

  const isHabitActionBusy = isTogglingHabit || isDeletingHabit;

  return (
    <Screen title="Habits" subtitle="Manage your routines">
      <View style={styles.actions}>
        <TouchableOpacity style={styles.createButton} onPress={createHabit}>
          <Text style={styles.createButtonText}>+ Create habit</Text>
        </TouchableOpacity>
      </View>

      <View style={styles.tabs}>
        <TouchableOpacity
          activeOpacity={0.85}
          style={[styles.tab, tab === "active" ? styles.tabActive : null]}
          onPress={() => setTab("active")}>
          <Text style={[styles.tabText, tab === "active" ? styles.tabTextActive : null]}>Active</Text>
        </TouchableOpacity>
        <TouchableOpacity
          activeOpacity={0.85}
          style={[styles.tab, tab === "inactive" ? styles.tabActive : null]}
          onPress={() => setTab("inactive")}>
          <Text style={[styles.tabText, tab === "inactive" ? styles.tabTextActive : null]}>Inactive</Text>
        </TouchableOpacity>
      </View>

      {habitsQuery.isLoading ? <ActivityIndicator color={colors.accentSoft} /> : null}
      <FlatList
        data={visibleHabits}
        contentContainerStyle={styles.listContent}
        keyExtractor={(item) => item.id}
        ListEmptyComponent={
          <Text style={styles.emptyText}>
            {tab === "active" ? "No active habits yet." : "No inactive habits."}
          </Text>
        }
        renderItem={({ item }) => (
          <View style={styles.card}>
            <TouchableOpacity
              activeOpacity={0.85}
              onPress={() => openHabit(item.id)}>
              <Text style={styles.cardTitle}>{item.emoji} {item.name}</Text>
              {item.description ? <Text style={styles.cardDescription}>{item.description}</Text> : null}
              <Text style={styles.cardStatus}>{item.isActive ? "Active" : "Inactive"}</Text>
            </TouchableOpacity>

            <View style={styles.cardActions}>
              <TouchableOpacity
                activeOpacity={0.85}
                style={[styles.actionButton, styles.secondaryActionButton]}
                onPress={() => openHabit(item.id)}>
                <Text style={styles.secondaryActionButtonText}>Edit</Text>
              </TouchableOpacity>

              <TouchableOpacity
                activeOpacity={0.85}
                style={[
                  styles.actionButton,
                  styles.secondaryActionButton,
                  isHabitActionBusy ? styles.actionButtonDisabled : null
                ]}
                disabled={isHabitActionBusy}
                onPress={() => toggleHabit(item.id)}>
                <Text style={styles.secondaryActionButtonText}>{item.isActive ? "Inactivate" : "Activate"}</Text>
              </TouchableOpacity>

              <TouchableOpacity
                activeOpacity={0.85}
                style={[
                  styles.actionButton,
                  styles.dangerActionButton,
                  isHabitActionBusy ? styles.actionButtonDisabled : null
                ]}
                disabled={isHabitActionBusy}
                onPress={() => deleteHabit(item.id, item.name)}>
                <Text style={styles.dangerActionButtonText}>Delete</Text>
              </TouchableOpacity>
            </View>
          </View>
        )}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  actions: {
    marginBottom: 16
  },
  tabs: {
    flexDirection: "row",
    gap: 8,
    marginBottom: 16
  },
  tab: {
    flex: 1,
    alignItems: "center",
    borderRadius: 10,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    paddingVertical: 10
  },
  tabActive: {
    borderColor: colors.accentSoft,
    backgroundColor: colors.surfaceAlt
  },
  tabText: {
    fontWeight: "600",
    color: colors.textSecondary
  },
  tabTextActive: {
    color: colors.textPrimary
  },
  createButton: {
    borderRadius: 12,
    backgroundColor: colors.accent,
    paddingHorizontal: 16,
    paddingVertical: 12
  },
  createButtonText: {
    textAlign: "center",
    fontWeight: "600",
    color: colors.white
  },
  listContent: {
    paddingBottom: 24
  },
  emptyText: {
    color: colors.textSecondary
  },
  card: {
    marginBottom: 12,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: 16
  },
  cardActions: {
    marginTop: 14,
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  actionButton: {
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 8
  },
  actionButtonDisabled: {
    opacity: 0.5
  },
  secondaryActionButton: {
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceAlt
  },
  secondaryActionButtonText: {
    fontWeight: "600",
    color: colors.textPrimary
  },
  dangerActionButton: {
    backgroundColor: colors.danger
  },
  dangerActionButtonText: {
    fontWeight: "600",
    color: colors.white
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: "600",
    color: colors.textPrimary
  },
  cardDescription: {
    color: colors.textSecondary
  },
  cardStatus: {
    marginTop: 4,
    fontSize: 12,
    color: colors.textMuted
  }
});
