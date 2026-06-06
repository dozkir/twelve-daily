# Guia de Desenvolvimento Local

## Pré-requisitos

| Ferramenta | Versão mínima | Instalação |
|---|---|---|
| .NET SDK | 10 | https://dotnet.microsoft.com/download |
| Node.js | 20 LTS | https://nodejs.org |
| Docker Desktop | Qualquer recente | https://www.docker.com/products/docker-desktop |

> O Expo CLI não precisa ser instalado globalmente. Todos os comandos utilizam `npx expo` diretamente.

---

## Primeira vez

### 1. Clone o repositório
```bash
git clone https://github.com/<org>/twelve-daily.git
cd twelve-daily
```

### 2. Configure as variáveis de ambiente

**Linux / macOS:**
```bash
cp .env.example .env
```

**Windows (PowerShell):**
```powershell
Copy-Item .env.example .env
```

Preencha os valores no `.env` criado:
```env
# Banco de dados
POSTGRES_USER=twelvedaily
POSTGRES_PASSWORD=senha_local
POSTGRES_DB=twelvedaily

# JWT
JWT_SECRET=chave_secreta_minimo_32_caracteres
JWT_ISSUER=twelve-daily
JWT_AUDIENCE=twelve-daily-clients
JWT_EXPIRY_MINUTES=15

# Refresh Token
REFRESH_TOKEN_EXPIRY_DAYS=7

# Hangfire Dashboard
HANGFIRE_USER=admin
HANGFIRE_PASSWORD=senha_hangfire

# Expo (prefixo EXPO_PUBLIC_ é obrigatório para expor ao cliente)
EXPO_PUBLIC_API_URL=http://localhost:5000
EXPO_PUBLIC_SIGNALR_URL=http://localhost:5000/hubs

# Google Calendar (OAuth2)
GOOGLE_CLIENT_ID=xxx.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=xxx
GOOGLE_REDIRECT_URI=http://localhost:5000/auth/google/callback
```

> ⚠️ O arquivo `.env` **não é versionado**. Nunca commite segredos.

---

## Rodando localmente

### API + Banco (Docker)

Sobe o PostgreSQL e a API .NET em containers:
```bash
docker compose up -d
```

Para ver os logs da API:
```bash
docker compose logs -f api
```

Para parar:
```bash
docker compose down
```

> O banco persiste em volume Docker entre execuções. Para resetar: `docker compose down -v`

---

### Aplicação Expo (Web)

```bash
cd apps/client
npm install
npx expo start --web
```

Abre automaticamente em `http://localhost:8081`. Hot reload ativo.

---

### Aplicação Expo (Mobile)

```bash
cd apps/client
npx expo start
```

- Escaneia o QR code com o app **Expo Go** no celular (iOS ou Android)
- O celular precisa estar na mesma rede Wi-Fi que o computador

---

### Packages compartilhados (Turborepo)

Para instalar dependências e buildar todos os packages JS/TS de uma vez a partir da raiz:
```bash
npm install
npx turbo build
```

---

## Rodando os testes

### Unit Tests
```bash
cd apps/api
dotnet test TwelveDaily.UnitTests
```

### Integration Tests
> Requer Docker rodando (TestContainers sobe um container PostgreSQL automaticamente)

```bash
cd apps/api
dotnet test TwelveDaily.IntegrationTests
```

### Todos os testes
```bash
cd apps/api
dotnet test
```

---

## Migrations (EF Core)

### Aplicar migrations pendentes
```bash
cd apps/api
dotnet ef database update --project TwelveDaily.Infrastructure --startup-project TwelveDaily.Api
```

### Criar nova migration
```bash
dotnet ef migrations add <NomeDaMigration> --project TwelveDaily.Infrastructure --startup-project TwelveDaily.Api
```

---

## Regenerar cliente da API (orval)

Sempre que DTOs do .NET forem alterados, regenere o cliente TypeScript:
```bash
npx orval
```

> O arquivo gerado fica em `packages/api-client/`. Nunca editar manualmente — será sobrescrito.

---

## Build de produção

### API (imagem Docker)
```bash
docker build -t twelve-daily-api ./apps/api
```

### Expo Web (arquivos estáticos)
```bash
cd apps/client
npx expo export --platform web
# Saída em: apps/client/dist/
```

### Mobile (EAS Build)

Antes do primeiro build, autentique-se:
```bash
npx eas login
```

Em seguida:
```bash
cd apps/client
npx eas build --platform all
```

> Requer conta Expo e projeto configurado no EAS. Ver: https://docs.expo.dev/build/introduction/

