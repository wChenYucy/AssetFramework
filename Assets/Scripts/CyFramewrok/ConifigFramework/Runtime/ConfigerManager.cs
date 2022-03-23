using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigerManager : Singleton<ConfigerManager>
{
    /// <summary>
    /// 储存所有已经加载的配置表
    /// </summary>
    protected Dictionary<string, ExcelBase> m_AllExcelData = new Dictionary<string, ExcelBase>();

    /// <summary>
    /// 加载数据表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path">二进制路径</param>
    /// <returns></returns>
    public T LoadData<T>(string assetName,string assetBundleName) where T : ExcelBase
    {
        if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(assetBundleName))
        {
            return null;
        }

        string key = assetName + assetBundleName;
        if (m_AllExcelData.ContainsKey(key))
        {
            Debug.LogError("重复加载相同配置文件" + key);
            return m_AllExcelData[key] as T;
        }

        T data = BinarySerializeOpt.BinaryDeserialize<T>(assetName, assetBundleName);

#if UNITY_EDITOR
        if (data == null)
        {
            Debug.Log(key + "不存在，从xml加载数据了！");
            string xmlPath = key.Replace("Binary", "Xml").Replace(".bytes", ".xml");
            data = BinarySerializeOpt.XmlDeserialize<T>(xmlPath);
        }
#endif

        if (data != null)
        {
            data.Init();
        }

        m_AllExcelData.Add(key, data);

        return data;
    }

    /// <summary>
    /// 根据路径查找数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T FindData<T>(string assetName,string assetBundleName) where T:ExcelBase
    {
        if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(assetBundleName))
        {
            return null;
        }
        ExcelBase excelBase = null;
        string key = assetName + assetBundleName;
        if (m_AllExcelData.TryGetValue(key, out excelBase))
        {
            return excelBase as T;
        }
        else
        {
            excelBase = LoadData<T>(assetName, assetBundleName);
        }

        return (T)excelBase;
    }
}

public class CFG
{
    //配置表路径
    public const string TABLE_MONSTER = "Assets/GameData/Data/Binary/MonsterData.bytes";
    public const string TABLE_BUFF = "Assets/GameData/Data/Binary/BuffData.bytes";
}
