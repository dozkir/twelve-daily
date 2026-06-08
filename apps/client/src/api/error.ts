import axios from "axios";

export const getApiErrorMessage = (error: unknown) => {
  if (!axios.isAxiosError(error)) {
    return "Unexpected error. Try again.";
  }

  if (error.code === "ERR_NETWORK") {
    return "Could not connect to the API. Verify if backend is running and EXPO_PUBLIC_API_URL is correct.";
  }

  const responseMessage = error.response?.data?.message;
  if (typeof responseMessage === "string" && responseMessage.trim().length > 0) {
    return responseMessage;
  }

  if (typeof error.message === "string" && error.message.trim().length > 0) {
    return error.message;
  }

  return "Request failed. Try again.";
};

