import {
  CLEAR_NEXT_HABIT_NOTIFICATION_TYPE,
  NEXT_HABIT_NOTIFICATION_TYPE
} from "@/src/notifications/constants";

export interface NextHabitNotificationPayload {
  type: typeof NEXT_HABIT_NOTIFICATION_TYPE;
  habitId: string;
  date: string;
  habitName: string;
  habitEmoji: string;
  scheduledStartTime: string;
  scheduledEndTime: string;
  actionToken: string;
}

export interface ClearNextHabitNotificationPayload {
  type: typeof CLEAR_NEXT_HABIT_NOTIFICATION_TYPE;
}

export type HabitNotificationPayload =
  | NextHabitNotificationPayload
  | ClearNextHabitNotificationPayload;

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

const getString = (record: Record<string, unknown>, key: string) => {
  const value = record[key];
  return typeof value === "string" ? value : null;
};

export const parseHabitNotificationPayload = (
  input: unknown
): HabitNotificationPayload | null => {
  if (!isRecord(input)) {
    return null;
  }

  const type = getString(input, "type");
  if (type === CLEAR_NEXT_HABIT_NOTIFICATION_TYPE) {
    return { type };
  }

  if (type !== NEXT_HABIT_NOTIFICATION_TYPE) {
    return null;
  }

  const habitId = getString(input, "habitId");
  const date = getString(input, "date");
  const habitName = getString(input, "habitName") ?? "Hábito";
  const habitEmoji = getString(input, "habitEmoji") ?? "✅";
  const scheduledStartTime = getString(input, "scheduledStartTime");
  const scheduledEndTime = getString(input, "scheduledEndTime");
  const actionToken = getString(input, "actionToken");

  if (!habitId || !date || !scheduledStartTime || !scheduledEndTime || !actionToken) {
    return null;
  }

  return {
    type,
    habitId,
    date,
    habitName,
    habitEmoji,
    scheduledStartTime,
    scheduledEndTime,
    actionToken
  };
};

