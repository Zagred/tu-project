#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${APP_ROOT:-/home/vagrant/tu-project}"
BANKAPP_SOURCE_DIR="${BANKAPP_SOURCE_DIR:-BankApp}"
export DEBIAN_FRONTEND=noninteractive

if ls -1 "$APP_ROOT" | grep -qx 'BankAPP'; then
  BANKAPP_SOURCE_DIR="BankAPP"
fi

if ls -1 "$APP_ROOT" | grep -qx 'BankApp'; then
  BANKAPP_SOURCE_DIR="BankApp"
fi

echo "Using APP_ROOT=$APP_ROOT"
echo "Using BANKAPP_SOURCE_DIR=$BANKAPP_SOURCE_DIR"

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
  sudo env \
    BANKAPP_API_IMAGE="$BANKAPP_API_IMAGE" \
    BANKAPP_WEB_IMAGE="$BANKAPP_WEB_IMAGE" \
    BANKAPP_DB_CONNECTION_STRING="${BANKAPP_DB_CONNECTION_STRING:-}" \
    BANKAPP_API_BASE_ADDRESS="${BANKAPP_API_BASE_ADDRESS:-}" \
    docker compose -f docker-compose.deploy.yml pull

  sudo env \
    BANKAPP_API_IMAGE="$BANKAPP_API_IMAGE" \
    BANKAPP_WEB_IMAGE="$BANKAPP_WEB_IMAGE" \
    BANKAPP_DB_CONNECTION_STRING="${BANKAPP_DB_CONNECTION_STRING:-}" \
    BANKAPP_API_BASE_ADDRESS="${BANKAPP_API_BASE_ADDRESS:-}" \
    docker compose -f docker-compose.deploy.yml up -d

  sudo env \
    BANKAPP_API_IMAGE="$BANKAPP_API_IMAGE" \
    BANKAPP_WEB_IMAGE="$BANKAPP_WEB_IMAGE" \
    BANKAPP_DB_CONNECTION_STRING="${BANKAPP_DB_CONNECTION_STRING:-}" \
    BANKAPP_API_BASE_ADDRESS="${BANKAPP_API_BASE_ADDRESS:-}" \
    docker compose -f docker-compose.deploy.yml ps
else
  sudo env \
    BANKAPP_SOURCE_DIR="$BANKAPP_SOURCE_DIR" \
    BANKAPP_DB_CONNECTION_STRING="${BANKAPP_DB_CONNECTION_STRING:-}" \
    BANKAPP_API_BASE_ADDRESS="${BANKAPP_API_BASE_ADDRESS:-}" \
    docker compose up -d --build

  sudo env \
    BANKAPP_SOURCE_DIR="$BANKAPP_SOURCE_DIR" \
    BANKAPP_DB_CONNECTION_STRING="${BANKAPP_DB_CONNECTION_STRING:-}" \
    BANKAPP_API_BASE_ADDRESS="${BANKAPP_API_BASE_ADDRESS:-}" \
    docker compose ps
fi
