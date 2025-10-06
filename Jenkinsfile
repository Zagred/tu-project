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
                        sshpass -p "$PASS" ssh -tt -o StrictHostKeyChecking=no $USER@${APP_VM} '
                            export ANDROID_HOME=\$HOME/Android/Sdk
                            export PATH=\$ANDROID_HOME/cmdline-tools/latest/bin:\$ANDROID_HOME/platform-tools:\$PATH
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
