using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Store;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class ConsumeEventView : ActivityBaseView
{
    public GameObject Go_LoadingNode;
    [Header("左边")]
    public GameObject Go_Reddot;
    public CButton Btn_Ticket;
    public Text Txt_TicketCount;
    public Text Txt_ExchangeCount;
    public Text Txt_LotteryCount;
    public Text Txt_ConsumeGemCount;
    public Text Txt_ConsumePCoinCount;
    public Text Txt_EventLeftTime;
    [Header("右边")]
    public CButton Btn_Preview;
    public CButton Btn_Twist_1;
    public CButton Btn_Twist_10;
    public Transform PreviewRewardContent;

    private ActivityInfo _info;
    private GashaponType _curGashaponType = GashaponType.ConsumeEvent;
    private string _curGashaponId = "";
    private const int OneTimeGasha = 1;
    private const int TenTimeGasha = 10;
    private List<ConsumeEventRewardItem> _eventRewardItems = new List<ConsumeEventRewardItem>();

    public override void Init(ActivityInfo info)
    {
        base.Init(info);
        this._info = info;
        _curGashaponId = GashaponDataManager.Inst.GetGashaponViewCfg(_curGashaponType)?.GashaId;

        InitUI();
        Go_LoadingNode.SetActive(true);
    }

    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        _info = info;

        var consumeCarnivalInfo = info.consumeCarnivalInfo;
        Txt_TicketCount.text = consumeCarnivalInfo.currentVoucher.ToString();
        Txt_ExchangeCount.text = consumeCarnivalInfo.totalVoucher.ToString();
        Txt_LotteryCount.text = consumeCarnivalInfo.usedVoucher.ToString();
        Txt_ConsumeGemCount.text = consumeCarnivalInfo.gemAmount.ToString();
        Txt_ConsumePCoinCount.text = consumeCarnivalInfo.communityCoinAmount.ToString();
        if (!string.IsNullOrEmpty(info.leftTime))
        {
            Txt_EventLeftTime.text = "距活动结束还有：" + info.leftTime;
        }

        Go_LoadingNode.SetActive(false);

        Go_Reddot.gameObject.SetActive(consumeCarnivalInfo.reddot == 1);

        RefreshOwnedState();
    }

    private void RefreshOwnedState()
    {
        if (_eventRewardItems == null || _eventRewardItems.Count == 0)
        {
            _eventRewardItems = PreviewRewardContent.GetComponentsInChildren<ConsumeEventRewardItem>().ToList();
        }
        _eventRewardItems.ForEach(x =>
        {
            x.RefreshOwnedState();
        });
    }

    private void InitUI()
    {

        Btn_Preview.onClick.RemoveAllListeners();
        Btn_Twist_1.onClick.RemoveAllListeners();
        Btn_Twist_10.onClick.RemoveAllListeners();

        Btn_Preview.onClick.AddListener(OnBtnPreviewClick);
        Btn_Twist_1.onClick.AddListener(OnBtnTwistOnce);
        Btn_Twist_10.onClick.AddListener(OnBtnTwistTenTimes);

        var previewBtns = PreviewRewardContent.GetComponentsInChildren<CButton>().ToList();
        previewBtns.ForEach(x =>
        {
            x.onClick.RemoveAllListeners();
            x.onClick.AddListener(OnBtnPreviewClick);
        });
        Btn_Ticket.onClick.RemoveAllListeners();
        Btn_Ticket.onClick.AddListener(OnBtnPreviewClick);
    }

    public void OnBtnPreviewClick()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(_curGashaponId);
        var gashaponData = GashaponDataManager.Inst.gashaponData(_curGashaponId);
        UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam() {
            gashaponData = gashaponData,
            rewardCurrency = gashaponData.CurrencyType,
            title = gashaponData.Name,
        });
    }

    private void OnBtnTwistOnce()
    {
        if (_info.consumeCarnivalInfo.currentVoucher - OneTimeGasha < 0)
        {
            TipPanel.ShowToast("当前抽奖券余额不足");
            return;
        }
        GashaponDataManager.Inst.RequestGashaponTask(_info.activityId, OneTimeGasha, OnGashaOnceRsp);
    }

    private void OnBtnTwistTenTimes()
    {
        if (_info.consumeCarnivalInfo.currentVoucher - TenTimeGasha < 0)
        {
            TipPanel.ShowToast("当前抽奖券余额不足");
            return;
        }
        GashaponDataManager.Inst.RequestGashaponTask(_info.activityId, TenTimeGasha, OnGashaTenRsp);
    }

    protected void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        Go_Reddot.SetActive(false);

        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
            new GashaponTwistAnimParam() {
                gashaponId = _curGashaponId
            });
        panel.PlayOneTwistAnimation(gashaponRsp.rewardList, null);
        RefreshOwnedState();
    }

    protected void OnGashaTenRsp(GashaponRsp gashaponRsp)
    {
        Go_Reddot.SetActive(false);

        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
            new GashaponTwistAnimParam() {
                gashaponId = _curGashaponId
            });
        panel.PlayTenTwistAnimation(gashaponRsp.rewardList, null);
    }
}
