#!/usr/bin/env bash
#
# Gera o APK Android (release, para sideload) do Twelve Daily LOCALMENTE.
#
# A New Architecture está desabilitada (app.json: "newArchEnabled": false).
# Sem ela não há a codegen C++, cujos caminhos estouravam o limite de 260
# caracteres do Windows — então o build roda direto do repositório, sem truques.
#
# Uso (no Git Bash, a partir de qualquer lugar):
#   ./scripts/build-android-apk.sh
#
# Resultado:
#   <raiz-do-repo>/twelve-daily.apk   → instale no Android via sideload.
#
# Pré-requisitos (já presentes nesta máquina): Git Bash, Java 17, ANDROID_HOME,
# e dependências instaladas (apps/client/node_modules). Se faltar:
#   (cd apps/client && npm install)

set -euo pipefail

API_URL="https://api-twelvedaily.doze.dev.br"   # URL da API embutida no bundle (produção)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT/apps/client"

echo ">> [1/3] Prebuild (regenera android/ a partir do app.json)"
npx expo prebuild --platform android --clean --no-install

echo ">> [2/3] Compilando APK release (pode levar ~5-10 min na 1a vez; baixa Gradle/deps)"
cd android
EXPO_PUBLIC_API_URL="$API_URL" ./gradlew assembleRelease

echo ">> [3/3] Copiando o APK para a raiz do repo"
APK_SRC="$REPO_ROOT/apps/client/android/app/build/outputs/apk/release/app-release.apk"
APK_OUT="$REPO_ROOT/twelve-daily.apk"
cp "$APK_SRC" "$APK_OUT"

echo ""
echo "==================================================================="
echo " ✅ APK pronto: $(cygpath -w "$APK_OUT")"
echo "    Instale no Android (sideload). A API aponta para: $API_URL"
echo "==================================================================="
