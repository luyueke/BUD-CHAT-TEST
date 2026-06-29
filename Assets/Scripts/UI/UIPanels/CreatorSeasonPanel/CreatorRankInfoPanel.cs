using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CreatorRankInfoPanel : BasePanel<CreatorRankInfoPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private Text TitleText;
    [SerializeField] private CButton OverRallRankBtn;
    [SerializeField] private GameObject OverRallRankSelect;
    [SerializeField] private CButton AllTypeRankBtn;
    [SerializeField] private GameObject AllTypeRankSelect;
    [SerializeField] private GameObject PullDownInfo;
    [SerializeField] private List<Toggle> RankItems;
    [SerializeField] private List<RankUserInfoItem> RankUserInfoItems;
    [SerializeField] private Image BadgeImg;
    [SerializeField] private Text SelfScore;
    [SerializeField] private SuperTextMesh NameText;
    [SerializeField] private Text SelfRanking;
    [SerializeField] private HeadViewWidget HeadInfo;
    [SerializeField] private CButton RankTypeBtn;
    [SerializeField] private Text RankTypeText;
    [SerializeField] private GameObject RankTypeSelect;
    [SerializeField] private List<Toggle> RankTypeItems;

    private int _curSelectRankIndex = 0;
    private int _curSelectRankTypeIndex = 0;

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        OverRallRankBtn.onClick.AddListener(OnOverRallRankBtnClick);
        AllTypeRankBtn.onClick.AddListener(OnAllTypeRankBtnClick);
        for(int i = 0; i < RankItems.Count; i++)
        {
            int index = i;
            RankItems[i].onValueChanged.AddListener((isOn) => OnRankItemValueChanged(isOn, index));
        }
        for(int i = 0; i < RankTypeItems.Count; i++)
        {
            int index = i;
            RankTypeItems[i].onValueChanged.AddListener((isOn) => OnRankTypeItemValueChanged(isOn, index));
        }
        RankTypeBtn.onClick.AddListener(OnRankTypeBtnClick);
    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        LoadLocalAndApplyToUI();
    }
    private void LoadLocalAndApplyToUI()
    {
        _curSelectRankIndex = 0;
        _curSelectRankTypeIndex = 0;
        OverRallRankSelect.SetActive(true);
        AllTypeRankSelect.SetActive(false);
        PullDownInfo.SetActive(false);
        RankItems[_curSelectRankIndex].SetIsOnWithoutNotify(true);
        RankTypeItems[_curSelectRankTypeIndex].SetIsOnWithoutNotify(true);
        RefreshBySeverInfo();
    }

    private void OnRankItemValueChanged(bool isOn, int index)
    {
        if(isOn)
        {
            _curSelectRankIndex = index + 1;
            RefreshBySeverInfo();
        }
    }
    private void OnRankTypeItemValueChanged(bool isOn, int index)
    {
        if(isOn)
        {
            _curSelectRankTypeIndex = index;
            var str = "";
            switch(index)
            {
                case 0:
                    str = "全服实时";
                    break;
                case 1:
                    str = "全服上周";
                    break;
                case 2:
                    str = "好友实时";
                    break;
            }
            RankTypeText.text = str;
            RankTypeSelect.SetActive(false);
            RefreshBySeverInfo();
        }
    }
    private void OnRankTypeBtnClick()
    {
        RankTypeSelect.SetActive(!RankTypeSelect.activeSelf);
    }
    private void OnOverRallRankBtnClick()
    {
        _curSelectRankIndex = 0;
        OverRallRankSelect.SetActive(true);
        AllTypeRankSelect.SetActive(false);
        PullDownInfo.SetActive(false);
        RefreshBySeverInfo();
    }
    private void OnAllTypeRankBtnClick()
    {
        OverRallRankSelect.SetActive(false);
        AllTypeRankSelect.SetActive(true);
        PullDownInfo.SetActive(!PullDownInfo.activeSelf);
        RankTypeItems[_curSelectRankTypeIndex].SetIsOnWithoutNotify(true);
        if(_curSelectRankIndex == 0)
        {
            _curSelectRankIndex = 1;
            RefreshBySeverInfo();
        }
    }

    /// <summary>
    /// 切换榜单类型/时间维度后、新数据返回前清空展示，避免仍显示上一类型的头像、昵称与分数等。
    /// </summary>
    private void ClearRankUiBeforeRefresh()
    {
        if (RankUserInfoItems != null)
        {
            foreach (var item in RankUserInfoItems)
            {
                if (item != null)
                {
                    item.SetData(null);
                }
            }
        }

        if (SelfScore != null)
        {
            SelfScore.text = string.Empty;
        }

        if (NameText != null)
        {
            NameText.SetText(string.Empty);
        }

        if (SelfRanking != null)
        {
            SelfRanking.text = string.Empty;
        }

        if (HeadInfo != null)
        {
            HeadInfo.Remote_HeadImg?.ResetRawImage();
            if (HeadInfo.Img_HeadCycle != null)
            {
                HeadInfo.Img_HeadCycle.gameObject.SetActive(false);
            }

            HeadInfo.Trans_BottomEffect?.ClearChildren();
            HeadInfo.Trans_TopEffect?.ClearChildren();
        }
    }

    private void RefreshBySeverInfo()
    {
        ClearRankUiBeforeRefresh();

        CreatorRequestCtrl.Inst.RequestScoreRankInfo(_curSelectRankTypeIndex, (int)_curSelectRankIndex,"s15-0", (res) =>
        {
            if (res.list == null || res.list.Count == 0)
            {
                return;
            }
            for (int i = 0; i < RankUserInfoItems.Count; i++)
            {
                if (i < res.list.Count)
                {
                    RankUserInfoItems[i].SetData(res.list[i]);
                }
                else
                {
                    RankUserInfoItems[i].SetData(null);
                }
            }
            //刷新自己的信息
            RefreshSelfInfo(res.userRankData);
        }, (error) =>
        {
            Debug.LogError("RequestScoreRankInfo error: " + error);
        });
        ApplySelectTypeIcon();
    }

    private string GetSelectTypeTitleText()
    {
        return _curSelectRankIndex switch
        {
            0 => "总榜创作排行榜",
            1 => "2D皮肤创作排行榜",
            2 => "3D皮肤创作排行榜",
            3 => "动作创作排行榜",
            4 => "地图创作排行榜",
            5 => "工具创作排行榜",
            _ => "总榜创作排行榜",
        };
    }

    private void ApplySelectTypeIcon()
    {
        if (BadgeImg == null) return;

        var spriteName = string.Format("rankTypeImg{0}", _curSelectRankIndex);
        const string atlasPath = "Assets/Loadable/UI/UIPanel/CreatorSeasonPanel/CreatorSeasonPanel.spriteatlas";
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, gameObject);

        BadgeImg.sprite = sprite;
        BadgeImg.enabled = sprite != null;
        TitleText.text = GetSelectTypeTitleText();
    }

    private void RefreshSelfInfo(UserRankData userRankData)
    {
        SelfScore.text = userRankData.userRank.score.ToString();
        NameText.SetText(AccountDataManager.Inst.UserInfo.nickname);
        SelfRanking.text = userRankData.userRank.rank == 0 ? "未上榜" : string.Format("第{0}名", userRankData.userRank.rank);
        if (HeadInfo != null && AccountDataManager.Inst != null && AccountDataManager.Inst.UserInfo != null)
        {
            HeadInfo.InitHeadCycle(AccountDataManager.Inst.UserInfo);
        }
    }
}
