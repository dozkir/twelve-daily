import axios, {
  type AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig
} from "axios";

export const AXIOS_INSTANCE: AxiosInstance = axios.create();

export interface ApiClientConfig {
  baseUrl: string;
  getAccessToken: () => string | null;
  /**
   * Renova o access token a partir do refresh token persistido. Deve resolver com
   * o novo access token, ou `null` se a renovação falhar (refresh token inválido
   * ou expirado). Quando omitido, um 401 simplesmente dispara `onUnauthorized`.
   */
  refreshAccessToken?: () => Promise<string | null>;
  onUnauthorized?: () => Promise<void> | void;
}

let requestInterceptorId: number | null = null;
let responseInterceptorId: number | null = null;

/**
 * Single-flight: enquanto um refresh está em andamento, todas as requisições que
 * tomarem 401 aguardam a MESMA promessa em vez de dispararem refreshes paralelos.
 * O backend rotaciona o refresh token a cada uso, então refreshes concorrentes com
 * o mesmo token fariam o segundo falhar — e deslogariam o usuário sem necessidade.
 */
let refreshPromise: Promise<string | null> | null = null;

type RetriableConfig = InternalAxiosRequestConfig & { _retry?: boolean };

// Endpoints de autenticação (login/register/refresh/logout) não devem disparar o
// fluxo de refresh-and-retry: um 401 ali é uma falha legítima (ex.: credenciais
// inválidas, refresh token morto) que deve propagar, não ser re-tentada.
const isAuthRoute = (url?: string) => !!url && url.includes("/auth/");

/**
 * Wires the shared axios instance used by the orval-generated client: sets the
 * base URL, attaches the bearer token on every request, and handles 401s by
 * renovando o access token e re-tentando a requisição original uma vez. Só quando
 * a renovação falha é que `onUnauthorized` é chamado. Call once during app
 * bootstrap; safe to call again to re-configure (previous interceptors are ejected).
 */
export const configureApiClient = ({
  baseUrl,
  getAccessToken,
  refreshAccessToken,
  onUnauthorized
}: ApiClientConfig) => {
  AXIOS_INSTANCE.defaults.baseURL = baseUrl;

  if (requestInterceptorId !== null) {
    AXIOS_INSTANCE.interceptors.request.eject(requestInterceptorId);
  }
  requestInterceptorId = AXIOS_INSTANCE.interceptors.request.use((config) => {
    const token = getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  });

  if (responseInterceptorId !== null) {
    AXIOS_INSTANCE.interceptors.response.eject(responseInterceptorId);
  }
  responseInterceptorId = AXIOS_INSTANCE.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const config = error.config as RetriableConfig | undefined;
      const status = error.response?.status;

      const canRetry =
        status === 401 &&
        !!config &&
        !config._retry &&
        !isAuthRoute(config.url) &&
        !!refreshAccessToken;

      if (canRetry && config) {
        config._retry = true;

        // Compartilha um único refresh entre todas as requisições concorrentes.
        refreshPromise = refreshPromise ?? refreshAccessToken!();
        let newToken: string | null;
        try {
          newToken = await refreshPromise;
        } finally {
          refreshPromise = null;
        }

        if (newToken) {
          config.headers.Authorization = `Bearer ${newToken}`;
          return AXIOS_INSTANCE.request(config);
        }
      }

      if (status === 401 && onUnauthorized) {
        await onUnauthorized();
      }

      return Promise.reject(error);
    }
  );
};

export const customInstance = <T>(config: AxiosRequestConfig, options?: AxiosRequestConfig): Promise<T> => {
  return AXIOS_INSTANCE.request<T>({ ...config, ...options }).then(({ data }) => data);
};
