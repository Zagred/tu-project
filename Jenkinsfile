pipeline {
    agent any

    environment {
        APP_VM = "192.168.56.104"
        PROJECT_DIR = "/home/vagrant/tu-project/bank-mobile-app"
    }

    stages {
        stage('Checkout') {
            steps {
                git url: 'https://github.com/Zagred/tu-project.git', branch: 'main'
            }
        }

        stage('Build Android App') {
            steps {
                withCredentials([usernamePassword(credentialsId: 'vagrant-login', usernameVariable: 'USER', passwordVariable: 'PASS')]) {
                    sh """
                        sshpass -p "$PASS" ssh -tt -o StrictHostKeyChecking=no $USER@${APP_VM} "
                            cd ${PROJECT_DIR} && \
                            git pull && \
                            ./gradlew assembleDebug
                        "
                    """
                }
            }
        }
        stage('Publish to Nexus') {
            steps {
                withCredentials([usernamePassword(credentialsId: 'nexus-login', usernameVariable: 'NEXUS_USER', passwordVariable: 'NEXUS_PASS')]) {
                    sh """
                        sshpass -p "$PASS" ssh -tt -o StrictHostKeyChecking=no $USER@${APP_VM} "
                            cd ${PROJECT_DIR} && \
                            ./gradlew publish
                        "
                    """
                }
            }
        }
    }

    post {
        always {
            echo 'Build stage finished.'
        }
    }
}
