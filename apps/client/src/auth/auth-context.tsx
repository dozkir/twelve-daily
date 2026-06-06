import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import type { AuthTokens, LoginRequest, RegisterRequest } from "@twelve-daily/api-client";
import { createApiClient } from "@twelve-daily/api-client";

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

const useAuthState = () => {
  const [isReady, setIsReady] = useState(false);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [refreshToken, setRefreshToken] = useState<string | null>(null);

  const storeTokens = useCallback(async (tokens: AuthTokens) => {
    setAccessToken(tokens.accessToken);
    setRefreshToken(tokens.refreshToken);
    await tokenStorage.setRefreshToken(tokens.refreshToken);
  }, []);

  return {
    isReady,
    setIsReady,
    accessToken,
    setAccessToken,
    refreshToken,
    setRefreshToken,
    storeTokens
  };
};

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const { isReady, setIsReady, accessToken, setAccessToken, refreshToken, setRefreshToken, storeTokens } =
    useAuthState();
  const hasBootstrappedRef = useRef(false);

  const api = useMemo(
    () =>
      createApiClient({
        baseUrl: API_URL,
        getAccessToken: () => accessToken,
        onUnauthorized: async () => {
          setAccessToken(null);
          setRefreshToken(null);
          await tokenStorage.clearRefreshToken();
        }
      }),
    [accessToken, setAccessToken, setRefreshToken]
  );

  const refresh = useCallback(async () => {
    const token = refreshToken ?? (await tokenStorage.getRefreshToken());

    if (!token) {
      setAccessToken(null);
      setRefreshToken(null);
      return;
    }

    try {
      const tokens = await api.refresh(token);
      await storeTokens(tokens);
    } catch {
      setAccessToken(null);
      setRefreshToken(null);
      await tokenStorage.clearRefreshToken();
    }
  }, [api, refreshToken, setAccessToken, setRefreshToken, storeTokens]);

  const login = useCallback(
    async (payload: LoginRequest) => {
      const tokens = await api.login(payload);
      await storeTokens(tokens);
    },
    [api, storeTokens]
  );

  const register = useCallback(
    async (payload: RegisterRequest) => {
      const tokens = await api.register(payload);
      await storeTokens(tokens);
    },
    [api, storeTokens]
  );

  const logout = useCallback(async () => {
    const token = refreshToken ?? (await tokenStorage.getRefreshToken());

    try {
      if (token) {
        await api.logout(token);
      }
    } finally {
      setAccessToken(null);
      setRefreshToken(null);
      await tokenStorage.clearRefreshToken();
    }
  }, [api, refreshToken, setAccessToken, setRefreshToken]);

  useEffect(() => {
    if (hasBootstrappedRef.current) {
      return;
    }

    hasBootstrappedRef.current = true;

    void (async () => {
      await refresh();
      setIsReady(true);
    })();
  }, [refresh, setIsReady]);

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

