using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DownLoadAssetBundle : DownLoadItem
{
    private UnityWebRequest webRequest;

    public DownLoadAssetBundle(string url, string path) : base(url, path)
    {
        
    }

    public override IEnumerator Download(Action callback = null)
    {
        webRequest = UnityWebRequest.Get(url);
        startDownLoad = true;
        webRequest.timeout = 30;
        yield return webRequest.SendWebRequest();
        startDownLoad = false;

        if (webRequest.isNetworkError)
        {
            Debug.LogError("Download Error" + webRequest.error);
        }
        else
        {
            byte[] bytes = webRequest.downloadHandler.data;
            FileUtils.CreateFile(saveFilePath, bytes);
            if (callback != null)
            {
                callback();
            }
        }
    }

    public override void Destroy()
    {
        if (webRequest != null)
        {
            webRequest.Dispose();
            webRequest = null;
        }
    }

    public override long GetCurLength()
    {
        if (webRequest != null)
        {
            return (long)webRequest.downloadedBytes;
        }
        return 0;
    }

    public override long GetLength()
    {
        return 0;
    }

    public override float GetProcess()
    {
        if (webRequest != null)
        {
            return (long)webRequest.downloadProgress;
        }
        return 0;
    }
}