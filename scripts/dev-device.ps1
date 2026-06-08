<#
.SYNOPSIS
  Sobe API + banco (docker compose) e roda o app no dispositivo fisico via WiFi (LAN).

.DESCRIPTION
  Ordem garantida: docker up -> espera o swagger -> (opcional) regenera o api-client
  -> expo no dispositivo (Metro em modo LAN).

  Fluxo LAN: o dispositivo alcanca a API e o Metro pelo IP do PC na rede.
    - API   -> <IP-do-PC>:5000  (definido em apps/client/.env: EXPO_PUBLIC_API_URL)
    - Metro -> <IP-do-PC>:8081  (Expo em modo LAN)
  Libere as portas 5000 e 8081 no firewall do Windows, e mantenha o telemovel
  na mesma WiFi do PC. O PC precisa continuar a correr o Metro durante o teste.

  O api-client gerado e' commitado e ja' reflete o contrato atual da API, entao
  -Generate so' e' necessario DEPOIS de mudar um DTO/endpoint C#.

.PARAMETER Rebuild
  Reconstroi a imagem da API (use apos mudar codigo C#): docker compose up -d --build.

.PARAMETER Generate
  Regenera packages/api-client a partir do swagger da API no ar (apos mudar a API).

.PARAMETER StartOnly
  Apenas inicia o Metro (modo LAN) para o dev client ja' instalado; nao reconstroi
  o app nativo. Use para testar por WiFi sem cabo (apos a 1a instalacao via ADB).

.EXAMPLE
  ./scripts/dev-device.ps1                 # sobe tudo e roda expo run:android (instala via ADB)
  ./scripts/dev-device.ps1 -StartOnly      # so' Metro em LAN (app ja' instalado, testa por WiFi)
  ./scripts/dev-device.ps1 -Rebuild -Generate   # apos mudar a API
#>
param(
  [switch]$Rebuild,
  [switch]$Generate,
  [switch]$StartOnly
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# 1) API + banco
Write-Host "==> Subindo API + banco (docker compose)..." -ForegroundColor Cyan
if ($Rebuild) { docker compose up -d --build } else { docker compose up -d }

# 2) Espera o swagger responder (API pronta apos migrations)
$swagger = "http://localhost:5000/swagger/v1/swagger.json"
Write-Host "==> Aguardando a API em $swagger ..." -ForegroundColor Cyan
$ready = $false
for ($i = 0; $i -lt 60; $i++) {
  try {
    if ((Invoke-WebRequest -Uri $swagger -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200) { $ready = $true; break }
  } catch { Start-Sleep -Seconds 1 }
}
if (-not $ready) { throw "A API nao respondeu em $swagger. Veja: docker logs twelvedaily-api" }
Write-Host "    API pronta." -ForegroundColor Green

# 3) (Opcional) regenera o cliente tipado a partir da API no ar
if ($Generate) {
  Write-Host "==> Regenerando packages/api-client (orval)..." -ForegroundColor Cyan
  $env:ORVAL_OPENAPI_URL = $swagger
  npm run api:generate
}

# 4) Lembrete do destino da API que o app vai usar (LAN)
$envFile = Join-Path $root "apps/client/.env"
if (Test-Path $envFile) {
  $apiUrl = (Select-String -Path $envFile -Pattern '^\s*EXPO_PUBLIC_API_URL=' | Select-Object -First 1).Line
  if ($apiUrl) { Write-Host "==> App vai usar $apiUrl (confirme que e' o IP do PC na LAN e que a porta esta' liberada no firewall)." -ForegroundColor Yellow }
}

# 5) App no dispositivo fisico (Metro em modo LAN p/ alcance por WiFi)
Set-Location (Join-Path $root "apps/client")
if ($StartOnly) {
  Write-Host "==> Iniciando Metro em modo LAN (--clear)..." -ForegroundColor Cyan
  npx expo start --dev-client --host lan --clear
} else {
  Write-Host "==> Build nativo + instala via ADB + Metro (expo run:android)..." -ForegroundColor Cyan
  npx expo run:android
}
