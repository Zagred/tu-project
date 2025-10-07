pipeline {
    agent any

    environment {
        APP_VM = "192.168.56.104"
        PROJECT_DIR = "/home/vagrant/tu-project/bank-mobile-app"

        // These must be created as separate username/password credentials
        VM_USER = credentials('vagrant-login_USR')
        VM_PASS = credentials('vagrant-login_PSW')
        NEXUS_USER = credentials('nexus-login_USR')
        NEXUS_PASS = credentials('nexus-login_PSW')
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
                    sshpass -p "${VM_PASS}" ssh -tt -o StrictHostKeyChecking=no ${VM_USER}@${APP_VM} "
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
                    sshpass -p "${VM_PASS}" ssh -tt -o StrictHostKeyChecking=no ${VM_USER}@${APP_VM} "
                        export NEXUS_USER='${NEXUS_USER}' && \
                        export NEXUS_PASS='${NEXUS_PASS}' && \
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
