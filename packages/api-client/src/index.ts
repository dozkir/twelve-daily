// Legacy hand-written client — being migrated to the orval-generated hooks below.
export * from "./client";
export * from "./types";

// orval-generated React Query hooks + request functions.
export * from "./generated/client";
export { configureApiClient, type ApiClientConfig } from "./http/mutator";
