pipeline {
    agent any

    environment {
        APP_VM = "192.168.56.104"
        PROJECT_DIR = "/home/vagrant/tu-project/bank-mobile-app"

        VAGRANT_CREDS = credentials('vagrant-login')
        NEXUS_CREDS = credentials('nexus-login')
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
                    sshpass -p "${VAGRANT_CREDS_PSW}" ssh -tt -o StrictHostKeyChecking=no ${VAGRANT_CREDS_USR}@${APP_VM} "
                        cd ${PROJECT_DIR} && \
                        git pull && \
                        ./gradlew assembleDebug
                    "
                """
            }
        }

        stage('Publish to Nexus') {
            steps {
                sh """
                    sshpass -p "${VAGRANT_CREDS_PSW}" ssh -tt -o StrictHostKeyChecking=no ${VAGRANT_CREDS_USR}@${APP_VM} "
                        export NEXUS_USER='${NEXUS_CREDS_USR}' && \
                        export NEXUS_PASS='${NEXUS_CREDS_PSW}' && \
                        cd ${PROJECT_DIR} && \
                        ./gradlew publish
                    "
                """
            }
        }
    }

    post {
        always {
            echo 'Build stage finished.'
        }
    }
}
