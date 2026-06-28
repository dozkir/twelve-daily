# Infraestrutura — Hospedagem

> 📌 O projeto é **self-hosted** na máquina **`onze`** (Debian), compartilhada com outros
> projetos. O plano operacional completo (setup do host, DNS/TLS, CD, backups, hardening) é
> mantido num documento de trabalho **local** (não versionado). Esta página é o resumo de alto nível.

> **Histórico:** versões anteriores deste documento previam uma stack gratuita em nuvem
> (Fly.io para API + Postgres, Azure Static Web Apps para a Web). Esse plano foi
> **substituído** pelo self-hosting no `onze`.

---

## Onde cada peça roda

| Peça | Hospedagem | Observação |
|---|---|---|
| .NET API | Container no `onze` (Docker Compose) | atrás do Caddy interno |
| Web (Expo export estático) | Container no `onze` (Docker Compose) | atrás do Caddy interno |
| PostgreSQL | Container no `onze` — **dedicado ao twelve-daily** | rede interna privada, com backups |
| Imagens Docker | **GitHub Container Registry (ghcr.io)** | construídas pelo CI, baixadas pelo `onze` |
| Exposição + HTTPS | **Cloudflare Tunnel** (+ Caddy interno) | TLS na borda da Cloudflare; **sem** abrir portas/IP público |
| DNS | **Cloudflare** (`doze.dev.br`, wildcard `*.doze.dev.br`) | subdomínios roteados pelo Caddy no `onze` |
| CI/CD | **GitHub Actions** | build/test + imagens GHCR + deploy (ver [cicd.md](cicd.md)) |
| Mobile (iOS/Android) | **EAS Build** (nuvem Expo) | distribuição pelas stores |

---

## Máquina `onze`

- **VM Debian** dentro de um **Proxmox**, host **compartilhado** com outros projetos (ex: Home Assistant).
- Cada projeto é um **stack Docker Compose** isolado; um **Caddy interno** roteia por
  subdomínio.
- Exposição via **Cloudflare Tunnel** (conexão de **saída**) — **sem** IP público, **sem**
  port forwarding, **sem** portas abertas. Domínio próprio `doze.dev.br` (DNS na Cloudflare).
- Sem Kubernetes — Docker Compose é suficiente e mais simples para 1 nó.

## Banco de Dados

- **Local e produção**: PostgreSQL em **container Docker** (instância **dedicada** ao
  twelve-daily, isolada em rede interna).
- Migrations gerenciadas pelo EF Core e aplicadas **no startup** da API.
- **Backups** automáticos (pg_dump + cópia offsite) — *planejado*.

---

> Trocar de provedor/host no futuro é, em sua maior parte, reconfigurar o destino do deploy
> no CI/CD — o código da aplicação não muda.
