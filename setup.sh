#!/usr/bin/env bash
set -euo pipefail

SSH_USER="${SSH_USER:-vagrant}"
SSH_PASSWORD="${SSH_PASSWORD:-vagrant}"
SSH_OPTS=(
  -o StrictHostKeyChecking=no
  -o UserKnownHostsFile=/dev/null
  -o PreferredAuthentications=password,keyboard-interactive
  -o PubkeyAuthentication=no
)

declare -A VM_IPS=(
  [nexus]="192.168.56.101"
  [sonarqube]="192.168.56.102"
  [jenkinsvm]="192.168.56.103"
  [app]="192.168.56.104"
)

declare -A VM_SCRIPTS=(
  [nexus]="userdata/nexus-setup.sh"
  [sonarqube]="userdata/sonar-setup.sh"
  [jenkinsvm]="userdata/jenkins-setup.sh"
  [app]="userdata/app-setup.sh"
)

usage() {
  cat <<EOF
Usage: bash setup.sh [all|nexus|sonarqube|jenkinsvm|app|restore-jenkins]

Defaults:
  SSH_USER=$SSH_USER
  SSH_PASSWORD=$SSH_PASSWORD

Examples:
  bash setup.sh all
  bash setup.sh app
  SSH_PASSWORD=vagrant bash setup.sh jenkinsvm
  JENKINS_BACKUP=jenkins-home-backup.tgz bash setup.sh restore-jenkins

If sshpass is installed, the password is sent automatically.
Without sshpass, ssh will prompt for the password.
EOF
}

ssh_cmd() {
  if command -v sshpass >/dev/null 2>&1; then
    sshpass -p "$SSH_PASSWORD" ssh "${SSH_OPTS[@]}" "$@"
  else
    ssh "${SSH_OPTS[@]}" "$@"
  fi
}

wait_for_port() {
  local name="$1"
  local ip="$2"
  local timeout_seconds="${SSH_WAIT_TIMEOUT:-600}"
  local elapsed=0

  echo "Waiting for $name SSH on $ip:22 ..."
  while ! (echo >/dev/tcp/"$ip"/22) >/dev/null 2>&1; do
    if (( elapsed >= timeout_seconds )); then
      echo "Timed out waiting for $name SSH on $ip:22" >&2
      return 1
    fi
    sleep 5
    elapsed=$((elapsed + 5))
  done
}

run_vm_setup() {
  local name="$1"
  local ip="${VM_IPS[$name]}"
  local script="${VM_SCRIPTS[$name]}"
  local remote_script="/tmp/${name}-setup.sh"

  if [ ! -f "$script" ]; then
    echo "Missing local setup script: $script" >&2
    return 1
  fi

  wait_for_port "$name" "$ip"
  echo "Running $script on $name ($ip) ..."
  ssh_cmd "$SSH_USER@$ip" "cat > '$remote_script' && chmod +x '$remote_script' && sudo bash '$remote_script'" < "$script"
  echo "$name setup complete."
}

restore_jenkins_backup() {
  local ip="${VM_IPS[jenkinsvm]}"
  local backup="${JENKINS_BACKUP:-jenkins-home-backup.tgz}"
  local remote_backup="/tmp/jenkins-home-backup.tgz"

  if [ ! -f "$backup" ]; then
    echo "Missing Jenkins backup archive: $backup" >&2
    return 1
  fi

  wait_for_port jenkinsvm "$ip"
  echo "Uploading $backup to jenkinsvm ($ip) ..."
  ssh_cmd "$SSH_USER@$ip" "cat > '$remote_backup'" < "$backup"
  echo "Restoring Jenkins home from $backup ..."
  ssh_cmd "$SSH_USER@$ip" "sudo systemctl stop jenkins && sudo tar -xzf '$remote_backup' -C /var/lib && sudo chown -R jenkins:jenkins /var/lib/jenkins && sudo systemctl start jenkins"
  echo "Jenkins backup restore complete."
}

target="${1:-all}"

case "$target" in
  -h|--help|help)
    usage
    ;;
  all)
    run_vm_setup nexus
    run_vm_setup sonarqube
    run_vm_setup jenkinsvm
    run_vm_setup app
    ;;
  nexus|sonarqube|jenkinsvm|app)
    run_vm_setup "$target"
    ;;
  restore-jenkins)
    restore_jenkins_backup
    ;;
  *)
    usage
    exit 1
    ;;
esac
