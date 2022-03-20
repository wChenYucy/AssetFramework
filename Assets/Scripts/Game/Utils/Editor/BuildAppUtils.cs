using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildAppUtils
{
    private static string appName = PlayerSettings.productName;//RealConfig.GetRealFram().m_AppName;
    public static string AndroidPath = Application.dataPath + "/../BuildTarget/Android/";
    public static string IosPath = Application.dataPath + "/../BuildTarget/IOS/";
    public static string WindowsPath = Application.dataPath + "/../BuildTarget/Windows/";
    public static string MacosPath = Application.dataPath + "/../BuildTarget/Macos/";
    public static string DefaultPath = Application.dataPath + "/../BuildTarget/Default/";
    
    [MenuItem("GameUtils/BuildApp")]
    public static void BuildApp()
    {
        // 构建AssetBundle
        AssetBundleEditorUtils.BuildAssetBundle();
        
        //复制AssetBundle
        CopyAssetBundle(AssetBundleEditorUtils.AssetBundleBuildPath);
        //生成可执行程序
        BuildPipeline.BuildPlayer(GetBuildScene(),GetBuildPath(),EditorUserBuildSettings.activeBuildTarget,BuildOptions.None);
        
        //移除AssetBundle
        DeleteAssetBundle();
    }

    private static void CopyAssetBundle(string srcPath)
    {
        try
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }

            if (!Directory.Exists(srcPath))
            {
                Debug.Log("存放AssetBundle的目录：" + srcPath + "不存在！");
                return;
            }

            string[] files = Directory.GetFileSystemEntries(srcPath);
            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    CopyAssetBundle(file);
                }
                else
                {
                    File.Copy(file,
                        Application.streamingAssetsPath + Path.DirectorySeparatorChar + Path.GetFileName(file), true);
                }
            }
        }
        catch
        {
            Debug.LogError("无法复制：" + srcPath + "  到" + Application.streamingAssetsPath);
        }
    }

    /// <summary>
    /// 获得设置中激活的Scene路径
    /// </summary>
    /// <returns>激活的Scene路径数组</returns>
    private static string[] GetBuildScene()
    {
        List<string> scenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if(!scene.enabled) continue;
            scenes.Add(scene.path);
        }

        return scenes.ToArray();
    }

    private static string GetBuildPath()
    {
        string buildPath = "";
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            CheckAndCreateDirectory(AndroidPath);
            buildPath = String.Format($"{AndroidPath}{appName}_{EditorUserBuildSettings.activeBuildTarget}_{DateTime.Now:yyyy_MM_dd_HH_mm}.apk");
        }
        else if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
        {
            CheckAndCreateDirectory(IosPath);
            buildPath = String.Format($"{IosPath}{appName}_{EditorUserBuildSettings.activeBuildTarget}_{DateTime.Now:yyyy_MM_dd_HH_mm}");
        }
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
                 EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
        {
            CheckAndCreateDirectory(WindowsPath);
            buildPath = String.Format($"{WindowsPath}{appName}_{EditorUserBuildSettings.activeBuildTarget}_{DateTime.Now:yyyy_MM_dd_HH_mm}/{appName}.exe");
        }
        else if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX)
        {
            CheckAndCreateDirectory(MacosPath);
            buildPath = String.Format($"{MacosPath}{appName}_{EditorUserBuildSettings.activeBuildTarget}_{DateTime.Now:yyyy_MM_dd_HH_mm}.app");
        }
        else
        {
            CheckAndCreateDirectory(DefaultPath);
            buildPath = String.Format($"{DefaultPath}{appName}_{EditorUserBuildSettings.activeBuildTarget}_{DateTime.Now:yyyy_MM_dd_HH_mm}");
        }

        return buildPath;
    }
    private static void CheckAndCreateDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
    
    private static void DeleteAssetBundle()
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(Application.streamingAssetsPath);
            FileSystemInfo[] fileInfo = dir.GetFileSystemInfos();
            foreach (FileSystemInfo info in fileInfo)
            {
                if (info is DirectoryInfo)
                {
                    DirectoryInfo subdir = new DirectoryInfo(info.FullName);
                    subdir.Delete(true);
                }
                else
                {
                    File.Delete(info.FullName);
                }
            }
        }
        catch(Exception e)
        {
            Debug.LogError(e);
        }
    }
}
