using System.Collections;
using System.Collections.Generic;
using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class ChristmasCoinConsumptionRebateView : ActivityBaseView
{
    [SerializeField] private Text Txt_CurrencyAmount;
    [SerializeField] private Text Txt_BottomTips;
    [Header("下方领奖Item")]
    [SerializeField] private CButton btn_GoSnowGash;
    [SerializeField] private CButton btn_GoChristmas;
    [SerializeField] private CButton btn_WinnerList;
    [SerializeField] private CButton btn_TipsBtn;
    [SerializeField] private CButton btn_CloseTipsBtn;
    [SerializeField] private GameObject Go_Tips;
    [SerializeField] private GameObject Go_WinnerList;
    [SerializeField] private S5ChristmasCoinWinnerView WinnerView;
    private ActivityInfo _info;

    public override void Init(ActivityInfo info) {
        base.Init(info);
        this._info = info;
        UpdateCurrency(_info.currencyAmount);
        AddListener();
        RefreshWinnerList(info);
    }

    private void RefreshWinnerList(ActivityInfo info)
    {
        if (info.christmasCoinRebatesInfo != null)
        {
            WinnerView.InitData(info.christmasCoinRebatesInfo);

            if (info.christmasCoinRebatesInfo.isPrizeDraw == 0)
            {
                Txt_BottomTips.text = "开奖后通过邮件发送相应的中奖数量";
            }
            else
            {
                Txt_BottomTips.text = "已开奖，请前往邮箱查收你的返利礼品哦！";
            }
        }
    }

    private void AddListener()
    {
        btn_GoSnowGash.onClick.AddListener(OnBtnSnowGashClick);
        btn_GoChristmas.onClick.AddListener(OnBtnChristmasGashClick);
        btn_WinnerList.onClick.AddListener(OnBtnWinnerListClick);
        btn_TipsBtn.onClick.AddListener(OnBtnTipsClick);
        btn_CloseTipsBtn.onClick.AddListener(OnBtnCloseTipsClick);
    }

    private void OnBtnSnowGashClick()
    {
        var activityCenterPanel = UIManager.Inst.FindPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel);
        if (activityCenterPanel != null)
        {
            activityCenterPanel.OnTabClick(ActivityId.ElfFrost.ToString());
        }
    }

    private void OnBtnChristmasGashClick()
    {
        if (BusinessLiveManager.Inst.IsGashaponLive("lottery.jingleBells"))
        {
            GashaponDataManager.Inst.JumpToGashapon("lottery.jingleBells");
        }
        else
        {
            TipPanel.ShowToast("扭蛋预告：圣诞响叮当扭蛋将于12月20日0点上线");
        }
    }

    private void OnBtnWinnerListClick()
    {
        Go_WinnerList.SetActive(true);
    }

    private void OnBtnTipsClick()
    {
        Go_Tips.SetActive(true);
    }

    private void OnBtnCloseTipsClick()
    {
        Go_Tips.SetActive(false);
    }
    
    public override void RefrashData(ActivityInfo info) {
        base.RefrashData(info);
        _info = info;
        UpdateCurrency(_info.currencyAmount);
        RefreshWinnerList(info);
    }
    
    private void UpdateCurrency(int currencyAmount)
    {
        Txt_CurrencyAmount.text = currencyAmount.ToString();
    }
}


