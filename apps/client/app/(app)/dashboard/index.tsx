import { dashboardGetWeekly } from "@twelve-daily/api-client";
import { BlurView } from "expo-blur";
import { useQuery } from "@tanstack/react-query";
import { ActivityIndicator, StyleSheet, Text, View } from "react-native";

import { startOfWeekIso } from "@/src/date";
import { colors } from "@/src/theme";
import { Screen } from "@/src/ui/screen";

export default function DashboardScreen() {
  const weekStart = startOfWeekIso(new Date());

  const dashboardQuery = useQuery({
    queryKey: ["dashboard", weekStart],
    queryFn: () => dashboardGetWeekly({ weekStart })
  });

  const data = dashboardQuery.data;

  return (
    <Screen title="Dashboard" subtitle={`Week from ${weekStart}`}>
      <View style={styles.container}>
        <View style={styles.content}>
          <View style={styles.card}>
            <Text style={styles.cardLabel}>Total scheduled</Text>
            {dashboardQuery.isLoading ? <ActivityIndicator color={colors.accentSoft} /> : <Text style={styles.cardValue}>{data?.total ?? 0}</Text>}
          </View>
          <View style={[styles.card, styles.cardSpacing]}>
            <Text style={styles.cardLabel}>Completed</Text>
            {dashboardQuery.isLoading ? <ActivityIndicator color={colors.accentSoft} /> : <Text style={styles.cardValue}>{data?.completed ?? 0}</Text>}
          </View>
          <View style={[styles.card, styles.cardSpacing]}>
            <Text style={styles.cardLabel}>Completion rate</Text>
            {dashboardQuery.isLoading ? <ActivityIndicator color={colors.accentSoft} /> : <Text style={styles.cardValue}>{Math.round(data?.completionRate ?? 0)}%</Text>}
          </View>
        </View>

        <BlurView intensity={32} tint="dark" style={styles.blurOverlay}>
          <View style={styles.overlayCard}>
            <Text style={styles.overlayTitle}>Em breve</Text>
            <Text style={styles.overlayText}>
              O dashboard está em preparação e logo estará disponível para você.
            </Text>
          </View>
        </BlurView>
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    position: "relative"
  },
  content: {
    opacity: 0.5
  },
  card: {
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: 16
  },
  cardSpacing: {
    marginTop: 12
  },
  cardLabel: {
    color: colors.textSecondary
  },
  cardValue: {
    fontSize: 24,
    fontWeight: "600",
    color: colors.textPrimary
  },
  blurOverlay: {
    ...StyleSheet.absoluteFillObject,
    alignItems: "center",
    justifyContent: "center",
    overflow: "hidden",
    borderRadius: 20
  },
  overlayCard: {
    width: "100%",
    borderRadius: 20,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: "rgba(23, 16, 34, 0.72)",
    paddingHorizontal: 24,
    paddingVertical: 28
  },
  overlayTitle: {
    textAlign: "center",
    fontSize: 24,
    fontWeight: "700",
    color: colors.textPrimary
  },
  overlayText: {
    marginTop: 10,
    textAlign: "center",
    lineHeight: 22,
    color: colors.textSecondary
  }
});
