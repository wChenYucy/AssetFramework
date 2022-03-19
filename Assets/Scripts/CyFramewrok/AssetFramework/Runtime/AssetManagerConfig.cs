using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetManagerConfig
{
    public string ConfigPath = Application.streamingAssetsPath + "/assetbundleconfig";
    public int MaxAssetCacheCount = 500;
    public MonoBehaviour startCoroutineMono = null;
}
