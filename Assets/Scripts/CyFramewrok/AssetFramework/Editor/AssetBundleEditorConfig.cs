using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetBundleEditorConfig",menuName = "AssetBundleEditorConfig",order = 0)]
public class AssetBundleEditorConfig : ScriptableObject
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
