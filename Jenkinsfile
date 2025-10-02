pipeline {
    agent any

    environment {
        ANSIBLE_CREDENTIALS = "vagrant-id_rsa"
    }

    stages {
        stage('Checkout') {
            steps {
                git url: 'https://github.com/Zagred/tu-project.git', branch: 'main'
            }
        }

        stage('Configure App VM with Ansible') {
            steps {
                ansiblePlaybook(
                    playbook: 'ansible/app.yaml',
                    inventory: 'ansible/inventory.ini',
                    extras: '-u vagrant --private-key /home/vagrant/.ssh/id_rsa -o StrictHostKeyChecking=no'
                )
            }
        }

        stage('Build Kotlin App') {
            steps {
                sshagent(credentials: ["${ANSIBLE_CREDENTIALS}"]) {
                    sh """
                    ssh -o StrictHostKeyChecking=no vagrant@192.168.56.104 '
                        cd /home/vagrant/tu-project &&
                        git pull &&
                        ./gradlew build
                    '
                    """
                }
            }
        }
    }
}
