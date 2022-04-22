using UnityEngine;
using UnityEngine.EventSystems;

public class UGUIFrameworkTest : MonoBehaviour
{
    public int test = 10;
    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(this);
        Resources.UnloadUnusedAssets();
        AssetManagerConfig config = new AssetManagerConfig
        {
            startCoroutineMono = this
        };
        AssetManager.Instance.Init(config);
    }

    // Update is called once per frame
    void Start()
    {
        UIManager.Instance.Init(transform.Find("UIRoot") as RectTransform, transform.Find("UIRoot/WndRoot") as RectTransform, transform.Find("UIRoot/UICamera").GetComponent<Camera>(), transform.Find("UIRoot/EventSystem").GetComponent<EventSystem>());
        
        UIManager.Instance.Register<MenuUi>(ConStr.MENUPANEL);
        UIManager.Instance.Register<LoadingUi>(ConStr.LOADINGPANEL);
        GameMapManager.Instance.Init(this);
        //AssetManager.Instance.PreloadAsset<AudioClip>("senlin.mp3","sound");
        AssetManager.Instance.PreloadGameObject("Assets/GameData/Prefab/Attack.prefab", 10, transform);
        //GameObject go = AssetManager.Instance.LoadGameObject("Attack.prefab", "attack", transform);
        // AssetManager.Instance.ReleaseAsset(go, false);
        
        //加载场景
        GameMapManager.Instance.LoadScene(ConStr.MENUSCENE);
    }
    private void Update()
    {
        UIManager.Instance.OnUpdate();
    }
}
