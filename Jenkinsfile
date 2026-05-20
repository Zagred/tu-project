pipeline {
  agent any

  environment {
    REPO_URL    = 'https://github.com/Zagred/tu-project.git'
    REPO_BRANCH = 'main'

    APP_VM       = '192.168.56.104'
    REMOTE_ROOT  = '/home/vagrant/tu-project'
    PROJECT_DIR  = '/home/vagrant/tu-project/BankApp'
    PUBLISH_ROOT = '/tmp/bankapp-publish'
    SSH_OPTS     = '-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=15 -o PreferredAuthentications=password -o PubkeyAuthentication=no -o NumberOfPasswordPrompts=1'

    CONFIGURATION = 'Release'
    MOBILE_CONFIGURATION = 'Debug'
    ANDROID_TFM = 'net10.0-android'

    API_URL = 'http://192.168.56.104:7083'
    WEB_URL = 'http://192.168.56.104:5000'

    SONAR_HOST_URL    = 'http://192.168.56.102:9000'
    SONAR_PROJECT_KEY = 'tu-bank-web-api'

    NEXUS_URL        = 'http://192.168.56.101:8081'
    NEXUS_REPOSITORY = 'android-apps'

    DOCKER_API_IMAGE = 'bankapp-api'
    DOCKER_WEB_IMAGE = 'bankapp-web'

    VAGRANT_CREDS = credentials('vagrant-login')
    NEXUS_CREDS   = credentials('nexus-login')
    SONAR_TOKEN   = credentials('sonartoken')
    DOCKERHUB_CREDS = credentials('docker-hub-credentials')
    DB_CONNECTION_STRING = credentials('bankapp-db-connection-string')
  }

  stages {
    stage('Checkout') {
      steps {
        git url: 'https://github.com/Zagred/tu-project.git', branch: 'main'
      }
    }

    stage('Update App VM Source') {
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

    stage('Restore') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PROJECT_DIR='$PROJECT_DIR' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$PROJECT_DIR"
dotnet restore BankAPI/BankAPI.csproj
dotnet restore BankWeb/BankWeb.csproj
REMOTE_SCRIPT
        '''
      }
    }

    stage('Build') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PROJECT_DIR='$PROJECT_DIR' CONFIGURATION='$CONFIGURATION' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$PROJECT_DIR"
dotnet build BankAPI/BankAPI.csproj -c "$CONFIGURATION" --no-restore
dotnet build BankWeb/BankWeb.csproj -c "$CONFIGURATION" --no-restore
REMOTE_SCRIPT
        '''
      }
    }

    stage('SonarQube') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PROJECT_DIR='$PROJECT_DIR' CONFIGURATION='$CONFIGURATION' SONAR_HOST_URL='$SONAR_HOST_URL' SONAR_PROJECT_KEY='$SONAR_PROJECT_KEY' SONAR_TOKEN='$SONAR_TOKEN' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$PROJECT_DIR"
export PATH="$PATH:/home/vagrant/.dotnet/tools"

dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /n:"Bank Web API" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.exclusions="**/bin/**,**/obj/**"

dotnet build BankAPI/BankAPI.csproj -c "$CONFIGURATION" --no-restore
dotnet build BankWeb/BankWeb.csproj -c "$CONFIGURATION" --no-restore

dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
REMOTE_SCRIPT
        '''
      }
    }

    stage('Publish') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PROJECT_DIR='$PROJECT_DIR' PUBLISH_ROOT='$PUBLISH_ROOT' CONFIGURATION='$CONFIGURATION' BUILD_NUMBER='$BUILD_NUMBER' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$PROJECT_DIR"
rm -rf "$PUBLISH_ROOT"
mkdir -p "$PUBLISH_ROOT/api" "$PUBLISH_ROOT/web" "$PUBLISH_ROOT/artifacts"

dotnet publish BankAPI/BankAPI.csproj -c "$CONFIGURATION" --no-restore -o "$PUBLISH_ROOT/api"
dotnet publish BankWeb/BankWeb.csproj -c "$CONFIGURATION" --no-restore -o "$PUBLISH_ROOT/web"

tar -C "$PUBLISH_ROOT/api" -czf "$PUBLISH_ROOT/artifacts/bankapi-$BUILD_NUMBER.tar.gz" .
tar -C "$PUBLISH_ROOT/web" -czf "$PUBLISH_ROOT/artifacts/bankweb-$BUILD_NUMBER.tar.gz" .
REMOTE_SCRIPT
        '''
      }
    }

    stage('Build Mobile APK') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PROJECT_DIR='$PROJECT_DIR' PUBLISH_ROOT='$PUBLISH_ROOT' MOBILE_CONFIGURATION='$MOBILE_CONFIGURATION' ANDROID_TFM='$ANDROID_TFM' BUILD_NUMBER='$BUILD_NUMBER' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$PROJECT_DIR"
mkdir -p "$PUBLISH_ROOT/artifacts"

dotnet publish BankAPP/BankAPP.csproj \
  -f "$ANDROID_TFM" \
  -c "$MOBILE_CONFIGURATION" \
  -p:AndroidPackageFormat=apk \
  -p:EmbedAssembliesIntoApk=true

APK_PATH="$(find "BankAPP/bin/$MOBILE_CONFIGURATION/$ANDROID_TFM" -type f -name '*Signed.apk' | sort | tail -n 1)"
cp "$APK_PATH" "$PUBLISH_ROOT/artifacts/bankapp-mobile-$BUILD_NUMBER.apk"
REMOTE_SCRIPT
        '''
      }
    }

    stage('Upload Artifacts to Nexus') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PUBLISH_ROOT='$PUBLISH_ROOT' BUILD_NUMBER='$BUILD_NUMBER' NEXUS_URL='$NEXUS_URL' NEXUS_REPOSITORY='$NEXUS_REPOSITORY' NEXUS_USER='$NEXUS_CREDS_USR' NEXUS_PASS='$NEXUS_CREDS_PSW' bash -s" <<'REMOTE_SCRIPT'
set -e
curl -fsS -u "$NEXUS_USER:$NEXUS_PASS" --upload-file "$PUBLISH_ROOT/artifacts/bankapi-$BUILD_NUMBER.tar.gz" "$NEXUS_URL/repository/$NEXUS_REPOSITORY/web-api/$BUILD_NUMBER/bankapi-$BUILD_NUMBER.tar.gz"
curl -fsS -u "$NEXUS_USER:$NEXUS_PASS" --upload-file "$PUBLISH_ROOT/artifacts/bankweb-$BUILD_NUMBER.tar.gz" "$NEXUS_URL/repository/$NEXUS_REPOSITORY/web-api/$BUILD_NUMBER/bankweb-$BUILD_NUMBER.tar.gz"
curl -fsS -u "$NEXUS_USER:$NEXUS_PASS" --upload-file "$PUBLISH_ROOT/artifacts/bankapp-mobile-$BUILD_NUMBER.apk" "$NEXUS_URL/repository/$NEXUS_REPOSITORY/mobile/$BUILD_NUMBER/bankapp-mobile-$BUILD_NUMBER.apk"
REMOTE_SCRIPT
        '''
      }
    }

    stage('Build and Push Docker Images') {
      steps {
        sh '''
          set -e
          echo "$DOCKERHUB_CREDS_PSW" | docker login -u "$DOCKERHUB_CREDS_USR" --password-stdin

          docker build \
            -f docker/BankAPI.Dockerfile \
            --build-arg APP_DIR=BankApp \
            -t "$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:$BUILD_NUMBER" \
            -t "$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:latest" \
            .

          docker build \
            -f docker/BankWeb.Dockerfile \
            --build-arg APP_DIR=BankApp \
            -t "$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:$BUILD_NUMBER" \
            -t "$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:latest" \
            .

          docker push "$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:$BUILD_NUMBER"
          docker push "$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:latest"
          docker push "$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:$BUILD_NUMBER"
          docker push "$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:latest"
          docker logout
        '''
      }
    }

    stage('Deploy App VM with Docker') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "REMOTE_ROOT='$REMOTE_ROOT' BANKAPP_API_IMAGE='$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:$BUILD_NUMBER' BANKAPP_WEB_IMAGE='$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:$BUILD_NUMBER' BANKAPP_DB_CONNECTION_STRING='$DB_CONNECTION_STRING' DOCKERHUB_USER='$DOCKERHUB_CREDS_USR' DOCKERHUB_PASS='$DOCKERHUB_CREDS_PSW' bash -s" <<'REMOTE_SCRIPT'
set -e
sudo env \
  APP_ROOT="$REMOTE_ROOT" \
  BANKAPP_API_IMAGE="$BANKAPP_API_IMAGE" \
  BANKAPP_WEB_IMAGE="$BANKAPP_WEB_IMAGE" \
  BANKAPP_DB_CONNECTION_STRING="$BANKAPP_DB_CONNECTION_STRING" \
  DOCKERHUB_USER="$DOCKERHUB_USER" \
  DOCKERHUB_PASS="$DOCKERHUB_PASS" \
  bash "$REMOTE_ROOT/userdata/app-docker-deploy.sh"
REMOTE_SCRIPT
        '''
      }
    }

    stage('Smoke Test') {
      steps {
        sh '''
          set -e
          curl --retry 15 --retry-all-errors --retry-delay 2 --connect-timeout 5 -fsS "$API_URL/swagger/v1/swagger.json" >/dev/null
          curl --retry 15 --retry-all-errors --retry-delay 2 --connect-timeout 5 -fsS "$API_URL/api/users/testuser" >/dev/null
          curl --retry 15 --retry-all-errors --retry-delay 2 --connect-timeout 5 -fsS "$WEB_URL/" >/dev/null
        '''
      }
    }
  }

  post {
    success {
      echo 'Web, API, and mobile build, analysis, publish, deploy, and smoke tests completed.'
    }
  }
}
