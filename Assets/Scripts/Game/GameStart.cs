using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameStart : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log(Application.persistentDataPath);
        DontDestroyOnLoad(this);
        Resources.UnloadUnusedAssets();
        HotPatchManager.Instance.Init(this);
        UIManager.Instance.Init(transform.Find("UIRoot") as RectTransform, transform.Find("UIRoot/WndRoot") as RectTransform, transform.Find("UIRoot/UICamera").GetComponent<Camera>(), transform.Find("UIRoot/EventSystem").GetComponent<EventSystem>());
        RegisterUI();
    }
    // Use this for initialization
    void Start ()
    {
        UIManager.Instance.PopUpWnd(ConStr.HOTPATCH, resource: true);
    }

    private void Update()
    {
        UIManager.Instance.OnUpdate();
    }

    public static void OpenCommonConfirm(string title, string str, UnityEngine.Events.UnityAction confirmAction, UnityEngine.Events.UnityAction cancleAction)
    {
        GameObject commonObj = GameObject.Instantiate(Resources.Load<GameObject>("CommonConfirm")) as GameObject;
        commonObj.transform.SetParent(UIManager.Instance.m_UiRoot, false);
        CommonConfirm commonItem = commonObj.GetComponent<CommonConfirm>();
        commonItem.Show(title,str, confirmAction, cancleAction);
    }
    
    private GameObject m_obj;
    
    public IEnumerator StartGame(Image image, Text text)
    {
        image.fillAmount = 0.1f;
        yield return null;
        text.text = "加载本地数据... ...";
        AssetManagerConfig config = new AssetManagerConfig();
        config.startCoroutineMono = this;
        AssetManager.Instance.Init(config);
        image.fillAmount = 0.2f;
        yield return new WaitForSeconds(1);
        text.text = "加载数据表... ...";
        LoadConfiger();
        image.fillAmount = 0.7f;
        yield return new WaitForSeconds(1);
        text.text = "加载配置... ...";
        image.fillAmount = 0.9f;
        yield return new WaitForSeconds(1);
        text.text = "初始化地图... ...";
        GameMapManager.Instance.Init(this);
        image.fillAmount = 1f;
    }

    //注册UI窗口
    void RegisterUI()
    {
        UIManager.Instance.Register<MenuUi>(ConStr.MENUPANEL);
        UIManager.Instance.Register<LoadingUi>(ConStr.LOADINGPANEL);
        UIManager.Instance.Register<HotPatchUi>(ConStr.HOTPATCH);
    }

    //加载配置表
    void LoadConfiger()
    {
        //ConfigerManager.Instance.LoadData<MonsterData>(CFG.TABLE_MONSTER);
        //ConfigerManager.Instance.LoadData<BuffData>(CFG.TABLE_BUFF);
    }
    

}
