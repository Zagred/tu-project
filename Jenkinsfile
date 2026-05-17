pipeline {
  agent any

  environment {
    REPO_URL    = 'https://github.com/Zagred/tu-project.git'
    REPO_BRANCH = 'main'

    APP_VM      = '192.168.56.104'
    REMOTE_ROOT = '/home/vagrant/tu-project'
    PROJECT_DIR = '/home/vagrant/tu-project/BankAPP'
    SSH_OPTS    = '-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=15 -o PreferredAuthentications=password -o PubkeyAuthentication=no -o NumberOfPasswordPrompts=1'

    CONFIGURATION = 'Debug'
    ANDROID_TFM   = 'net10.0-android'

    SONAR_HOST_URL    = 'http://192.168.56.102:9000'
    SONAR_PROJECT_KEY = 'tu-bank-mobile-app'

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

    stage('Test SSH to App VM') {
      steps {
        sh '''
          set -e
          echo "Testing SSH from Jenkins to $APP_VM as $VAGRANT_CREDS_USR"
          command -v sshpass
          command -v ssh
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" '
            set -e
            echo "SSH OK"
            hostname
            whoami
          '
        '''
      }
    }

    stage('Prepare App VM') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" "
            set -e
            if [ -d '$REMOTE_ROOT/.git' ]; then
              cd '$REMOTE_ROOT'
              git fetch origin '$REPO_BRANCH'
              git reset --hard 'origin/$REPO_BRANCH'
            else
              rm -rf '$REMOTE_ROOT'
              git clone --branch '$REPO_BRANCH' '$REPO_URL' '$REMOTE_ROOT'
            fi
          "
        '''
      }
    }

    stage('Build') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" "
            set -e
            cd '$PROJECT_DIR'
            export ANDROID_HOME=/opt/android-sdk
            export ANDROID_SDK_ROOT=/opt/android-sdk
            export PATH=\$PATH:/home/vagrant/.dotnet/tools:/opt/android-sdk/platform-tools:/opt/android-sdk/cmdline-tools/latest/bin
            dotnet restore BankAPP.slnx /p:AndroidSdkDirectory=/opt/android-sdk/
            dotnet build BankAPP.slnx -c '$CONFIGURATION' --no-restore /p:AndroidSdkDirectory=/opt/android-sdk/
          "
        '''
      }
    }

    stage('SonarQube') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" "
            set -e
            cd '$PROJECT_DIR'
            export ANDROID_HOME=/opt/android-sdk
            export ANDROID_SDK_ROOT=/opt/android-sdk
            export PATH=\$PATH:/home/vagrant/.dotnet/tools:/opt/android-sdk/platform-tools:/opt/android-sdk/cmdline-tools/latest/bin
            export SONAR_TOKEN='$SONAR_TOKEN'

            dotnet sonarscanner begin \
              /k:'$SONAR_PROJECT_KEY' \
              /n:'Bank Mobile App' \
              /d:sonar.host.url='$SONAR_HOST_URL' \
              /d:sonar.token=\$SONAR_TOKEN \
              /d:sonar.exclusions='**/bin/**,**/obj/**'

            dotnet build BankAPP.slnx -c '$CONFIGURATION' --no-restore /p:AndroidSdkDirectory=/opt/android-sdk/

            dotnet sonarscanner end /d:sonar.token=\$SONAR_TOKEN
          "
        '''
      }
    }

    stage('Publish APK to Nexus') {
      steps {
        sh '''
          set -e
          sshpass -p "$VAGRANT_CREDS_PSW" ssh $SSH_OPTS "$VAGRANT_CREDS_USR@$APP_VM" "
            set -e
            cd '$PROJECT_DIR'

            APK=\$(find 'BankAPP/bin/$CONFIGURATION/$ANDROID_TFM' -maxdepth 1 -name '*Signed.apk' | head -n 1)
            if [ -z \"\$APK\" ]; then
              APK=\$(find 'BankAPP/bin/$CONFIGURATION/$ANDROID_TFM' -maxdepth 1 -name '*.apk' | head -n 1)
            fi

            if [ -z \"\$APK\" ]; then
              echo 'No APK found to publish.'
              exit 1
            fi

            curl -fsS \
              -u '$NEXUS_CREDS_USR:$NEXUS_CREDS_PSW' \
              --upload-file \"\$APK\" \
              '$NEXUS_URL/repository/$NEXUS_REPOSITORY/$BUILD_NUMBER/'\$(basename \"\$APK\")
          "
        '''
      }
    }
  }

  post {
    always {
      echo 'Build, SonarQube analysis, and Nexus publish finished.'
    }
  }
}
