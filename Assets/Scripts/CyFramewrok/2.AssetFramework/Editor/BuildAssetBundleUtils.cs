using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

public class BuildAssetBundleUtils
{
    #region 配置路径，考虑是否加入配置文件

    //AssetBundle构建目录
    public static readonly string AssetBundleBuildPath =
        Application.dataPath + "/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget;
    //xml配置文件输出路径
    private static readonly string XMLOUTPUTPATH = Application.dataPath + "/AssetBundleConfig.xml";
    //二进制配置文件输出路径
    private static readonly string BINARYOUTPUTPATH = Application.dataPath + "/AssetBundleConfig.bytes";

    private static readonly string AssetBundleInfoConfigPath =
        Application.dataPath + "/../Version/" + EditorUserBuildSettings.activeBuildTarget;
    private static readonly string HotPatchAssetBundlePath =
        Application.dataPath + "/../Hotfix/" + EditorUserBuildSettings.activeBuildTarget;

    #endregion
    
    // 文件夹资源字典
    private static Dictionary<string, string> dirPathDic = new();
    // 预制体资源字典
    private static  Dictionary<string,List<string>>prefabPathDic=new();
    
    // 已经标记为被打包的资源列表（用来过滤Asset，构建AssetBundle之间的依赖关系）
    private static List<string>bundledAssetPaths = new();
    
    // 需要写入配置文件的资源列表（用来过滤不会动态加载的资源，防止配置表过大并且存在很多不需要的信息）
    private static List<string> validConfigPath = new();
    
    //记录本次打包的AssetBundle的信息
    private static Dictionary<string, AssetBundleInfoBase> assetBundleInfoBases = new();

    /// <summary>
    /// AssetBundle构建函数
    /// </summary>
    [MenuItem("CyFramework/BuildAssetBundle")]
    public static void NormalBuildAssetBundle()
    {
        BuildAssetBundle();
    }

    public static void BuildAssetBundle(bool hotfix = false, string assetBundleMD5Config = "",
        string hotFixCount = "1")
    {
        //清空列表
        ClearAllCollection();

        //清除AssetBundle名称
        ClearAssetBundleName();
        
        //加载文件
        LoadFilesFromPath();

        //设置AssetBundle名称名称
        SetAssetBundleNameToFiles();

        if (!Directory.Exists(AssetBundleBuildPath))
            Directory.CreateDirectory(AssetBundleBuildPath);
        
        //生成配置表
        CreateAssetBundleConfig();

        //构建AB包
        BuildPipeline.BuildAssetBundles(AssetBundleBuildPath, BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);

        //清除AssetBundle名称
        ClearAssetBundleName();
        
        if (hotfix)
        {
            HandleHotPatchAssetBundle(assetBundleMD5Config, hotFixCount);
        }
        else
        {
            WriteAssetBundleInfoConfig();
        }

        #region 生成AssetBundle配置表代码

        SetAssetBundleName("assetbundleconfig", "Assets/AssetBundleConfig.bytes");
        BuildPipeline.BuildAssetBundles(AssetBundleBuildPath, BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);
        ClearAssetBundleName();
        
        if (File.Exists(BINARYOUTPUTPATH))
        {
            File.Delete(BINARYOUTPUTPATH);
            File.Delete(BINARYOUTPUTPATH + ".meta");
        }

        #endregion
        

        //刷新数据库
        AssetDatabase.Refresh();

        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 清空所有的集合中原始数据
    /// </summary>
    private static void ClearAllCollection()
    {
        dirPathDic.Clear();
        bundledAssetPaths.Clear();
        prefabPathDic.Clear();
        validConfigPath.Clear();
        ClearAssetBundleName();
    }
    
    /// <summary>
    /// 根据编辑器配置文件构建两个资源字典
    /// </summary>
    /// <exception cref="Exception">AssetBundle名称重复异常与资源名称重复异常</exception>
    private static void LoadFilesFromPath()
    {
        string configPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:AssetBundleStrategyTable")[0]);
        AssetBundleStrategyTable assetBundleStrategyTable = AssetDatabase.LoadAssetAtPath<AssetBundleStrategyTable>(configPath);
        foreach (var path in assetBundleStrategyTable.DirPaths)
        {
            if (dirPathDic.ContainsKey(path.AssetBundleName))
            {
                throw new Exception("AB包配置名字重复，请检查！");
            }
            else
            {
                dirPathDic.Add(path.AssetBundleName,path.Path);
                bundledAssetPaths.Add(path.Path);
                validConfigPath.Add(path.Path);
            }
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", assetBundleStrategyTable.PrefabPaths.ToArray());

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            EditorUtility.DisplayProgressBar("查找Prefab","Prefab:"+path,i*1.0f/prefabGuids.Length);
            if (!IsBundledPath(path))
            {
                validConfigPath.Add(path);
                string[] dependPaths = AssetDatabase.GetDependencies(path);
                
                List<string> validDependPaths = new List<string>();
                for (int j = 0; j < dependPaths.Length; j++)
                {
                    if (!dependPaths[j].EndsWith(".cs") && !IsBundledPath(dependPaths[j]) && !dependPaths[j].EndsWith(".FBX"))
                    {
                        bundledAssetPaths.Add(dependPaths[j]);
                        validDependPaths.Add(dependPaths[j]);
                    }
                }

                string name = path.Split('/')[^1].Split('.')[0];
                if (prefabPathDic.ContainsKey(name))
                {
                    throw new Exception("存在相同名字的Prefab！名字："+name);
                }
                else
                {
                    prefabPathDic.Add(name,validDependPaths);
                }
            }
        }
    }
    
    /// <summary>
    /// 判断一个资源是否被设置AssetBundle名称
    /// 上层利用该函数查找没有被设置AssetBundle名称但却被要打AssetBundle的资源依赖的资源。
    /// </summary>
    /// <param name="path">要被判断的资源路径</param>
    /// <returns>判断结果</returns>
    private static bool IsBundledPath(string path)
    {
        for (int i = 0; i < bundledAssetPaths.Count; i++)
        {
            if (path == bundledAssetPaths[i] || (path.Contains(bundledAssetPaths[i]) && path.Replace(bundledAssetPaths[i],"")[0]=='/'))
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// 为所有待打包的资源设置AssetBundle名称
    /// </summary>
    private static void SetAssetBundleNameToFiles()
    {
        foreach (var dirName in dirPathDic.Keys)
        {
            SetAssetBundleName(dirName,dirPathDic[dirName]);
        }
        foreach (var prefabName in prefabPathDic.Keys)
        {
            SetAssetBundleName(prefabName,prefabPathDic[prefabName]);
        }
    }
    
    /// <summary>
    /// 为列表中的每一个资源设置AssetBundle名称
    /// </summary>
    /// <param name="assetBundleName">待设置的AssetBundle名称</param>
    /// <param name="assetPaths">要操作的资源列表</param>
    private static void SetAssetBundleName(string assetBundleName, List<string> assetPaths)
    {
        foreach (var assetPath in assetPaths)
        {
            SetAssetBundleName(assetBundleName,assetPath);
        }
    }
    
    /// <summary>
    /// 为单个资源设置AssetBundle名称
    /// </summary>
    /// <param name="assetBundleName">待设置的AssetBundle名称</param>
    /// <param name="assetPath">要操作的资源路径</param>
    /// <exception cref="Exception">资源未找到异常</exception>
    private static void SetAssetBundleName(string assetBundleName,string assetPath)
    {
        Debug.LogWarning(assetPath);
        AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
        if (assetImporter == null)
        {
            throw new Exception("不存在此路径文件：" + assetPath);
        }
        else
        {
            assetImporter.assetBundleName = assetBundleName;
        }
    }

    /// <summary>
    /// 根据资源信息生成配置文件
    /// </summary>
    private static void CreateAssetBundleConfig()
    {
        //资源字典<资源路径，资源AssetBundle名称>
        Dictionary<string,string> resPathDictionary=new Dictionary<string, string>();  
        
        //获取所有被设置了AssetBundle名称的资源
        string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < allBundlesName.Length; i++)
        {
            string[] allPathInBundleName = AssetDatabase.GetAssetPathsFromAssetBundle(allBundlesName[i]);
            for (int j = 0; j < allPathInBundleName.Length; j++)
            {
                //过滤脚本文件
                if(allPathInBundleName[j].EndsWith(".cs"))
                    continue;
                resPathDictionary.Add(allPathInBundleName[j],allBundlesName[i]);
            }
        }
        
        //删除旧版本AssetBundle
        DirectoryInfo directoryInfo=new DirectoryInfo(AssetBundleBuildPath);
        FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (allBundlesName.IsStrInList(files[i].Name) || files[i].Name.EndsWith(".meta") ||
                files[i].Name.EndsWith(".manifest") ) 
            {
                continue;
            }
            else
            {
                Debug.LogWarning("此AB包"+files[i].Name+"已过时，系统将自动删除");
                if (File.Exists(files[i].FullName))
                {
                    File.Delete(files[i].FullName);
                    File.Delete(files[i].FullName + ".meta");
                }

                if (File.Exists(files[i].FullName + ".manifest"))
                {
                    File.Delete(files[i].FullName+ ".manifest");
                    File.Delete(files[i].FullName + ".manifest.meta");
                }
            }
        }
        
        AssetBundleConfig config = new AssetBundleConfig();
        config.ItemList=new List<ItemConfig>();
        foreach (string path in resPathDictionary.Keys)
        {
            //如果不需要动态加载，则无需写入配置文件
            if(!IsValidPath(path))
                continue;
            ItemConfig itemConfig=new ItemConfig();
            itemConfig.AssetPath = path;
            itemConfig.AssetBundleName = resPathDictionary[path];
            itemConfig.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            itemConfig.AssetCrc = GUIDUtils.GetCRC32(itemConfig.AssetPath);
            itemConfig.DependAssetBundle=new List<string>();
            string[] dependPath = AssetDatabase.GetDependencies(path);
            for (int i = 0; i < dependPath.Length; i++)
            {
                // 排除自身以及脚本文件
                if (dependPath[i] == path || dependPath[i].EndsWith(".cs"))
                    continue;
                //如果被依赖项
                if (resPathDictionary.TryGetValue(dependPath[i], out string assetBundleName))
                {
                    if(!itemConfig.DependAssetBundle.Contains(assetBundleName))
                        itemConfig.DependAssetBundle.Add(assetBundleName);
                }
            }
            config.ItemList.Add(itemConfig);
        }
        
        //写入xml
        SerializeUtils.XmlSerialize(XMLOUTPUTPATH, config);

        //写入二进制
        foreach (ItemConfig abBase in config.ItemList)
        {
            abBase.AssetPath = "";
        }

        SerializeUtils.BinarySerialize(BINARYOUTPUTPATH, config);
        
    }
    
    /// <summary>
    /// 判断资源是否为动态加载的资源
    /// </summary>
    /// <param name="path">资源的路径</param>
    /// <returns>判断结果</returns>
    private static bool IsValidPath(string path)
    {
        for (int i = 0; i < validConfigPath.Count; i++)
        {
            if (path.Contains(validConfigPath[i]))
                return true;
        }

        return false;
    }
    

    /// <summary>
    /// 清除所有资源的AssetBundle名称
    /// </summary>
    private static void ClearAssetBundleName()
    {
        string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
        for (int j = 0; j < assetBundleNames.Length; j++)
        {
            AssetDatabase.RemoveAssetBundleName(assetBundleNames[j], true);
            EditorUtility.DisplayProgressBar("清除AB包名","名字"+assetBundleNames[j],j*1.0f/assetBundleNames.Length);
        }
    }


    private static void WriteAssetBundleInfoConfig()
    {
        DirectoryInfo directory = new DirectoryInfo(AssetBundleBuildPath);
        FileInfo[] files = directory.GetFiles("*", SearchOption.AllDirectories);
        AssetBundleInfo assetBundleInfo = new AssetBundleInfo();
        assetBundleInfo.assetBundleInfoList = new List<AssetBundleInfoBase>();
        for (int i = 0; i < files.Length; i++)
        {
            if(files[i].Name.EndsWith(".cs") || files[i].Name.EndsWith("manifest"))
                continue;
            AssetBundleInfoBase assetBundleInfoBase = new AssetBundleInfoBase();
            assetBundleInfoBase.name = files[i].Name;
            assetBundleInfoBase.md5 = GUIDUtils.BuildFileMd5(files[i].FullName);
            assetBundleInfoBase.size = files[i].Length / 1024.0f;
            assetBundleInfo.assetBundleInfoList.Add(assetBundleInfoBase);
        }

        string path = Application.dataPath + "/Resources/AssetBundleInfoConfig.bytes";
        SerializeUtils.BinarySerialize(path, assetBundleInfo);

        //将打版的版本拷贝到外部进行储存
        if (!Directory.Exists(AssetBundleInfoConfigPath))
        {
            Directory.CreateDirectory(AssetBundleInfoConfigPath);
        }
        string targetPath = AssetBundleInfoConfigPath + "/ABInfoConfig_" + PlayerSettings.bundleVersion + ".bytes";
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }
        File.Copy(path, targetPath);
    }

    private static void HandleHotPatchAssetBundle(string assetBundleInfoConfig, string hotfixCount)
    {
        //查找更新的AssetBundle
        assetBundleInfoBases.Clear();
        AssetBundleInfo assetBundleInfo = SerializeUtils.BinaryDeserialize<AssetBundleInfo>(assetBundleInfoConfig);

        foreach (var assetBundleInfoBase in assetBundleInfo.assetBundleInfoList)
        {
            assetBundleInfoBases.Add(assetBundleInfoBase.name, assetBundleInfoBase);
        }

        List<string> changeList = new List<string>();
        DirectoryInfo directory = new DirectoryInfo(AssetBundleBuildPath);
        FileInfo[] fileInfos = directory.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < fileInfos.Length; i++)
        {
            if(fileInfos[i].Name.EndsWith(".cs") || fileInfos[i].Name.EndsWith(".manifest"))
                continue;
            string name = fileInfos[i].Name;
            string md5 = GUIDUtils.BuildFileMd5(fileInfos[i].FullName);
            if (!assetBundleInfoBases.ContainsKey(name))
            {
                changeList.Add(name);
            }

            if (assetBundleInfoBases.TryGetValue(name, out AssetBundleInfoBase md5Base))
            {
                if (!md5Base.md5.Equals(md5))
                {
                    changeList.Add(name);
                }
            }
        }

        //复制更新后的AssetBundle
        if (!Directory.Exists(HotPatchAssetBundlePath))
        {
            Directory.CreateDirectory(HotPatchAssetBundlePath);
        }

        FileUtils.DeleteFilesInDir(HotPatchAssetBundlePath);

        foreach (var name in changeList)
        {
            if (!name.EndsWith(".manifest"))
            {
                File.Copy(AssetBundleBuildPath + "/" + name, HotPatchAssetBundlePath + "/" + name);
            }
        }

        //生成Patches更新信息文件
        DirectoryInfo directoryInfo = new DirectoryInfo(HotPatchAssetBundlePath);
        FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        Patches patches = new Patches();
        patches.Version = Int32.Parse(hotfixCount);
        patches.Files = new List<Patch>();
        for (int i = 0; i < files.Length; i++) 
        {
            Patch patch = new Patch();
            patch.Name = files[i].Name;
            patch.Md5 = GUIDUtils.BuildFileMd5(files[i].FullName);
            patch.Size = files[i].Length / 1024.0f;
            patch.Platform = EditorUserBuildSettings.activeBuildTarget.ToString();
            patch.Url = "http://127.0.0.1:8080/AssetBundle/" + PlayerSettings.bundleVersion + "/" + hotfixCount + "/" + files[i].Name;
            patches.Files.Add(patch);
        }

        SerializeUtils.XmlSerialize(HotPatchAssetBundlePath + "/patch.xml", patches);
    }
}
