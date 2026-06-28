# Infraestrutura — CI/CD

## Repositório
GitHub (monorepo) — todos os apps e packages no mesmo repositório. Branch padrão: **`master`**.

> **Estado atual:** estão implementados o **build/teste** (`.github/workflows/dotnet.yml`), o **build/push de imagens** e o **deploy no `onze`** (ambos em `.github/workflows/images.yml`). O **EAS (mobile)** segue planejado. O plano operacional detalhado do host é um documento de trabalho **local** (não versionado).

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

## Pipeline: Build & Push de imagens ✅

Implementado em `.github/workflows/images.yml`. Disparado a cada **push na `master`**
(e manualmente via `workflow_dispatch`). Dois jobs publicam no GitHub Container Registry:

```yaml
jobs:
  api:   # context ./apps/api → ghcr.io/dozkir/twelve-daily-api:latest (+ tag por SHA)
  web:   # context . (raiz; client usa file: api-client) → ghcr.io/dozkir/twelve-daily-web:latest
         # build-arg EXPO_PUBLIC_API_URL embute a URL da API no bundle (em build-time)
```

> Autentica no `ghcr.io` com o `GITHUB_TOKEN` automático (`permissions: packages: write`);
> sem token manual. Usa cache de layers do Actions (`type=gha`). A imagem da Web é
> construída com contexto na **raiz** do repo porque o cliente depende de
> `packages/api-client` via `file:`.

## Pipeline: Deploy no `onze` ✅

Job `deploy` em `images.yml` (roda **após** `api`/`web`), no **self-hosted runner** do `onze`
(`runs-on: [self-hosted, onze]`). Modelo **image-based**: o host puxa as imagens novas e
recria os containers.

```yaml
deploy:
  needs: [api, web]
  runs-on: [self-hosted, onze]
  # cd /srv/twelve-daily → docker compose pull && up -d && image prune -f
```

> ⚠️ Como o `onze` usa **Cloudflare Tunnel** (sem portas de entrada), o GitHub na nuvem **não**
> consegue fazer SSH direto nele. Por isso o deploy roda num **self-hosted runner** instalado no
> `onze` (conexão de saída). Sem secrets `SSH_*`: o runner tem Docker local e o **host já está
> autenticado no `ghcr.io`** (login persistente do `doze`, `read:packages`) → o `docker compose
> pull` baixa as imagens privadas. Sem Fly.io/Azure — tudo no host self-hosted.

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

| Nome | Tipo | Usado em |
|---|---|---|
| `EXPO_PUBLIC_API_URL` | **Variable** (repo) | build-arg da imagem Web em `images.yml` (`https://api-twelvedaily.doze.dev.br`) |
| `EXPO_TOKEN` | Secret | EAS Build autenticado (mobile) |

> Para publicar imagens no **ghcr.io** dentro do próprio repositório, o `GITHUB_TOKEN`
> automático já basta (com permissão `packages: write`) — não é preciso um token manual.
> Com **self-hosted runner** no `onze`, **não** são necessários secrets `SSH_*`: o runner já
> tem acesso local ao Docker. As credenciais do Cloudflare Tunnel ficam **no host** (`/srv/edge/cloudflared/`),
> não em secrets do GitHub.

