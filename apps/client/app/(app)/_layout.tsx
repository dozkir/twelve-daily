import { Ionicons } from "@expo/vector-icons";
import { Redirect, Tabs } from "expo-router";

import { useAuth } from "@/src/auth/auth-context";
import { colors } from "@/src/theme";

function getTabBarIcon(routeName: string, focused: boolean, color: string, size: number) {
  const iconName = (() => {
    switch (routeName) {
      case "timeline/index":
        return focused ? "time" : "time-outline";
      case "habits/index":
        return focused ? "list" : "list-outline";
      case "dashboard/index":
        return focused ? "grid" : "grid-outline";
      case "settings/index":
        return focused ? "settings" : "settings-outline";
      default:
        return focused ? "ellipse" : "ellipse-outline";
    }
  })();

  return <Ionicons name={iconName} size={size} color={color} />;
}

export default function AppLayout() {
  const { isReady, isAuthenticated } = useAuth();

  if (!isReady) {
    return null;
  }

  if (!isAuthenticated) {
    return <Redirect href="/(auth)/login" />;
  }

  return (
    <Tabs screenOptions={({ route }) => ({
      headerShown: false,
      tabBarStyle: {
        backgroundColor: colors.surface,
        borderTopColor: colors.border
      },
      tabBarActiveTintColor: colors.accentSoft,
      tabBarInactiveTintColor: colors.textMuted,
      tabBarIcon: ({ color, size, focused }) => getTabBarIcon(route.name, focused, color, size)
    })}>
      <Tabs.Screen name="timeline/index" options={{ title: "Timeline", tabBarLabel: "Timeline" }} />
      <Tabs.Screen name="habits/index" options={{ title: "Habits", tabBarLabel: "Habits" }} />
      <Tabs.Screen name="dashboard/index" options={{ title: "Dashboard", tabBarLabel: "Dashboard" }} />
      <Tabs.Screen name="settings/index" options={{ title: "Settings", tabBarLabel: "Settings" }} />
      <Tabs.Screen name="habits/new" options={{ title: "Create habit", href: null }} />
      <Tabs.Screen name="habits/[id]" options={{ title: "Edit habit", href: null }} />
    </Tabs>
  );
}

