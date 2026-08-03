import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { Alert, StyleSheet, Text, TouchableOpacity, View } from "react-native";

import { useAuth } from "@/src/auth/auth-context";
import {
  useLogoutAllMutation,
  useProfileQuery,
  useUpdateTimezoneMutation
} from "@/src/settings/queries";
import { colors } from "@/src/theme";
import { useGuardedPress } from "@/src/ui/press-guard";
import { Screen } from "@/src/ui/screen";
import { TimezoneSelect } from "@/src/ui/timezone-select";

interface TimezoneFormValues {
  timezone: string;
}

export default function SettingsScreen() {
  const { logout } = useAuth();

  const profileQuery = useProfileQuery();
  const logoutAllMutation = useLogoutAllMutation();
  const updateTimezoneMutation = useUpdateTimezoneMutation();

  const { control, handleSubmit, reset, formState } = useForm<TimezoneFormValues>({
    defaultValues: { timezone: "" }
  });

  const profileTimezone = profileQuery.data?.timezone;

  useEffect(() => {
    if (profileTimezone) {
      reset({ timezone: profileTimezone });
    }
  }, [profileTimezone, reset]);

  const { onPress: signOut, isRunning: isSigningOut } = useGuardedPress(() => logout());

  const { onPress: signOutEverywhere, isRunning: isSigningOutEverywhere } = useGuardedPress(
    async () => {
      try {
        await logoutAllMutation.mutateAsync();
        await logout();
      } catch {
        Alert.alert("Error", "Could not sign out from the other devices.");
      }
    }
  );

  const saveTimezone = handleSubmit((values) => {
    updateTimezoneMutation.mutate(values.timezone, {
      onSuccess: () => {
        reset({ timezone: values.timezone });
        Alert.alert("Timezone updated", "Your timezone has been saved.");
      },
      onError: () => Alert.alert("Error", "Could not update your timezone.")
    });
  });

  return (
    <Screen title="Settings" subtitle="Profile and security">
      <View style={styles.card}>
        <Text style={styles.cardLabel}>Email</Text>
        <Text style={styles.cardValue}>{profileQuery.data?.email ?? "-"}</Text>
      </View>

      <View style={styles.timezoneSection}>
        <TimezoneSelect control={control} name="timezone" label="Timezone" />
        <TouchableOpacity
          style={[styles.saveButton, (!formState.isDirty || updateTimezoneMutation.isPending) ? styles.saveButtonDisabled : null]}
          onPress={saveTimezone}
          disabled={!formState.isDirty || updateTimezoneMutation.isPending}>
          <Text style={styles.buttonText}>
            {updateTimezoneMutation.isPending ? "Saving..." : "Save timezone"}
          </Text>
        </TouchableOpacity>
      </View>

      <TouchableOpacity
        style={[styles.logoutButton, isSigningOut ? styles.buttonDisabled : null]}
        onPress={signOut}
        disabled={isSigningOut || isSigningOutEverywhere}>
        <Text style={styles.buttonText}>{isSigningOut ? "Logging out..." : "Logout"}</Text>
      </TouchableOpacity>

      <TouchableOpacity
        style={[styles.logoutAllButton, isSigningOutEverywhere ? styles.buttonDisabled : null]}
        onPress={signOutEverywhere}
        disabled={isSigningOut || isSigningOutEverywhere}>
        <Text style={styles.buttonText}>
          {isSigningOutEverywhere ? "Logging out..." : "Logout all devices"}
        </Text>
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
  cardValue: {
    fontWeight: "600",
    color: colors.textPrimary
  },
  timezoneSection: {
    marginTop: 16
  },
  saveButton: {
    borderRadius: 12,
    backgroundColor: colors.accent,
    paddingHorizontal: 16,
    paddingVertical: 12
  },
  saveButtonDisabled: {
    opacity: 0.5
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
  buttonDisabled: {
    opacity: 0.5
  },
  buttonText: {
    textAlign: "center",
    fontWeight: "600",
    color: colors.white
  }
});
