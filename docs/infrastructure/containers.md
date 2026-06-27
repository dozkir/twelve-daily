# Infraestrutura — Containers

## Visão Geral

| Container | Ambiente | Descrição |
|---|---|---|
| `db` | Local + Produção (`onze`) | PostgreSQL (dedicado ao twelve-daily) |
| `api` | Local + Produção (`onze`) | TwelveDaily.Api (.NET) |
| `web` | Produção (`onze`) | Expo export estático (nginx) atrás do Caddy |
| `caddy` | Produção (`onze`) | Reverse proxy compartilhado + HTTPS automático |

> Em **desenvolvimento** o front-end web roda sem container: `npx expo start --web`
> (servidor Node nativo). Em **produção** no `onze`, o bundle estático (`expo export`) é
> servido por um container `web` atrás do Caddy. Ver [deployment.md](deployment.md).

---

## docker-compose.yml (local)

Orquestra `db` + `api` para desenvolvimento local.

```yaml
services:
  db:
    image: postgres:17
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

  api:
    build:
      context: ./apps/api
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__Default: Host=db;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      Jwt__Secret: ${JWT_SECRET}
      Jwt__Issuer: ${JWT_ISSUER}
      Jwt__Audience: ${JWT_AUDIENCE}
    ports:
      - "5000:8080"
    depends_on:
      - db

volumes:
  pgdata:
```

> Variáveis de ambiente via `.env` na raiz (não versionado).

---

## Dockerfile — API

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TwelveDaily.Api/TwelveDaily.Api.csproj", "TwelveDaily.Api/"]
COPY ["TwelveDaily.Application/TwelveDaily.Application.csproj", "TwelveDaily.Application/"]
COPY ["TwelveDaily.Domain/TwelveDaily.Domain.csproj", "TwelveDaily.Domain/"]
COPY ["TwelveDaily.Infrastructure/TwelveDaily.Infrastructure.csproj", "TwelveDaily.Infrastructure/"]
RUN dotnet restore "TwelveDaily.Api/TwelveDaily.Api.csproj"
COPY . .
RUN dotnet publish "TwelveDaily.Api/TwelveDaily.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TwelveDaily.Api.dll"]
```

---

## Mobile

Não é conteinerizado — ciclo de vida diferente:

| Etapa | Como |
|---|---|
| Desenvolvimento | `npx expo start` → QR code → **Expo Go** no celular |
| Build | **EAS Build** (nuvem Expo — sem necessidade de Mac/Xcode local) |
| Produção | **App Store** (iOS) + **Google Play** (Android) via EAS Submit |

