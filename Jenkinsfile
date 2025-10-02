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
                sshagent (credentials: ['vagrant-key']) {
                    sh """
                    ssh -o StrictHostKeyChecking=no vagrant@${APP_VM} '
                        cd ${PROJECT_DIR} && \
                        git pull && \
                        ./gradlew assembleDebug --no-daemon
                    '
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
