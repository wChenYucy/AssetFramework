using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class HotPatchManager : Singleton<HotPatchManager>
{
    //开启携程的Mono类
    private MonoBehaviour mono;
    
    //当前构建版本
    private string currentVersion;
    //当前构建包名
    private string currentPackName;
    
    //服务器热更配置表
    private HotPatchInfo hotPatchInfo;
    //服务器热更数据
    private VersionInfo currentVersionInfo;
    private Patches currentPatches;
    
    //所有属于热更的AssetBundle信息
    private Dictionary<string, Patch> hotPatches;
    
    //需要下载的AssetBundle列表
    private List<Patch> needDownloadAssetBundles;
    //需要下载的AssetBundle字典
    private Dictionary<string, Patch> needDownloadAssetBundleDic;
    //需要下载的AssetBundle在服务器热更配置表中的MD5码
    private Dictionary<string, string> needDownloadAssetBundleMD5;
    
    //是否正在下载
    private bool isDownloadStart;
    //已经下载完成的AssetBundle
    private List<Patch> alreadyDownloadAssetBundleList;
    //正在下载的AssetBundle
    private DownLoadAssetBundle currentDownloadAssetBundle;
    
    //尝试重新下载的次数
    private int tryDownloadAgainCount;
    //最大尝试重新下载的次数
    private int maxDownloadAgainCount;
    
    //下载AssetBundle的总数量
    public int DownloadTotalCount { get; set; }
    
    //下载AssetBundle的总大小 单位kb
    public float DownloadTotalSize { get; set; }
    
    //下载热更配置表失败回调
    public Action HotPatchInfoDownloadError;
    
    //下载AssetBundle失败回调
    public Action<string> AssetBundleDownloadError;
    
    //热更新成功回调
    public Action AssetBundleDownloadOver;
    
    //初始化函数
    public void Init(MonoBehaviour mono)
    {
        this.mono = mono;
        
        hotPatches = new Dictionary<string, Patch>();
        needDownloadAssetBundles = new List<Patch>();
        needDownloadAssetBundleDic = new Dictionary<string, Patch>();
        needDownloadAssetBundleMD5 = new Dictionary<string, string>();
        
        DownloadTotalCount = 0;
        DownloadTotalSize = 0;

        isDownloadStart = false;
        alreadyDownloadAssetBundleList = new List<Patch>();
        tryDownloadAgainCount = 0;
        maxDownloadAgainCount = 4;
    }
    

    public void CheckVersion(Action<bool> hotPatchCallBack)
    {
        tryDownloadAgainCount = 0;
        hotPatches.Clear();
        ReadVersion();
        mono.StartCoroutine(CheckHotPatch(hotPatchCallBack));
    }

    private void ReadVersion()
    {
        TextAsset text = Resources.Load<TextAsset>("Version");
        if (text == null)
        {
            Debug.LogError("读取本地配置信息失败");
        }

        string[] all = text.text.Split('\n');
        if (all.Length > 0)
        {
            string[] infoList = all[0].Split(';');
            if (infoList.Length >= 2)
            {
                currentVersion = infoList[0].Split('|')[1];
                currentPackName = infoList[1].Split('|')[1];
            }
        }
    }

    IEnumerator CheckHotPatch(Action<bool> callBack)
    {
        #region 下载服务器最新配置表

        string versionConfigUrl = "https://127.0.0.1/HotPatchInfo.xml";
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(versionConfigUrl);
        unityWebRequest.timeout = 30;
        yield return unityWebRequest.SendWebRequest();

        if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("连接服务器超时，请检查本地网络配置！");
            yield break;
        }
        FileUtils.CreateFile(Constant.ServerHotPatchConfigPath,unityWebRequest.downloadHandler.data);
        if (File.Exists(Constant.ServerHotPatchConfigPath))
        {
            hotPatchInfo = SerializeUtils.XmlDeserialize<HotPatchInfo>(Constant.ServerHotPatchConfigPath);
        }
        else
        {
            Debug.Log("下载热更新表失败！");
            yield break;
        }
        
        if (hotPatchInfo == null)
        {
            HotPatchInfoDownloadError?.Invoke();
            yield break;
        }

        foreach (var versionInfo in hotPatchInfo.GameVersion)
        {
            if (versionInfo.Version == currentVersion)
            {
                currentVersionInfo = versionInfo;
                break;
            }
        }

        #endregion

        #region 对比配置表信息，确定需要下载的资源

        if (IsNeedHotPatch())
        {
            if (File.Exists(Constant.ServerHotPatchConfigPath))
            {
                if (File.Exists(Constant.LocalHotPatchConfigPath))
                    File.Delete(Constant.LocalHotPatchConfigPath);
                File.Move(Constant.ServerHotPatchConfigPath, Constant.LocalHotPatchConfigPath);
            }
        }
            
        HandleHotPatchAssetBundle();
            
        DownloadTotalCount = needDownloadAssetBundles.Count;
        DownloadTotalSize = needDownloadAssetBundles.Sum(x => x.Size);

        #endregion
        
        //执行检查热更完成回调
        callBack?.Invoke(needDownloadAssetBundles.Count > 0);
    }

    /// <summary>
    /// 判断是否需要热更新
    /// </summary>
    /// <returns>判断结果</returns>
    private bool IsNeedHotPatch()
    {
        if (!File.Exists(Constant.LocalHotPatchConfigPath))
            return true;
        HotPatchInfo local = SerializeUtils.XmlDeserialize<HotPatchInfo>(Constant.LocalHotPatchConfigPath);

        VersionInfo localVersionInfo = null;

        foreach (var versionInfo in local.GameVersion)
        {
            if (versionInfo.Version == currentVersion)
            {
                localVersionInfo = versionInfo;
                break;
            }
        }

        if (localVersionInfo != null && localVersionInfo.Pathces != null && currentPatches != null &&
            local.GameVersion[^1].Pathces[^1].Version != currentPatches.Version) 
            return true;

        return false;
    }

    private void HandleHotPatchAssetBundle()
    {
        needDownloadAssetBundles.Clear();
        needDownloadAssetBundleDic.Clear();
        needDownloadAssetBundleMD5.Clear();
        if (currentVersionInfo != null && currentVersionInfo.Pathces != null && currentVersionInfo.Pathces.Length > 0)
        {
            currentPatches = currentVersionInfo.Pathces[^1];
            if (currentPatches != null && currentPatches.Files != null)
            {
                foreach (var patch in currentPatches.Files)
                {
                    hotPatches.Add(patch.Name, patch);
                    if ((Application.platform == RuntimePlatform.OSXPlayer ||
                         Application.platform == RuntimePlatform.OSXEditor) && patch.Platform.Contains("StandaloneOSX"))
                    {
                        CheckAssetBundleUpdate(patch);
                    }
                    else if((Application.platform == RuntimePlatform.WindowsPlayer ||
                             Application.platform == RuntimePlatform.OSXEditor) && patch.Platform.Contains("StandaloneWindows64"))
                    {
                        CheckAssetBundleUpdate(patch);
                    }
                    else if((Application.platform == RuntimePlatform.Android ||
                             Application.platform == RuntimePlatform.OSXEditor) && patch.Platform.Contains("Android"))
                    {
                        CheckAssetBundleUpdate(patch);
                    }
                    else if((Application.platform == RuntimePlatform.IPhonePlayer ||
                             Application.platform == RuntimePlatform.OSXEditor) && patch.Platform.Contains("IOS"))
                    {
                        CheckAssetBundleUpdate(patch);
                    }
                }
            }
        }
    }

    private void CheckAssetBundleUpdate(Patch patch)
    {
        string path = Constant.AssetBundleDownloadPath + patch.Name;
        if (File.Exists(path))
        {
            string md5 = GUIDUtils.BuildFileMd5(path);
            if (md5 != patch.Md5)
            {
                needDownloadAssetBundles.Add(patch);
                needDownloadAssetBundleDic.Add(patch.Name, patch);
                needDownloadAssetBundleMD5.Add(patch.Name, patch.Md5);
            }
        }
        else
        {
            needDownloadAssetBundles.Add(patch);
            needDownloadAssetBundleDic.Add(patch.Name, patch);
        }
    }

    public IEnumerator DownLoadAssetBundle(Action callBack, List<Patch> allPatch = null)
    {
        isDownloadStart = true;
        alreadyDownloadAssetBundleList.Clear();
        if (allPatch == null)
            allPatch = needDownloadAssetBundles;
        if (!Directory.Exists(Constant.AssetBundleDownloadPath))
            Directory.CreateDirectory(Constant.AssetBundleDownloadPath);
        List<DownLoadAssetBundle> assetBundles = new List<DownLoadAssetBundle>();
        foreach (var patch in allPatch)
        {
            assetBundles.Add(new DownLoadAssetBundle(patch.Url, Constant.AssetBundleDownloadPath));
        }

        foreach (var downLoadAssetBundle in assetBundles)
        {
            currentDownloadAssetBundle = downLoadAssetBundle;
            yield return mono.StartCoroutine(currentDownloadAssetBundle.Download());
            Patch patch = FindPatchByName(currentDownloadAssetBundle.FileName);
            if (patch != null)
            {
                alreadyDownloadAssetBundleList.Add(patch);
            }
            currentDownloadAssetBundle.Destroy();
        }
        
        List<Patch> downLoadList = new List<Patch>();
        foreach (DownLoadAssetBundle downLoad in assetBundles)
        {
            string md5 = "";
            if (needDownloadAssetBundleMD5.TryGetValue(downLoad.FileName, out md5))
            {
                if (GUIDUtils.BuildFileMd5(downLoad.SaveFilePath) != md5)
                {
                    Debug.Log(string.Format("此文件{0}MD5校验失败，即将重新下载", downLoad.FileName));
                    Patch patch = FindPatchByName(downLoad.FileName);
                    if (patch != null)
                    {
                        downLoadList.Add(patch);
                    }
                }
            }
        }
        
        if (downLoadList.Count <= 0)
        {
            needDownloadAssetBundleMD5.Clear();
            if (callBack != null)
            {
                isDownloadStart = false;
                callBack();
            }
            AssetBundleDownloadOver?.Invoke();
        }
        else
        {
            if (tryDownloadAgainCount >= maxDownloadAgainCount)
            {
                string allName = "";
                isDownloadStart = false;
                foreach (Patch patch in downLoadList)
                {
                    allName += patch.Name + ";";
                }
                Debug.LogError("资源重复下载4次MD5校验都失败，请检查资源" + allName);
                AssetBundleDownloadError?.Invoke(allName);
            }
            else
            {
                tryDownloadAgainCount++;
                needDownloadAssetBundleMD5.Clear();
                foreach (Patch patch in downLoadList)
                {
                    needDownloadAssetBundleMD5.Add(patch.Name, patch.Md5);
                }
                //自动重新下载校验失败的文件
                mono.StartCoroutine(DownLoadAssetBundle(callBack, downLoadList));
            }
        }
    }
    
    /// <summary>
    /// 根据名字查找对象的热更Patch
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Patch FindPatchByName(string name)
    {
        needDownloadAssetBundleDic.TryGetValue(name, out var patch);
        return patch;
    }
    
    /// <summary>
    /// 获取下载进度
    /// </summary>
    /// <returns></returns>
    public float GetProgress()
    {
        return GetLoadSize() / DownloadTotalSize;
    }

    /// <summary>
    /// 获取已经下载总大小
    /// </summary>
    /// <returns></returns>
    public float GetLoadSize()
    {
        float alreadySize = alreadyDownloadAssetBundleList.Sum(x => x.Size);
        float curAlreadySize = 0;
        if (currentDownloadAssetBundle != null)
        {
            Patch patch = FindPatchByName(currentDownloadAssetBundle.FileName);
            if (patch != null && !alreadyDownloadAssetBundleList.Contains(patch))
            {
                curAlreadySize = currentDownloadAssetBundle.GetProcess() * patch.Size;
            }
        }

        return alreadySize + curAlreadySize;
    }
}
