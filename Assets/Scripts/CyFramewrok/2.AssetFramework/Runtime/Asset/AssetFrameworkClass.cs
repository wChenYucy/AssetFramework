using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// AssetBundle内存，用来记录AssetBundle和当前该AssetBundle的引用数
/// </summary>
public class AssetBundleItem : IReusable
{
    public AssetBundle assetBundle;
    public int ReferenceCount;

    public void OnSpawn()
    {
        assetBundle = null;
        ReferenceCount = 0;
    }

    public void OnRecycle()
    {
        OnSpawn();
    }
}

/// <summary>
/// 资源基类，描述所有可能被动态加载的资源
/// </summary>
public class AssetItem
{
    #region 基础描述信息
    
    public string AssetName;
    public string AssetBundleName;
    public uint Crc;
    public List<string> DependentAssetBundle;
    public AssetBundle AssetBundle;
    #endregion

    #region 动态加载信息
    public Object AssetObject;
    private int refCount;
    //public bool ClearSelf;
    public int RefCount
    {
        get { return refCount;}
        set
        {
            refCount = value;
            if (refCount < 0)
            {
                Debug.LogError("refcount < 0" + refCount + " ," + (AssetObject != null ? AssetObject.name : "name is null"));
            }
        }
    }
    

    #endregion

    public AssetItem()
    {
        Crc = 0;
        AssetName = string.Empty;
        AssetBundleName = string.Empty;
        DependentAssetBundle = null;
        AssetBundle = null;
        AssetObject = null;
        RefCount = 0;
        //ClearSelf = true;
    }
}

/// <summary>
/// GameObject类，保存被实例化的GameObject
/// </summary>
public class GameObjectItem : IReusable
{
    public AssetItem AssetItem;
    public GameObject CloneGameObject;
    public bool ClearSelf;
    public void OnSpawn()
    {
        AssetItem = null;
        CloneGameObject = null;
        ClearSelf = false;
    }

    public void OnRecycle()
    {
        OnSpawn();
    }
}

/// <summary>
/// 异步资源加载优先级
/// </summary>
public enum AsyncLoadPriority
{
    RES_HIGHT = 0,//最高优先级
    RES_MIDDLE,//一般优先级
    RES_SLOW,//低优先级
    RES_NUM,
}

/// <summary>
/// 异步加载信息块
/// </summary>
public class AsyncAssetLoadBlock : IReusable
{
#if UNITY_EDITOR
    //资源路径
    public string Path;
#endif
    //待加载资源的ResourceItem的Crc
    public uint Crc;

    //待加载资源的类型
    public Type AssetType;

    //资源异步加载优先级
    public AsyncLoadPriority Priority;

    // 是否为GameObject
    public bool IsGameObject;

    public List<AsyncAssetCallBack> AsyncCallBackList = new List<AsyncAssetCallBack>();
    public void OnSpawn()
    {
#if UNITY_EDITOR
        //资源路径
        Path = "";
#endif
        Crc = 0;
        AssetType = null;
        Priority = AsyncLoadPriority.RES_SLOW;
        IsGameObject = false;
        AsyncCallBackList.Clear();
    }

    public void OnRecycle()
    {
        OnSpawn();
    }
}

/// <summary>
/// 异步加载Asset回调类
/// </summary>
public class AsyncAssetCallBack : IReusable
{
    // 唯一ID
    public long GUID;
    //资源加载回调
    public (AsyncLoadFinishCallBack, object) CallBack;
    // 停止加载
    public bool CancelAsyncLoad;
    public void OnSpawn()
    {
        CallBack = default;
        GUID = -1;
        CancelAsyncLoad = false;
    }

    public void OnRecycle()
    {
        OnSpawn();
    }
}

/// <summary>
/// 异步加载GameObject回调类
/// </summary>
public class AsyncGameObjectCallBack : AsyncAssetCallBack , IReusable
{
    // 实例化资源显示回调
    public Action<GameObject> ShowGameObjectAction;
    // 设置
    // 父物体
    public Transform Parent;
    // 清除自身
    public bool ClearSelf;
    
    public new void OnSpawn()
    {
        CallBack = default;
        GUID = -1;
        ShowGameObjectAction = null;
        Parent = null;
        ClearSelf = false;
        CancelAsyncLoad = false;
    }

    public new void OnRecycle()
    {
        OnSpawn();
    }
}

/// <summary>
/// 异步加载回调函数
/// </summary>
public delegate void AsyncLoadFinishCallBack( UnityEngine.Object obj, object param1 = null);

