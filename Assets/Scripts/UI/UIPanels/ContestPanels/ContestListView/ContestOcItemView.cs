using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using GameData;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ContestOcItemView : ContestBaseItemView
{
    [SerializeField] private CButton activeBtn;
    [SerializeField] private Text likeNum;
    [SerializeField] private GameObject rankObj;
    [SerializeField] private Text rankNum;
    [SerializeField] private ItemBgColor bgColor;
    [SerializeField] private ItemBgColor themeColor;

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

        likeNum.text = $"{GameUtils.ToBudCommonNumString(likeValue)} {LocalizationManager.Inst.GetLocalizedText("票")}";

        var bgColorValue = data.creationInfo?.bgColor;
        if (!string.IsNullOrEmpty(bgColorValue))
        {
            bgColor?.SetColor(bgColorValue);
        }

        var themeColorValue = data.creationInfo?.themeColor;
        if (!string.IsNullOrEmpty(themeColorValue))
        {
            themeColor?.SetColor(themeColorValue);
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
