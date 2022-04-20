using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetBundleStrategyTable",menuName = "AssetBundleStrategyTable",order = 0)]
public class AssetBundleStrategyTable : ScriptableObject
{
    public List<string> PrefabPaths=new List<string>();
    public List<DirNameAndPath> DirPaths = new List<DirNameAndPath>();

    [System.Serializable]
    public struct DirNameAndPath
    {
        public string AssetBundleName;
        public string Path;
    }
}
