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

      <TouchableOpacity style={styles.logoutButton} onPress={() => logout()}>
        <Text style={styles.buttonText}>Logout</Text>
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
  buttonText: {
    textAlign: "center",
    fontWeight: "600",
    color: colors.white
  }
});
