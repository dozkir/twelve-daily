import type { CreateHabitScheduleRequest, HabitDetail } from "@twelve-daily/api-client";

import { formatShortTime } from "@/src/date";

export const dayOptions = [
  "Sunday",
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday"
] as const;

export type DayOption = typeof dayOptions[number];

export interface HabitDayScheduleFormValue {
  enabled: boolean;
  startTime: string;
  endTime: string;
}

export type HabitDaySchedules = Record<DayOption, HabitDayScheduleFormValue>;

export interface HabitFormValues {
  name: string;
  emoji: string;
  description: string;
  useDifferentTimesByDay: boolean;
  daysOfWeek: DayOption[];
  startTime: string;
  endTime: string;
  daySchedules: HabitDaySchedules;
}

export const getTodayDayName = (): DayOption => dayOptions[new Date().getDay()];

export const createDaySchedules = (
  selectedDays: readonly DayOption[] = [getTodayDayName()],
  startTime = "07:00",
  endTime = "08:00"
): HabitDaySchedules => Object.fromEntries(
  dayOptions.map((day) => [day, {
    enabled: selectedDays.includes(day),
    startTime,
    endTime
  }])
) as HabitDaySchedules;

export const getDefaultHabitFormValues = (): HabitFormValues => {
  const selectedDays = [getTodayDayName()];

  return {
    name: "",
    emoji: "✨",
    description: "",
    useDifferentTimesByDay: false,
    daysOfWeek: selectedDays,
    startTime: "07:00",
    endTime: "08:00",
    daySchedules: createDaySchedules(selectedDays, "07:00", "08:00")
  };
};

const isDayOption = (value: string): value is DayOption => (
  dayOptions.includes(value as DayOption)
);

export const buildHabitFormInitialValues = (habit: HabitDetail): HabitFormValues => {
  const normalizedSchedules = habit.schedules
    .filter((schedule): schedule is HabitDetail["schedules"][number] & { dayOfWeek: DayOption } => isDayOption(schedule.dayOfWeek))
    .sort((left, right) => dayOptions.indexOf(left.dayOfWeek) - dayOptions.indexOf(right.dayOfWeek));
  const activeSchedules = normalizedSchedules.filter((schedule) => schedule.isActive);
  const selectedSchedules = activeSchedules.length > 0 ? activeSchedules : normalizedSchedules;
  const selectedDays = selectedSchedules.length > 0 ? selectedSchedules.map((schedule) => schedule.dayOfWeek) : [getTodayDayName()];
  const firstSchedule = selectedSchedules[0];
  const sharedStartTime = formatShortTime(firstSchedule?.startTime ?? "07:00");
  const sharedEndTime = formatShortTime(firstSchedule?.endTime ?? "08:00");
  const daySchedules = createDaySchedules(selectedDays, sharedStartTime, sharedEndTime);

  selectedSchedules.forEach((schedule) => {
    daySchedules[schedule.dayOfWeek] = {
      enabled: true,
      startTime: formatShortTime(schedule.startTime),
      endTime: formatShortTime(schedule.endTime)
    };
  });

  const uniqueTimeRanges = new Set(selectedSchedules.map((schedule) => `${formatShortTime(schedule.startTime)}-${formatShortTime(schedule.endTime)}`));

  return {
    name: habit.name,
    emoji: habit.emoji,
    description: habit.description ?? "",
    useDifferentTimesByDay: uniqueTimeRanges.size > 1,
    daysOfWeek: selectedDays,
    startTime: sharedStartTime,
    endTime: sharedEndTime,
    daySchedules
  };
};

export const buildHabitSchedulesPayload = (values: HabitFormValues): CreateHabitScheduleRequest[] => {
  if (values.useDifferentTimesByDay) {
    return dayOptions
      .filter((day) => values.daySchedules[day].enabled)
      .map((day) => ({
        dayOfWeek: day,
        startTime: values.daySchedules[day].startTime,
        endTime: values.daySchedules[day].endTime,
        isActive: true
      }));
  }

  return values.daysOfWeek.map((dayOfWeek) => ({
    dayOfWeek,
    startTime: values.startTime,
    endTime: values.endTime,
    isActive: true
  }));
};

