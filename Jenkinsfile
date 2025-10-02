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
        stage('Build on VM') {
    steps {
        sshagent(['SSH_KEY_ID']) {
            sh """
                ssh -o StrictHostKeyChecking=no vagrant@192.168.56.104 '
                    cd /home/vagrant/tu-project/bank-mobile-app &&
                    git pull &&
                    ./gradlew assembleDebug --no-daemon
                '
                scp -o StrictHostKeyChecking=no vagrant@192.168.56.104:/home/vagrant/tu-project/bank-mobile-app/app/build/outputs/apk/debug/app-debug.apk .
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
            version: "${env.BUILD_ID}-${env.BUILD_TIMESTAMP}",
            repository: "${RELEASE_REPO}",
            credentialsId: "${NEXUS_LOGIN}",
            artifacts: [[
                artifactId: 'bank-mobile-app',
                classifier: '',
                file: 'app-debug.apk',
                type: 'apk'
            ]]
        )
    }
}

    }
}
