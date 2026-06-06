export type DayType = "past" | "today" | "future";

export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface HabitCheckResponse {
  habitId: string;
  date: string;
  checkedAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  timezone: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface DailyHabitItem {
  habitId: string;
  name: string;
  emoji: string;
  description?: string | null;
  startTime: string;
  endTime: string;
  checkedAt?: string | null;
}

export interface DailyHabitDay {
  date: string;
  type: DayType;
  items: DailyHabitItem[];
}

export interface DailyHabitsResponse {
  days: DailyHabitDay[];
}

export interface HabitListItem {
  id: string;
  name: string;
  emoji: string;
  description?: string | null;
  isActive: boolean;
  syncGoogleCalendar: boolean;
}

export interface HabitSchedule {
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  isActive: boolean;
}

export interface HabitDetail {
  id: string;
  name: string;
  emoji: string;
  description?: string | null;
  isActive: boolean;
  syncGoogleCalendar: boolean;
  schedules: HabitSchedule[];
}

export interface CreateHabitScheduleRequest {
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  isActive: boolean;
}

export interface CreateHabitRequest {
  name: string;
  emoji: string;
  description?: string;
  syncGoogleCalendar: boolean;
  schedules: CreateHabitScheduleRequest[];
}

export interface UpdateHabitRequest {
  name: string;
  emoji: string;
  description?: string;
  syncGoogleCalendar: boolean;
}

export interface UpdateHabitSchedulesRequest {
  schedules: CreateHabitScheduleRequest[];
}

export interface DayCompletion {
  date: string;
  total: number;
  completed: number;
}

export interface WeeklyDashboardResponse {
  total: number;
  completed: number;
  completionRate: number;
  dayByDay: DayCompletion[];
}

export interface UserProfile {
  id: string;
  email: string;
  timezone: string;
  createdAt: string;
}

export interface RegisterPushTokenRequest {
  token: string;
  deviceLabel?: string | null;
}

