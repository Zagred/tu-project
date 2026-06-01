#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${APP_ROOT:-/home/vagrant/tu-project}"
BANKAPP_SOURCE_DIR="${BANKAPP_SOURCE_DIR:-BankApp}"
BANKAPP_RUNTIME_ENV_FILE="${BANKAPP_RUNTIME_ENV_FILE:-/home/vagrant/bankapp-runtime.env}"
export DEBIAN_FRONTEND=noninteractive

if ls -1 "$APP_ROOT" | grep -qx 'BankAPP'; then
  BANKAPP_SOURCE_DIR="BankAPP"
fi

if ls -1 "$APP_ROOT" | grep -qx 'BankApp'; then
  BANKAPP_SOURCE_DIR="BankApp"
fi

echo "Using APP_ROOT=$APP_ROOT"
echo "Using BANKAPP_SOURCE_DIR=$BANKAPP_SOURCE_DIR"

COMPOSE_ENV_ARGS=()
if [ -f "$APP_ROOT/.env.runtime" ]; then
  COMPOSE_ENV_ARGS+=(--env-file "$APP_ROOT/.env.runtime")
fi

if [ -f "$BANKAPP_RUNTIME_ENV_FILE" ]; then
  COMPOSE_ENV_ARGS+=(--env-file "$BANKAPP_RUNTIME_ENV_FILE")
fi

COMPOSE_ENV_VARS=()

add_compose_env_var() {
  local name="$1"
  local value="${!name:-}"

  if [ -n "$value" ]; then
    COMPOSE_ENV_VARS+=("$name=$value")
  fi
}

prepare_compose_env() {
  COMPOSE_ENV_VARS=()

  for name in "$@"; do
    add_compose_env_var "$name"
  done
}

run_compose() {
  sudo env "${COMPOSE_ENV_VARS[@]}" docker compose "${COMPOSE_ENV_ARGS[@]}" "$@"
}

sudo apt-get update -y
sudo apt-get install -y docker.io docker-compose-v2

sudo systemctl enable docker
sudo systemctl start docker
sudo usermod -aG docker vagrant

sudo systemctl stop bankweb bankapi 2>/dev/null || true
sudo systemctl disable bankweb bankapi 2>/dev/null || true
sudo systemctl reset-failed bankweb bankapi 2>/dev/null || true

cd "$APP_ROOT"

if [ -n "${DOCKERHUB_USER:-}" ] && [ -n "${DOCKERHUB_PASS:-}" ]; then
  echo "$DOCKERHUB_PASS" | sudo docker login -u "$DOCKERHUB_USER" --password-stdin
fi

if [ -n "${BANKAPP_API_IMAGE:-}" ] && [ -n "${BANKAPP_WEB_IMAGE:-}" ]; then
  prepare_compose_env \
    BANKAPP_API_IMAGE \
    BANKAPP_WEB_IMAGE \
    BANKAPP_DB_CONNECTION_STRING \
    BANKAPP_WEB_PUBLIC_URL \
    BANKAPP_WEB_VM_URL \
    BANKAPP_API_BASE_ADDRESS \
    BANKAPP_OLLAMA_BASE_ADDRESS \
    BANKAPP_EMAIL_SMTP_SERVER \
    BANKAPP_EMAIL_PORT \
    BANKAPP_EMAIL_SENDER_NAME \
    BANKAPP_EMAIL_SENDER_EMAIL \
    BANKAPP_EMAIL_USERNAME \
    BANKAPP_EMAIL_PASSWORD

  run_compose -f docker-compose.deploy.yml pull
  run_compose -f docker-compose.deploy.yml up -d
  run_compose -f docker-compose.deploy.yml ps
else
  prepare_compose_env \
    BANKAPP_SOURCE_DIR \
    BANKAPP_DB_CONNECTION_STRING \
    BANKAPP_WEB_PUBLIC_URL \
    BANKAPP_WEB_VM_URL \
    BANKAPP_API_BASE_ADDRESS \
    BANKAPP_OLLAMA_BASE_ADDRESS \
    BANKAPP_EMAIL_SMTP_SERVER \
    BANKAPP_EMAIL_PORT \
    BANKAPP_EMAIL_SENDER_NAME \
    BANKAPP_EMAIL_SENDER_EMAIL \
    BANKAPP_EMAIL_USERNAME \
    BANKAPP_EMAIL_PASSWORD

  run_compose up -d --build
  run_compose ps
fi
