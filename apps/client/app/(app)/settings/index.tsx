import { Alert, StyleSheet, Text, TouchableOpacity, View } from "react-native";

import { useAuth } from "@/src/auth/auth-context";
import { useLogoutAllMutation, useProfileQuery, useSendTestPushMutation } from "@/src/settings/queries";
import { colors } from "@/src/theme";
import { Screen } from "@/src/ui/screen";

export default function SettingsScreen() {
  const { logout } = useAuth();

  const profileQuery = useProfileQuery();
  const logoutAllMutation = useLogoutAllMutation();
  const testNotificationMutation = useSendTestPushMutation();

  return (
    <Screen title="Settings" subtitle="Profile and security">
      <View style={styles.card}>
        <Text style={styles.cardLabel}>Email</Text>
        <Text style={styles.cardValue}>{profileQuery.data?.email ?? "-"}</Text>
        <Text style={[styles.cardLabel, styles.cardLabelSpacing]}>Timezone</Text>
        <Text style={styles.cardValue}>{profileQuery.data?.timezone ?? "-"}</Text>
      </View>

      <TouchableOpacity style={styles.logoutButton} onPress={() => logout()}>
        <Text style={styles.buttonText}>Logout</Text>
      </TouchableOpacity>

      <TouchableOpacity
        style={styles.testNotificationButton}
        onPress={() => testNotificationMutation.mutate(undefined, {
          onSuccess: () => Alert.alert("Teste enviado", "A requisicao foi enviada para o backend. Se houver habito elegivel, o push remoto deve aparecer."),
          onError: () => Alert.alert("Erro", "Falha ao acionar o teste remoto de notificacao.")
        })}
        disabled={testNotificationMutation.isPending}
      >
        <Text style={styles.buttonText}>
          {testNotificationMutation.isPending ? "Enviando..." : "Gerar notificacao de teste"}
        </Text>
      </TouchableOpacity>

      <TouchableOpacity style={styles.logoutAllButton} onPress={() => logoutAllMutation.mutate(undefined, { onSuccess: () => logout() })}>
        <Text style={styles.buttonText}>Logout all devices</Text>
      </TouchableOpacity>
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    padding: 16
  },
  cardLabel: {
    color: colors.textSecondary
  },
  cardLabelSpacing: {
    marginTop: 8
  },
  cardValue: {
    fontWeight: "600",
    color: colors.textPrimary
  },
  logoutButton: {
    marginTop: 16,
    borderRadius: 12,
    backgroundColor: colors.accent,
    paddingHorizontal: 16,
    paddingVertical: 12
  },
  logoutAllButton: {
    marginTop: 12,
    borderRadius: 12,
    backgroundColor: colors.danger,
    paddingHorizontal: 16,
    paddingVertical: 12
  },
  testNotificationButton: {
    marginTop: 12,
    borderRadius: 12,
    backgroundColor: colors.accentStrong,
    paddingHorizontal: 16,
    paddingVertical: 12
  },
  buttonText: {
    textAlign: "center",
    fontWeight: "600",
    color: colors.white
  }
});
