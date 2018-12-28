using UnityEditor;
using UnityEngine;

public class BuildScript : MonoBehaviour
{
    [MenuItem("Build/Build Windows")]
    public static void MyBuild()
    {
        // BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        // buildPlayerOptions.scenes = new[] 
		// 					{
		// 						"Assets/Scenes/PhoneMenu.unity", 
		// 						"Assets/Scenes/Phone.unity", 
		// 						"Assets/Scenes/Chats.unity", 
		// 						"Assets/Scenes/Gallery.unity", 
		// 						"Assets/Scenes/Mail.unity", 
		// 						"Assets/Scenes/Spark.unity", 
		// 						"Assets/Scenes/Jabbr.unity", 
		// 						"Assets/Scenes/Vloggr.unity"
		// 					};
        // buildPlayerOptions.locationPathName = "WindowsBuild";
        // buildPlayerOptions.target = BuildTarget.StandaloneWindows;
        // buildPlayerOptions.options = BuildOptions.None;
        // BuildPipeline.BuildPlayer(buildPlayerOptions);

		// Get filename.
        string path = "D:/KAIGAN GAMES/JENKINS_BUILD/Simulacra-pipedreams";
        string[] levels = new string[] 
						{
							"Assets/Scenes/PhoneMenu.unity", 
							"Assets/Scenes/Phone.unity", 
							"Assets/Scenes/Chats.unity", 
							"Assets/Scenes/Gallery.unity", 
							"Assets/Scenes/Mail.unity", 
							"Assets/Scenes/Spark.unity", 
							"Assets/Scenes/Jabbr.unity", 
							"Assets/Scenes/Vloggr.unity"
						};
						
		// Build player.
        BuildPipeline.BuildPlayer(levels, path + "/BuiltGame.exe", BuildTarget.StandaloneWindows, BuildOptions.None);
    }
}