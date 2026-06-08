# Spec — Google Calendar

> ⏸️ **Adiado.** Com o fim da materialização de instâncias, esta spec (baseada em criar eventos "ao gerar `HabitInstance`") está suspensa. Ver [habit-check-refactor](habit-check-refactor.md) §10.4. Modelo-alvo quando reativada: **evento recorrente (RRULE)** por hábito/schedule, criado/atualizado ao mudar o schedule — não um evento por dia. A flag `Habit.SyncGoogleCalendar` permanece reservada.

## Visão Geral
Integração opcional que permite criar eventos no Google Calendar. Cada hábito pode ter a flag `SyncGoogleCalendar` ativada individualmente.

---

## Pré-requisito: Conectar Conta Google

O usuário precisa autorizar o acesso ao Google Calendar uma única vez:

```
GET /auth/google
→ Redireciona para o consent screen do Google (OAuth2)
→ Solicita scope: calendar.events

GET /auth/google/callback?code=...
→ API troca o authorization code por access + refresh token
→ Salva GoogleConnection (tokens criptografados)
→ Redireciona de volta para o app

Response: { connected: true, email: "usuario@gmail.com" }
```

### Desconectar

```
DELETE /auth/google
→ Remove GoogleConnection do usuário
→ Revoga o token no Google

Response 204
```

### Verificar status

```
GET /auth/google/status

Response 200:
{
  connected: true,
  email: "usuario@gmail.com"
}
```

---

## Comportamento por Hábito

| `Habit.SyncGoogleCalendar` | `GoogleConnection` existe? | Resultado |
|---|---|---|
| `false` | — | Sem evento no Calendar |
| `true` | Não | Sem evento (instância criada normalmente) |
| `true` | Sim | Evento criado no Google Calendar |

---

## Conteúdo do Evento

```json
{
  "summary": "🏋️ Academia",
  "description": "Treino de musculação",
  "start": {
    "dateTime": "2026-03-29T07:00:00-03:00",
    "timeZone": "America/Sao_Paulo"
  },
  "end": {
    "dateTime": "2026-03-29T08:00:00-03:00",
    "timeZone": "America/Sao_Paulo"
  }
}
```

---

## Resiliência
- Falha na API do Google **nunca impede** a criação da `HabitInstance`
- Em caso de erro: loga, `GoogleCalendarEventId` fica `null`, instância é salva normalmente
- Retry automático não é necessário — o evento no Calendar é um complemento, não dado crítico

---

## Google Cloud — Configuração necessária

| Item | Descrição |
|---|---|
| Google Cloud Project | Projeto criado no Google Cloud Console |
| OAuth2 Client ID | Credentials → Create OAuth client ID (Web application) |
| Redirect URI | `https://<api-url>/auth/google/callback` |
| Scope | `https://www.googleapis.com/auth/calendar.events` |
| Consent Screen | Configurar com nome do app, logo, scopes solicitados |

> Credenciais do OAuth (`client_id`, `client_secret`) vão nas variáveis de ambiente da API — nunca hardcoded.

