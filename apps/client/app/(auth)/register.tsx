import { Link, router } from "expo-router";
import { useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";

import { useAuth } from "@/src/auth/auth-context";
import { getApiErrorMessage } from "@/src/api/error";
import { colors } from "@/src/theme";
import { getDeviceTimezone } from "@/src/timezones";
import { FormInput } from "@/src/ui/form-input";
import { Screen } from "@/src/ui/screen";
import { TimezoneSelect } from "@/src/ui/timezone-select";

const schema = z.object({
  email: z.string().email("Invalid email"),
  password: z.string().min(6, "Password must be at least 6 chars"),
  timezone: z.string().min(1, "Timezone is required")
});

type RegisterValues = z.infer<typeof schema>;

export default function RegisterScreen() {
  const { register } = useAuth();
  const [submitError, setSubmitError] = useState<string | null>(null);
  // `formState.isSubmitting` só desabilita o botão no render seguinte, então
  // toques em sequência rápida ainda chegariam aqui e disparariam vários cadastros.
  // Esta trava é síncrona e fecha essa janela.
  const isSubmittingRef = useRef(false);
  const timezone = getDeviceTimezone();

  const { control, handleSubmit, formState } = useForm<RegisterValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: "", password: "", timezone }
  });

  const submit = handleSubmit(async (values) => {
    if (isSubmittingRef.current) {
      return;
    }

    isSubmittingRef.current = true;
    setSubmitError(null);

    try {
      await register(values);
      router.replace("/(app)/timeline");
    } catch (error) {
      setSubmitError(getApiErrorMessage(error));
    } finally {
      isSubmittingRef.current = false;
    }
  });

  return (
    <Screen title="Create account" subtitle="Start your routine with Twelve Daily">
      <FormInput control={control} name="email" label="Email" placeholder="name@email.com" />
      <FormInput control={control} name="password" label="Password" secureTextEntry />
      <TimezoneSelect control={control} name="timezone" label="Timezone" />

      <TouchableOpacity
        style={[styles.button, formState.isSubmitting ? styles.buttonDisabled : null]}
        onPress={submit}
        disabled={formState.isSubmitting}>
        <Text style={styles.buttonText}>
          {formState.isSubmitting ? "Creating..." : "Create account"}
        </Text>
      </TouchableOpacity>

      {submitError ? <Text style={styles.errorText}>{submitError}</Text> : null}

      <View style={styles.footer}>
        <Text style={styles.footerText}>Already have an account?</Text>
        <Link href="/(auth)/login" asChild>
          <TouchableOpacity>
            <Text style={styles.linkText}>Sign in</Text>
          </TouchableOpacity>
        </Link>
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  button: {
    marginTop: 8,
    borderRadius: 12,
    backgroundColor: colors.accent,
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
  },
  errorText: {
    marginTop: 10,
    textAlign: "center",
    color: colors.error
  },
  footer: {
    marginTop: 16,
    flexDirection: "row",
    justifyContent: "center",
    gap: 4
  },
  footerText: {
    color: colors.textSecondary
  },
  linkText: {
    fontWeight: "600",
    color: colors.accentSoft
  }
});
