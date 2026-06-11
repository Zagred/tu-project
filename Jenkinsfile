pipeline {
  agent any

  environment {
    REPO_URL    = 'https://github.com/Zagred/tu-project.git'
    REPO_BRANCH = 'dev'

    REMOTE_ROOT  = '/home/vagrant/tu-project-dev'
    PROJECT_DIR  = '/home/vagrant/tu-project-dev/BankApp'
    PUBLISH_ROOT = '/tmp/bankapp-dev-publish'
    SSH_OPTS     = '-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=15 -o PreferredAuthentications=password -o PubkeyAuthentication=no -o NumberOfPasswordPrompts=1'

    CONFIGURATION = 'Debug'

    APP_VM        = credentials('bankapp-app-vm-host')
    VAGRANT_CREDS = credentials('vagrant-login')
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
            "PROJECT_DIR='$PROJECT_DIR' CONFIGURATION='$CONFIGURATION' bash -s" <<'REMOTE_SCRIPT'
set -e
cd "$PROJECT_DIR"
dotnet restore BankAPI.Tests/BankAPI.Tests.csproj
dotnet test BankAPI.Tests/BankAPI.Tests.csproj -c "$CONFIGURATION" --no-restore
REMOTE_SCRIPT
        '''
      }
    }

    stage('Promote to Test') {
      steps {
        withCredentials([usernamePassword(
          credentialsId: 'github-credentials',
          usernameVariable: 'GITHUB_USER',
          passwordVariable: 'GITHUB_TOKEN'
        )]) {
          sh '''
            set -e
            git config user.email "jenkins@bankapp.local"
            git config user.name "Jenkins"

            git remote set-url origin "https://$GITHUB_USER:$GITHUB_TOKEN@github.com/Zagred/tu-project.git"
            git fetch origin dev test

            git checkout -B test origin/test
            git merge --no-ff origin/dev -m "Promote dev to test from Jenkins build $BUILD_NUMBER"
            git push origin test
          '''
        }
      }
    }
  }

  post {
    success {
      echo 'Dev validation completed and promoted to test.'
    }
  }
}
