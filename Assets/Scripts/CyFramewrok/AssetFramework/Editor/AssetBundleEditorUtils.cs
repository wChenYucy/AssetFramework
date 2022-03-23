using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

public class AssetBundleEditorUtils
{
    //AssetBundle构建目录
    public static string AssetBundleBuildPath =
        Application.dataPath + "/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget;
    //xml配置文件输出路径
    private static readonly string XMLOUTPUTPATH = Application.dataPath + "/AssetBundleConfig.xml";
    //二进制配置文件输出路径
    private static readonly string BINARYOUTPUTPATH = Application.dataPath + "/AssetBundleConfig.bytes";
    
    // 文件夹资源字典
    private static Dictionary<string, string> dirPathDic = new();
    // 预制体资源字典
    private static  Dictionary<string,List<string>>prefabPathDic=new();
    // 已经标记为被打包的资源列表（用来过滤Asset，构建AssetBundle之间的依赖关系）
    private static List<string>bundledAssetPaths = new();
    // 需要写入配置文件的资源列表（用来过滤不会动态加载的资源，防止配置表过大并且存在很多不需要的信息）
    private static List<string> validConfigPath = new();

    /// <summary>
    /// AssetBundle构建函数
    /// </summary>
    [MenuItem("CyFramework/BuildAssetBundle")]
    public static void BuildAssetBundle()
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
        CreateConfig();

        //构建AB包
        BuildPipeline.BuildAssetBundles(AssetBundleBuildPath, BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);

        //清除AssetBundle名称
        ClearAssetBundleName();
        
        SetAssetBundleName("assetbundleconfig", "Assets/AssetBundleConfig.bytes");
        BuildPipeline.BuildAssetBundles(AssetBundleBuildPath, BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);
        ClearAssetBundleName();
        if (File.Exists(BINARYOUTPUTPATH))
        {
            File.Delete(BINARYOUTPUTPATH);
            File.Delete(BINARYOUTPUTPATH + ".meta");
        }

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
        string configPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:AssetBundleEditorConfig")[0]);
        AssetBundleEditorConfig assetBundleEditorConfig = AssetDatabase.LoadAssetAtPath<AssetBundleEditorConfig>(configPath);
        foreach (var path in assetBundleEditorConfig.DirPaths)
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

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", assetBundleEditorConfig.PrefabPaths.ToArray());

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
    private static void CreateConfig()
    {
        //资源字典<资源路径，资源AssetBundle名称>
        Dictionary<string,string> resPathDictionary=new Dictionary<string, string>();  
        
        //获取所有被打包的资源路径与AssetBundle名称
        string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < bundleNames.Length; i++)
        {
            string[] allPathInBundleName = AssetDatabase.GetAssetPathsFromAssetBundle(bundleNames[i]);
            for (int j = 0; j < allPathInBundleName.Length; j++)
            {
                //过滤脚本文件以及不会动态加载的文件
                if(allPathInBundleName[j].EndsWith(".cs"))
                    continue;
                resPathDictionary.Add(allPathInBundleName[j],bundleNames[i]);
            }
        }
        
        //删除旧版本AssetBundle
        DeleteOldAssetBundle();
        
        //生成配置文件
        BuildConfig(resPathDictionary);
    }
    
    /// <summary>
    /// 判断资源是否为动态加载的资源
    /// </summary>
    /// <param name="path">资源的路径</param>
    /// <returns>判断结果</returns>
    private static bool ValidPath(string path)
    {
        for (int i = 0; i < validConfigPath.Count; i++)
        {
            if (path.Contains(validConfigPath[i]))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 删除当前批次未生成的AssetBundle
    /// </summary>
    private static void DeleteOldAssetBundle()
    {
        string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo directoryInfo=new DirectoryInfo(AssetBundleBuildPath);
        FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (IsNameInList(files[i].Name, allBundlesName) || files[i].Name.EndsWith(".meta") ||
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
    }
    
    /// <summary>
    /// 判断一个AssetBundle名称是否在一个名称列表中
    /// </summary>
    /// <param name="name">AssetBundle名称</param>
    /// <param name="names">AssetBundle名称列表</param>
    /// <returns></returns>
    private static bool IsNameInList(string name, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            if (name == names[i])
                return true;
        }

        return false;
    }
    
    /// <summary>
    /// 根据资源字典构建配置表
    /// </summary>
    /// <param name="resPathDictionary">资源字典(<资源路径，AssetBundle名称>)</param>
    private static void BuildConfig(Dictionary<string ,string> resPathDictionary)
    {
        AssetBundleConfig config = new AssetBundleConfig();
        config.ItemList=new List<ItemConfig>();
        foreach (string path in resPathDictionary.Keys)
        {
            if(!ValidPath(path))
                continue;
            ItemConfig itemConfig=new ItemConfig();
            itemConfig.AssetPath = path;
            itemConfig.AssetBundleName = resPathDictionary[path];
            itemConfig.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            itemConfig.AssetCrc = Crc32.GetCRC32(itemConfig.AssetName+itemConfig.AssetBundleName);
            itemConfig.DependAssetBundle=new List<string>();
            string[] dependPath = AssetDatabase.GetDependencies(path);
            for (int i = 0; i < dependPath.Length; i++)
            {
                string tempPath = dependPath[i];
                if(tempPath==path || tempPath.EndsWith(".cs"))
                    continue;
                
                if (resPathDictionary.TryGetValue(tempPath, out string assetBundleName))
                {
                    if(assetBundleName == resPathDictionary[path])
                        continue;
                    
                    if(!itemConfig.DependAssetBundle.Contains(assetBundleName))
                        itemConfig.DependAssetBundle.Add(assetBundleName);
                }
            }
            config.ItemList.Add(itemConfig);
        }
        
        //写入xml
        if(File.Exists(XMLOUTPUTPATH))
            File.Delete(XMLOUTPUTPATH);
        using (FileStream fileStream=new FileStream(XMLOUTPUTPATH,FileMode.Create,FileAccess.ReadWrite,FileShare.ReadWrite))
        {
            using (StreamWriter streamWriter=new StreamWriter(fileStream,System.Text.Encoding.UTF8))
            {
                XmlSerializer xs=new XmlSerializer(config.GetType());
                xs.Serialize(streamWriter,config);
            }
        }
        
        //写入二进制
        // foreach (ItemConfig abBase in config.ItemList )
        // {
        //     abBase.AssetPath = "";
        // }
        if(File.Exists(BINARYOUTPUTPATH))
            File.Delete(BINARYOUTPUTPATH);
        using (FileStream fileStream=new FileStream(BINARYOUTPUTPATH,FileMode.Create,FileAccess.ReadWrite,FileShare.ReadWrite))
        {
            BinaryFormatter binaryFormatter=new BinaryFormatter();
            binaryFormatter.Serialize(fileStream,config);
        }
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
}
