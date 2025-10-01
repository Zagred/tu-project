pipeline {
    agent any  // Runs on the Jenkins VM

    stages {
        stage('Checkout') {
            steps {
                // Pull code from GitHub
                git 'https://github.com/Zagred/tu-project.git'
            }
        }

        stage('Archive APK') {
            steps {
                // Archive APKs produced by your manual Docker run
                archiveArtifacts artifacts: 'app/build/outputs/apk/debug/*.apk', fingerprint: true
            }
        }
    }

    post {
        success {
            echo "✅ APK archived successfully."
        }
        failure {
            echo "❌ Build failed. Check logs."
        }
    }
}
