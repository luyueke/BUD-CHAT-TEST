using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using System;
using UnityEngine;
using UnityEngine.UI;
using GameData;

public class ActiveContestItem : MonoBehaviour
{
    public RemoteImageBehaviour mainImg;
    public Button mainBtn;
    public Text titleTxt;
    public Text timeLeft;
    public ItemBgColor topColorItem;
    public Action onClickAction;
    public RectTransform resizeWindow;

    private ContestInfo curInfo;
    public ContestInfo ContestInfo => curInfo;
    
    public void SetItemInfo(ContestInfo info, Action onSelect = null, bool useStreamer = false)
    {
        curInfo = info;
        onClickAction = onSelect;
        titleTxt.SetText(info.contestName);
        timeLeft.SetText(ContestEventManager.GetLeftTimeStr(info));
        string urlToLoad = useStreamer ? info.streamerUrl : info.bannerUrl;
        mainImg.Load(urlToLoad, onCompleted:OnLoadBanner);
        mainBtn.onClick.RemoveAllListeners();
        mainBtn.onClick.AddListener(OnBtnClick);

        DataUtil.TryGetFromList(info.themeColorList, 1, out string color2);
        
        topColorItem.SetColor(color2);
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
        if(this && resizeWindow)
        {
            mainImg.RawImage.FitTexture(resizeWindow.rect.size);
        }
    }
}
