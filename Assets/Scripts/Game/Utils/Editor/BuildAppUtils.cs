using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        BuildAssetBundleUtils.NormalBuildAssetBundle();
        
        SaveVersion(PlayerSettings.bundleVersion, PlayerSettings.applicationIdentifier);
        
        //复制AssetBundle
        CopyAssetBundle(BuildAssetBundleUtils.AssetBundleBuildPath);
        //生成可执行程序
        Debug.Log(GetBuildPath());
        BuildPipeline.BuildPlayer(GetBuildScene(),GetBuildPath(),EditorUserBuildSettings.activeBuildTarget,BuildOptions.None);
        
        //移除AssetBundle
        DeleteFilesInDir(Application.streamingAssetsPath);
    }

    private static void SaveVersion(string version,string package)
    {
        string content = "Version|" + version + ";PackageName|" + package + ";";
        string savePath = Application.dataPath + "/Resources/Version.txt";
        string all;
        string firstLine;
        
        using (FileStream fs = new FileStream(savePath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.ReadWrite))
        {
            using (StreamReader sr = new StreamReader(fs,System.Text.Encoding.UTF8))
            {
                all = sr.ReadToEnd();
                firstLine = all.Split("\r")[0];
            }
        }
        using (FileStream fs = new FileStream(savePath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.ReadWrite))
        {
            using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
            {
                if (string.IsNullOrEmpty(all))
                    all = content;
                else
                    all = all.Replace(firstLine, content);
                sw.Write(all);
            }
        }
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
    
    public static void DeleteFilesInDir(string path)
    {
        try
        {
            DirectoryInfo dir = new DirectoryInfo(path);
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

    #region PC平台构建

    [MenuItem("GameUtils/BuildPCApp")]
    public static void PCBuild()
    {
        DeleteFilesInDir(MacosPath);
        
        PCSetting pcSetting = GetPCSetting();
        
        string suffix= SetPCSetting(pcSetting);
        
        // 构建AssetBundle
        BuildAssetBundleUtils.NormalBuildAssetBundle();
        
        //复制AssetBundle
        CopyAssetBundle(BuildAssetBundleUtils.AssetBundleBuildPath);
        
        CheckAndCreateDirectory(MacosPath);
        string name = String.Format($"{appName}{suffix}_{DateTime.Now:yyyy_MM_dd_HH_mm}");
        string buildPath = String.Format($"{MacosPath}{name}.app");
    
        BuildPipeline.BuildPlayer(GetBuildScene(),buildPath,EditorUserBuildSettings.activeBuildTarget,BuildOptions.None);
        WriteBuildName(name);
        //移除AssetBundle
        DeleteFilesInDir(Application.streamingAssetsPath);
    }

    private static void WriteBuildName(string name)
    {
        FileInfo fileInfo = new FileInfo(Application.dataPath + "/../BuildName.txt");
        using StreamWriter sw = fileInfo.CreateText();
        sw.WriteLine(name);
    }

    private static PCSetting GetPCSetting()
    {
        PCSetting pcSetting = new PCSetting();
        string[] settings = Environment.GetCommandLineArgs();
        int count = settings.Length;
        string str;
        for (int i = 0; i < count; i++)
        {
            str = settings[i];
            if (str.StartsWith("Version"))
            {
                var temp= str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (temp.Length == 2)
                {
                    pcSetting.Version = temp[1].Trim();
                }
            }
            else if (str.StartsWith("Name"))
            {
                var temp= str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (temp.Length == 2)
                {
                    pcSetting.Name = temp[1].Trim();
                }
            }
            else if(str.StartsWith("Debug"))
            {
                var temp= str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (temp.Length == 2)
                {
                    Boolean.TryParse(temp[1],out pcSetting.IsDebug);
                }
            }
        }
        return pcSetting;
    }

    private static string SetPCSetting(PCSetting pcSetting)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("_");
        if (!string.IsNullOrEmpty(pcSetting.Version))
        {
            PlayerSettings.bundleVersion = pcSetting.Version;
            sb.Append(pcSetting.Version);
        }
        if (!string.IsNullOrEmpty(pcSetting.Name))
        {
            PlayerSettings.productName = pcSetting.Name;
        }
        if (!pcSetting.IsDebug)
        {
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.connectProfiler = true;
            sb.Append("_").Append(pcSetting.IsDebug);
        }

        return sb.ToString();
    }

    private class PCSetting
    {
        public string Version = "";
        public string Name = "";
        public bool IsDebug = false;
    }

    #endregion

    #region Android平台构建

    [MenuItem("GameUtils/BuildAndroidApp")]
    public static void AndroidBuild()
    {
        DeleteFilesInDir(AndroidPath);
        
        //接收并设置参数
        string suffix;
        AndroidSetting androidSetting = GetAndroidSetting();
        suffix = SetAndroidSetting(androidSetting);
        
        //设置Android秘钥
        
        PlayerSettings.Android.keystoreName = "user.keystore";
        PlayerSettings.Android.keystorePass = "123456";
        PlayerSettings.Android.keyaliasName = "chenyu";
        PlayerSettings.Android.keyaliasPass = "123456";
        
        
        // 构建AssetBundle
        BuildAssetBundleUtils.NormalBuildAssetBundle();
        
        //复制AssetBundle
        CopyAssetBundle(BuildAssetBundleUtils.AssetBundleBuildPath);
        
        CheckAndCreateDirectory(AndroidPath);
        string name = String.Format($"{appName}{suffix}_{DateTime.Now:yyyy_MM_dd_HH_mm}");
        string buildPath = String.Format($"{AndroidPath}{name}.apk");
    
        BuildPipeline.BuildPlayer(GetBuildScene(),buildPath,EditorUserBuildSettings.activeBuildTarget,BuildOptions.None);
        WriteBuildName(name);
        //移除AssetBundle
        DeleteFilesInDir(Application.streamingAssetsPath);
    }

    private static AndroidSetting GetAndroidSetting()
    {
        AndroidSetting androidSetting  = new AndroidSetting();
        string[] settings = Environment.GetCommandLineArgs();
        int count = settings.Length;
        string str;
        for (int i = 0; i < count; i++)
        {
            str = settings[i];
            if (str.StartsWith("Platform"))
            {
                var temp= str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (temp.Length == 2)
                {
                    androidSetting.platform = Enum.Parse<AndroidSetting.Platform>(temp[1], true);
                }
            }
            if (str.StartsWith("Version"))
            {
                var temp= str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (temp.Length == 2)
                {
                    androidSetting.Version = temp[1].Trim();
                }
            }
            else if (str.StartsWith("Name"))
            {
                var temp= str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (temp.Length == 2)
                {
                    androidSetting.Name = temp[1].Trim();
                }
            }
            else if (str.StartsWith("MutilRendering"))
            {
                var temp= str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (temp.Length == 2)
                {
                    Boolean.TryParse(temp[1],out androidSetting.MutilRendering);
                }
            }
            else if(str.StartsWith("Debug"))
            {
                var temp= str.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (temp.Length == 2)
                {
                    Boolean.TryParse(temp[1],out androidSetting.IsDebug);
                }
            }
        }
        return androidSetting;
    }

    private static string SetAndroidSetting(AndroidSetting androidSetting)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("_");
        if (androidSetting.platform != AndroidSetting.Platform.Default)
        {
            string symbol = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android,symbol+";"+androidSetting.platform.ToString());
            sb.Append(androidSetting.platform.ToString());
        }
        if (!string.IsNullOrEmpty(androidSetting.Version))
        {
            PlayerSettings.bundleVersion = androidSetting.Version;
            sb.Append("_").Append(androidSetting.Version);
        }
        if (!string.IsNullOrEmpty(androidSetting.Name))
        {
            PlayerSettings.productName = androidSetting.Name;
        }
        if (!androidSetting.MutilRendering)
        {
            PlayerSettings.MTRendering = androidSetting.MutilRendering;
            sb.Append("_MT:").Append(androidSetting.MutilRendering);
        }
        if (!androidSetting.IsDebug)
        {
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.connectProfiler = true;
            sb.Append("_DB:").Append(androidSetting.IsDebug);
        }

        return sb.ToString();
    }
    
    private class AndroidSetting
    {
        public enum Platform
        {
            Default,
            Xiaomi,
            Huawei,
            Oppo,
            Vivo
        }

        public Platform platform;
        public string Version = "";
        public string Name = "";
        public bool MutilRendering = true;
        public bool IsDebug = false;
    }

    #endregion
    
}