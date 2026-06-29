pipeline {
  agent any

  environment {
    REPO_URL    = 'https://github.com/Zagred/tu-project.git'
    REPO_BRANCH = 'prod'

    REMOTE_ROOT = '/home/vagrant/tu-project-prod'
    SSH_OPTS    = '-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=15 -o PreferredAuthentications=password -o PubkeyAuthentication=no -o NumberOfPasswordPrompts=1'

    API_PORT = '7083'
    WEB_PORT = '5000'

    DOCKER_API_IMAGE = 'bankapp-api'
    DOCKER_WEB_IMAGE = 'bankapp-web'

    APP_VM               = credentials('bankapp-app-vm-host')
    VAGRANT_CREDS        = credentials('vagrant-login')
    DOCKERHUB_CREDS      = credentials('docker-hub-credentials')
    DB_CONNECTION_STRING = credentials('bankapp-db-connection-string')
  }

  stages {
    stage('Checkout') {
      steps {
        git url: env.REPO_URL, branch: env.REPO_BRANCH
      }
    }

    stage('Sync Deploy Files') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "REPO_URL='$REPO_URL' REPO_BRANCH='$REPO_BRANCH' REMOTE_ROOT='$REMOTE_ROOT' bash -s" <<'REMOTE_SCRIPT'
set -e
rm -rf "$REMOTE_ROOT"
git clone --branch "$REPO_BRANCH" "$REPO_URL" "$REMOTE_ROOT"
REMOTE_SCRIPT
        '''
      }
    }

    stage('Docker Build and Push') {
      steps {
        sh '''
          set -e
          echo "$DOCKERHUB_CREDS_PSW" | docker login -u "$DOCKERHUB_CREDS_USR" --password-stdin

          docker build -f docker/BankAPI.Dockerfile --build-arg APP_DIR=BankApp -t "$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:$BUILD_NUMBER" -t "$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:latest" .
          docker build -f docker/BankWeb.Dockerfile --build-arg APP_DIR=BankApp -t "$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:$BUILD_NUMBER" -t "$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:latest" .

          docker push "$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:$BUILD_NUMBER"
          docker push "$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:latest"
          docker push "$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:$BUILD_NUMBER"
          docker push "$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:latest"
          docker logout
        '''
      }
    }

    stage('Deploy') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "REMOTE_ROOT='$REMOTE_ROOT' BANKAPP_API_IMAGE='$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:$BUILD_NUMBER' BANKAPP_WEB_IMAGE='$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:$BUILD_NUMBER' BANKAPP_DB_CONNECTION_STRING='$DB_CONNECTION_STRING' BANKAPP_WEB_PUBLIC_URL='http://$APP_VM:$WEB_PORT' BANKAPP_WEB_VM_URL='http://$APP_VM:$WEB_PORT' DOCKERHUB_USER='$DOCKERHUB_CREDS_USR' DOCKERHUB_PASS='$DOCKERHUB_CREDS_PSW' bash -s" <<'REMOTE_SCRIPT'
set -e
sudo env APP_ROOT="$REMOTE_ROOT" BANKAPP_API_IMAGE="$BANKAPP_API_IMAGE" BANKAPP_WEB_IMAGE="$BANKAPP_WEB_IMAGE" BANKAPP_DB_CONNECTION_STRING="$BANKAPP_DB_CONNECTION_STRING" BANKAPP_WEB_PUBLIC_URL="$BANKAPP_WEB_PUBLIC_URL" BANKAPP_WEB_VM_URL="$BANKAPP_WEB_VM_URL" DOCKERHUB_USER="$DOCKERHUB_USER" DOCKERHUB_PASS="$DOCKERHUB_PASS" bash "$REMOTE_ROOT/userdata/app-docker-deploy.sh"
REMOTE_SCRIPT
        '''
      }
    }

    stage('Smoke Test') {
      steps {
        sh '''
          set -e
          curl --retry 15 --retry-all-errors --retry-delay 2 --connect-timeout 5 -fsS "http://$APP_VM:$API_PORT/swagger/v1/swagger.json" >/dev/null
          curl --retry 15 --retry-all-errors --retry-delay 2 --connect-timeout 5 -fsS "http://$APP_VM:$API_PORT/api/users/testuser" >/dev/null
          curl --retry 15 --retry-all-errors --retry-delay 2 --connect-timeout 5 -fsS "http://$APP_VM:$WEB_PORT/" >/dev/null
        '''
      }
    }
  }

  post {
    success {
      echo 'Production deployment completed.'
    }
  }
}
