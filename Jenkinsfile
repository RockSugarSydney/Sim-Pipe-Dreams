pipeline{
    agent any
    stages{
        stage('Initialization'){
            steps{
                echo "initialization project ..."
            }
        }

        stage('Build Project for Windows Platform'){
            steps{
                bat "LocalUNITY3D -quit -batchmode -nographics -executeMethod BuildScript.MyBuild -logFile D:/KAIGAN GAMES/JENKINS_BUILD/Simulacra-pipedreams"
            }
        }

        stage('Archiving Build Project'){
            steps{
                echo 'Now Archiving...'
                archiveArtifacts artifacts: '**/target/*.exe',
                onlyIfSuccessful: true
            }
        }
    }
}