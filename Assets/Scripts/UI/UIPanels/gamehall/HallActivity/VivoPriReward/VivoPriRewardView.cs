using System;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class VivoPriRewardView : BasePanel<VivoPriRewardView>
{
    public CButton btn_close;
    public CButton btn_vivo;
    public Text txt_btn;
    public Text text_tip;
    public GameObject[] maskGos;
    public Button[] previewButtons;

    private BudTimer _timer;
    private int _day = -1;


    protected override void Awake()
    {
        base.Awake();

        btn_close.onClick.AddListener(CloseSelf);
        btn_vivo.onClick.AddListener(OnVivo);
        MessageHelper.AddListener(MessageName.UpdateVivoPriState, RefreshUI);
        for (int i = 0; i < previewButtons.Length; i++)
        {
            int index = i;
            previewButtons[i].onClick.AddListener(() => OnPreviewClick(index));
        }
        CheckTime();
        _timer = TimerManager.Inst.Run("VivoPriRewardPackView", 0, 1, () =>
        {
            CheckTime();
        });
        RefreshUI();

    }

    void CheckTime()
    {
        DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        if (now.Day != _day)
        {
            _day = now.Day;
            VivoPriRewardPackMgr.Inst.GetVivoPriRewardInfo(); //跨天刷新状态
        }
    }
    public void OnPreviewClick(int index)
    {
        if (index == 0)
        {
            var data = new RewardPreviewInfo(BUDRewardType.RewardCoin, CurrencyType.Coin, "", "", "");
            PreviewManager.Inst.ShowPreview(data);
        }
        else if (index == 1)
        {
            var data = new RewardPreviewInfo(BUDRewardType.RewardBadge, CurrencyType.Badge, "", "", "");
            PreviewManager.Inst.ShowPreview(data);
        }
    }

    public void RefreshUI()
    {
        try
        {
            var budRewardStatus = VivoPriRewardPackMgr.Inst.GetBudRewardStatus();
            var isVivoStartFromGameCenter = VivoPriRewardPackMgr.Inst.IsVivoStartFromGameCenter;
            if (budRewardStatus == BudRewardStatus.Claimed)
            {
                maskGos[0].SetActive(true);
                maskGos[1].SetActive(true);
                txt_btn.text = "已领取";
                text_tip.text = "启动特权奖励已发放，请明日再来";
                return;
            }
            maskGos[0].SetActive(false);
            maskGos[1].SetActive(false);
            if (isVivoStartFromGameCenter)
            {
                txt_btn.text = "领取";
                text_tip.text = "请领取vivo游戏中心启动特权奖励";
            }
            else
            {
                txt_btn.text = "去启动";
                text_tip.text = "前往vivo游戏中心启动每日可以获得启动特权奖励";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("refresh ui error" + e.Message);
        }

    }

    void OnVivo()
    {
        if (!GlobalFuncExtensions.CheckCanClick())
        {
            return;
        }
        var budRewardStatus = VivoPriRewardPackMgr.Inst.GetBudRewardStatus();
        if (budRewardStatus == BudRewardStatus.Claimed)
        {
            return;
        }
        var isVivoStartFromGameCenter = VivoPriRewardPackMgr.Inst.IsVivoStartFromGameCenter;
        if (isVivoStartFromGameCenter)
        {
            VivoPriRewardPackMgr.Inst.ReceiveVivoPriReward();
        }
        else
        {
            VivoPriRewardPackMgr.Inst.JumpToVivoGameCenter();
        }
    }

    protected override void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.UpdateVivoPriState, RefreshUI);
        TimerManager.Inst.Stop(_timer);
        _timer = null;
        base.OnDestroy();
    }



}

