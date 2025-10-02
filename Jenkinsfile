pipeline {
    agent any

    environment {
        ANSIBLE_CREDENTIALS = "vagrant-key"
        APP_VM = "192.168.56.104"
        REPO_URL = "https://github.com/Zagred/tu-project.git"
        PROJECT_DIR = "/home/vagrant/tu-project/bank-mobile-app"

        NEXUS_USER = 'admin'
        NEXUS_PASS = 'rdxx12rd'
        NEXUSIP = '192.168.56.101'
        NEXUSPORT = '8081'
        NEXUS_LOGIN = 'nexuslogin'
    }

    stages {
        stage('Configure App VM with Ansible') {
            steps {
                ansiblePlaybook(
                    playbook: 'ansible/setup-app-vm.yaml',
                    inventory: 'ansible/inventory.ini',
                    credentialsId: "${ANSIBLE_CREDENTIALS}"
                )
            }
        }

        stage('Build Bank Mobile App') {
            steps {
                sshagent([ANSIBLE_CREDENTIALS]) {
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
                    groupId: 'com.bank.app',
                    version: "${env.BUILD_ID}",
                    repository: "apk-snapshots",
                    credentialsId: "${NEXUS_LOGIN}",
                    artifacts: [
                        [artifactId: 'bank-mobile-app',
                         classifier: 'debug',
                         file: "${PROJECT_DIR}/app/build/outputs/apk/debug/app-debug.apk",
                         type: 'apk']
                    ]
                )
            }
        }
    }
}
