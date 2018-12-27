using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using UnityEditor.Build.Reporting;

public class BuildSimulacra : MonoBehaviour
{
    [MenuItem("Build/Build StandaloneWindows")]
    public static void buildWindows(){
        // BuildPlayerOptions buildPlayerOptions   = new BuildPlayerOptions();
        // buildPlayerOptions.scenes               = new[] {"Assets/Scenes/PhoneMenu.unity", "Assets/Scenes/Phone.unity", "Assets/Scenes/Chats.unity", "Assets/Scenes/Gallery.unity", "Assets/Scenes/Mail.unity", "Assets/Scenes/Spark.unity", "Assets/Scenes/Jabbr.unity", "Assets/Scenes/Vloggr.unity"};
        // buildPlayerOptions.locationPathName     = "WindowsBuild";
        // buildPlayerOptions.target               = BuildTarget.StandaloneWindows;
        // buildPlayerOptions.options              = BuildOptions.None;

        // BuildReport report      = BuildPipeline.BuildPlayer(buildPlayerOptions);
        // BuildSummary summary    = report.summary;

        //get filename
        string path     = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
        string[] levels = new string[] {"Assets/Scenes/PhoneMenu.unity", "Assets/Scenes/Phone.unity", "Assets/Scenes/Chats.unity", "Assets/Scenes/Gallery.unity", "Assets/Scenes/Mail.unity", "Assets/Scenes/Spark.unity", "Assets/Scenes/Jabbr.unity", "Assets/Scenes/Vloggr.unity"};

        //Build player
        BuildPipeline.BuildPlayer(levels, path + "/BuiltGame.exe", BuildTarget.StandaloneWindows, BuildOptions.None);

        // Copy a file from the project folder to the build folder, alongside the built game.
        FileUtil.CopyFileOrDirectory("Assets/Templates/Readme.txt", path + "Readme.txt");

        // Run the game (Process class from System.Diagnostics).
        Process proc = new Process();
        proc.StartInfo.FileName = path + "/BuiltGame.exe";
        proc.Start();
    }
}