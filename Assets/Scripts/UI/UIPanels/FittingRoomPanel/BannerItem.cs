using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using Game.Store;
using GameData;
using UnityEngine;
using UnityEngine.UI;

public class BannerItem : MonoBehaviour
{
    public RemoteImageBehaviour mainImg;
    public Button mainBtn;
    public Action onClickAction;
    public RectTransform resizeWindow;

    private ShapeBannerData curInfo;
    public ShapeBannerData ContestInfo => curInfo;

    public void SetItemInfo(ShapeBannerData info, Action onSelect = null)
    {
        curInfo = info;
        onClickAction = onSelect;
        string urlToLoad = info.banner_url;
        mainImg.Load(urlToLoad, onCompleted: OnLoadBanner);
        mainBtn.onClick.RemoveAllListeners();
        mainBtn.onClick.AddListener(OnBtnClick);
    }

    public void SetImgPool(IPool pool)
    {
        mainImg.InitializeWithPool(pool);
    }

    public void ChangeRawImageState(bool isRelease)
    {
        mainImg.ChangeRawImageState(isRelease);
    }

    private void OnBtnClick()
    {
        OnClickInternal();
        onClickAction?.Invoke();
    }

    protected virtual void OnClickInternal() { }

    protected virtual void OnLoadBanner(bool isSucc, bool fromCache)
    {
        if (this && resizeWindow)
        {
            mainImg.RawImage.FitTexture(resizeWindow.rect.size);
        }
    }
}
