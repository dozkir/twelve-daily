export const toIsoDate = (date: Date): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
};

const parseIsoDate = (isoDate: string): Date => {
  const [year, month, day] = isoDate.split("-").map(Number);
  return new Date(year, (month || 1) - 1, day || 1, 12, 0, 0, 0);
};

export const shiftIsoDate = (isoDate: string, days: number): string => {
  const value = parseIsoDate(isoDate);
  value.setDate(value.getDate() + days);
  return toIsoDate(value);
};

export const formatTimelineDayLabel = (isoDate: string, todayIsoDate = toIsoDate(new Date())): string => {
  if (isoDate === todayIsoDate) {
    return "Today";
  }

  return new Intl.DateTimeFormat("en-US", { weekday: "long" }).format(parseIsoDate(isoDate));
};

export const formatTimelineDateLabel = (isoDate: string): string => {
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric"
  }).format(parseIsoDate(isoDate));
};

export const startOfWeekIso = (date: Date): string => {
  const copy = new Date(date);
  const day = copy.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  copy.setDate(copy.getDate() + diff);

  return toIsoDate(copy);
};

export const parseTimeToMinutes = (time?: string | null): number | null => {
  if (typeof time !== "string") {
    return null;
  }

  const trimmedTime = time.trim();

  if (!trimmedTime) {
    return null;
  }

  const match = /^(\d{1,2}):(\d{2})(?::\d{2})?$/.exec(trimmedTime);

  if (!match) {
    return null;
  }

  const hours = Number(match[1]);
  const minutes = Number(match[2]);

  if (!Number.isInteger(hours) || !Number.isInteger(minutes) || hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
    return null;
  }

  return (hours * 60) + minutes;
};

export const formatShortTime = (time?: string | null): string => {
  if (typeof time !== "string") {
    return "";
  }

  const trimmedTime = time.trim();
  const match = /^(\d{1,2}):(\d{2})(?::\d{2})?$/.exec(trimmedTime);

  if (!match) {
    return trimmedTime;
  }

  return `${String(Number(match[1])).padStart(2, "0")}:${match[2]}`;
};

export const timeStringToDate = (time?: string | null, baseDate = new Date()): Date => {
  const value = new Date(baseDate);
  const totalMinutes = parseTimeToMinutes(time);

  value.setSeconds(0, 0);

  if (totalMinutes === null) {
    value.setHours(0, 0, 0, 0);
    return value;
  }

  value.setHours(Math.floor(totalMinutes / 60), totalMinutes % 60, 0, 0);
  return value;
};

export const formatTimeValue = (date: Date): string => {
  return `${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
};

export const formatHourLabel = (hour: number): string => `${String(hour).padStart(2, "0")}:00`;

export const buildHourRange = (startHour: number, endHourExclusive: number): number[] => {
  return Array.from({ length: Math.max(endHourExclusive - startHour, 0) }, (_, index) => startHour + index);
};

