pipeline {
  agent any

  environment {
    PROJECT_DIR = "bank-mobile-app"

    SONARSERVER = "sonarserver"
    SONAR_HOST_URL = "http://192.168.56.102:9000"
    SONAR_PROJECT_KEY = "tu-bank-mobile-app"
    SONAR_PROJECT_NAME = "Bank Mobile App"

    NEXUS_CREDS = credentials('nexus-login')

    DOCKER_CREDS = credentials('docker-hub-credentials') // username+password
    DOCKER_IMAGE = "pacopandev/bank-mobile-app"
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
          cd ${PROJECT_DIR}
          ./gradlew clean assembleDebug
        """
      }
    }

    stage('Unit Test') {
      steps {
        sh """
          cd ${PROJECT_DIR}
          ./gradlew test
        """
      }
    }

    stage('SonarQube Analysis') {
      steps {
        withSonarQubeEnv("${SONARSERVER}") {
          sh """
            cd ${PROJECT_DIR}
            sonar-scanner \
              -Dsonar.host.url=${SONAR_HOST_URL} \
              -Dsonar.projectKey=${SONAR_PROJECT_KEY} \
              -Dsonar.projectName="${SONAR_PROJECT_NAME}" \
              -Dsonar.sources=app/src/main \
              -Dsonar.tests=app/src/test \
              -Dsonar.exclusions=**/build/**,**/.gradle/** \
              -Dsonar.token=$SONAR_AUTH_TOKEN
          """
        }
      }
    }

    stage('Quality Gate') {
      steps {
        timeout(time: 30, unit: 'MINUTES') {
          waitForQualityGate abortPipeline: true
        }
      }
    }

    stage('Publish to Nexus') {
      steps {
        sh """
          export NEXUS_USER='${NEXUS_CREDS_USR}'
          export NEXUS_PASS='${NEXUS_CREDS_PSW}'
          cd ${PROJECT_DIR}
          ./gradlew publish -Dgradle.publish.allowInsecureProtocol=true
        """
      }
    }

    stage('Build & Push Docker Image') {
      steps {
        sh """
          test -f ${PROJECT_DIR}/app/build/outputs/apk/debug/app-debug.apk

          docker build \
            --build-arg APK_PATH=${PROJECT_DIR}/app/build/outputs/apk/debug/app-debug.apk \
            -t ${DOCKER_IMAGE}:${BUILD_NUMBER} \
            -t ${DOCKER_IMAGE}:latest \
            .
        """

        sh """
          echo '${DOCKER_CREDS_PSW}' | docker login -u '${DOCKER_CREDS_USR}' --password-stdin
          docker push ${DOCKER_IMAGE}:${BUILD_NUMBER}
          docker push ${DOCKER_IMAGE}:latest
        """
      }
    }
  }

  post {
    always {
      echo '✅ Pipeline completed.'
    }
  }
}
