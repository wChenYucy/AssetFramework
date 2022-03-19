using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameStart : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
        Resources.UnloadUnusedAssets();
        AssetManagerConfig config = new AssetManagerConfig();
        config.startCoroutineMono = this;
        AssetManager.Instance.Init(config);

    }

    public void Start()
    {
        UIManager.Instance.Init(transform.Find("UIRoot") as RectTransform, transform.Find("UIRoot/WndRoot") as RectTransform, transform.Find("UIRoot/UICamera").GetComponent<Camera>(), transform.Find("UIRoot/EventSystem").GetComponent<EventSystem>());
        
        UIManager.Instance.Register<MenuUi>(ConStr.MENUPANEL);
        UIManager.Instance.Register<LoadingUi>(ConStr.LOADINGPANEL);
        GameMapManager.Instance.Init(this);
        //ResourceManager.Instance.PreloadResource<AudioClip>("senlin.mp3","sound");
        // AssetManager.Instance.PreloadGameObject("Attack.prefab", "attack", 10, transform);
        // GameObject go = AssetManager.Instance.LoadGameObject("Attack.prefab", "attack", transform);
        // AssetManager.Instance.ReleaseAsset(go, false);
        
        //加载场景
        //GameMapManager.Instance.LoadScene(ConStr.MENUSCENE);
    }

    private void Update()
    {
        UIManager.Instance.OnUpdate();
        
    }
    
//     private void OnApplicationQuit()
//     {
// #if UNITY_EDITOR
//         ResourceManager.Instance.ClearCache();
//         Resources.UnloadUnusedAssets();
//         Debug.Log("清空编辑器缓存");
// #endif
//     }
}
