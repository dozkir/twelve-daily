# Infraestrutura — CI/CD

## Repositório
GitHub (monorepo) — todos os apps e packages no mesmo repositório. Branch padrão: **`master`**.

> **Estado atual:** apenas o **pipeline de build/teste** abaixo está implementado (`.github/workflows/dotnet.yml`). Os pipelines de **deploy** (Fly.io / Azure / EAS) descritos mais adiante são **planejados — ainda não implementados**.

---

## Pipeline: Build & Test ✅

Disparado em todo push e PR para `master` (`.github/workflows/dotnet.yml`).

```yaml
jobs:
  # O id do job é "build" — é o status check obrigatório no ruleset de master.
  build:
    - dotnet restore
    - dotnet build --configuration Release
    - dotnet test TwelveDaily.UnitTests
    - dotnet test TwelveDaily.IntegrationTests  # requer Docker no runner
```

> GitHub Actions runners (`ubuntu-latest`) já têm Docker instalado — TestContainers funciona sem configuração extra.
> ⚠️ Não renomeie o job `build`: ele é o status check obrigatório do ruleset de `master`.

---

## Pipeline: Merge na `master` *(planejado)*

Disparado ao fazer merge em `master`.

```yaml
jobs:
  deploy-api:
    - Build da imagem Docker da API
    - Push para GitHub Container Registry (ghcr.io)
    - Deploy para Fly.io via flyctl

  deploy-web:
    - npx orval         ← regenera packages/api-client/ (garante tipos sincronizados)
    - npm run build     ← npx expo export --platform web
    - Deploy para Azure Static Web Apps
```

---

## Pipeline: Release Mobile

Disparado ao criar uma tag `v*` ou manualmente.

```yaml
jobs:
  eas-build:
    - npx eas build --platform all --non-interactive
    # Gera .ipa (iOS) e .apk/.aab (Android) na nuvem EAS
    # Opcional:
    - npx eas submit   ← publica nas stores automaticamente
```

---

## Secrets necessários (GitHub)

| Secret | Usado em |
|---|---|
| `FLY_API_TOKEN` | Deploy para Fly.io |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deploy para Azure Static Web Apps |
| `EXPO_TOKEN` | EAS Build autenticado |
| `GHCR_TOKEN` | Push de imagens para ghcr.io |

