import { defineConfig } from "orval";

export default defineConfig({
  api: {
    input: {
      target: process.env.ORVAL_OPENAPI_URL ?? "http://localhost:5275/swagger/v1/swagger.json"
    },
    output: {
      target: "./packages/api-client/src/generated/client.ts",
      schemas: "./packages/api-client/src/generated/model",
      client: "react-query",
      mode: "split",
      httpClient: "axios",
      override: {
        mutator: {
          path: "./packages/api-client/src/http/mutator.ts",
          name: "customInstance"
        }
      }
    }
  }
});

