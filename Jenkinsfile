pipeline {
    agent any

    environment {
        ANSIBLE_CREDENTIALS = "vagrant-key"
    }

    stages {
        stage('Checkout') {
            steps {
                git url: 'https://github.com/Zagred/tu-project.git', branch: 'main'
            }
        }

        stage('Build Bank Mobile App on App VM') {
            steps {
                sshagent(credentials: ["${ANSIBLE_CREDENTIALS}"]) {
                    sh """
                    ssh -o StrictHostKeyChecking=no vagrant@192.168.56.104 '
                        # Set environment variables
                        export JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64 &&
                        export ANDROID_HOME=/home/vagrant/Android/Sdk &&
                        export PATH=\$JAVA_HOME/bin:\$ANDROID_HOME/tools:\$ANDROID_HOME/platform-tools:\$PATH &&

                        # Navigate to the project directory
                        cd /home/vagrant/tu-project/bank-mobile-app &&

                        # Pull latest code
                        git pull &&

                        # Build the app using Gradle wrapper
                        ./gradlew assembleDebug --no-daemon
                    '
                    """
                }
            }
        }

        stage('Archive APK') {
            steps {
                sshagent(credentials: ["${ANSIBLE_CREDENTIALS}"]) {
                    sh """
                    scp -o StrictHostKeyChecking=no vagrant@192.168.56.104:/home/vagrant/tu-project/bank-mobile-app/app/build/outputs/apk/debug/app-debug.apk ./app-debug.apk
                    """
                }
                archiveArtifacts artifacts: 'app-debug.apk', fingerprint: true
            }
        }
    }
}
