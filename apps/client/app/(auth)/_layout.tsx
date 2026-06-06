import { Redirect, Stack } from "expo-router";

import { useAuth } from "@/src/auth/auth-context";

export default function AuthLayout() {
  const { isReady, isAuthenticated } = useAuth();

  if (!isReady) {
    return null;
  }

  if (isAuthenticated) {
    return <Redirect href="/(app)/timeline" />;
  }

  return <Stack screenOptions={{ headerShown: false }} />;
}

