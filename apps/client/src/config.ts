const DEFAULT_API_URL = "http://localhost:5000";

export const API_URL = (process.env.EXPO_PUBLIC_API_URL ?? DEFAULT_API_URL).replace(/\/$/, "");

