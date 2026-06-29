using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using Network.Http;
using Basic.Utils;
using EventTracking;
using System;
using Game.Event;
using UI.Base;
using Network.Message;
using Game.Store;


public class AnniversaryLuckyKoiView : ActivityBaseView
{

    [Header("UI相关")]


    public Button btn_once; //单抽
    public Button btn_ten; //十连抽
    public Button btn_help; //帮助
    public Button btn_preview; //预览

    public Text txt_had_lottery_count;//已抽次数
    public Text txt_consume_count;//消费商品币数量

    public Text txt_ticket_count;//券数量
    public GameObject[] doneGos;
    public Button tipsBtn;

    public Button[] btn_preview_list;


    string RulePath = "Assets/Loadable/UI/UIPanel/AnniversaryLuckyKoiView/Rule.json";
    private BudTimer _timer;
    private int _hour = -1;
    public void Awake()
    {
        btn_once.onClick.AddListener(OnOnceClicked);
        btn_ten.onClick.AddListener(OnTenClicked);
        btn_help.onClick.AddListener(OnHelpClicked);
        btn_preview.onClick.AddListener(() => { OnPreviewClicked(); });
        tipsBtn.onClick.AddListener(OnTipsClicked);

        for (int i = 0; i < btn_preview_list.Length; i++)
        {
            int index = i;
            btn_preview_list[i].onClick.AddListener(() => PreviewReward(index));
        }

        AnniversaryLuckyKoiMgr.Inst.view = this;

        AnniversaryLuckyKoiMgr.Inst.GetInfoGashaponInfo();

        // CheckTime();
        // _timer = TimerManager.Inst.Run("AnniversaryLuckyKoiView", 0, 1, () =>
        // {
        //     CheckTime();
        // });
        RefreshUI();
    }

    void CheckTime()
    {
        // DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        // if (now < AnniversaryLuckyKoiMgr.ACTIVITY_END_TIME)
        // {
        //     if (now.Hour != _hour)
        //     {
        //         var day = (AnniversaryLuckyKoiMgr.ACTIVITY_END_TIME - now).Duration().TotalDays;
        //         day = (int)day;
        //         var hour = (AnniversaryLuckyKoiMgr.ACTIVITY_END_TIME - now).Duration().Hours % 24;
        //         _hour = now.Hour;
        //         txt_timeDown.text = $"{day}天{(hour + 1)}小时";
        //         AnniversaryMonthCardMgr.Inst.GetMonthCardInfo(); //重新拉信息 确保领取刷新
        //     }
        // }
        // else
        // {
        //     TimerManager.Inst.Stop(_timer);
        //     _timer = null;
        //     txt_timeDown.text = "活动已结束";
        // }
    }

    void OnDestroy()
    {
        TimerManager.Inst.Stop(_timer);
        _timer = null;
    }

    public void RefreshUI()
    {
        var isDuringActivity = AnniversaryLuckyKoiMgr.Inst.IsDuringActivity();
        var gashaponInfoRsp = AnniversaryLuckyKoiMgr.Inst.gashaponInfoRsp;
        if (!isDuringActivity || gashaponInfoRsp == null)
        {
            LoggerUtils.Log("AnniversaryLuckyKoiView RefreshUI: isDuringActivity: " + isDuringActivity + " gashaponInfoRsp is null: " + (gashaponInfoRsp == null));
            txt_had_lottery_count.text = "";
            txt_consume_count.text = "";
            txt_ticket_count.text = "";
            foreach (var go in doneGos)
            {
                go?.SetActive(false);
            }
            return;
        }
        int idx = 2; //只有2-4有状态
        for (int i = 0; i < doneGos.Length; i++)
        {
            var go = doneGos[i];
            go.SetActive(AnniversaryLuckyKoiMgr.Inst.CheckHadEarnReward(idx));
            idx++;
        }
        txt_had_lottery_count.text = AnniversaryLuckyKoiMgr.Inst.GetHadLotteryCount().ToString();
        txt_consume_count.text = AnniversaryLuckyKoiMgr.Inst.GetConsumeCount().ToString();
        txt_ticket_count.text = AnniversaryLuckyKoiMgr.Inst.GetTicketCount().ToString();
    }

    void OnOnceClicked()
    {
        if (!GlobalFuncExtensions.CheckCanClick())
        {
            return;
        }
        AnniversaryLuckyKoiMgr.Inst.SendGashaponRequestOnce();
    }

    void OnTenClicked()
    {
        if (!GlobalFuncExtensions.CheckCanClick())
        {
            return;
        }
        AnniversaryLuckyKoiMgr.Inst.SendGashaponRequestTenTimes();
    }

    void OnTipsClicked()
    {
        UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.KoiGachaCoin);
    }

    void OnPreviewClicked(string bundleId = "")
    {
        //预览奖励
        // var info = AnniversaryMonthCardMgr.Inst.RewardTypeList[AnniversaryMonthCardMgr.MonthCardType.Silver][index];
        // PreviewManager.Inst.ShowPreview(info);
        var gashaponData = AnniversaryLuckyKoiMgr.Inst.gashaponData;
        var gashaponInfoRsp = AnniversaryLuckyKoiMgr.Inst.gashaponInfoRsp;
        if (gashaponData == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }
        var contain = gashaponData.RewardList.Any(x => x.RewardId == -99999);
        // gashaponData.RewardList.RemoveAll(x => x.RewardId == -99999);
        if (!contain)
        {
            //添加社区商品币  后端说奖池不能配  产品说要预览展示。预览又是通用的，只好构造这个结构,纯展示用
            gashaponData.RewardList.Insert(0, new GashaponRewardData()
            {
                RewardId = -99999,
                BundleId = "",
                RewardType = Product.RewardType.RewardCommunityCoin,
                Level = Product.Level.S,
                Num = 888,
                Name = "社区商品币",
                iconSprite = null,
            });
        }
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            title = gashaponData.Name,
            gashaponData = gashaponData,
            gashaponInfoRsp = gashaponInfoRsp,
            rewardCurrency = CurrencyType.KoiGachaCoin,
            rulePath = RulePath
        });
        previewPanel.SetBundleViewBgClolr("#6453BB");
        if (!string.IsNullOrEmpty(bundleId))
        {
            previewPanel.Turn2Preview(bundleId);
        }
    }


    void OnHelpClicked()
    {
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, RulePath);
    }

    void PreviewReward(int index)
    {
        var key = index + 1;
        if (key == 3)
        {
            //皮奥套装
            OnPreviewClicked("101");
        }
        else if (key == 4)
        {
            //卡斯帕套装
            OnPreviewClicked("99");
        }
        else
        {
            // var info = AnniversaryLuckyKoiMgr.Inst.RewardTypeList[key];
            // PreviewManager.Inst.ShowPreview(info);
            OnPreviewClicked();
        }
    }
}
