# Spec — Autenticação e Segurança

## Tokens

| Token | Duração | Armazenamento |
|---|---|---|
| JWT Access Token | 15 minutos | Memória (front-end) — nunca em localStorage |
| Refresh Token | 7 dias | Banco de dados + cookie httpOnly ou secure storage (mobile) |

---

## Endpoints públicos
Apenas dois endpoints não exigem autenticação:
- `POST /auth/register`
- `POST /auth/login`

Todos os demais endpoints requerem `Authorization: Bearer <access_token>`.

---

## Fluxo de Login

```
POST /auth/login
Body: { email, password }

Response 200:
{
  accessToken: string,        // JWT, expira em 15min
  accessTokenExpiresAt: string,  // UTC ISO 8601 — usado pelo front para agendar refresh silencioso
  refreshToken: string,       // opaco, expira em 7 dias
  refreshTokenExpiresAt: string  // UTC ISO 8601
}
```

---

## Fluxo de Refresh

```
POST /auth/refresh
Body: { refreshToken }

Response 200:
{
  accessToken: string,
  accessTokenExpiresAt: string,
  refreshToken: string,          // novo token (rotação obrigatória)
  refreshTokenExpiresAt: string
}

Response 401: token inválido, expirado ou revogado → redirecionar para login
```

> **Rotação obrigatória**: a cada refresh, o token antigo é revogado e um novo é emitido.
> Isso garante que tokens vazados sejam detectados (uso após revogação gera 401).

---

## Fluxo de Registro

```
POST /auth/register
Body: { email, password, timezone }  // timezone: IANA ID

Response 201:
{
  accessToken: string,
  accessTokenExpiresAt: string,
  refreshToken: string,
  refreshTokenExpiresAt: string
}
```

---

## Logout

```
POST /auth/logout
Header: Authorization: Bearer <access_token>
Body: { refreshToken }

Response 204
```

Revoga o refresh token. O access token expira naturalmente em até 15 minutos.

---

## Logout de todos os dispositivos

```
POST /auth/logout-all
Header: Authorization: Bearer <access_token>

Response 204
```

Revoga **todos** os refresh tokens do usuário. Força login em todos os dispositivos.

---

## Perfil e Configurações

```
GET /users/me
Response 200: { id, email, timezone, createdAt }

PUT /users/me/timezone
Body: { timezone: "America/Sao_Paulo" }
Response 204

PUT /users/me/password
Body: { currentPassword, newPassword }
Response 204
```

---

## Regras de Segurança
- Senhas armazenadas com **bcrypt** (ou equivalente — nunca plain text)
- Refresh tokens são strings aleatórias opacas (não JWT)
- Um usuário pode ter múltiplos refresh tokens ativos (múltiplos dispositivos)
- Revogar todos os tokens de um usuário: endpoint `POST /auth/logout-all`

