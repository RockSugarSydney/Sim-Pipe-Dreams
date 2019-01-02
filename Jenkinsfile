pipeline{
    agent any
    stages{
        stage('Initialization'){
            steps{
                success{
                    echo "initialization project ..."
                }
            }
        }

        stage('Build Project'){
            steps{
                bat "-quit -batchmode -nographics -executeMethod BuildScript.MyBuild -logFile D:/KAIGAN GAMES/JENKINS_BUILD/Simulacra-pipedreams"
            }
        }

        stage('Archiving Build Project'){
            steps{
                success{
                    echo 'Now Archiving...'
                    archiveArtifacts artifacts: '**/target/*.exe',
                    onlyIfSuccessful: true
                }
            }
        }
    }
}