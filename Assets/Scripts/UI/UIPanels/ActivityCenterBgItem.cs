using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ActivityCenterBgItem : MonoBehaviour
{
    [SerializeField] private ColorBgPanel colorBgPanel;
    [SerializeField] private Image bgImg;
    [SerializeField] private RawImage customBgImg;

    private Sprite tmpSprite;

    public void InitCustomBgItem(string bgColor, string atlasPath, List<string> bgSpriteIds)
    {
        bgImg.color = DataUtil.DeSerializeColorCheckHash(bgColor);
        if (customBgImg != null) {
            customBgImg.gameObject.SetActive(false);
        }
        bgImg.gameObject.SetActive(true);
        if (bgSpriteIds == null || bgSpriteIds.Count == 0 || string.IsNullOrEmpty(atlasPath))
        {
            return;
        }

        var collected = new List<Sprite>(bgSpriteIds.Count);
        for (int i = 0; i < bgSpriteIds.Count; i++)
        {
            string iconName = bgSpriteIds[i];
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, gameObject);
            if (sprite != null)
            {
                collected.Add(sprite);
            }
        }
        colorBgPanel.sprites = collected.ToArray();
        colorBgPanel.RefreshSprite();
    }


    List<Sprite> textureCollect;
    int loadIndex = 0;
    int loadCount = 0;
    public void InitCustomBgItemForUrl(string bgColor, List<string> bgSpriteIds)
    {
        bgImg.color = DataUtil.DeSerializeColorCheckHash(bgColor);
        if (customBgImg != null)
        {
            customBgImg.gameObject.SetActive(false);
        }
        bgImg.gameObject.SetActive(true);
        if (bgSpriteIds == null || bgSpriteIds.Count == 0)
        {
            return;
        }
        loadIndex = 0;
        loadCount = bgSpriteIds.Count;
        textureCollect = new List<Sprite>(bgSpriteIds.Count);
        for(int i = 0; i < bgSpriteIds.Count; i++)
        {
            StartCoroutine(LoadImage(bgSpriteIds[i]));
        }
    }

    private IEnumerator LoadImage(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        if(www.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            Sprite sprite = Sprite.Create(texture,new Rect(0f,0f,texture.width,texture.height),new Vector2(0.5f,0.5f));
            textureCollect.Add(sprite);
            loadIndex++;
            if(loadIndex == loadCount)
            {
                colorBgPanel.sprites = textureCollect.ToArray();
                colorBgPanel.InitData();
                colorBgPanel.RefreshSprite();
            }
        }
    }

    public void SetRoration(Vec3 roration)
    {
        colorBgPanel?.SetRoration(roration);
    }

    public void SetBgImageVisible(bool isVisible)
    {
        bgImg.gameObject.SetActive(isVisible);
    }

    public void SetImagesColor(Color color)
    {
        colorBgPanel.SetImagesColor(color);
    }


    public void InitCustomBg(string atlasPath, string bgPath) {
        if (bgImg != null) {
            bgImg.gameObject.SetActive(false);
        }
        customBgImg.gameObject.SetActive(true);
        customBgImg.texture = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, bgPath, gameObject).texture;
        customBgImg.GetComponent<UIBGSizeToFit>().Resize();
    }

    public void InitCustomTextureBg(string bgPath) {
        if (bgImg != null) {
            bgImg.gameObject.SetActive(false);
        }

        if (tmpSprite != null) {
            Destroy(tmpSprite);
            tmpSprite = null;
        }
        customBgImg.gameObject.SetActive(true);
        var tmpTexture = XAssetLoaderMgr.Inst.LoadResource<Texture2D>(bgPath, gameObject);
        customBgImg.texture = tmpTexture;
        customBgImg.GetComponent<UIBGSizeToFit>().Resize();
    }

    public void InitCustomTextureBg(Texture2D tmpTexture)
    {
        if (bgImg != null)
        {
            bgImg.gameObject.SetActive(false);
        }

        if (tmpSprite != null)
        {
            Destroy(tmpSprite);
            tmpSprite = null;
        }
        customBgImg.gameObject.SetActive(true);
        customBgImg.texture = tmpTexture;
        customBgImg.GetComponent<UIBGSizeToFit>().Resize();
    }

    private void OnDestroy() {
        if (tmpSprite != null) {
            Destroy(tmpSprite);
            tmpSprite = null;
        }
    }
}
