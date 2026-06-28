# Imagem da Web (cliente Expo exportado como estático, servido por nginx).
#
# IMPORTANTE: o contexto de build é a RAIZ do monorepo, porque o cliente depende
# de packages/api-client via "file:../../packages/api-client".
#
# Build (a partir da raiz do repo):
#   docker build -f apps/client/Dockerfile \
#     --build-arg EXPO_PUBLIC_API_URL=https://api-twelvedaily.doze.dev.br \
#     -t twelve-daily-web .

# --- Stage 1: build do export web ---
FROM node:22-bookworm-slim AS build
WORKDIR /repo

# Copia o package consumido como fonte TS + o app cliente.
COPY packages/api-client packages/api-client
COPY apps/client apps/client

WORKDIR /repo/apps/client
RUN npm install

# A URL da API é embutida no bundle EM TEMPO DE BUILD (prefixo EXPO_PUBLIC_).
ARG EXPO_PUBLIC_API_URL
ENV EXPO_PUBLIC_API_URL=${EXPO_PUBLIC_API_URL}
RUN npm run build   # expo export --platform web → apps/client/dist

# --- Stage 2: nginx servindo o estático ---
FROM nginx:alpine AS final
COPY apps/client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /repo/apps/client/dist /usr/share/nginx/html
EXPOSE 80
