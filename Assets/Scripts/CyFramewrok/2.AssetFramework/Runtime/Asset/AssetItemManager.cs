using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class AssetItemManager : Singleton<AssetItemManager>
{
    public bool LoadFromAssetBundle = true;
    
    //所有AssetItem的字典
    public Dictionary<uint, AssetItem> assetItemsDic = new();
    //正在被使用的AssetItem的字典
    public Dictionary<uint, AssetItem> usedAssetItemDic = new();
    //引用数为0但被缓存的AssetItem链表
    public LinkedList<AssetItem> cachedAssetList = new();
    //当前被缓存的AssetItem的数量
    private int cachedCount = 0;
    //最大缓存AssetItem的数量
    private int maxNoReferenceCount = 500;
    // 执行协程的mono类
    private MonoBehaviour startCoroutineMono;

    /// <summary>
    /// 初始化AssetItemManager，加载配置文件信息并设置异步加载资源协程
    /// </summary>
    /// <param name="mono">异步加载协程执行类</param>
    internal void InitAssetItemManager(AssetManagerConfig managerConfig)
    {
        maxNoReferenceCount = managerConfig.MaxAssetCacheCount;
        
        for (int i = 0; i < (int)AsyncLoadPriority.RES_NUM; i++)
        {
            asyncLoadList[i] = new List<AsyncAssetLoadBlock>();
        }
        startCoroutineMono = managerConfig.startCoroutineMono;
        startCoroutineMono.StartCoroutine(AsyncLoadCoroutine());
#if UNITY_EDITOR
        if(!LoadFromAssetBundle)
            return;
#endif
        AssetBundleConfig config = AssetBundleManager.Instance.LoadAssetBundleConfig(managerConfig.ConfigPath);
        for (int i = 0; i < config.ItemList.Count; i++)
        {
            ItemConfig item = config.ItemList[i];
            AssetItem assetItem = new AssetItem();
            assetItem.Crc = item.AssetCrc;
            assetItem.AssetName = item.AssetName;
            assetItem.AssetBundleName = item.AssetBundleName;
            assetItem.DependentAssetBundle = item.DependAssetBundle;
            if (assetItemsDic.ContainsKey(assetItem.Crc))
            {
                Debug.LogError("重复的Crc，资源名称为：" + assetItem.AssetName + "，位于" + assetItem.AssetBundleName + "包中");
            }
            else
            {
                assetItemsDic.Add(assetItem.Crc,assetItem);
            }
        }
    }
    
    /// <summary>
    /// 根据Crc从usedResourceItemDic、resourceItemsDic和noReferenceList获取ResourceItem
    /// </summary>
    /// <param name="crc">资源的crc</param>
    /// <returns>ResourceItem</returns>
    
    /// <summary>
    /// 从usedAssetItemDic、assetItemsDic中获取AssetItem
    /// </summary>
    /// <param name="crc">资源的Crc</param>
    /// <returns>AssetItem</returns>
    internal AssetItem FindOrGetAssetItem(uint crc)
    {
        //尝试从已经被使用的ResourceItem字典中获得
        if (usedAssetItemDic.TryGetValue(crc, out AssetItem item) || item != null)
        {
            return item;
        }
        
        //从总的ResourceItem字典中获得
        if (!assetItemsDic.TryGetValue(crc, out item) || item == null)
        {
#if UNITY_EDITOR
            if (!LoadFromAssetBundle)
            {
                item = new AssetItem();
                item.Crc = crc;
                assetItemsDic.Add(crc, item);
                return item;
            }
#endif
            Debug.LogError($"LoadResourceItem Error:未找到crc值为{crc}的资源信息");
            return null;
        }

        //如果ResourceItem被缓存在没有被引用的列表中，则移除它
        if (cachedAssetList.Contains(item))
        {
            cachedAssetList.Remove(item);
            cachedCount--;
            return item;
        }
        
        // 加载ResourceItem依赖的AssetBundle
        if (item.AssetBundle == null && !string.IsNullOrEmpty(item.AssetBundleName))
        {
            AssetBundleManager.Instance.LoadAssetBundle(item);
        }
        return item;
    }

    /// <summary>
    /// 根据Crc加载并填充AssetItem
    /// </summary>
    /// <param name="crc">资源的Crc</param>
    /// <typeparam name="T">资源的类型</typeparam>
    /// <returns>加载后的资源</returns>
    internal T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        uint crc = GUIDUtils.GetCRC32(path);
        AssetItem item = FindOrGetAssetItem(crc);
        UnityEngine.Object obj = null;
#if UNITY_EDITOR
        if (!LoadFromAssetBundle)
        {
            if (item != null && item.AssetObject != null)
            {
                obj = item.AssetObject as T;
            }
            else
            {
                if (item == null)
                {
                    item = new AssetItem();
                    item.Crc = crc;
                }
                obj = AssetDatabase.LoadAssetAtPath<T>(path);
                item.AssetObject = obj;
            }
        }
#endif
        if (obj == null)
        {
            //如果当前资源的Object已经被加载，即被缓存在LinkList中，直接返回资源的Object
            if (item.AssetObject != null)
            {
                obj = item.AssetObject as T;
            }
            //如果当前资源的Object未被加载，则从资源所在的AssetBundle中加载资源的Object
            else
            {
                obj = item.AssetBundle.LoadAsset<T>(item.AssetName);
                item.AssetObject = obj;
            }
        }

        //缓存资源
        AddUsedAssetItem(item);
        
        return obj as T;
        
    }

    /// <summary>
    /// 缓存被使用资源的AssetItem
    /// </summary>
    /// <param name="item">要缓存的AssetItem</param>
    /// <param name="refCount">资源的引用次数（异步时可能同时引用多次）</param>
    internal void AddUsedAssetItem(AssetItem item, int refCount = 1)
    {
        //todo 考虑优化（可能性很小）
        if (item == null || item.AssetObject == null)
        {
            Debug.LogError("ResourceItem is null");
            return;
        }
        item.RefCount += refCount;
        if (!usedAssetItemDic.ContainsKey(item.Crc))
        {
            usedAssetItemDic.Add(item.Crc, item);
        }
    }
    
    
    // 正在进行异步加载的列表（二维列表）
    private List<AsyncAssetLoadBlock>[] asyncLoadList = new List<AsyncAssetLoadBlock>[4];
    // 正在进行异步加载数据块的字典
    private Dictionary<uint, AsyncAssetLoadBlock> asyncLoadDic = new();
    // 正在进行异步加载数据块回调的字典（用来取消异步加载）
    private Dictionary<long, AsyncAssetCallBack> asyncAssetCallBacks = new();
    // AsyncAssetLoadBlock对象池
    private ClassObjectPool<AsyncAssetLoadBlock> asyncAssetLoadBlockPool =
        ObjectPoolManager.Instance.GetOrCreateClassObjectPool<AsyncAssetLoadBlock>(500);
    // AsyncAssetCallBack对象池
    private ClassObjectPool<AsyncAssetCallBack> asyncAssetCallBackPool =
        ObjectPoolManager.Instance.GetOrCreateClassObjectPool<AsyncAssetCallBack>(500);
    //AsyncGameObjectCallBack对象池
    private ClassObjectPool<AsyncGameObjectCallBack> asyncGameObjectCallBackPool =
        ObjectPoolManager.Instance.GetOrCreateClassObjectPool<AsyncGameObjectCallBack>(500);
    //最长连续卡着加载资源的时间，单位微妙
    private const long MAXLOADRESTIME = 200000;
    //异步加载初始GUID
    private static long baseGUID = 0;
    
    /// <summary>
    /// 为每次异步加载分配唯一GUID
    /// </summary>
    /// <returns>GUID</returns>
    private static long GetAsyncGUID()
    {
        return baseGUID++;
    }

    /// <summary>
    /// 异步加载Asset
    /// </summary>
    /// <param name="assetName">asset名称</param>
    /// <param name="assetBundleName">asset所在AssetBundle名称</param>
    /// <param name="onLoadFinishCallBack">asset加载完成回调函数</param>
    /// <param name="priority">异步加载优先级</param>
    /// <param name="param1">onLoadFinishCallBack回调用户自定义参数</param>
    /// <param name="isGameObject">是否为GameObject加载</param>
    /// <param name="parent">GameObject父物体</param>
    /// <param name="clearSelf">是否清除GameObject</param>
    /// <typeparam name="T">asset类型</typeparam>
    /// <returns>异步加载GUID</returns>
    internal long AsyncLoadAsset<T>(string path, AsyncLoadFinishCallBack onLoadFinishCallBack,
        AsyncLoadPriority priority, object param1, bool isGameObject = false, Transform parent = null,
        bool clearSelf = true)
    {
        uint crc = GUIDUtils.GetCRC32(path);
        if (!asyncLoadDic.TryGetValue(crc, out AsyncAssetLoadBlock block) || block == null)
        {
            block = asyncAssetLoadBlockPool.Spawn(true);
            block.Crc = crc;
            block.AssetType = typeof(T);
            block.Priority = priority;
            block.IsGameObject = isGameObject;
#if UNITY_EDITOR
            block.Path = path; 
#endif
            asyncLoadDic.Add(crc,block);
            asyncLoadList[(int)priority].Add(block);
        }

        AsyncAssetCallBack asyncAssetCallBack = null;
        //添加回调
        if (!isGameObject)
        {
            asyncAssetCallBack = asyncAssetCallBackPool.Spawn(true);
            block.AsyncCallBackList.Add(asyncAssetCallBack);
        }
        else
        {
            AsyncGameObjectCallBack asyncGameObjectCallBack = asyncGameObjectCallBackPool.Spawn(true);
            asyncGameObjectCallBack.Parent = parent;
            asyncGameObjectCallBack.ClearSelf = clearSelf;
           
            block.AsyncCallBackList.Add(asyncGameObjectCallBack);
            asyncAssetCallBack = asyncGameObjectCallBack;
        }
        
        (AsyncLoadFinishCallBack, object) callBack;
        callBack.Item1 = onLoadFinishCallBack;
        callBack.Item2 = param1;
        asyncAssetCallBack.CallBack = callBack;
        asyncAssetCallBack.GUID = GetAsyncGUID();
        if (!asyncAssetCallBacks.ContainsKey(asyncAssetCallBack.GUID))
        {
            asyncAssetCallBacks.Add(asyncAssetCallBack.GUID, asyncAssetCallBack);
        }
        return asyncAssetCallBack.GUID;
    }
    
    /// <summary>
    /// 取消异步加载
    /// </summary>
    /// <param name="guid">异步加载GUID</param>
    /// <returns>取消结果</returns>
    internal bool CancelAsyncLoad(long guid)
    {
        if (!asyncAssetCallBacks.TryGetValue(guid, out AsyncAssetCallBack asyncCallBack)||asyncCallBack == null)
        {
            return false;
        }

        asyncCallBack.CancelAsyncLoad = true;
        return true;
    }
    
    /// <summary>
    /// 异步加载核心类
    /// </summary>
    /// <returns></returns>
    IEnumerator AsyncLoadCoroutine()
    {
        yield return new WaitForSeconds(3);
        long lastYieldTime = DateTime.Now.Ticks;
        List<AsyncAssetCallBack> callBackList = null;
        while (true)
        {
            AsyncAssetLoadBlock block = null;
            while (true)
            {
                block = GetAsyncLoadParamByPriority();
                if (block != null)
                {
                    break;
                }
                yield return null;
            }

            Type assetType = block.AssetType;
            UnityEngine.Object obj = null;
            AssetItem item = FindOrGetAssetItem(block.Crc);;
#if UNITY_EDITOR
            if (!LoadFromAssetBundle)
            {
                if (item != null && item.AssetObject != null)
                {
                    obj = item.AssetObject;
                }
                else
                {
                    if (item == null)
                    {
                        item = new AssetItem();
                        item.Crc = block.Crc;
                    }

                    obj = AssetDatabase.LoadAssetAtPath(block.Path, block.AssetType);
                    item.AssetObject = obj;
                }
                yield return new WaitForSeconds(0.5f);
            }
#endif
            if (obj == null)
            {
                if (item != null && item.AssetBundle != null)
                {
                    AssetBundleRequest abRequest = null;
                    abRequest = item.AssetBundle.LoadAssetAsync(item.AssetName,assetType);
                    yield return abRequest;
                    if (abRequest.isDone)
                    {
                        obj = abRequest.asset;
                        item.AssetObject = obj;
                    }
                    lastYieldTime = DateTime.Now.Ticks;
                }
            }
            
            //缓存ResourceItem并设置引用计数
            callBackList = block.AsyncCallBackList;
            AssetItemManager.Instance.AddUsedAssetItem(item, callBackList.Count);

            AsyncAssetCallBack assetCallBack = null;
            AsyncGameObjectCallBack gameObjectCallBack = null;

            for (int i = 0; i < callBackList.Count; i++)
            {
                if (block.IsGameObject)
                {
                    gameObjectCallBack = callBackList[i] as AsyncGameObjectCallBack;
                    if (!gameObjectCallBack.CancelAsyncLoad)
                    {
                        GameObject gameObject = GameObject.Instantiate(obj as GameObject);
                        if (gameObjectCallBack.Parent == null)
                        {
                            gameObject.transform.SetParent(gameObjectCallBack.Parent);
                        }
                        if (gameObjectCallBack.ShowGameObjectAction == null)
                        {
                            gameObject.SetActive(true);
                        }
                        else
                        {
                            gameObjectCallBack.ShowGameObjectAction.Invoke(gameObject);
                        }

                        AssetManager.Instance.AddUsedGameObjectItem(item, gameObject, gameObjectCallBack.ClearSelf);

                        gameObjectCallBack.CallBack.Item1?.Invoke(gameObject, gameObjectCallBack.CallBack.Item2);
                    }
                    else
                    {
                        ReleaseAsset(obj,true);
                    }
                    if (asyncAssetCallBacks.ContainsKey(gameObjectCallBack.GUID))
                    {
                        asyncAssetCallBacks.Remove(gameObjectCallBack.GUID);
                    }
                    asyncGameObjectCallBackPool.Recycle(gameObjectCallBack);
                }
                else
                {
                    assetCallBack = callBackList[i];
                    if (!assetCallBack.CancelAsyncLoad)
                    {
                        assetCallBack.CallBack.Item1?.Invoke(obj, assetCallBack.CallBack.Item2);
                    }
                    else
                    {
                        ReleaseAsset(obj,true);
                    }
                    if (asyncAssetCallBacks.ContainsKey(assetCallBack.GUID))
                    {
                        asyncAssetCallBacks.Remove(assetCallBack.GUID);
                    }
                    asyncAssetCallBackPool.Recycle(assetCallBack);
                }
                
            }
            
            callBackList.Clear();
            asyncLoadDic.Remove(block.Crc);
            asyncAssetLoadBlockPool.Recycle(block);
            if (DateTime.Now.Ticks - lastYieldTime > MAXLOADRESTIME)
            {
                yield return null;
                lastYieldTime = DateTime.Now.Ticks;
            }

            yield return new WaitForSeconds(0.5f);
        } 
    }
    
    /// <summary>
    /// 根据优先级获得执行异步加载的数据块
    /// </summary>
    /// <returns>异步加载数据块</returns>
    private AsyncAssetLoadBlock GetAsyncLoadParamByPriority()
    {
        AsyncAssetLoadBlock block = null;
        int priority = -1;
        if (asyncLoadList[(int)AsyncLoadPriority.RES_HIGHT].Count > 0)
        {
            priority = (int)AsyncLoadPriority.RES_HIGHT;
        }
        else if (asyncLoadList[(int)AsyncLoadPriority.RES_MIDDLE].Count > 0)
        {
            priority = (int)AsyncLoadPriority.RES_MIDDLE;
        }
        else if(asyncLoadList[(int)AsyncLoadPriority.RES_SLOW].Count > 0)
        {
            priority = (int)AsyncLoadPriority.RES_SLOW;
        }
        if (priority != -1)
        {
            block = asyncLoadList[priority][0];
            asyncLoadList[priority].RemoveAt(0);
        }
        return block;
    }
    
    /// <summary>
    /// 释放不需要实例化的资源，例如Texture,音频等等（同步、异步相同）
    /// </summary>
    /// <param name="obj">要释放的资源对象</param>
    /// <param name="unloadAsset">是否在引用为0时缓存当前对象</param>
    /// <returns>释放结果</returns>
    internal bool ReleaseAsset(UnityEngine.Object obj, bool unloadAsset = false)
    {
        AssetItem item = null;
        foreach (AssetItem res in usedAssetItemDic.Values)
        {
            if (res.AssetObject == obj)
            {
                item = res;
            }
        }

        if (item == null)
        {
            Debug.LogWarning("AssetDic里不存在改资源：" + obj.name + "  可能释放了多次");
            return false;
        }

        DecreaseAssetItemReference(item);

        ClearOrCacheAssetItem(item, unloadAsset);
        return true;
    }

    /// <summary>
    /// 释放不需要实例化的资源，例如Texture,音频等等（同步、异步相同）
    /// </summary>
    /// <param name="crc">资源的Crc</param>
    /// <param name="unloadAsset">是否在引用为0时缓存当前对象</param>
    /// <returns>释放结果</returns>
    internal bool ReleaseAsset(uint crc, bool unloadAsset = false , int refCount = 1)
    {
        if (!usedAssetItemDic.TryGetValue(crc, out AssetItem item) || null == item)
        {
            Debug.LogWarning("AssetDic里不存在改资源：" + crc + "  可能释放了多次");
            return false;
        }

        DecreaseAssetItemReference(item, refCount);
        
        ClearOrCacheAssetItem(item, unloadAsset);
        
        Debug.Log(cachedAssetList.Count);
        return true;
    }

    /// <summary>
    /// 减少ResourceItem引用计数
    /// </summary>
    /// <param name="assetItem">ResourceItem</param>
    /// <param name="count">减少的数量</param>
    private void DecreaseAssetItemReference(AssetItem assetItem, int count = 1)
    {
        if (assetItem != null)
        {
            assetItem.RefCount -= count;
        }
    }
    
    /// <summary>
    /// 卸载ResourceItem中的AssetBundle和Object
    /// </summary>
    /// <param name="item">要卸载AssetBundle和Object的ResourceItem</param>
    /// <param name="clearAssetItem">是否用缓存来代替卸载</param>
    private void ClearOrCacheAssetItem(AssetItem item, bool clearAssetItem = false)
    {
        if (item == null || item.RefCount > 0)
        {
            return;
        }

        if (usedAssetItemDic.ContainsKey(item.Crc))
        {
            usedAssetItemDic.Remove(item.Crc);
        }
        
        if (!clearAssetItem)
        {
            cachedAssetList.AddFirst(item);
            cachedCount++;
            
            ClearHalfOfCache();
            return;
        }

        //释放assetbundle引用
        AssetBundleManager.Instance.UnloadAssetBundle(item);
        
        if (item.AssetObject != null)
        {
            item.AssetObject = null;
#if UNITY_EDITOR
            Resources.UnloadUnusedAssets();
#endif
        }
    }

    /// <summary>
    /// 当缓存数量超过预设的缓存数量时，清空一半缓存。
    /// </summary>
    private void ClearHalfOfCache()
    {
        if (cachedCount < maxNoReferenceCount)
            return;
        for (int i = 0; i < maxNoReferenceCount / 2; i++)
        {
            AssetItem item = cachedAssetList.Last.Value;
            ClearOrCacheAssetItem(item, true);
            cachedAssetList.RemoveLast();
            
        }
    }

    public void ClearCache()
    {
        for (int i = 0; i < cachedAssetList.Count; i++)
        {
            AssetItem item = cachedAssetList.Last.Value;
            ClearOrCacheAssetItem(item, true);
            cachedAssetList.RemoveLast();
            
        }
    }
}
