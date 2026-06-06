# Infraestrutura — Hospedagem

## Stack Gratuita

| Peça | Serviço (Primário) | Alternativa | Custo |
|---|---|---|---|
| .NET API | **Fly.io** *(always-on, 1 máquina free)* | — | Grátis |
| PostgreSQL | **Fly.io Postgres** *(always-on, volume persistente)* | Railway ($5 crédito/mês) | Grátis |
| Imagens Docker | **GitHub Container Registry (ghcr.io)** | — | Grátis |
| Expo Web (estático) | **Azure Static Web Apps Free** | Cloudflare Pages | Grátis |
| CI/CD | **GitHub Actions** | — | Grátis |
| Mobile builds | **EAS Build** | — | Grátis (até 30 builds/mês) |

**Custo total estimado: $0/mês.**

---

## Fly.io — API

- 1 máquina **always-on** (não escala até zero — necessário para o Hangfire)
- Deploy via imagem Docker do GitHub Container Registry
- Variáveis de ambiente configuradas via `fly secrets`
- Fly.io free tier: 3 máquinas shared-cpu-1x 256MB — suficiente para o projeto

## Fly.io — PostgreSQL

- Banco gerenciado pelo próprio Fly.io
- Volume persistente — dados sobrevivem a reinicializações
- API e banco no mesmo provedor — sem latência de rede entre eles
- Conexão interna via hostname Fly (sem expor porta pública)

---

## Azure Static Web Apps — Expo Web

- Serve os arquivos estáticos gerados por `npx expo export --platform web`
- HTTPS automático + CDN global
- Deploy automático via GitHub Actions
- Tier Free: suficiente para o projeto

---

## Banco de Dados

- **Local**: PostgreSQL via Docker (`docker compose up`)
- **Produção**: Fly.io Postgres (always-on, volume persistente)
- Migrations gerenciadas pelo EF Core
- Sem acesso root nos ambientes

---

> Migração para outra nuvem no futuro é apenas reconfiguração do pipeline de CI/CD — o código não muda.

