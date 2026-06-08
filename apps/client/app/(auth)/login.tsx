import { Link, router } from "expo-router";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";

import { useAuth } from "@/src/auth/auth-context";
import { getApiErrorMessage } from "@/src/api/error";
import { colors } from "@/src/theme";
import { FormInput } from "@/src/ui/form-input";
import { Screen } from "@/src/ui/screen";

const schema = z.object({
  email: z.string().email("Invalid email"),
  password: z.string().min(1, "Password is required")
});

type LoginValues = z.infer<typeof schema>;

export default function LoginScreen() {
  const { login } = useAuth();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const { control, handleSubmit, formState } = useForm<LoginValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: "", password: "" }
  });

  const submit = handleSubmit(async (values) => {
    setSubmitError(null);

    try {
      await login(values);
      router.replace("/(app)/timeline");
    } catch (error) {
      setSubmitError(getApiErrorMessage(error));
    }
  });

  return (
    <Screen title="Welcome back" subtitle="Log in to track your day">
      <FormInput control={control} name="email" label="Email" placeholder="name@email.com" />
      <FormInput control={control} name="password" label="Password" secureTextEntry />

      <TouchableOpacity style={styles.button} onPress={submit}>
        <Text style={styles.buttonText}>
          {formState.isSubmitting ? "Signing in..." : "Sign in"}
        </Text>
      </TouchableOpacity>

      {submitError ? <Text style={styles.errorText}>{submitError}</Text> : null}

      <View style={styles.footer}>
        <Text style={styles.footerText}>No account yet?</Text>
        <Link href="/(auth)/register" asChild>
          <TouchableOpacity>
            <Text style={styles.linkText}>Create one</Text>
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
