using UnityEngine;

public class Constant
{

    #region 热更相关常量

    public static readonly string ServerHotPatchConfigPath = Application.persistentDataPath + "/ServerHotPatchInfo.xml";
    public static readonly string LocalHotPatchConfigPath = Application.persistentDataPath + "/LocalHotPatchInfo.xml";
    public static readonly string AssetBundleDownloadPath = Application.persistentDataPath + "/Download/";

    #endregion
    
}
