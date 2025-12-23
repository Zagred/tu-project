pipeline {
  agent any

  environment {
    APP_VM      = "192.168.56.104"
    PROJECT_DIR = "/home/vagrant/tu-project/bank-mobile-app"

    VAGRANT_CREDS = credentials('vagrant-login')   // gives VAGRANT_CREDS_USR / VAGRANT_CREDS_PSW
    NEXUS_CREDS   = credentials('nexus-login')     // gives NEXUS_CREDS_USR / NEXUS_CREDS_PSW
    SONAR_TOKEN   = credentials('sonartoken')      // secret text recommended
    SONAR_HOST_URL = "http://192.168.56.102:9000"
    SONAR_PROJECT_KEY = "tu-bank-mobile-app"
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
          "cd $PROJECT_DIR && ./gradlew clean assembleDebug"
        """
      }
    }

    stage('Unit Test') {
      steps {
        sh """
          sshpass -p "$VAGRANT_CREDS_PSW" ssh -o StrictHostKeyChecking=no $VAGRANT_CREDS_USR@$APP_VM \\
          "cd $PROJECT_DIR && ./gradlew test"
        """
      }
    }

    stage('SonarQube Analysis (CLI on App VM)') {
      steps {
        sh """
          sshpass -p "$VAGRANT_CREDS_PSW" ssh -o StrictHostKeyChecking=no $VAGRANT_CREDS_USR@$APP_VM \\
          "cd $PROJECT_DIR && \\
           export SONAR_TOKEN='$SONAR_TOKEN' && \\
           sonar-scanner \\
             -Dsonar.host.url=$SONAR_HOST_URL \\
             -Dsonar.projectKey=$SONAR_PROJECT_KEY \\
             -Dsonar.projectName='Bank Mobile App' \\
             -Dsonar.token=\\$SONAR_TOKEN"
        """
      }
    }

    stage('Publish to Nexus') {
      steps {
        sh """
          sshpass -p "$VAGRANT_CREDS_PSW" ssh -o StrictHostKeyChecking=no $VAGRANT_CREDS_USR@$APP_VM \\
          "export NEXUS_USER='$NEXUS_CREDS_USR' && export NEXUS_PASS='$NEXUS_CREDS_PSW' && \\
           cd $PROJECT_DIR && ./gradlew publish -Dgradle.publish.allowInsecureProtocol=true"
        """
      }
    }
  }

  post {
    always {
      echo '✅ Build and analysis pipeline completed.'
    }
  }
}
