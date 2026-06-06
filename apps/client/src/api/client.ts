import { createApiClient } from "@twelve-daily/api-client";

import { API_URL } from "@/src/config";

export const makeAuthedClient = (
  accessToken: string | null,
  onUnauthorized: () => Promise<void> | void
) => {
  return createApiClient({
    baseUrl: API_URL,
    getAccessToken: () => accessToken,
    onUnauthorized
  });
};

