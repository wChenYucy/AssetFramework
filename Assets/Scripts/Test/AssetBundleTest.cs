using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEditor;

public class AssetBundleTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AssetBundle assetBundleConfigText =
            AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/assetbundleconfig");
        TextAsset textAsset=assetBundleConfigText.LoadAsset<TextAsset>("AssetBundleConfig.bytes");
        MemoryStream memoryStream=new MemoryStream(textAsset.bytes);
        BinaryFormatter binaryFormatter=new BinaryFormatter();
        AssetBundleConfig assetBundleConfig = (AssetBundleConfig)binaryFormatter.Deserialize(memoryStream);
        memoryStream.Close();
        string path = "Assets/GameData/Prefab/Attack.prefab";
        uint crc = Crc32.GetCRC32(path);
        ItemConfig itemConfig = null;
        for (int i = 0; i < assetBundleConfig.ItemList.Count; i++)
        {
            if (assetBundleConfig.ItemList[i].AssetCrc == crc)
            {
                itemConfig = assetBundleConfig.ItemList[i];
                break;
            }
                
        }
        for (int i = 0; i < itemConfig.DependAssetBundle.Count; i++)
        {
            AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + itemConfig.DependAssetBundle[i]);
        }
        AssetBundle assetBundle=AssetBundle.LoadFromFile(Application.streamingAssetsPath+"/"+itemConfig.AssetBundleName);
        GameObject.Instantiate(assetBundle.LoadAsset<GameObject>(itemConfig.AssetName));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
