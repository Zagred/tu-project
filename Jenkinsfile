pipeline {
    agent any

    environment {
        APP_VM = "192.168.56.104"
        PROJECT_DIR = "/home/vagrant/tu-project/bank-mobile-app"

        VAGRANT_CREDS = credentials('vagrant-login')
        NEXUS_CREDS   = credentials('nexus-login')

        NEXUS_VERSION = "nexus3"
        NEXUS_PROTOCOL = "http"
        NEXUS_URL = "192.168.56.101:8081"
        NEXUS_REPOSITORY = "app-snapshots"
        ARTVERSION = "${env.BUILD_ID}"
    }

    stages {
        stage('Checkout') {
            steps {
                git url: 'https://github.com/Zagred/tu-project.git', branch: 'main'
            }
        }

        stage('Build Android App') {
            steps {
                sh '''
                    sshpass -p "$VAGRANT_CREDS_PSW" ssh -o StrictHostKeyChecking=no $VAGRANT_CREDS_USR@$APP_VM \
                    "cd $PROJECT_DIR && ./gradlew clean assembleDebug"
                '''
            }
        }

        stage('Unit Test') {
            steps {
                sh '''
                    sshpass -p "$VAGRANT_CREDS_PSW" ssh -o StrictHostKeyChecking=no $VAGRANT_CREDS_USR@$APP_VM \
                    "cd $PROJECT_DIR && ./gradlew test"
                '''
            }
        }

        stage('Publish to Nexus') {
            steps {
                sh '''
                    sshpass -p "$VAGRANT_CREDS_PSW" ssh -o StrictHostKeyChecking=no $VAGRANT_CREDS_USR@$APP_VM \
                    "export NEXUS_USER='$NEXUS_CREDS_USR' && export NEXUS_PASS='$NEXUS_CREDS_PSW' && cd $PROJECT_DIR && ./gradlew publish -Dgradle.publish.allowInsecureProtocol=true"
                '''
            }
        }
    }

    post {
        always {
            echo 'Build stage finished.'
        }
    }
}
