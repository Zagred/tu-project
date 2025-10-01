pipeline {
    agent {
        docker {
            image 'pacopandev/android-build:latest'
            args '-u root:root'
        }
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Dependencies') {
            steps {
                sh "./gradlew dependencies"
            }
        }

        stage('Assemble Debug APK') {
            steps {
                sh "./gradlew clean assembleDebug"
            }
        }

        stage('Archive APK') {
            steps {
                archiveArtifacts artifacts: 'app/build/outputs/apk/debug/*.apk', fingerprint: true
            }
        }
    }

    post {
        success {
            echo "✅ Build successful. APK is archived."
        }
        failure {
            echo "❌ Build failed. Check logs."
        }
    }
}
