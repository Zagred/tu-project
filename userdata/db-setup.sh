#!/usr/bin/env bash
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive

SQL_CONTAINER_NAME="${SQL_CONTAINER_NAME:-bankapp-sql}"
SQL_IMAGE="${SQL_IMAGE:-mcr.microsoft.com/mssql/server:2022-latest}"
SQL_VOLUME="${SQL_VOLUME:-bankapp-sql-data}"
SQL_PORT="${SQL_PORT:-1433}"
SQL_PASSWORD="${SQL_PASSWORD:?SQL_PASSWORD must be set before provisioning the DB VM}"

sudo apt-get update -y
sudo apt-get install -y ca-certificates curl docker.io

if ! swapon --show | grep -q '^'; then
  sudo fallocate -l 2G /swapfile
  sudo chmod 600 /swapfile
  sudo mkswap /swapfile
  sudo swapon /swapfile
  if ! grep -q '^/swapfile ' /etc/fstab; then
    echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab >/dev/null
  fi
fi

sudo systemctl enable docker
sudo systemctl start docker

if sudo docker ps -a --format '{{.Names}}' | grep -qx "$SQL_CONTAINER_NAME"; then
  sudo docker rm -f "$SQL_CONTAINER_NAME"
fi

sudo docker volume create "$SQL_VOLUME" >/dev/null
sudo docker pull "$SQL_IMAGE"

sudo docker run -d \
  --name "$SQL_CONTAINER_NAME" \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -e MSSQL_PID=Developer \
  -e MSSQL_SA_PASSWORD="$SQL_PASSWORD" \
  -p "$SQL_PORT:1433" \
  -v "$SQL_VOLUME:/var/opt/mssql" \
  "$SQL_IMAGE" >/dev/null

echo "Waiting for SQL Server to finish startup..."
SQL_READY=0
for _ in $(seq 1 120); do
  if sudo docker logs "$SQL_CONTAINER_NAME" 2>&1 | grep -q "Recovery is complete"; then
    echo "SQL Server is ready enough for application startup."
    SQL_READY=1
    break
  fi
  sleep 2
done

if [ "$SQL_READY" -ne 1 ]; then
  echo "SQL Server did not become ready. Container status and recent logs:"
  sudo docker ps -a --filter "name=$SQL_CONTAINER_NAME"
  sudo docker logs --tail 120 "$SQL_CONTAINER_NAME" || true
  exit 1
fi

sudo docker ps --filter "name=$SQL_CONTAINER_NAME"
echo "Database VM setup complete. SQL Server: 192.168.56.105:$SQL_PORT"
