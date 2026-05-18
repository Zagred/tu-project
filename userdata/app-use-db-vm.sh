#!/usr/bin/env bash
set -euo pipefail

DB_HOST="${DB_HOST:-192.168.56.105}"
DB_PORT="${DB_PORT:-1433}"
DB_NAME="${DB_NAME:-BankAppDb}"
DB_USER="${DB_USER:-sa}"
DB_PASSWORD="${DB_PASSWORD:-BankApp_Strong_Pass_123!}"
API_PORT="${API_PORT:-7083}"

cat >/etc/systemd/system/bankapi.service <<EOT
[Unit]
Description=Bank API
After=network-online.target
Wants=network-online.target

[Service]
WorkingDirectory=/opt/bankapp/api
ExecStart=/opt/bankapp/api/BankAPI
Restart=always
RestartSec=5
User=www-data
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://0.0.0.0:${API_PORT}"
Environment="ConnectionStrings__DefaultConnection=Server=${DB_HOST},${DB_PORT};Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD};TrustServerCertificate=True;"
Environment="DOTNET_PRINT_TELEMETRY_MESSAGE=false"

[Install]
WantedBy=multi-user.target
EOT

systemctl daemon-reload
systemctl restart bankapi
systemctl --no-pager -l status bankapi
