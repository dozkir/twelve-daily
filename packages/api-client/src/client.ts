import axios from "axios";
import type {
  AuthTokens,
  CreateHabitRequest,
  DailyHabitsResponse,
  HabitCheckResponse,
  HabitDetail,
  HabitListItem,
  LoginRequest,
  RegisterPushTokenRequest,
  RegisterRequest,
  UpdateHabitRequest,
  UpdateHabitSchedulesRequest,
  UserProfile,
  WeeklyDashboardResponse
} from "./types";

export interface ApiClientOptions {
  baseUrl: string;
  getAccessToken: () => string | null;
  onUnauthorized?: () => Promise<void> | void;
}

export const createApiClient = ({ baseUrl, getAccessToken, onUnauthorized }: ApiClientOptions) => {
  const api = axios.create({
    baseURL: baseUrl
  });

  api.interceptors.request.use((config) => {
    const token = getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  });

  api.interceptors.response.use(
    (response) => response,
    async (error) => {
      if (error?.response?.status === 401 && onUnauthorized) {
        await onUnauthorized();
      }

      return Promise.reject(error);
    }
  );

  return {
    async register(payload: RegisterRequest): Promise<AuthTokens> {
      const response = await api.post<AuthTokens>("/auth/register", payload);
      return response.data;
    },

    async login(payload: LoginRequest): Promise<AuthTokens> {
      const response = await api.post<AuthTokens>("/auth/login", payload);
      return response.data;
    },

    async refresh(refreshToken: string): Promise<AuthTokens> {
      const response = await api.post<AuthTokens>("/auth/refresh", { refreshToken });
      return response.data;
    },

    async getDailyHabits(date: string): Promise<DailyHabitsResponse> {
      const response = await api.get<DailyHabitsResponse>("/habits/daily", {
        params: { date }
      });
      return response.data;
    },

    async checkHabit(habitId: string, date: string): Promise<HabitCheckResponse> {
      const response = await api.put<HabitCheckResponse>(`/habits/${habitId}/check`, { date });
      return response.data;
    },

    async uncheckHabit(habitId: string, date: string): Promise<void> {
      await api.delete(`/habits/${habitId}/check`, { params: { date } });
    },

    async checkHabitFromNotification(habitId: string, date: string, actionToken: string): Promise<HabitCheckResponse> {
      const response = await api.post<HabitCheckResponse>(`/habits/${habitId}/check/from-notification`, {
        date,
        actionToken
      });
      return response.data;
    },

    async getHabitsList(): Promise<HabitListItem[]> {
      const response = await api.get<HabitListItem[]>("/habits");
      return response.data;
    },

    async getHabitDetail(id: string): Promise<HabitDetail> {
      const response = await api.get<HabitDetail>(`/habits/${id}`);
      return response.data;
    },

    async createHabit(payload: CreateHabitRequest): Promise<string> {
      const response = await api.post<string>("/habits", payload);
      return response.data;
    },

    async updateHabit(id: string, payload: UpdateHabitRequest): Promise<void> {
      await api.put(`/habits/${id}`, payload);
    },

    async updateHabitSchedules(id: string, payload: UpdateHabitSchedulesRequest): Promise<void> {
      await api.put(`/habits/${id}/schedules`, payload);
    },

    async toggleHabit(id: string): Promise<void> {
      await api.patch(`/habits/${id}/toggle`);
    },

    async deleteHabit(id: string): Promise<void> {
      await api.delete(`/habits/${id}`);
    },

    async getWeeklyDashboard(weekStart: string): Promise<WeeklyDashboardResponse> {
      const response = await api.get<WeeklyDashboardResponse>("/dashboard/weekly", {
        params: { weekStart }
      });
      return response.data;
    },

    async getProfile(): Promise<UserProfile> {
      const response = await api.get<UserProfile>("/users/me");
      return response.data;
    },

    async updateTimezone(timezone: string): Promise<void> {
      await api.put("/users/me/timezone", { timezone });
    },

    async updatePassword(currentPassword: string, newPassword: string): Promise<void> {
      await api.put("/users/me/password", { currentPassword, newPassword });
    },

    async registerPushToken(payload: RegisterPushTokenRequest): Promise<void> {
      await api.post("/users/push-token", payload);
    },

    async sendRemotePushTest(): Promise<void> {
      await api.post("/users/push-test");
    },

    async syncActivePushNotification(): Promise<void> {
      await api.post("/users/push-sync");
    },

    async logout(refreshToken: string): Promise<void> {
      await api.post("/auth/logout", { refreshToken });
    },

    async logoutAll(): Promise<void> {
      await api.post("/auth/logout-all");
    }
  };
};

