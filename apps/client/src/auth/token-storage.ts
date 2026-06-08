import AsyncStorage from "@react-native-async-storage/async-storage";
import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";

const REFRESH_TOKEN_KEY = "td.refreshToken";

export const tokenStorage = {
  async getRefreshToken(): Promise<string | null> {
    if (Platform.OS === "web") {
      return AsyncStorage.getItem(REFRESH_TOKEN_KEY);
    }

    return SecureStore.getItemAsync(REFRESH_TOKEN_KEY);
  },

  async setRefreshToken(value: string): Promise<void> {
    if (Platform.OS === "web") {
      await AsyncStorage.setItem(REFRESH_TOKEN_KEY, value);
      return;
    }

    await SecureStore.setItemAsync(REFRESH_TOKEN_KEY, value);
  },

  async clearRefreshToken(): Promise<void> {
    if (Platform.OS === "web") {
      await AsyncStorage.removeItem(REFRESH_TOKEN_KEY);
      return;
    }

    await SecureStore.deleteItemAsync(REFRESH_TOKEN_KEY);
  }
};

