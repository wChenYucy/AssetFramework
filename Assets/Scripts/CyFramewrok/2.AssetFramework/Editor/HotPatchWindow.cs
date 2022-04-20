using System;
using PlasticGui.WorkspaceWindow.CodeReview;
using UnityEditor;
using UnityEngine;

public class HotPatchWindow : EditorWindow
{
    [MenuItem("CyFramework/OpenHotFixWindow")]
    public static void OpenWindow()
    {
        HotPatchWindow window = EditorWindow.GetWindow<HotPatchWindow>(false, "热更包界面", true);
        window.Show();
    }

    private string md5Path = "";
    private string hotCount = "1";

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        md5Path = EditorGUILayout.TextField("MD5配置文件路径：", md5Path);
        if (GUILayout.Button("选择配置文件",GUILayout.Width(150),GUILayout.Height(20)))
        {
            md5Path = "/Users/chenyu/Codes/Unity/Assets/AssetFramework/Version/Android/ABInfoConfig_0.1.bytes";
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        hotCount = EditorGUILayout.TextField("热更新补丁版本：", hotCount);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("开始构建热更包",GUILayout.Width(150),GUILayout.Height(20)))
        {
            if (!string.IsNullOrEmpty(md5Path) && md5Path.EndsWith(".bytes"))
            {
                BuildAssetBundleUtils.BuildAssetBundle(true, md5Path, hotCount);
            }
        }
        EditorGUILayout.EndHorizontal();
    }
}
