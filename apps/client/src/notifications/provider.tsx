import { usersRegisterPushToken, usersSyncActivePushNotification } from "@twelve-daily/api-client";
import { useQueryClient } from "@tanstack/react-query";
import * as Notifications from "expo-notifications";
import { AppState } from "react-native";
import { useEffect, useRef, type ReactNode } from "react";

import { useAuth } from "@/src/auth/auth-context";
import {
  buildDeviceLabel,
  clearPresentedNextHabitNotificationAsync,
  ensureNotificationSetupAsync,
  getExpoPushTokenAsync,
  handleNotificationResponseAsync,
  handleReceivedNotificationAsync,
  subscribeToNotifeeForegroundEvents
} from "@/src/notifications/service";

export const NotificationProvider = ({ children }: { children: ReactNode }) => {
  const { accessToken, isAuthenticated } = useAuth();
  const queryClient = useQueryClient();
  const lastRegisteredSignatureRef = useRef<string | null>(null);

  const syncActiveNotification = async () => {
    try {
      await usersSyncActivePushNotification();
    } catch (error) {
      console.warn("Active push notification sync failed", error);
    }
  };

  useEffect(() => {
    void ensureNotificationSetupAsync();
  }, []);

  useEffect(() => {
    const unsubscribeNotifeeForeground = subscribeToNotifeeForegroundEvents();
    const receivedSubscription = Notifications.addNotificationReceivedListener(
      (notification) => {
        void handleReceivedNotificationAsync(notification);
      }
    );

    const responseSubscription =
      Notifications.addNotificationResponseReceivedListener((response) => {
        void (async () => {
          const didComplete = await handleNotificationResponseAsync(response);
          if (!didComplete) {
            return;
          }

          await Promise.all([
            queryClient.invalidateQueries({ queryKey: ["daily"] }),
            queryClient.invalidateQueries({ queryKey: ["dashboard"] })
          ]);
        })();
      });

    return () => {
      unsubscribeNotifeeForeground();
      receivedSubscription.remove();
      responseSubscription.remove();
    };
  }, [queryClient]);

  useEffect(() => {
    const subscription = AppState.addEventListener("change", (nextState) => {
      if (!isAuthenticated || nextState !== "active") {
        return;
      }

      void syncActiveNotification();
    });

    return () => {
      subscription.remove();
    };
  }, [isAuthenticated]);

  useEffect(() => {
    if (!isAuthenticated) {
      lastRegisteredSignatureRef.current = null;
      void clearPresentedNextHabitNotificationAsync();
      return;
    }

    void (async () => {
      try {
        await ensureNotificationSetupAsync();

        const expoPushToken = await getExpoPushTokenAsync();
        if (!expoPushToken) {
          return;
        }

        const deviceLabel = await buildDeviceLabel();
        const signature = `${accessToken ?? ""}:${expoPushToken}:${deviceLabel}`;
        if (signature === lastRegisteredSignatureRef.current) {
          await syncActiveNotification();
          return;
        }

        await usersRegisterPushToken({
          token: expoPushToken,
          deviceLabel
        });

        await syncActiveNotification();

        lastRegisteredSignatureRef.current = signature;
      } catch (error) {
        console.warn("Push token registration skipped due to setup error", error);
      }
    })();
  }, [accessToken, isAuthenticated]);

  return children;
};

