pipeline {
  agent any

  environment {
    REPO_URL    = 'https://github.com/Zagred/tu-project.git'
    REPO_BRANCH = 'main'

    REMOTE_ROOT  = '/home/vagrant/tu-project'
    PROJECT_DIR  = '/home/vagrant/tu-project/BankApp'
    PUBLISH_ROOT = '/tmp/bankapp-publish'
    SSH_OPTS     = '-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=15 -o PreferredAuthentications=password -o PubkeyAuthentication=no -o NumberOfPasswordPrompts=1'

    CONFIGURATION        = 'Release'
    MOBILE_CONFIGURATION = 'Debug'
    ANDROID_TFM          = 'net10.0-android'
    ANDROID_SDK_DIR      = '/opt/android-sdk'

    API_PORT = '7083'
    WEB_PORT = '5000'

    SONAR_PROJECT_KEY = 'tu-bank-web-api'
    SONAR_PORT        = '9000'

    NEXUS_REPOSITORY = 'android-apps'
    NEXUS_PORT       = '8081'

    DOCKER_API_IMAGE = 'bankapp-api'
    DOCKER_WEB_IMAGE = 'bankapp-web'

    APP_VM               = credentials('bankapp-app-vm-host')
    SONAR_HOST           = credentials('bankapp-sonar-host')
    NEXUS_HOST           = credentials('bankapp-nexus-host')
    VAGRANT_CREDS        = credentials('vagrant-login')
    NEXUS_CREDS          = credentials('nexus-login')
    SONAR_TOKEN          = credentials('sonartoken')
    DOCKERHUB_CREDS      = credentials('docker-hub-credentials')
    DB_CONNECTION_STRING = credentials('bankapp-db-connection-string')
  }

  stages {
    stage('Checkout') {
      steps {
        git url: env.REPO_URL, branch: env.REPO_BRANCH
      }
    }

    stage('Sync Source') {
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

    stage('Build API and Web') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PROJECT_DIR='$PROJECT_DIR' CONFIGURATION='$CONFIGURATION' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$PROJECT_DIR"
dotnet restore BankAPI/BankAPI.csproj
dotnet restore BankWeb/BankWeb.csproj
dotnet build BankAPI/BankAPI.csproj -c "$CONFIGURATION" --no-restore
dotnet build BankWeb/BankWeb.csproj -c "$CONFIGURATION" --no-restore
REMOTE_SCRIPT
        '''
      }
    }

    stage('Unit Tests') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PROJECT_DIR='$PROJECT_DIR' PUBLISH_ROOT='$PUBLISH_ROOT' CONFIGURATION='$CONFIGURATION' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$PROJECT_DIR"
rm -rf "$PUBLISH_ROOT/test-results"
mkdir -p "$PUBLISH_ROOT/test-results/coverage"

dotnet restore BankAPI.Tests/BankAPI.Tests.csproj
dotnet test BankAPI.Tests/BankAPI.Tests.csproj \
  -c "$CONFIGURATION" \
  --logger "trx;LogFileName=bankapi-tests.trx" \
  --results-directory "$PUBLISH_ROOT/test-results" \
  /p:CollectCoverage=true \
  /p:CoverletOutput="$PUBLISH_ROOT/test-results/coverage/coverage" \
  /p:CoverletOutputFormat=opencover
REMOTE_SCRIPT
        '''
      }
    }

    stage('SonarQube') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PROJECT_DIR='$PROJECT_DIR' PUBLISH_ROOT='$PUBLISH_ROOT' CONFIGURATION='$CONFIGURATION' SONAR_HOST_URL='http://$SONAR_HOST:$SONAR_PORT' SONAR_PROJECT_KEY='$SONAR_PROJECT_KEY' SONAR_TOKEN='$SONAR_TOKEN' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$PROJECT_DIR"
export PATH="$PATH:/home/vagrant/.dotnet/tools"

dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /n:"Bank Web API" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.exclusions="**/bin/**,**/obj/**,**/wwwroot/lib/**" \
  /d:sonar.coverage.exclusions="**/*Tests*/**,**/Program.cs,**/Migrations/**" \
  /d:sonar.cs.vstest.reportsPaths="$PUBLISH_ROOT/test-results/bankapi-tests.trx" \
  /d:sonar.cs.opencover.reportsPaths="$PUBLISH_ROOT/test-results/coverage/coverage.opencover.xml"

dotnet build BankAPI/BankAPI.csproj -c "$CONFIGURATION" --no-restore
dotnet build BankWeb/BankWeb.csproj -c "$CONFIGURATION" --no-restore
dotnet build BankAPI.Tests/BankAPI.Tests.csproj -c "$CONFIGURATION" --no-restore
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
REMOTE_SCRIPT
        '''
      }
    }

    stage('Publish Artifacts') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PROJECT_DIR='$PROJECT_DIR' PUBLISH_ROOT='$PUBLISH_ROOT' CONFIGURATION='$CONFIGURATION' MOBILE_CONFIGURATION='$MOBILE_CONFIGURATION' ANDROID_TFM='$ANDROID_TFM' ANDROID_SDK_DIR='$ANDROID_SDK_DIR' BUILD_NUMBER='$BUILD_NUMBER' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$PROJECT_DIR"
export ANDROID_HOME="$ANDROID_SDK_DIR"
export ANDROID_SDK_ROOT="$ANDROID_SDK_DIR"
export PATH="$PATH:$ANDROID_SDK_DIR/platform-tools:$ANDROID_SDK_DIR/cmdline-tools/latest/bin"

rm -rf "$PUBLISH_ROOT"
mkdir -p "$PUBLISH_ROOT/api" "$PUBLISH_ROOT/web" "$PUBLISH_ROOT/artifacts"

dotnet publish BankAPI/BankAPI.csproj -c "$CONFIGURATION" --no-restore -o "$PUBLISH_ROOT/api"
dotnet publish BankWeb/BankWeb.csproj -c "$CONFIGURATION" --no-restore -o "$PUBLISH_ROOT/web"
dotnet publish BankAPP/BankAPP.csproj -f "$ANDROID_TFM" -c "$MOBILE_CONFIGURATION" -p:AndroidPackageFormat=apk -p:EmbedAssembliesIntoApk=true -p:AndroidSdkDirectory="$ANDROID_SDK_DIR/"

tar -C "$PUBLISH_ROOT/api" -czf "$PUBLISH_ROOT/artifacts/bankapi-$BUILD_NUMBER.tar.gz" .
tar -C "$PUBLISH_ROOT/web" -czf "$PUBLISH_ROOT/artifacts/bankweb-$BUILD_NUMBER.tar.gz" .
find "BankAPP/bin/$MOBILE_CONFIGURATION/$ANDROID_TFM" -type f -name '*Signed.apk' | sort | tail -n 1 | xargs -I{} cp "{}" "$PUBLISH_ROOT/artifacts/bankapp-mobile-$BUILD_NUMBER.apk"
REMOTE_SCRIPT
        '''
      }
    }

    stage('Upload to Nexus') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PUBLISH_ROOT='$PUBLISH_ROOT' BUILD_NUMBER='$BUILD_NUMBER' NEXUS_URL='http://$NEXUS_HOST:$NEXUS_PORT' NEXUS_REPOSITORY='$NEXUS_REPOSITORY' NEXUS_USER='$NEXUS_CREDS_USR' NEXUS_PASS='$NEXUS_CREDS_PSW' bash -s" <<'REMOTE_SCRIPT'
set -e
curl -fsS -u "$NEXUS_USER:$NEXUS_PASS" --upload-file "$PUBLISH_ROOT/artifacts/bankapi-$BUILD_NUMBER.tar.gz" "$NEXUS_URL/repository/$NEXUS_REPOSITORY/web-api/$BUILD_NUMBER/bankapi-$BUILD_NUMBER.tar.gz"
curl -fsS -u "$NEXUS_USER:$NEXUS_PASS" --upload-file "$PUBLISH_ROOT/artifacts/bankweb-$BUILD_NUMBER.tar.gz" "$NEXUS_URL/repository/$NEXUS_REPOSITORY/web-api/$BUILD_NUMBER/bankweb-$BUILD_NUMBER.tar.gz"
curl -fsS -u "$NEXUS_USER:$NEXUS_PASS" --upload-file "$PUBLISH_ROOT/artifacts/bankapp-mobile-$BUILD_NUMBER.apk" "$NEXUS_URL/repository/$NEXUS_REPOSITORY/mobile/$BUILD_NUMBER/bankapp-mobile-$BUILD_NUMBER.apk"
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
            "REMOTE_ROOT='$REMOTE_ROOT' BANKAPP_API_IMAGE='$DOCKERHUB_CREDS_USR/$DOCKER_API_IMAGE:$BUILD_NUMBER' BANKAPP_WEB_IMAGE='$DOCKERHUB_CREDS_USR/$DOCKER_WEB_IMAGE:$BUILD_NUMBER' BANKAPP_DB_CONNECTION_STRING='$DB_CONNECTION_STRING' DOCKERHUB_USER='$DOCKERHUB_CREDS_USR' DOCKERHUB_PASS='$DOCKERHUB_CREDS_PSW' bash -s" <<'REMOTE_SCRIPT'
set -e
sudo env APP_ROOT="$REMOTE_ROOT" BANKAPP_API_IMAGE="$BANKAPP_API_IMAGE" BANKAPP_WEB_IMAGE="$BANKAPP_WEB_IMAGE" BANKAPP_DB_CONNECTION_STRING="$BANKAPP_DB_CONNECTION_STRING" DOCKERHUB_USER="$DOCKERHUB_USER" DOCKERHUB_PASS="$DOCKERHUB_PASS" bash "$REMOTE_ROOT/userdata/app-docker-deploy.sh"
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
      echo 'Web, API, mobile, Nexus upload, Docker push, and deployment completed.'
    }
  }
}
