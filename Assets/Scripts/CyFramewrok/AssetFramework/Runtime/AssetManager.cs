using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetManager : Singleton<AssetManager>
{
    
    /// <summary>
    /// 初始化AssetManager
    /// </summary>
    /// <param name="mono"></param>
    public void Init(AssetManagerConfig config)
    {
        if (config == null)
        {
#if UNITY_EDITOR
        Debug.LogError("AssetManagerConfig 为空，请创建配置文件");
#else
        //todo 没有配置文件报错
        Application.Quit();
#endif
        }
        
        AssetItemManager.Instance.InitAssetItemManager(config);
    }

    #region 常规资源

    #region 同步加载资源
    
    /// <summary>
    /// 预加载资源
    /// 目前仅支持同步加载
    /// </summary>
    /// <param name="assetName">资源名称</param>
    /// <param name="assetBundleName">资源所在AssetBundle名称</param>
    /// <typeparam name="T">资源类型</typeparam>
    public void PreloadAsset<T>(string assetName, string assetBundleName) where T : UnityEngine.Object
    {
        UnityEngine.Object obj = LoadAsset<T>(assetName, assetBundleName);
        ReleaseAsset(obj, false);
    }
    
    /// <summary>
    /// 资源同步加载，适用于除了GameObject外的资源
    /// </summary>
    /// <param name="assetName">Asset名称</param>
    /// <param name="assetBundleName">AssetBundle名称</param>
    /// <typeparam name="T">加载资源的类型</typeparam>
    /// <returns>加载后的资源</returns>
    public T LoadAsset<T>(string assetName, string assetBundleName) where T : UnityEngine.Object
    {
        //空值判断
        if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(assetBundleName))
        {
            return null;
        }
        
        //计算Crc
        uint crc = Crc32.GetCRC32(assetName + assetBundleName);

        return AssetItemManager.Instance.LoadAsset<T>(crc);
    }

    #endregion

    #region 异步加载资源

    /// <summary>
    /// 异步加载资源（仅仅是不需要实例化的资源，例如音频，图片等等）
    /// </summary>
    /// <param name="assetName">资源名称</param>
    /// <param name="assetBundleName">资源所在AssetBundle名称</param>
    /// <param name="onLoadFinishCallBack">资源异步加载完成回调函数</param>
    /// <param name="priority">资源加载优先级</param>
    /// <param name="param1">资源加载自定义参数</param>
    public long AsyncLoadAsset<T>(string assetName, string assetBundleName,
        AsyncLoadFinishCallBack onLoadFinishCallBack,
        AsyncLoadPriority priority, object param1 = null) where T : UnityEngine.Object
    {
        //空值判断
        if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(assetBundleName))
        {
            return -1;
        }
        
        uint crc = Crc32.GetCRC32(assetName + assetBundleName);
        AssetItem item = AssetItemManager.Instance.FindOrGetAssetItem(crc);
        if (item.AssetObject != null)
        {
            AssetItemManager.Instance.AddUsedAssetItem(item);
            onLoadFinishCallBack?.Invoke(item.AssetObject, param1);
            return -1;
        }
        return AssetItemManager.Instance.AsyncLoadAsset<T>(assetName,assetBundleName,onLoadFinishCallBack,priority,param1);
    }

    /// <summary>
    /// 取消异步资源加载
    /// </summary>
    /// <param name="guid">异步加载GUID</param>
    /// <returns>取消结果</returns>
    public bool CancelAsyncLoad(long guid)
    {
        return AssetItemManager.Instance.CancelAsyncLoad(guid);
    }
    #endregion

    #region 卸载资源
    
    /// <summary>
    /// 卸载资源，适用于除GameObject外的类型
    /// </summary>
    /// <param name="obj">要卸载的资源</param>
    /// <param name="unloadAsset">是否在引用为0时缓存它</param>
    /// <returns>卸载结果</returns>
    public bool ReleaseAsset(UnityEngine.Object obj, bool unloadAsset = false)
    {
        if (obj == null)
        {
            return false;
        }
        return AssetItemManager.Instance.ReleaseAsset(obj, unloadAsset);
    }
    

    #endregion
    
    #endregion
    
    #region GameObject资源
    
    #region 同步GameObject加载
    
    //GameObjectItem的类对象池
    private ClassObjectPool<GameObjectItem> gameObjectItemPool =
        ObjectPoolManager.Instance.GetOrCreateClassObjectPool<GameObjectItem>(1000);
    
    //GameObjectItem对象池
    private Dictionary<uint, List<GameObject>> gameObjectPool = new();
    //正在使用的GameObjectItem字典
    private Dictionary<int, GameObjectItem> usedGameObjectItemDic = new();
    
    /// <summary>
    /// 预加载GameObject
    /// 目前仅支持同步加载
    /// </summary>
    /// <param name="assetName">GameObject名称</param>
    /// <param name="assetBundleName">GameObject所在AssetBundle名称</param>
    /// <param name="count">GameObject数量</param>
    /// <param name="parent">GameObject父物体</param>
    /// <param name="showGameObjectAction">GameObject显示时的回调方法</param>
    /// <param name="clearSelf">是否清除自身</param>
    public void PreloadGameObject(string assetName, string assetBundleName, int count = 1, Transform parent = null)
    {
        List<GameObject> tempGameObjectList = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            GameObject go = LoadGameObject(assetName, assetBundleName, parent, false);
            tempGameObjectList.Add(go);
        }
        
        for (int i = 0; i < count; i++)
        {
            GameObject obj = tempGameObjectList[i];
            ReleaseAsset(obj);
        }

        tempGameObjectList.Clear();
        
    }
    
    /// <summary>
    /// 资源同步加载，适用于GameObject类型的资源
    /// </summary>
    /// <param name="assetName">Asset名称</param>
    /// <param name="assetBundleName">AssetBundle名称</param>
    /// <param name="parent">父节点</param>
    /// <param name="showGameObjectAction">显示GameObject的回调函数</param>
    /// <param name="clearSelf">是否在跳转场景时清空自己</param>
    /// <returns>GameObject</returns>
    public GameObject LoadGameObject(string assetName, string assetBundleName, Transform parent = null, bool clearSelf = true)
    {
        //空值判断
        if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(assetBundleName))
        {
            return null;
        }
        
        //计算Crc
        uint crc = Crc32.GetCRC32(assetName + assetBundleName);
        GameObjectItem gameObjectItem = gameObjectItemPool.Spawn(true);
        gameObjectItem.AssetItem = AssetItemManager.Instance.FindOrGetAssetItem(crc);
        gameObjectItem.ClearSelf = clearSelf;
        
        //获取GameObject
        GameObject gameObject = GetGameObjectFromPools(crc);
        if (gameObject == null)
        {
            GameObject prefab = AssetItemManager.Instance.LoadAsset<GameObject>(gameObjectItem.AssetItem);
            if (prefab != null)
            {
                gameObject = GameObject.Instantiate(prefab);
            }
            else
            {
                Debug.LogError("resourceItem的AssetObject未能成功加载！");
                return null;
            }
        }

        gameObjectItem.CloneGameObject = gameObject;
        
        if (parent != null)
            gameObject.transform.SetParent(parent, false);
        
        //添加到使用列表
        usedGameObjectItemDic.Add(gameObject.GetInstanceID(),gameObjectItem);
        
        return gameObject;
    }
    
    /// <summary>
    /// 根据Crc从GameObjectItemDic获得换出的GameObject
    /// </summary>
    /// <param name="crc">资源的Crc</param>
    /// <returns>被缓存的GameObject</returns>
    private GameObject GetGameObjectFromPools(uint crc)
    {
        if (gameObjectPool.TryGetValue(crc, out List<GameObject> gameObjects) &&
            gameObjects.Count > 0)
        {
            GameObject gameObject = gameObjects[0];
            gameObjects.RemoveAt(0);
#if UNITY_EDITOR
            if (!ReferenceEquals(gameObject, null) && gameObject.name.EndsWith("(Recycle)"))
                gameObject.name = gameObject.name.Replace("(Recycle)", "");
#endif
            return gameObject;
        }

        return null;
    }

    #endregion
    
    #region 异步GameObject加载

    /// <summary>
    /// 异步GameObject加载
    /// </summary>
    /// <param name="assetName">GameObject名称</param>
    /// <param name="assetBundleName">GameObject所在的AssetBundle名称</param>
    /// <param name="onLoadFinishCallBack">加载结束回调</param>
    /// <param name="priority">GameObject加载优先级</param>
    /// <param name="parent">GameObject父物体</param>
    /// <param name="showGameObjectAction">GameObject显示回调</param>
    /// <param name="clearSelf">是否清除自身</param>
    /// <param name="param1">回调函数参数</param>
    public long AsyncLoadGameObject(string assetName, string assetBundleName, AsyncLoadFinishCallBack onLoadFinishCallBack,
        AsyncLoadPriority priority, Transform parent = null, bool clearSelf = true, object param1 = null)
    {
        //空值判断
        if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(assetBundleName))
        {
            return -1;
        }
        uint crc = Crc32.GetCRC32(assetName + assetBundleName);
        GameObject gameObject = GetGameObjectFromPools(crc);
        if (gameObject != null)
        {
            Debug.Log("异步缓存");
            if (parent != null)
            {
                gameObject.transform.SetParent(parent);
            }
            GameObjectItem gameObjectItem = gameObjectItemPool.Spawn(true);
            gameObjectItem.AssetItem = AssetItemManager.Instance.FindOrGetAssetItem(crc);
            gameObjectItem.CloneGameObject = gameObject;
            gameObjectItem.ClearSelf = clearSelf;
            usedGameObjectItemDic.Add(gameObject.GetInstanceID(), gameObjectItem);
            onLoadFinishCallBack?.Invoke(gameObject,param1);
            return -1;
        }

        return AssetItemManager.Instance.AsyncLoadAsset<GameObject>(assetName, assetBundleName,
            onLoadFinishCallBack, priority, param1, true, parent, clearSelf);
    }

    /// <summary>
    /// 向usedGameObjectItemDic中添加GameObjectItem
    /// </summary>
    /// <param name="item">GameObjectItem中的AssetItem</param>
    /// <param name="gameObject">GameObjectItem中的CloneGameObject</param>
    /// <param name="clearSelf">GameObjectItem中的ClearSelf</param>
    public void AddUsedGameObjectItem(AssetItem item,GameObject gameObject,bool clearSelf)
    {
        GameObjectItem gameObjectItem = gameObjectItemPool.Spawn(true);
        gameObjectItem.AssetItem = item;
        gameObjectItem.CloneGameObject = gameObject;
        gameObjectItem.ClearSelf = clearSelf;
        usedGameObjectItemDic.Add(gameObject.GetInstanceID(), gameObjectItem);
    }
    
    #endregion
    
    #region 卸载GameObject
    
    /// <summary>
    /// 卸载资源，适用于GameObject类型
    /// </summary>
    /// <param name="gameObject">要卸载的GameObject</param>
    /// <param name="destroyGameObject">是否删除GameObject</param>
    /// <param name="unloadAsset">是否在AssetItem引用为0时缓存它</param>
    /// <returns>卸载结果</returns>
    public bool ReleaseAsset(GameObject gameObject, bool destroyGameObject = false,  bool unloadAsset = false)
    {
        if (gameObject == null)
            return false;
        int tempID = gameObject.GetInstanceID();
        if (!usedGameObjectItemDic.TryGetValue(tempID, out GameObjectItem gameObjectItem) ||
            gameObjectItem == null)
        {
            Debug.LogError("要释放的GameObject："+gameObject.name+"不是由ResourceManage加载的，无法释放");
            return false;
        }
#if UNITY_EDITOR
        gameObject.name += "(Recycle)";
#endif
        if (destroyGameObject == false)
        {
            if (!gameObjectPool.TryGetValue(gameObjectItem.AssetItem.Crc, out List<GameObject> st) || st == null)
            {
                st = new List<GameObject>();
                gameObjectPool.Add(gameObjectItem.AssetItem.Crc,st);
            }

            //缓存GameObject
            st.Add(gameObject);
            
        }
        else
        {
            Debug.Log("Destroy GameObject");
            GameObject.DestroyImmediate(gameObject);
            AssetItemManager.Instance.ReleaseAsset(gameObjectItem.AssetItem.Crc, unloadAsset);
        }
        
        usedGameObjectItemDic.Remove(tempID);
        gameObjectItemPool.Recycle(gameObjectItem);
        return true;
    }

    /// <summary>
    /// GameObject类型是否正在被使用
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool IsGameObjectUsed(GameObject obj)
    {
        if (usedGameObjectItemDic.ContainsKey(obj.GetInstanceID()))
            return true;
        return false;
    }

    public void ClearHalfOfGameObjectPool(GameObject go)
    {
        //todo 根据GameObject查找Crc
        if(go == null)
            return;
        //ClearHalfOfGameObjectPool(go.GetInstanceID());
    }

    private List<GameObject> clearGameObjectList;
    
    public void ClearHalfOfGameObjectPool(uint crc)
    {
        if (!gameObjectPool.TryGetValue(crc, out clearGameObjectList))
        {
            Debug.LogError("不存在存储"+crc+"GameObject的对象池！清除失败");
            return;
        }

        int count = clearGameObjectList.Count / 2;
        AssetItemManager.Instance.ReleaseAsset(crc, false, count);

        GameObject item = null;
        for (int i = 0; i < count; i++)
        {
            item = clearGameObjectList[0];
            clearGameObjectList.Remove(item);
            Object.Destroy(item);
        }

        clearGameObjectList = null;
    }
    public void RemoveGameObjectPool(uint crc , bool unloadAsset = false)
    {
        if (!gameObjectPool.TryGetValue(crc, out clearGameObjectList))
        {
            Debug.LogError("不存在存储"+crc+"GameObject的对象池！清除失败");
            return;
        }
        int count = clearGameObjectList.Count;
        AssetItemManager.Instance.ReleaseAsset(crc, unloadAsset, count);
        GameObject item = null;
        for (int i = 0; i < count; i++)
        {
            item = clearGameObjectList[0];
            clearGameObjectList.Remove(item);
            Object.Destroy(item);
        }

        if (gameObjectPool.ContainsKey(crc))
        {
            gameObjectPool.Remove(crc);
        }

        clearGameObjectList = null;
    }

    /// <summary>
    /// GameObject资源的强制卸载方法
    /// </summary>
    public void ClearUnusedGameObjectItem()
    {
        List<GameObjectItem> tempList = new List<GameObjectItem>();
        foreach (GameObjectItem item in usedGameObjectItemDic.Values)
        {
            if (item.ClearSelf)
            {
                tempList.Add(item);
            }
        }

        foreach (GameObjectItem item in tempList)
        {
            ReleaseAsset(item.CloneGameObject);
        }
        tempList.Clear();
    }

    #endregion
    
    #endregion
    
}
