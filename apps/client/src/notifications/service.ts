import AsyncStorage from "@react-native-async-storage/async-storage";
import notifee, {
  AndroidFlags,
  AndroidImportance,
  EventType,
  type Event
} from "@notifee/react-native";
import * as Application from "expo-application";
import Constants from "expo-constants";
import * as Device from "expo-device";
import * as Notifications from "expo-notifications";
import * as TaskManager from "expo-task-manager";
import { Platform } from "react-native";

import { API_URL } from "@/src/config";
import {
  ACTIVE_NOTIFICATION_ID_STORAGE_KEY,
  ACTIVE_NOTIFICATION_PAYLOAD_STORAGE_KEY,
  CLEAR_NEXT_HABIT_NOTIFICATION_TYPE,
  NEXT_HABIT_ANDROID_CHANNEL_ID,
  NEXT_HABIT_CATEGORY_ID,
  NEXT_HABIT_CHECK_ACTION_ID,
  NEXT_HABIT_NOTIFICATION_TYPE,
  NEXT_HABIT_TASK_NAME
} from "@/src/notifications/constants";
import {
  parseHabitNotificationPayload,
  type HabitNotificationPayload,
  type NextHabitNotificationPayload
} from "@/src/notifications/types";

const LOCAL_MIRROR_NOTIFICATION_SOURCE = "next-habit-local-mirror";
const ANDROID_ONGOING_NOTIFICATION_ID = "next-habit-active";

const getNotificationSource = (input: unknown) => {
  if (!input || typeof input !== "object") {
    return null;
  }

  const value = (input as Record<string, unknown>).presentationSource;
  return typeof value === "string" ? value : null;
};

Notifications.setNotificationHandler({
  handleNotification: async (notification) => {
    const shouldPresent =
      getNotificationSource(notification.request.content.data) ===
      LOCAL_MIRROR_NOTIFICATION_SOURCE;

    return {
      shouldShowBanner: shouldPresent,
      shouldShowList: shouldPresent,
    shouldPlaySound: false,
    shouldSetBadge: false
    };
  }
});

const isNative = Platform.OS === "ios" || Platform.OS === "android";
let registrationPromise: Promise<void> | null = null;

const getStoredActiveNotificationId = async () =>
  AsyncStorage.getItem(ACTIVE_NOTIFICATION_ID_STORAGE_KEY);

const getStoredActiveNotificationPayload = async () => {
  const raw = await AsyncStorage.getItem(ACTIVE_NOTIFICATION_PAYLOAD_STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    return parseHabitNotificationPayload(JSON.parse(raw));
  } catch {
    return null;
  }
};

const setStoredActiveNotificationId = async (notificationId: string | null) => {
  if (!notificationId) {
    await AsyncStorage.removeItem(ACTIVE_NOTIFICATION_ID_STORAGE_KEY);
    return;
  }

  await AsyncStorage.setItem(ACTIVE_NOTIFICATION_ID_STORAGE_KEY, notificationId);
};

const setStoredActiveNotificationPayload = async (
  payload: NextHabitNotificationPayload | null
) => {
  if (!payload) {
    await AsyncStorage.removeItem(ACTIVE_NOTIFICATION_PAYLOAD_STORAGE_KEY);
    return;
  }

  await AsyncStorage.setItem(
    ACTIVE_NOTIFICATION_PAYLOAD_STORAGE_KEY,
    JSON.stringify(payload)
  );
};

const buildLocalBody = (payload: NextHabitNotificationPayload) => {
  return `${payload.habitEmoji} ${payload.habitName}`;
};

const isNextHabitPayloadStillActive = (payload: NextHabitNotificationPayload) => {
  const scheduledEnd = new Date(payload.scheduledEndTime);
  if (Number.isNaN(scheduledEnd.getTime())) {
    return false;
  }

  return scheduledEnd.getTime() > Date.now();
};

const formatTime24h = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false
  });
};

const buildLocalTitle = (payload: NextHabitNotificationPayload) => {
  return `${formatTime24h(payload.scheduledStartTime)} - ${formatTime24h(payload.scheduledEndTime)}`;
};

const getNotificationPayloadData = (input: unknown) => {
  if (!input || typeof input !== "object") {
    return null;
  }

  const record = input as {
    request?: { content?: { data?: unknown } };
    data?: unknown;
  };

  if (record.request?.content?.data !== undefined) {
    return record.request.content.data;
  }

  if (record.data !== undefined) {
    return record.data;
  }

  return null;
};

export const clearPresentedNextHabitNotificationAsync = async () => {
  if (!isNative) {
    return;
  }

  if (Platform.OS === "android") {
    await setStoredActiveNotificationPayload(null);
    await notifee.cancelNotification(ANDROID_ONGOING_NOTIFICATION_ID);
    await setStoredActiveNotificationId(null);
    return;
  }

  await setStoredActiveNotificationPayload(null);

  const activeNotificationId = await getStoredActiveNotificationId();
  if (activeNotificationId) {
    try {
      await Notifications.dismissNotificationAsync(activeNotificationId);
    } catch {
      // Notification may already be gone; keep cleanup idempotent.
    }
  }

  await setStoredActiveNotificationId(null);
};

const presentNextHabitNotificationAsync = async (
  payload: NextHabitNotificationPayload
) => {
  await setStoredActiveNotificationPayload(payload);
  await clearPresentedNextHabitNotificationAsync();
  await setStoredActiveNotificationPayload(payload);

  if (Platform.OS === "android") {
    await notifee.displayNotification({
      id: ANDROID_ONGOING_NOTIFICATION_ID,
      title: buildLocalTitle(payload),
      body: buildLocalBody(payload),
      data: {
        ...payload,
        presentationSource: LOCAL_MIRROR_NOTIFICATION_SOURCE
      } as Record<string, string>,
      android: {
        channelId: NEXT_HABIT_ANDROID_CHANNEL_ID,
        color: "#171022",
        smallIcon: "ic_launcher",
        flags: [AndroidFlags.FLAG_NO_CLEAR],
        localOnly: true,
        onlyAlertOnce: true,
        ongoing: true,
        autoCancel: false
      }
    });

    await setStoredActiveNotificationId(ANDROID_ONGOING_NOTIFICATION_ID);
    return;
  }

  const notificationId = await Notifications.scheduleNotificationAsync({
    content: {
      title: buildLocalTitle(payload),
      body: buildLocalBody(payload),
      data: {
        ...payload,
        presentationSource: LOCAL_MIRROR_NOTIFICATION_SOURCE
      } as Record<string, unknown>,
      sound: false,
      categoryIdentifier: NEXT_HABIT_CATEGORY_ID
    },
    trigger: null
  });

  await setStoredActiveNotificationId(notificationId);
};

const restoreDismissedAndroidNotificationAsync = async () => {
  if (Platform.OS !== "android") {
    return;
  }

  const payload = await getStoredActiveNotificationPayload();
  if (!payload || payload.type !== NEXT_HABIT_NOTIFICATION_TYPE) {
    return;
  }

  if (!isNextHabitPayloadStillActive(payload)) {
    await setStoredActiveNotificationPayload(null);
    await setStoredActiveNotificationId(null);
    return;
  }

  await presentNextHabitNotificationAsync(payload);
};

const handleNotifeeEventAsync = async (event: Event) => {
  if (Platform.OS !== "android") {
    return;
  }

  if (event.type !== EventType.DISMISSED) {
    return;
  }

  const dismissedId = event.detail.notification?.id;
  if (dismissedId !== ANDROID_ONGOING_NOTIFICATION_ID) {
    return;
  }

  await restoreDismissedAndroidNotificationAsync();
};

if (Platform.OS === "android") {
  notifee.onBackgroundEvent(handleNotifeeEventAsync);
}

export const subscribeToNotifeeForegroundEvents = () => {
  if (Platform.OS !== "android") {
    return () => undefined;
  }

  return notifee.onForegroundEvent((event) => {
    void handleNotifeeEventAsync(event);
  });
};

const completeFromNotificationAsync = async (
  payload: NextHabitNotificationPayload
) => {
  const response = await fetch(
    `${API_URL}/habits/${payload.habitId}/check/from-notification`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        date: payload.date,
        actionToken: payload.actionToken
      })
    }
  );

  if (!response.ok) {
    const responseText = await response.text();
    throw new Error(
      responseText || `Failed to check habit from notification (${response.status}).`
    );
  }
};

export const handleHabitNotificationPayloadAsync = async (
  payload: HabitNotificationPayload | null
) => {
  if (!payload) {
    return;
  }

  if (payload.type === CLEAR_NEXT_HABIT_NOTIFICATION_TYPE) {
    await clearPresentedNextHabitNotificationAsync();
    return;
  }

  if (payload.type === NEXT_HABIT_NOTIFICATION_TYPE) {
    await presentNextHabitNotificationAsync(payload);
  }
};

export const handleReceivedNotificationAsync = async (
  notification: Notifications.Notification | null | undefined
) => {
  const source = getNotificationSource(getNotificationPayloadData(notification));
  if (source === LOCAL_MIRROR_NOTIFICATION_SOURCE) {
    return;
  }

  const payload = parseHabitNotificationPayload(getNotificationPayloadData(notification));
  await handleHabitNotificationPayloadAsync(payload);

  if (payload?.type === NEXT_HABIT_NOTIFICATION_TYPE) {
    const remoteNotificationId = notification?.request?.identifier;
    if (remoteNotificationId) {
      try {
        await Notifications.dismissNotificationAsync(remoteNotificationId);
      } catch {
        // Best effort: if the system notification is already gone, keep flow idempotent.
      }
    }
  }
};

export const handleNotificationResponseAsync = async (
  response: Notifications.NotificationResponse | null | undefined
) => {
  if (!response || response.actionIdentifier !== NEXT_HABIT_CHECK_ACTION_ID) {
    return false;
  }

  const payload = parseHabitNotificationPayload(
    getNotificationPayloadData(response.notification)
  );
  if (!payload || payload.type !== NEXT_HABIT_NOTIFICATION_TYPE) {
    return false;
  }

  await completeFromNotificationAsync(payload);
  await clearPresentedNextHabitNotificationAsync();
  return true;
};

export const handleNotificationTaskAsync = async (taskData: unknown) => {
  const record =
    taskData && typeof taskData === "object"
      ? (taskData as {
          actionIdentifier?: unknown;
          notification?: Notifications.Notification;
        })
      : null;

  const actionIdentifier =
    typeof record?.actionIdentifier === "string" ? record.actionIdentifier : null;

  const notification = record?.notification;

  if (actionIdentifier && notification) {
    await handleNotificationResponseAsync({
      actionIdentifier,
      notification
    } as Notifications.NotificationResponse);
    return;
  }

  if (notification) {
    await handleReceivedNotificationAsync(notification);
  }
};

if (isNative && !TaskManager.isTaskDefined(NEXT_HABIT_TASK_NAME)) {
  TaskManager.defineTask(NEXT_HABIT_TASK_NAME, async ({ data, error }) => {
    if (error) {
      console.warn("Notification background task failed", error);
      return;
    }

    try {
      await handleNotificationTaskAsync(data);
    } catch (taskError) {
      console.warn("Notification task handling failed", taskError);
    }
  });
}

export const ensureNotificationSetupAsync = async () => {
  if (!isNative) {
    return;
  }

  await Notifications.setNotificationCategoryAsync(NEXT_HABIT_CATEGORY_ID, [
    {
      identifier: NEXT_HABIT_CHECK_ACTION_ID,
      buttonTitle: "Check",
      options: {
        opensAppToForeground: false
      }
    }
  ]);

  if (Platform.OS === "android") {
    await Notifications.setNotificationChannelAsync(NEXT_HABIT_ANDROID_CHANNEL_ID, {
      name: "Próximo hábito",
      importance: Notifications.AndroidImportance.MAX,
      lockscreenVisibility: Notifications.AndroidNotificationVisibility.PUBLIC,
      bypassDnd: false,
      vibrationPattern: [0, 250, 150, 250],
      sound: null
    });

    await notifee.createChannel({
      id: NEXT_HABIT_ANDROID_CHANNEL_ID,
      name: "Próximo hábito",
      vibration: true,
      sound: undefined,
      importance: AndroidImportance.HIGH
    });
  }

  if (registrationPromise) {
    await registrationPromise;
    return;
  }

  registrationPromise = (async () => {
    const isRegistered = await TaskManager.isTaskRegisteredAsync(
      NEXT_HABIT_TASK_NAME
    );

    if (!isRegistered) {
      try {
        await Notifications.registerTaskAsync(NEXT_HABIT_TASK_NAME);
      } catch (error) {
        console.warn("Failed to register notification background task", error);
      }
    }
  })();

  await registrationPromise;
};

export const sendLocalTestNotificationAsync = async () => {
  if (!isNative || !Device.isDevice) {
    return false;
  }

  await ensureNotificationSetupAsync();

  const currentPermissions = await Notifications.getPermissionsAsync();
  let status = currentPermissions.status;

  if (status !== "granted") {
    const requestedPermissions = await Notifications.requestPermissionsAsync();
    status = requestedPermissions.status;
  }

  if (status !== "granted") {
    return false;
  }

  await Notifications.scheduleNotificationAsync({
    content: {
      title: "Teste de notificacao",
      body: "Se voce esta vendo isso, as notificacoes locais estao funcionando.",
      sound: false,
      ...(Platform.OS === "android"
        ? {
            channelId: NEXT_HABIT_ANDROID_CHANNEL_ID,
            priority: Notifications.AndroidNotificationPriority.MAX
          }
        : null)
    },
    trigger: null
  });

  return true;
};

export const getExpoPushTokenAsync = async () => {
  if (!isNative || !Device.isDevice) {
    return null;
  }

  const currentPermissions = await Notifications.getPermissionsAsync();
  let status = currentPermissions.status;

  if (status !== "granted") {
    const requestedPermissions = await Notifications.requestPermissionsAsync();
    status = requestedPermissions.status;
  }

  if (status !== "granted") {
    return null;
  }

  const projectId =
    Constants.easConfig?.projectId ??
    Constants.expoConfig?.extra?.eas?.projectId ??
    process.env.EXPO_PUBLIC_EAS_PROJECT_ID;

  if (!projectId) {
    console.warn(
      "Expo push projectId is not configured. Set EXPO_PUBLIC_EAS_PROJECT_ID to enable push token registration."
    );
    return null;
  }

  try {
    const token = await Notifications.getExpoPushTokenAsync({ projectId });
    return token.data;
  } catch (error) {
    console.warn(
      "Failed to get Expo push token. On Android, verify google-services.json and Firebase setup for this package.",
      error
    );
    return null;
  }
};

export const buildDeviceLabel = async () => {
  const name = Device.deviceName ?? Device.modelName ?? Application.applicationName;
  if (!name) {
    return Platform.OS === "ios" ? "iPhone" : "Android";
  }

  return name;
};

export const parseNotificationPayload = parseHabitNotificationPayload;




