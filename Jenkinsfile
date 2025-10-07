pipeline {
    agent any

    environment {
        APP_VM = "192.168.56.104"
        PROJECT_DIR = "/home/vagrant/tu-project/bank-mobile-app"

        VAGRANT_USER = credentials('vagrant-login_USR')
        VAGRANT_PASS = credentials('vagrant-login_PSW')
        NEXUS_USER   = credentials('nexus-login_USR')
        NEXUS_PASS   = credentials('nexus-login_PSW')

        NEXUS_VERSION = "nexus3"
        NEXUS_PROTOCOL = "http"
        NEXUS_URL = "192.168.56.101:8081"
        NEXUS_REPOSITORY = "app-snapshots"
        ARTVERSION = "${env.BUILD_ID}"
    }

    stages {
        stage('Checkout') {
            steps {
                git url: 'https://github.com/Zagred/tu-project.git', branch: 'main'
            }
        }

        stage('Build Android App') {
            steps {
                sh '''
                    sshpass -p "$VAGRANT_PASS" ssh -tt -o StrictHostKeyChecking=no $VAGRANT_USER@$APP_VM <<EOF
                        cd $PROJECT_DIR
                        ./gradlew clean assembleDebug
EOF
                '''
            }
        }

        stage('Unit Test') {
            steps {
                sh '''
                    sshpass -p "$VAGRANT_PASS" ssh -tt -o StrictHostKeyChecking=no $VAGRANT_USER@$APP_VM <<EOF
                        cd $PROJECT_DIR
                        ./gradlew test
EOF
                '''
            }
        }

        stage('Publish to Nexus') {
            steps {
                script {
                    sh '''
                        sshpass -p "$VAGRANT_PASS" ssh -tt -o StrictHostKeyChecking=no $VAGRANT_USER@$APP_VM <<EOF
                            export NEXUS_USER="$NEXUS_USER"
                            export NEXUS_PASS="$NEXUS_PASS"
                            cd $PROJECT_DIR
                            ./gradlew publish
EOF
                    '''
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
