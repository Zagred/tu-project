pipeline {
    agent any

    environment {
        APP_VM = "192.168.56.104"
        REPO_URL = "https://github.com/Zagred/tu-project.git"
        PROJECT_DIR = "/home/vagrant/tu-project/bank-mobile-app"
        RELEASE_REPO = 'bank-mobile-app-release'
        NEXUSIP = '192.168.56.101'
        NEXUSPORT = '8081'
        NEXUS_LOGIN = 'nexuslogin'
        SSH_CREDENTIALS = 'vagrant-key'   
    }

    stages {
        stage('Build Bank Mobile App') {
            steps {
                sshagent([SSH_CREDENTIALS]) {
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

        stage('Upload APK to Nexus') {
            steps {
                nexusArtifactUploader(
                    nexusVersion: 'nexus3',
                    protocol: 'http',
                    nexusUrl: "${NEXUSIP}:${NEXUSPORT}",
                    groupId: 'QA',
                    version: "${env.BUILD_ID}",
                    repository: "${RELEASE_REPO}",
                    credentialsId: "${NEXUS_LOGIN}",
                    artifacts: [
                        [artifactId: 'bank-mobile-app',
                         classifier: '',
                         file: "${PROJECT_DIR}/build/outputs/apk/debug/app-debug.apk",
                         type: 'apk']
                    ]
                )
            }
        }
    }
}
