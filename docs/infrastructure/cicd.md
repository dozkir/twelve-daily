# Infraestrutura — CI/CD

## Repositório
GitHub (monorepo) — todos os apps e packages no mesmo repositório. Branch padrão: **`master`**.

> **Estado atual:** apenas o **pipeline de build/teste** abaixo está implementado (`.github/workflows/dotnet.yml`). Os pipelines de **deploy** (self-hosted no `onze` via Cloudflare Tunnel + EAS) descritos mais adiante são **planejados — ainda não implementados**. O plano detalhado de deploy/CD está em [deployment.md](deployment.md).

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

## Pipeline: Deploy no `onze` *(planejado)*

Disparado ao concluir o build/test com sucesso em `master`. Modelo **image-based**
(detalhes e esboço do workflow em [deployment.md](deployment.md) §8):

```yaml
jobs:
  build-images:                       # roda na nuvem do GitHub
    - Build da imagem Docker da API → push GitHub Container Registry (ghcr.io)
    - Build da Web (expo export) → imagem estática (nginx) → push ghcr.io

  deploy:                             # roda em self-hosted runner NO onze
    - cd /srv/twelve-daily
        docker compose pull        # baixa as imagens novas do ghcr.io
        docker compose up -d        # recria os containers atualizados
        docker image prune -f       # limpa imagens antigas
```

> ⚠️ Como o `onze` usa **Cloudflare Tunnel** (sem portas de entrada), o GitHub na nuvem **não**
> consegue fazer SSH direto nele. O deploy roda num **self-hosted runner** instalado no `onze`
> (que conecta de saída ao GitHub). Alternativa: SSH via Cloudflare. Ver [deployment.md](deployment.md) §8.
> Sem Fly.io/Azure — tudo no host self-hosted.

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
| `EXPO_PUBLIC_API_URL` | URL da API embutida no build da Web (`https://api.twelvedaily.doze.dev.br`) |
| `EXPO_TOKEN` | EAS Build autenticado (mobile) |

> Para publicar imagens no **ghcr.io** dentro do próprio repositório, o `GITHUB_TOKEN`
> automático já basta (com permissão `packages: write`) — não é preciso um token manual.
> Com **self-hosted runner** no `onze`, **não** são necessários secrets `SSH_*`: o runner já
> tem acesso local ao Docker. As credenciais do Cloudflare Tunnel ficam **no host** (`/srv/edge/cloudflared/`),
> não em secrets do GitHub.

