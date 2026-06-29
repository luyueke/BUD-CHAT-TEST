using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using GameData;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ContestPropItemView : ContestBaseItemView
{
    [SerializeField] private CButton activeBtn;
    [SerializeField] private Text likeNum;
    [SerializeField] private Text priceNum;
    [SerializeField] private GameObject rankObj;
    [SerializeField] private Text rankNum;
    [SerializeField] private ItemBgColor bgColor;
    [SerializeField] private ItemBgColor themeColor1;
    [SerializeField] private ItemBgColor themeColor2;
    [SerializeField] private GameObject coverMask;
    [SerializeField] private GameObject priceTag;

    private ContestEntryInfo activeData;
    private Action<ContestEntryInfo> selectAction;
    
    public override void Init(ContestEntryInfo data, Action<ContestEntryInfo> onSelect)
    {
        activeData = data;
        selectAction = onSelect;
        
        activeBtn.onClick.RemoveAllListeners();
        activeBtn.onClick.AddListener(OnDraftsBtnClick);
        UpdateUI(data);
    }
    public override void UpdateRankState(bool isHide)
    {
        rankObj.SetActive(!isHide);
    }


    public override void HidePrice(bool isHide)
    {
        priceTag.SetActive(!isHide);
        coverMask.SetActive(!isHide);
    }


    private void UpdateUI(ContestEntryInfo data)
    {
        if (data == null)
        {
            return;
        }
        var likeValue = data.scoreInfo?.score ?? 0;
        
        var rankValue = data.scoreInfo?.rank ?? 0;
        rankObj.SetActive(rankValue > 0);
        rankNum.text = rankValue.ToString();
        
        likeNum.text = GameUtils.ToBudCommonNumString(likeValue);

        var priceValue = data.creationInfo?.price ?? 0;
        priceNum.text = priceValue.ToString();

        var bgColorValue = data.creationInfo?.bgColor;
        if (!string.IsNullOrEmpty(bgColorValue))
        {
            bgColor?.SetColor(bgColorValue);
        }
        
        var themeColorValue = data.creationInfo?.themeColor;
        if (!string.IsNullOrEmpty(themeColorValue))
        {
            themeColor1?.SetColor(themeColorValue);
            themeColor2?.SetColor(themeColorValue);
        }
    }
    
    private void OnDraftsBtnClick()
    {
        if (activeData == null)
        {
            return;
        }
        selectAction?.Invoke(activeData);
    }

}
