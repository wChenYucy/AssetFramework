using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(menuName = "CreateRealFramConfig",order = 0)]
public class CyFrameworkConfig : ScriptableObject
{
    //打包时生成AB包配置表的二进制路径
    //xml文件夹路径
    public string m_XmlPath;
    //二进制文件夹路径
    public string m_BinaryPath;
    //脚本文件夹路径
    public string m_ScriptsPath;
}

[CustomEditor(typeof(CyFrameworkConfig))]
public class CyFrameworkConfigInspector : Editor
{
    public SerializedProperty m_XmlPath;
    public SerializedProperty m_BinaryPath;
    public SerializedProperty m_ScriptsPath;

    private void OnEnable()
    {
        m_XmlPath = serializedObject.FindProperty("m_XmlPath");
        m_BinaryPath = serializedObject.FindProperty("m_BinaryPath");
        m_ScriptsPath = serializedObject.FindProperty("m_ScriptsPath");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(m_XmlPath, new GUIContent("Xml路径"));
        GUILayout.Space(5);
        EditorGUILayout.PropertyField(m_BinaryPath, new GUIContent("二进制路径"));
        GUILayout.Space(5);
        EditorGUILayout.PropertyField(m_ScriptsPath, new GUIContent("配置表脚本路径"));
        GUILayout.Space(5);
        serializedObject.ApplyModifiedProperties();
    }
}

public class CyFrameworkConfigManager
{
    private const string CyFrameworkConfigPath = "Assets/GameConfig/RealFramConfig.asset";

    public static CyFrameworkConfig GetRealFram()
    {
        CyFrameworkConfig realConfig = AssetDatabase.LoadAssetAtPath<CyFrameworkConfig>(CyFrameworkConfigPath);
        return realConfig;
    }
}
