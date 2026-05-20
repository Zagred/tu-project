#!/usr/bin/env bash

ensure_swap() {
  local size="${1:-2G}"
  local path="${2:-/swapfile}"

  if swapon --show | grep -q '^'; then
    return 0
  fi

  sudo fallocate -l "$size" "$path"
  sudo chmod 600 "$path"
  sudo mkswap "$path"
  sudo swapon "$path"

  if ! grep -q "^$path " /etc/fstab; then
    echo "$path none swap sw 0 0" | sudo tee -a /etc/fstab >/dev/null
  fi
}
