import { Redirect } from "expo-router";

import { useAuth } from "@/src/auth/auth-context";

export default function Index() {
  const { isReady, isAuthenticated } = useAuth();

  if (!isReady) {
    return null;
  }

  return <Redirect href={isAuthenticated ? "/(app)/timeline" : "/(auth)/login"} />;
}

