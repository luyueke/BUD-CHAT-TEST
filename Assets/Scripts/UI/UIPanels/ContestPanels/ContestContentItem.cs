using Com.TheFallenGames.OSA.Util.IO;
using System;
using Basic.Utils;
using GameData;
using UnityEngine;
using UnityEngine.UI;

public class ContestContentItem : MonoBehaviour
{
    public RemoteImageBehaviour banner;
    public GameObject selectGO;
    public Toggle itemTog;
    public Text titleTxt;
    public Action onSelectAction;

    public Text timeLeft;
    public ItemBgColor topColorItem;
    public RectTransform resizeWindow;

    public void SetItemInfo(ContestInfo info, Action onSelect = null)
    {
        onSelectAction = onSelect;
        titleTxt.text = info.contestName;
        timeLeft.text = ContestEventManager.GetLeftTimeStr(info);
        DataUtil.TryGetFromList(info.themeColorList, 1, out string color2);
        topColorItem.SetColor(color2);
        ContestEventManager.Inst.SetRawImage(banner, info.bannerUrl, OnLoadBanner);
        itemTog.onValueChanged.AddListener(OnTogSelect);
    }

    private void OnTogSelect(bool value)
    {
        if (value) onSelectAction?.Invoke();
    }
    
    private void OnLoadBanner(bool isSucc, bool fromCache)
    {
        if (this && resizeWindow)
        {
            banner.RawImage.FitTexture(resizeWindow.rect.size);
        }
    }
}
