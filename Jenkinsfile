pipeline {
    agent any
    tools {
        dotnetsdk 'dotnet-sdk-8.0'
    }
    environment {
        PATH = "$PATH:/root/.dotnet/tools"
    }
    stages {
        stage('Setup Environment') {
            steps {
                echo 'Installing necessary packages...'
                sh 'apt-get update && apt-get install -y libicu-dev'
            }
        }
        stage('Checkout') {
            steps {
                git branch: 'main',
                    credentialsId: 'deploy',
                    url: 'git@github.com:RommensArne/TestNet-repo.git'
            }
        }
        stage('Restore Dependencies') {
            steps {
                sh 'dotnet restore'
            }
        }
        stage('Install dotnet ef Tool') {
            steps {
                echo 'Installing dotnet-ef tool...'
                sh 'dotnet tool install --global dotnet-ef'
            }
        }
        stage('Apply Migrations') {
            steps {
                echo 'Applying database migrations...'
                sh 'dotnet ef database update --startup-project Rise.Server --project Rise.Persistence'
            }
        }
        stage('Publish Application') {
            steps {
                echo 'Publishing the application...'
                sh 'dotnet publish Rise.Server/Rise.Server.csproj -c Release -o ./publish/server'
                sh 'dotnet publish Rise.Client/Rise.Client.csproj -c Release -o ./publish/client'
            }
        }    
        stage('Deploy to AppServer') {
            steps {
                echo 'Preparing and copying files to AppServer...'
                withCredentials([sshUserPrivateKey(credentialsId: 'appserver', keyFileVariable: 'SSH_KEY')]) {
                    sh """
                        ssh -i $SSH_KEY -o StrictHostKeyChecking=no vagrant@172.24.128.6 '
                            sudo mkdir -p /home/server /home/client &&
                            sudo chown -R vagrant:vagrant /home/server /home/client &&
                            sudo chmod -R 755 /home/server /home/client
                        '
                        scp -i $SSH_KEY -o StrictHostKeyChecking=no -r ./publish/server/* vagrant@172.24.128.6:/home/server/
                        scp -i $SSH_KEY -o StrictHostKeyChecking=no -r ./publish/client/* vagrant@172.24.128.6:/home/client/
                    """
                }
            }
        }
    }
    post {
        success {
            echo 'Pipeline completed successfully! Database created successfully and application published!'
        }
        failure {
            echo 'Pipeline execution failed. Database creation failed.'
        }
    }
}
