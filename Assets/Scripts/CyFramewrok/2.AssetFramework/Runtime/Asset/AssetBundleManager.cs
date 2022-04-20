using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class AssetBundleManager : Singleton<AssetBundleManager>
{
    
    //已经加载的AssetBundle字典
    private Dictionary<uint, AssetBundleItem> assetBundleItemsDic = new Dictionary<uint, AssetBundleItem>();
    
    //AssetBundleItem对象池
    private ClassObjectPool<AssetBundleItem> assetBundleItemPool =
        ObjectPoolManager.Instance.GetOrCreateClassObjectPool<AssetBundleItem>(500);
    
    /// <summary>
    /// 加载配置文件
    /// </summary>
    internal AssetBundleConfig LoadAssetBundleConfig(string AssetBundleConfigPath)
    {
        AssetBundle configAssetBundle = AssetBundle.LoadFromFile(AssetBundleConfigPath);
        TextAsset configAsset = configAssetBundle.LoadAsset<TextAsset>("AssetBundleConfig");
        if (configAsset == null)
        {
            Debug.LogError("AssetBundleConfig is not exist!");
            return null;
        }
        MemoryStream memoryStream = new MemoryStream(configAsset.bytes);
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        AssetBundleConfig config = binaryFormatter.Deserialize(memoryStream) as AssetBundleConfig;
        memoryStream.Close();
        return config;
        
    }

    /// <summary>
    /// 加载AssetItem资源的AssetBundle
    /// </summary>
    /// <param name="assetItem">AssetItem</param>
    internal void LoadAssetBundle(AssetItem assetItem)
    {
        assetItem.AssetBundle = LoadAssetBundle(assetItem.AssetBundleName);
        for (int i = 0; i < assetItem.DependentAssetBundle.Count; i++)
        {
            LoadAssetBundle(assetItem.DependentAssetBundle[i]);
        }
        
    }

    /// <summary>
    /// 根据名称加载对应的AssetBundle
    /// </summary>
    /// <param name="name">AssetBundle的名称</param>
    /// <returns>加载后的AssetBundle</returns>
    private AssetBundle LoadAssetBundle(string name)
    {
        uint crc = GUIDUtils.GetCRC32(name);
        if (!assetBundleItemsDic.TryGetValue(crc, out AssetBundleItem item))
        {
            string path = Application.streamingAssetsPath + "/" + name;
            AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle == null)
            {
                Debug.LogError($"LoadResourceItem Error:未在{path}处找到名称为{name}的AssetBundle");
                return null;
            }

            item = assetBundleItemPool.Spawn(true);
            item.assetBundle = assetBundle;
            item.ReferenceCount++;
            assetBundleItemsDic.Add(crc,item);

        }
        else
        {
            item.ReferenceCount++;
        }

        return item.assetBundle;
    }
    
    /// <summary>
    /// 卸载AssetItem资源中的AssetBundle
    /// </summary>
    /// <param name="item">AssetItem</param>
    internal void UnloadAssetBundle(AssetItem item)
    {
        if(item == null)
            return;
        if (item.DependentAssetBundle != null && item.DependentAssetBundle.Count > 0)
        {
            for (int i = 0; i < item.DependentAssetBundle.Count; i++)
            {
                UnloadAssetBundle(item.DependentAssetBundle[i]);
            }
        }
        UnloadAssetBundle(item.AssetBundleName);
    }

    /// <summary>
    /// 根据名称卸载对应的AssetBundle
    /// </summary>
    /// <param name="name">AssetBundle的名称</param>
    private void UnloadAssetBundle(string name)
    {
        uint crc = GUIDUtils.GetCRC32(name);
        if (assetBundleItemsDic.TryGetValue(crc, out AssetBundleItem item) && item != null)
        {
            item.ReferenceCount--;
            if (item.ReferenceCount <= 0 && item.assetBundle != null)
            {
                item.assetBundle.Unload(true);
                assetBundleItemPool.Recycle(item);
                assetBundleItemsDic.Remove(crc);
            }
        }
    }
}
