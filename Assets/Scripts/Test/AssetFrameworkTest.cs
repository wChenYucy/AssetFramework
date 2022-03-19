using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class AssetFrameworkTest : MonoBehaviour
{
    public AudioSource AudioSourceItem;
    public Image Image;
    private AudioClip sound;
    private Sprite sprite;
    private GameObject gameObject;
    private int count = 0;
    private AssetBundle assetBundle;
    private void Awake()
    {
        Resources.UnloadUnusedAssets();
        AssetManagerConfig config = new AssetManagerConfig();
        config.startCoroutineMono = this;
        AssetManager.Instance.Init(config);

    }

    private void LoadSound(Object obj,object param)
    {
        sound = obj as AudioClip;
        AudioSourceItem.clip = sound;
        AudioSourceItem.Play();
        if (count < 0)
            count = 0;
        count++;
    }
    private void LoadSprite(Object obj,object param)
    {
        sprite = obj as Sprite;
        Image.sprite = sprite;
        if (count < 0)
            count = 0;
        count++;
    }

    private void LoadGameObject1(Object obj,object param)
    {
        gameObject = obj as GameObject;
        gameObject.name = "123";
        if (count < 0)
            count = 0;
        count++;
    }
    private void LoadGameObject2(Object obj,object param)
    {
        gameObject = obj as GameObject;
        gameObject.name = "456";
        if (count < 0)
            count = 0;
        count++;
    }

    private int i = 2;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            i = ++i % 2;
        }
        if(i == 0)
            SpriteTest();
        else if(i == 1)
            AudioClipTest();
        else
            GameObjectTest();
    }
    
    public void SpriteTest()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            long time = DateTime.Now.Ticks;
            sprite = AssetManager.Instance.LoadAsset<Sprite>("Navigator_lose_image.png", "losewindowui");
            Image.sprite = sprite;
            if (count < 0)
                count = 0;
            count++;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            long guid = AssetManager.Instance.AsyncLoadAsset<Sprite>("Navigator_lose_image.png", "losewindowui",LoadSprite,AsyncLoadPriority.RES_HIGHT);
            //AssetManager.Instance.CancelAsyncLoad(guid);
            //ResourceManager.Instance.AsyncLoadResource("Navigator_lose_image.png", "losewindowui",LoadSprite,AsyncLoadPriority.RES_HIGHT);

        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            Image.sprite = null;
            AssetManager.Instance.ReleaseAsset(sprite);
            count--;
            if(count == 0)
                sprite = null;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Image.sprite = null;
            AssetManager.Instance.ReleaseAsset(sprite,true);
            count--;
            if(count == 0)
                sprite = null;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AssetItemManager.Instance.ClearCache();
        }
    }
    public void AudioClipTest()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            sound = AssetManager.Instance.LoadAsset<AudioClip>("senlin.mp3", "sound");
            AudioSourceItem.clip = sound;
            AudioSourceItem.Play();
            if (count < 0)
                count = 0;
            count++;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            AssetManager.Instance.AsyncLoadAsset<AudioClip>("senlin.mp3", "sound",LoadSound,AsyncLoadPriority.RES_HIGHT);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            AudioSourceItem.Stop();
            AudioSourceItem.clip = null;
            AssetManager.Instance.ReleaseAsset(sound);
            count--;
            if(count == 0)
                sound = null;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            AudioSourceItem.Stop();
            AudioSourceItem.clip = null;
            AssetManager.Instance.ReleaseAsset(sound,true);
            count--;
            if(count == 0)
                sound = null;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AssetItemManager.Instance.ClearCache();
        }
    }
    public void GameObjectTest()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            gameObject = AssetManager.Instance.LoadGameObject("Attack.prefab", "attack");
            if (count < 0)
                count = 0;
            count++;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            AssetManager.Instance.AsyncLoadGameObject("Attack.prefab", "attack",LoadGameObject1,AsyncLoadPriority.RES_HIGHT);
            if (count < 0)
                count = 0;
            count++;
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            AssetManager.Instance.ReleaseAsset(gameObject);
            count--;
            if(count == 0)
                gameObject = null;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            AssetManager.Instance.ReleaseAsset(gameObject,true,true);
            count--;
            if(count == 0)
                gameObject = null;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            AssetManager.Instance.ReleaseAsset(gameObject,true,false);
            count--;
            if(count == 0)
                gameObject = null;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            uint crc = Crc32.GetCRC32("Attack.prefab" + "attack");
            AssetManager.Instance.ClearHalfOfGameObjectPool(crc);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            uint crc = Crc32.GetCRC32("Attack.prefab" + "attack");
            AssetManager.Instance.RemoveGameObjectPool(crc);
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            uint crc = Crc32.GetCRC32("Attack.prefab" + "attack");
            AssetManager.Instance.RemoveGameObjectPool(crc, true);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            AssetManager.Instance.PreloadGameObject("Attack.prefab", "attack",10);
        }
    }
}
