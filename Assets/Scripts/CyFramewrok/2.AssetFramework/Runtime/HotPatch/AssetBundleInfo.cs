using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
public class AssetBundleInfo
{
    [XmlElement("AssetBundleInfoList")]
    public List<AssetBundleInfoBase> assetBundleInfoList { get; set; }
}

[System.Serializable]
public class AssetBundleInfoBase
{
    [XmlAttribute("Name")]
    public string name { get; set; }
    [XmlAttribute("MD5")]
    public string md5 { get; set; }
    [XmlAttribute("Size")]
    public float size { get; set; }
}
