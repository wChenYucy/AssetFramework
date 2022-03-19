using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
public class AssetBundleConfig
{
    //资源列表
    [XmlElement("ItemList")]
    public List<ItemConfig> ItemList { get; set; }
}
[System.Serializable]
public class ItemConfig
{
    //资源路径
    [XmlAttribute("AssetPath")]
    public string AssetPath { get; set; }
    
    //资源名称
    [XmlAttribute("AssetName")]
    public string AssetName { get; set; }
    
    //资源所在AssetBundle名称
    [XmlAttribute("AssetBundleName")]
    public string AssetBundleName { get; set; }
    
    //资源Crc，根据AssetBundleName与AssetName组合计算获得
    [XmlAttribute("AssetCrc")]
    public uint AssetCrc { get; set; }
   
    //当前AssetBundle所依赖的AssetBundle名称
    [XmlElement("DependAssetBundle")]
    public List<string> DependAssetBundle { get; set; }
}
