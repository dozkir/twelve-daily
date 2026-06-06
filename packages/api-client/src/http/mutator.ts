import axios, { type AxiosInstance, type AxiosRequestConfig } from "axios";

export const AXIOS_INSTANCE: AxiosInstance = axios.create();

export interface ApiClientConfig {
  baseUrl: string;
  getAccessToken: () => string | null;
  onUnauthorized?: () => Promise<void> | void;
}

let requestInterceptorId: number | null = null;
let responseInterceptorId: number | null = null;

/**
 * Wires the shared axios instance used by the orval-generated client: sets the
 * base URL, attaches the bearer token on every request, and runs `onUnauthorized`
 * on a 401. Call once during app bootstrap; safe to call again to re-configure
 * (previous interceptors are ejected).
 */
export const configureApiClient = ({ baseUrl, getAccessToken, onUnauthorized }: ApiClientConfig) => {
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
    async (error) => {
      if (error?.response?.status === 401 && onUnauthorized) {
        await onUnauthorized();
      }

      return Promise.reject(error);
    }
  );
};

export const customInstance = <T>(config: AxiosRequestConfig, options?: AxiosRequestConfig): Promise<T> => {
  return AXIOS_INSTANCE.request<T>({ ...config, ...options }).then(({ data }) => data);
};
