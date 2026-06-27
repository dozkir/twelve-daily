import { router } from "expo-router";
import { useMemo, useState } from "react";
import { ActivityIndicator, Alert, FlatList, StyleSheet, Text, TouchableOpacity, View } from "react-native";

import { getApiErrorMessage } from "@/src/api/error";
import { useDeleteHabitMutation, useHabitsQuery, useToggleHabitMutation } from "@/src/habits/queries";
import { colors } from "@/src/theme";
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

  return (
    <Screen title="Habits" subtitle="Manage your routines">
      <View style={styles.actions}>
        <TouchableOpacity style={styles.createButton} onPress={() => router.push("/(app)/habits/new")}>
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
              onPress={() => router.push({ pathname: "/(app)/habits/[id]", params: { id: item.id } })}>
              <Text style={styles.cardTitle}>{item.emoji} {item.name}</Text>
              {item.description ? <Text style={styles.cardDescription}>{item.description}</Text> : null}
              <Text style={styles.cardStatus}>{item.isActive ? "Active" : "Inactive"}</Text>
            </TouchableOpacity>

            <View style={styles.cardActions}>
              <TouchableOpacity
                activeOpacity={0.85}
                style={[styles.actionButton, styles.secondaryActionButton]}
                onPress={() => router.push({ pathname: "/(app)/habits/[id]", params: { id: item.id } })}>
                <Text style={styles.secondaryActionButtonText}>Edit</Text>
              </TouchableOpacity>

              <TouchableOpacity
                activeOpacity={0.85}
                style={[styles.actionButton, styles.secondaryActionButton]}
                disabled={toggleHabitMutation.isPending || deleteHabitMutation.isPending}
                onPress={() => {
                  toggleHabitMutation.mutate(item.id, {
                    onError: (error) => Alert.alert("Unable to update habit", getApiErrorMessage(error))
                  });
                }}>
                <Text style={styles.secondaryActionButtonText}>{item.isActive ? "Inactivate" : "Activate"}</Text>
              </TouchableOpacity>

              <TouchableOpacity
                activeOpacity={0.85}
                style={[styles.actionButton, styles.dangerActionButton]}
                disabled={deleteHabitMutation.isPending || toggleHabitMutation.isPending}
                onPress={() => {
                  Alert.alert(
                    "Delete habit",
                    `Are you sure? All history for ${item.name} will be deleted too. If you want to keep this data, deactivate the habit instead.`,
                    [
                      { text: "Cancel", style: "cancel" },
                      {
                        text: "Delete",
                        style: "destructive",
                        onPress: () => deleteHabitMutation.mutate(item.id, {
                          onError: (error) => Alert.alert("Unable to delete habit", getApiErrorMessage(error))
                        })
                      }
                    ]
                  );
                }}>
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
