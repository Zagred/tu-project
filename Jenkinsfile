pipeline {
  agent any

  environment {
    APP_VM      = "192.168.56.104"
    PROJECT_DIR = "/home/vagrant/tu-project/bank-mobile-app"

    VAGRANT_CREDS = credentials('vagrant-login')
    NEXUS_CREDS   = credentials('nexus-login')
    SONAR_TOKEN   = credentials('sonartoken')

    SONAR_HOST_URL     = "http://192.168.56.102:9000"
    SONAR_PROJECT_KEY  = "tu-bank-mobile-app"

    registryCredential = "docker-hub-credentials"
    appRegistry        = "pacopandev/android-build"
  }

  stages {
    stage('Checkout') {
      steps {
        git url: 'https://github.com/Zagred/tu-project.git', branch: 'main'
      }
    }

    stage('Build Android App') {
      steps {
        sh """
          sshpass -p "$VAGRANT_CREDS_PSW" ssh -o StrictHostKeyChecking=no $VAGRANT_CREDS_USR@$APP_VM \\
          "set -e; cd $PROJECT_DIR; chmod +x ./gradlew || true; ./gradlew clean assembleDebug"
        """
      }
    }

    stage('Unit Test') {
      steps {
        sh """
          sshpass -p "$VAGRANT_CREDS_PSW" ssh -o StrictHostKeyChecking=no $VAGRANT_CREDS_USR@$APP_VM \\
          "set -e; cd $PROJECT_DIR; ./gradlew test"
        """
      }
    }

    stage('SonarQube Analysis (CLI on App VM)') {
      steps {
        sh """
          sshpass -p "$VAGRANT_CREDS_PSW" ssh -o StrictHostKeyChecking=no $VAGRANT_CREDS_USR@$APP_VM \\
          "set -e; cd $PROJECT_DIR; \\
           export SONAR_TOKEN='$SONAR_TOKEN'; \\
           sonar-scanner \\
             -Dsonar.host.url=$SONAR_HOST_URL \\
             -Dsonar.projectKey=$SONAR_PROJECT_KEY \\
             -Dsonar.projectName='Bank Mobile App' \\
             -Dsonar.sources=app/src/main \\
             -Dsonar.tests=app/src/test \\
             -Dsonar.exclusions=**/build/**,**/.gradle/** \\
             -Dsonar.sourceEncoding=UTF-8 \\
             -Dsonar.token=\\$SONAR_TOKEN"
        """
      }
    }

    stage('Publish to Nexus') {
      steps {
        sh """
          sshpass -p "$VAGRANT_CREDS_PSW" ssh -o StrictHostKeyChecking=no $VAGRANT_CREDS_USR@$APP_VM \\
          "set -e; export NEXUS_USER='$NEXUS_CREDS_USR'; export NEXUS_PASS='$NEXUS_CREDS_PSW'; \\
           cd $PROJECT_DIR; ./gradlew publish -Dgradle.publish.allowInsecureProtocol=true"
        """
      }
    }

    stage('Build & Push android-build image') {
      steps {
        script {
          dockerImage = docker.build("${appRegistry}:${BUILD_NUMBER}", ".")
        }
        script {
          docker.withRegistry('https://index.docker.io/v1/', registryCredential) {
            dockerImage.push("${BUILD_NUMBER}")
            dockerImage.push("latest")
          }
        }
      }
    }
  }

  post {
    always {
      echo '✅ Build and analysis pipeline completed.'
    }
  }
}
