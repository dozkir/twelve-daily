import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import {
  authLogin,
  authLogout,
  authRefresh,
  authRegister,
  configureApiClient,
  type AuthResult,
  type LoginRequest,
  type RegisterRequest
} from "@twelve-daily/api-client";

import { API_URL } from "@/src/config";
import { tokenStorage } from "@/src/auth/token-storage";

interface AuthContextValue {
  isReady: boolean;
  isAuthenticated: boolean;
  accessToken: string | null;
  login: (payload: LoginRequest) => Promise<void>;
  register: (payload: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refresh: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isReady, setIsReady] = useState(false);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [refreshToken, setRefreshToken] = useState<string | null>(null);
  const hasBootstrappedRef = useRef(false);

  // The generated client reads the token lazily through this ref, so a single
  // axios configuration stays valid as the token changes.
  const accessTokenRef = useRef<string | null>(null);
  accessTokenRef.current = accessToken;

  const storeTokens = useCallback(async (tokens: AuthResult) => {
    setAccessToken(tokens.accessToken);
    setRefreshToken(tokens.refreshToken);
    await tokenStorage.setRefreshToken(tokens.refreshToken);
  }, []);

  const clearSession = useCallback(async () => {
    setAccessToken(null);
    setRefreshToken(null);
    await tokenStorage.clearRefreshToken();
  }, []);

  // Configure the shared axios instance once, before any request is fired.
  const isConfiguredRef = useRef(false);
  if (!isConfiguredRef.current) {
    configureApiClient({
      baseUrl: API_URL,
      getAccessToken: () => accessTokenRef.current,
      onUnauthorized: clearSession
    });
    isConfiguredRef.current = true;
  }

  const refresh = useCallback(async () => {
    const token = refreshToken ?? (await tokenStorage.getRefreshToken());

    if (!token) {
      await clearSession();
      return;
    }

    try {
      const tokens = await authRefresh({ refreshToken: token });
      await storeTokens(tokens);
    } catch {
      await clearSession();
    }
  }, [refreshToken, clearSession, storeTokens]);

  const login = useCallback(
    async (payload: LoginRequest) => {
      const tokens = await authLogin(payload);
      await storeTokens(tokens);
    },
    [storeTokens]
  );

  const register = useCallback(
    async (payload: RegisterRequest) => {
      const tokens = await authRegister(payload);
      await storeTokens(tokens);
    },
    [storeTokens]
  );

  const logout = useCallback(async () => {
    const token = refreshToken ?? (await tokenStorage.getRefreshToken());

    try {
      if (token) {
        await authLogout({ refreshToken: token });
      }
    } finally {
      await clearSession();
    }
  }, [refreshToken, clearSession]);

  useEffect(() => {
    if (hasBootstrappedRef.current) {
      return;
    }

    hasBootstrappedRef.current = true;

    void (async () => {
      await refresh();
      setIsReady(true);
    })();
  }, [refresh]);

  const value: AuthContextValue = {
    isReady,
    isAuthenticated: !!accessToken,
    accessToken,
    login,
    register,
    logout,
    refresh
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider");
  }

  return context;
};
