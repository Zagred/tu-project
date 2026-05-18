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

    API_URL = 'http://192.168.56.104:7083'
    WEB_URL = 'http://192.168.56.104:5000'

    SONAR_HOST_URL    = 'http://192.168.56.102:9000'
    SONAR_PROJECT_KEY = 'tu-bank-web-api'

    NEXUS_URL        = 'http://192.168.56.101:8081'
    NEXUS_REPOSITORY = 'android-apps'

    VAGRANT_CREDS = credentials('vagrant-login')
    NEXUS_CREDS   = credentials('nexus-login')
    SONAR_TOKEN   = credentials('sonartoken')
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
            "REPO_BRANCH='$REPO_BRANCH' REMOTE_ROOT='$REMOTE_ROOT' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$REMOTE_ROOT"
git fetch origin "$REPO_BRANCH"
git reset --hard "origin/$REPO_BRANCH"
git clean -fdx
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

    stage('Upload Artifacts to Nexus') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PUBLISH_ROOT='$PUBLISH_ROOT' BUILD_NUMBER='$BUILD_NUMBER' NEXUS_URL='$NEXUS_URL' NEXUS_REPOSITORY='$NEXUS_REPOSITORY' NEXUS_USER='$NEXUS_CREDS_USR' NEXUS_PASS='$NEXUS_CREDS_PSW' bash -s" <<'REMOTE_SCRIPT'
set -e
curl -fsS -u "$NEXUS_USER:$NEXUS_PASS" --upload-file "$PUBLISH_ROOT/artifacts/bankapi-$BUILD_NUMBER.tar.gz" "$NEXUS_URL/repository/$NEXUS_REPOSITORY/web-api/$BUILD_NUMBER/bankapi-$BUILD_NUMBER.tar.gz"
curl -fsS -u "$NEXUS_USER:$NEXUS_PASS" --upload-file "$PUBLISH_ROOT/artifacts/bankweb-$BUILD_NUMBER.tar.gz" "$NEXUS_URL/repository/$NEXUS_REPOSITORY/web-api/$BUILD_NUMBER/bankweb-$BUILD_NUMBER.tar.gz"
REMOTE_SCRIPT
        '''
      }
    }

    stage('Deploy App VM') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" \
            "PUBLISH_ROOT='$PUBLISH_ROOT' bash -s" <<'REMOTE_SCRIPT'
set -e
sudo systemctl stop bankweb
sudo systemctl stop bankapi

sudo rm -rf /opt/bankapp/api /opt/bankapp/web
sudo mkdir -p /opt/bankapp/api /opt/bankapp/web
sudo cp -a "$PUBLISH_ROOT/api/." /opt/bankapp/api/
sudo cp -a "$PUBLISH_ROOT/web/." /opt/bankapp/web/
sudo chown -R www-data:www-data /opt/bankapp/api /opt/bankapp/web
sudo chmod +x /opt/bankapp/api/BankAPI /opt/bankapp/web/BankWeb

sudo systemctl daemon-reload
sudo systemctl start bankapi
sudo systemctl start bankweb
sudo systemctl --no-pager -l status bankapi
sudo systemctl --no-pager -l status bankweb
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
      echo 'Web and API build, analysis, publish, deploy, and smoke tests completed.'
    }
  }
}
