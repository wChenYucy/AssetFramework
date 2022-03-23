using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuUi : Window
{
    private MenuPanel m_MainPanel;
    private GameObject go;

    public override void Awake(params object[] paralist)
    {
        m_MainPanel = GameObject.GetComponent<MenuPanel>();
        AddButtonClickListener(m_MainPanel.m_StartButton, OnClickStart);
        AddButtonClickListener(m_MainPanel.m_LoadButton, OnClickLoad);
        AddButtonClickListener(m_MainPanel.m_ExitButton, OnClickExit);
        AssetManager.Instance.AsyncLoadAsset<Sprite>("Assets/GameData/Sprites/LoseWindow/Navigator_lose_image.png", LoadSprite1, AsyncLoadPriority.RES_HIGHT);
        AssetManager.Instance.AsyncLoadAsset<Sprite>("Assets/GameData/Sprites/LoseWindow/Btn_replay.png", LoadSprite2, AsyncLoadPriority.RES_HIGHT);
        AssetManager.Instance.AsyncLoadAsset<Sprite>("Assets/GameData/Sprites/LoseWindow/Navigator_lose_image_1.png", LoadSprite3, AsyncLoadPriority.RES_HIGHT);
        // m_MainPanel.m_audioSource.clip = AssetManager.Instance.LoadAsset<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
        // m_MainPanel.m_audioSource.Play();
        //go = AssetManager.Instance.LoadGameObject("Assets/GameData/Prefab/Attack.prefab");
    }
    public void LoadSprite1(Object sprite, object par)
    {
        m_MainPanel.m_Image1.sprite = sprite as Sprite;
        Debug.Log("Sprite1已经加载！");
    }
    public void LoadSprite2(Object sprite, object par)
    {
        m_MainPanel.m_Image2.sprite = sprite as Sprite;
        Debug.Log("Sprite2已经加载！");
    }
    public void LoadSprite3(Object sprite, object par)
    {
        m_MainPanel.m_Image3.sprite = sprite as Sprite;
        Debug.Log("Sprite3已经加载！");
    }
    

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //AssetManager.Instance.ReleaseAsset(m_MainPanel.m_audioSource.clip);
            AssetManager.Instance.ReleaseAsset(go);
            m_MainPanel.m_audioSource.clip = null;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            AssetManager.Instance.ReleaseAsset(go,true);
            //AssetItemManager.Instance.ClearCache();
            m_MainPanel.m_audioSource.clip = null;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            AssetManager.Instance.ReleaseAsset(go,true,true);
            //AssetItemManager.Instance.ClearCache();
            m_MainPanel.m_audioSource.clip = null;
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            AssetManager.Instance.ClearHalfOfGameObjectPool("Assets/GameData/Prefab/Attack.prefab");
            //AssetItemManager.Instance.ClearCache();
            m_MainPanel.m_audioSource.clip = null;
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            AssetManager.Instance.RemoveGameObjectPool("Assets/GameData/Prefab/Attack.prefab");
            //AssetItemManager.Instance.ClearCache();
            m_MainPanel.m_audioSource.clip = null;
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            AssetManager.Instance.RemoveGameObjectPool("Assets/GameData/Prefab/Attack.prefab", true);
            //AssetItemManager.Instance.ClearCache();
            m_MainPanel.m_audioSource.clip = null;
        }
    }

    void OnClickStart()
    {
        Debug.Log("点击了开始游戏！");
    }

    void OnClickLoad()
    {
        Debug.Log("点击了加载游戏！");
    }

    void OnClickExit()
    {
        Debug.Log("点击了退出游戏！");
    }
}
