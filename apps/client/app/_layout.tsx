import { Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";

import "@/src/notifications/background";

import { AuthProvider } from "@/src/auth/auth-context";
import { NotificationProvider } from "@/src/notifications/provider";

export default function RootLayout() {
  const [queryClient] = useState(() => new QueryClient());

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <NotificationProvider>
          <StatusBar style="light" />
          <Stack screenOptions={{ headerShown: false }} />
        </NotificationProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
}

