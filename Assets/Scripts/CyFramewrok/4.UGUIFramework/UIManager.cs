using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum UIMsgID
{
    None = 0,
}

public class UIManager : Singleton<UIManager>
{
    //UI节点
    public RectTransform m_UiRoot;
    //窗口节点
    private RectTransform m_WndRoot;
    //UI摄像机
    private Camera m_UICamera;
    //EventSystem节点
    private EventSystem m_EventSystem;
    //屏幕的宽高比
    private float m_CanvasRate = 0;

    private string m_UIPrefabPath = "Assets/GameData/Prefab/";
    //注册的字典
    private Dictionary<string, System.Type> m_RegisterDic = new Dictionary<string, System.Type>();
    //所有打开的窗口
    private Dictionary<string, Window> m_WindowDic = new Dictionary<string, Window>();
    //打开的窗口列表
    private List<Window> m_WindowList = new List<Window>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="uiRoot">UI父节点</param>
    /// <param name="wndRoot">窗口父节点</param>
    /// <param name="uiCamera">UI摄像机</param>
    public void Init(RectTransform uiRoot, RectTransform wndRoot, Camera uiCamera, EventSystem eventSystem)
    {
        m_UiRoot = uiRoot;
        m_WndRoot = wndRoot;
        m_UICamera = uiCamera;
        m_EventSystem = eventSystem;
        m_CanvasRate = Screen.height / (m_UICamera.orthographicSize * 2);
    }

    /// <summary>
    /// 设置所有节目UI路径
    /// </summary>
    /// <param name="path"></param>
    public void SetUIPrefabPath(string path)
    {
        m_UIPrefabPath = path;
    }

    /// <summary>
    /// 显示或者隐藏所有UI
    /// </summary>
    public void ShowOrHideUI(bool show)
    {
        if (m_UiRoot != null)
        {
            m_UiRoot.gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// 设置默认选择对象
    /// </summary>
    /// <param name="obj"></param>
    public void SetNormalSelectObj(GameObject obj)
    {
        if (m_EventSystem == null)
        {
            m_EventSystem = EventSystem.current;
        }
        m_EventSystem.firstSelectedGameObject = obj;
    }

    /// <summary>
    /// 窗口的更新
    /// </summary>
    public void OnUpdate()
    {
        for (int i = 0; i < m_WindowList.Count; i++)
        {
            if (m_WindowList[i] != null)
            {
                m_WindowList[i].OnUpdate();
            }
        }
    }

    /// <summary>
    /// 窗口注册方法
    /// </summary>
    /// <typeparam name="T">窗口泛型类</typeparam>
    /// <param name="name">窗口名</param>
    public void Register<T>(string name) where T : Window
    {
        m_RegisterDic[name] = typeof(T);
    }

    /// <summary>
    /// 发送消息给窗口
    /// </summary>
    /// <param name="name">窗口名</param>
    /// <param name="msgID">消息ID</param>
    /// <param name="paralist">参数数组</param>
    /// <returns></returns>
    public bool SendMessageToWnd(string name, UIMsgID msgID = 0, params object[] paralist)
    {
        Window wnd = FindWndByName<Window>(name);
        if (wnd != null)
        {
            return wnd.OnMessage(msgID, paralist);
        }
        return false;
    }

    /// <summary>
    /// 根据窗口名查找窗口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public T FindWndByName<T>(string name) where T : Window
    {
        Window wnd = null;
        if (m_WindowDic.TryGetValue(name, out wnd))
        {
            return (T)wnd;
        }

        return null;
    }

    /// <summary>
    /// 打开窗口
    /// </summary>
    /// <param name="wndName"></param>
    /// <param name="bTop"></param>
    /// <param name="para1"></param>
    /// <param name="para2"></param>
    /// <param name="para3"></param>
    /// <returns></returns>
    public Window PopUpWnd(string wndName, bool bTop = true, bool resource = false, params object[] paralist)
    {
        Window wnd = FindWndByName<Window>(wndName);
        if (wnd == null)
        {
            System.Type tp = null;
            if (m_RegisterDic.TryGetValue(wndName, out tp))
            {
                wnd = System.Activator.CreateInstance(tp) as Window;
            }
            else
            {
                Debug.LogError("找不到窗口对应的脚本，窗口名是：" + wndName);
                return null;
            }

            GameObject wndObj = null;
            if (resource)
            {
                wndObj = GameObject.Instantiate(Resources.Load<GameObject>(wndName.Replace(".prefab", "")));
            }
            else
            {
                wndObj = AssetManager.Instance.LoadGameObject(m_UIPrefabPath + wndName, m_WndRoot, false);
            }
            if (wndObj == null)
            {
                Debug.Log("创建窗口Prefab失败：" + wndName);
                return null;
            }

            if (!m_WindowDic.ContainsKey(wndName))
            {
                m_WindowList.Add(wnd);
                m_WindowDic.Add(wndName, wnd);
            }

            wnd.GameObject = wndObj;
            wnd.Transform = wndObj.transform;
            wnd.Name = wndName;
            wnd.Awake(paralist);
            wnd.Resource = resource;
            
            wndObj.transform.SetParent(m_WndRoot, false);
            if (bTop)
            {
                wndObj.transform.SetAsLastSibling();
            }

            wnd.OnShow(paralist);
        }
        else
        {
            ShowWnd(wndName, bTop, paralist);
        }

        return wnd;
    }

    /// <summary>
    /// 根据窗口名关闭窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="destory"></param>
    public void CloseWnd(string name, bool destory = false)
    {
        Window wnd = FindWndByName<Window>(name);
        CloseWnd(wnd, destory);
    }

    /// <summary>
    /// 根据窗口对象关闭窗口
    /// </summary>
    /// <param name="window"></param>
    /// <param name="destory"></param>
    public void CloseWnd(Window window, bool destory = false)
    {
        if (window != null)
        {
            window.OnDisable();
            window.OnClose();
            if (m_WindowDic.ContainsKey(window.Name))
            {
                m_WindowDic.Remove(window.Name);
                m_WindowList.Remove(window);
            }

            if (window.Resource)
            {
                GameObject.Destroy(window.GameObject);
            }
            else
            {
                if (destory)
                {
                    AssetManager.Instance.ReleaseAsset(window.GameObject, true);
                }
                else
                {
                    AssetManager.Instance.ReleaseAsset(window.GameObject);
                }
            }
            
            window.GameObject = null;
            window = null;
        }
    }

    /// <summary>
    /// 关闭所有窗口
    /// </summary>
    public void CloseAllWnd()
    {
        for (int i = m_WindowList.Count - 1; i >= 0; i--)
        {
            CloseWnd(m_WindowList[i]);
        }
    }

    /// <summary>
    /// 切换到唯一窗口
    /// </summary>
    public void SwitchStateByName(string name,bool bTop = true,params object[] paralist)
    {
        CloseAllWnd();
        PopUpWnd(name, bTop, false, paralist);
    }

    /// <summary>
    /// 根据名字隐藏窗口
    /// </summary>
    /// <param name="name"></param>
    public void HideWnd(string name)
    {
        Window wnd = FindWndByName<Window>(name);
        HideWnd(wnd);
    }

    /// <summary>
    /// 根据窗口对象隐藏窗口
    /// </summary>
    /// <param name="wnd"></param>

    public void HideWnd(Window wnd)
    {
        if (wnd != null)
        {
            wnd.GameObject.SetActive(false);
            wnd.OnDisable();
        }
    }

    /// <summary>
    /// 根据窗口名字显示窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="paralist"></param>
    public void ShowWnd(string name, bool bTop = true, params object[] paralist)
    {
        Window wnd = FindWndByName<Window>(name);
        ShowWnd(wnd, bTop, paralist);
    }

    /// <summary>
    /// 根据窗口对象显示窗口
    /// </summary>
    /// <param name="wnd"></param>
    /// <param name="paralist"></param>
    public void ShowWnd(Window wnd, bool bTop = true, params object[] paralist)
    {
        if (wnd != null)
        {
            if (wnd.GameObject != null && !wnd.GameObject.activeSelf) wnd.GameObject.SetActive(true);
            if (bTop) wnd.Transform.SetAsLastSibling();
            wnd.OnShow(paralist);
        }
    }
}
