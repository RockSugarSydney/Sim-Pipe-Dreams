using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using Ionic.Zip; // this uses the Unity port of DotNetZip https://github.com/r2d2rigo/dotnetzip-for-unity

public class BuildRadiator
{
    [MenuItem("BuildRadiator/Build Windows")]
    public static void StartWindows(){
        //get filename 
        string path     = EditorUtility.SaveFolderPanel("Build out Windows to ...", GetFolderProjectpath() + "/Builds/", "");
        var filename    = path.Split('/'); // do this so I can grab the project folder name
        BuildPlayer(BuildTarget.StandaloneWindows, filename[filename.Length-1], path + "/" );
    }

    [MenuItem("Buildradiator/Build Windows + Mac OSX + Linux")]
    public static void StartAll(){
        //get 
        string path     = EditorUtility.SaveFolderPanel("Build ot ALL STANDALONES to ...", GetProjectFolderPath() + "/Builds/", "");
        var filename    = path.Split('/');
        BuildPlayer(BuildTarget.StandaloneWindows, filename[filename.Length-1], path + "/");
        BuildPlayer(BuildTarget.StandaloneOSXUniversal, filename[filename.Length-1], path + "/");
        BuildPlayer(BuildTarget.StandaloneLinuxUniversal, filename[filename.Length-1], path + "/");
    }

    //this is main palyer builder function
    static void BuildPlayer(BuildTarget buildTarget, string filename, string path){
        string fileExtension    = "";
        string dataPath         = "";
        string modifier         = "";

        //configure path variables based on the platform we're tergetting
        switch(buildTarget){
            //if buildtarget is windows
            case buildTarget.StandaloneWindows:
            case buildTarget.StandaloneWindows64:
                modifier        = "_windows";
                fileExtension   = ".exe";
                dataPath        = "_Data/";
            break;
            //if buildtarget is OSX
            case buildTarget.StandaloneOSXIntel:
            case buildTarget.StandaloneOSXIntel64:
            case buildTarget.StandaloneOSXUniversal:
                modifier        = "_mac-osx";
                fileExtension   = ".app";
                dataPath        = fileExtension + "/Contents/";
            break;
            //if buildtarget is Linux
            case buildTarget.StandaloneLinuxUniversal:
            case buildTarget.StandaloneLinux:
            case buildTarget.StandaloneLinux64:
                modifier        = "_linux";
                dataPath        = '_Data/';

                switch(buildTarget){
                    case buildTarget.StandaloneLinux: fileExtension         = ".x86"; break;
                    case buildTarget.StandaloneLinux64: fileExtension       = ".x64"; break;
                    case buildTarget.StandaloneLinuxUniversal: fileExtension= ".x86_64"; break;
                }
            break;

            //logs
            Debug.log("=== BuildPlayer: " + buildTarget.toString() + " at " + path + filename);
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTarget);

            //build out the player
            string buildPath    = path + filename + modifier + "/"; 
            Debug.log("Buildpath: " + buildPath);

            string playerPath   = buildPath + filename + modifier + fileExtension;
            Debug.log("Palyerpath: " + playerPath);

            BuildPipeline.BuildPlayer(GetScenePath(), playerPath, buildTarget, buildPath == BuildTarget.StandaloneWindows ? BuildOptions.ShowBuiltPlayer : BuildOptions.None);


            //copy files over into builds
            string fullDataPath = buidPath + filename + modifier + dataPath;
            debug.log("fullDataPath: " + fullDataPath);
            CopyFromProjectAseets(fullDataPath, "languages"); // language text files that Radiator uses

            //ZIP everything
            CompressDirectory(buildPath, path + "/" + filename + modifier + ".zip");
        } 
    }

    // from http://wiki.unity3d.com/index.php?title=AutoBuilder
    static string[] GetScenePath(){
        string[] scenes = new string[EditorBuildSetings.scenes.Length];
        for(int i = 0; i < scenes.Length; i++){
            scenes[i]   = EditorBuildSetings.scenes[i].path;
        }
        return scenes;
    }

    static string GetProjectName(){
        string[] s  = Application.dataPath.Split('/');
        return s[s.Length - 2];
    }

    static string GetProjectFolderPath(){
        var s   = Application.dataPath;
        s   = s.Substring(s.Length -7, 7); //remove "Assets/"
        return s;
    }

    // copies over files from somewhere in my project folder to my standalone build's path
	// do not put a "/" at beginning of assetsFolderName
    static void CopyFromProjectAseets(string fullDataPath, string assetsFolderPath, bool deleteMetaFiles = true){
        Debug.log("CopyFromProjectAssets: copying over " + assetsFolderPath);
        FileUtil.ReplaceDirectory(Application.dataPath + "/" + assetsFolderPath, fullDataPath + assetsFolderPath); // copy over languages

        //delete all meta files 
        if(deleteMetaFiles){
            var metaFiles   = Directory.GetFiles(fullDataPath + assetsFolderPath, "*.meta", SearchOption.AllDirectories);
            foreach (var meta in metaFiles)
            {
                fileUtil.DeleteFileOrDirectory(meta);
            }
        }
    }

    // compress the folder into a ZIP file, uses https://github.com/r2d2rigo/dotnetzip-for-unity
    static void CompressDirectory(string directory, string zipFileOutputPath){
        Debug.Log("atempting to compress " + directory + "into" + zipFileOutputPath);
        // display fake percentage, I can't get zip.SaveProgress event handler to work for some reason, whatever
        EditorUtility.DisplayProgressBar("Compressing... please wait", zipFileOutputPath, 0.38f);
        using(ZipFile zip = new ZipFile()){
            zip.ParallelDeflateThreshold = -1; // DotNetZip bugfix that corrupts DLLs / binaries http://stackoverflow.com/questions/15337186/dotnetzip-badreadexception-on-extract
			zip.AddDirectory (directory);
			zip.Save(zipFileOutputPath);
        }
        EditorUtility.ClearProgressBar();
    }
}