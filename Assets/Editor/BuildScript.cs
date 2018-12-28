using UnityEditor;
using UnityEngine;

public class BuildScript : MonoBehaviour
{
    [MenuItem("Build/Build Windows")]
    public static void MyBuild()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scene1.unity", "Assets/Scene2.unity" };
        buildPlayerOptions.locationPathName = "WindowsBuild";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows;
        buildPlayerOptions.options = BuildOptions.None;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}