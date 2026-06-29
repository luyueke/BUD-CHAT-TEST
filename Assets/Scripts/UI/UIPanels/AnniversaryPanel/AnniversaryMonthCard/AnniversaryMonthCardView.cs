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


public class AnniversaryMonthCardView : ActivityBaseView
{

    [Header("UI相关")]


    public Button btn_monthCard_normal; //银卡
    public Button btn_monthCard_gold; //金卡
    public Text text_monthCard_normal; //银卡按钮文字
    public Text text_monthCard_gold; //金卡按钮文字


    public Button[] btns_preview_normal; //银卡预览
    public Button[] btns_preview_gold; //金卡预览


    public Text txt_timeDown;

    public GameObject normal_tick_go;
    public GameObject gold_tick_go;
    public Text txt_normal_tick;
    public Text txt_gold_tick;

    public Text txt_normal_reward_days;
    public Text txt_gold_reward_days;
    public CButton Btn_Rule;

    private BudTimer _timer;
    private int _hour = -1;
    public void Awake()
    {
        btn_monthCard_normal.onClick.AddListener(OnMonthCardNormalClicked);
        btn_monthCard_gold.onClick.AddListener(OnMonthCardGoldClicked);
        Btn_Rule.onClick.AddListener(OnRuleClicked);

        for (int i = 0; i < btns_preview_normal.Length; i++)
        {
            int index = i;
            btns_preview_normal[i].onClick.AddListener(() =>
            {
                OnMonthCardPreviewClicked(index);
            });
        }

        for (int i = 0; i < btns_preview_gold.Length; i++)
        {
            int index = i;
            btns_preview_gold[i].onClick.AddListener(() =>
            {
                OnMonthCardGoldPreviewClicked(index);
            });
        }


        AnniversaryMonthCardMgr.Inst.view = this;

        CheckTime();
        _timer = TimerManager.Inst.Run("AnniversaryMonthCardView", 0, 1, () =>
        {
            CheckTime();
        });
        RefreshUI();
    }

    void CheckTime()
    {
        DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        if (now < AnniversaryMonthCardMgr.ACTIVITY_END_TIME)
        {
            if (now.Hour != _hour)
            {
                var day = (AnniversaryMonthCardMgr.ACTIVITY_END_TIME - now).Duration().TotalDays;
                day = (int)day;
                var hour = (AnniversaryMonthCardMgr.ACTIVITY_END_TIME - now).Duration().Hours % 24;
                _hour = now.Hour;
                txt_timeDown.text = $"{day}天{(hour + 1)}小时";
                AnniversaryMonthCardMgr.Inst.GetMonthCardInfo(); //重新拉信息 确保领取刷新
            }
        }
        else
        {
            TimerManager.Inst.Stop(_timer);
            _timer = null;
            txt_timeDown.text = "活动已结束";
        }
    }

    void OnDestroy()
    {
        TimerManager.Inst.Stop(_timer);
        _timer = null;
    }


    public void RefreshUI()
    {
        var isDuringActivity = AnniversaryMonthCardMgr.Inst.IsDuringActivity();
        var subscribeStatusRsp = AnniversaryMonthCardMgr.Inst.subscribeStatusRsp;
        if (subscribeStatusRsp == null)
        {
            LoggerUtils.Log("AnniversaryMonthCardView RefreshUI: isDuringActivity: " + isDuringActivity + " subscribeStatusRsp is null: " + (subscribeStatusRsp == null));
            btn_monthCard_normal.gameObject.SetActive(false);
            btn_monthCard_gold.gameObject.SetActive(false);
            normal_tick_go.SetActive(false);
            gold_tick_go.SetActive(false);
            txt_normal_reward_days.text = "";
            txt_gold_reward_days.text = "";
            return;
        }
        normal_tick_go.SetActive(true);

        txt_normal_reward_days.text = "";
        if (AnniversaryMonthCardMgr.Inst.CheckHasReward(AnniversaryMonthCardMgr.MonthCardType.Silver))
        {
            if (int.Parse(subscribeStatusRsp.monthlyCard.basicCard.accumulationDays) > 1)
            {
                txt_normal_reward_days.text = $"已存{subscribeStatusRsp.monthlyCard.basicCard.accumulationDays}天奖励";
            }
            txt_normal_tick.text = subscribeStatusRsp.monthlyCard.basicCard.remainTime + "后到期";
            text_monthCard_normal.text = "领取";
        }
        else
        {
            if (AnniversaryMonthCardMgr.Inst.IsMonthCardActive(AnniversaryMonthCardMgr.MonthCardType.Silver))
            {
                txt_normal_tick.text = subscribeStatusRsp.monthlyCard.basicCard.remainTime + "后到期";
                text_monthCard_normal.text = "续期30元";
            }
            else
            {
                txt_normal_tick.text = "30天有效期";
                text_monthCard_normal.text = "30元";
            }
        }
        gold_tick_go.SetActive(true);

        txt_gold_reward_days.text = "";
        if (AnniversaryMonthCardMgr.Inst.CheckHasReward(AnniversaryMonthCardMgr.MonthCardType.Gold))
        {
            if (int.Parse(subscribeStatusRsp.monthlyCard.premiumCard.accumulationDays) > 1)
            {
                txt_gold_reward_days.text = $"已存{subscribeStatusRsp.monthlyCard.premiumCard.accumulationDays}天奖励";
            }
            txt_gold_tick.text = subscribeStatusRsp.monthlyCard.premiumCard.remainTime + "后到期";
            text_monthCard_gold.text = "领取";
        }
        else
        {
            if (AnniversaryMonthCardMgr.Inst.IsMonthCardActive(AnniversaryMonthCardMgr.MonthCardType.Gold))
            {
                txt_gold_tick.text = subscribeStatusRsp.monthlyCard.premiumCard.remainTime + "后到期";
                text_monthCard_gold.text = "续期60元";
            }
            else
            {
                txt_gold_tick.text = "30天有效期";
                text_monthCard_gold.text = "60元";
            }
        }
    }

    void OnMonthCardNormalClicked()
    {
        if (!GlobalFuncExtensions.CheckCanClick())
        {
            return;
        }
        var bReward = AnniversaryMonthCardMgr.Inst.CheckHasReward(AnniversaryMonthCardMgr.MonthCardType.Silver);
        if (bReward)
        {
            //领取奖励
            AnniversaryMonthCardMgr.Inst.ClaimMonthCardReward(SubscribeRewardReqType.silverMonthCardReward);
        }
        else
        {
            //购买 or 续订
            AnniversaryMonthCardMgr.Inst.ContinueVip(SubscribeVipType.MonthCard, AnniversaryMonthCardMgr.MonthCardType.Silver);
        }
    }

    void OnMonthCardGoldClicked()
    {
        if (!GlobalFuncExtensions.CheckCanClick())
        {
            return;
        }
        var bReward = AnniversaryMonthCardMgr.Inst.CheckHasReward(AnniversaryMonthCardMgr.MonthCardType.Gold);
        if (bReward)
        {
            //领取奖励
            AnniversaryMonthCardMgr.Inst.ClaimMonthCardReward(SubscribeRewardReqType.goldMonthCardReward);
        }
        else
        {
            //购买 or 续订
            AnniversaryMonthCardMgr.Inst.ContinueVip(SubscribeVipType.MonthCard, AnniversaryMonthCardMgr.MonthCardType.Gold);
        }
    }

    private void OnRuleClicked()
    {
        //todo
        UIManager.Inst.OpenPanel<ActivityRulePanel>(PanelId.ActivityRulePanel, "Assets/Loadable/UI/UIPanel/AnniversaryMonthCardView/Rule.json");
    }

    void OnMonthCardPreviewClicked(int index)
    {
        //预览奖励
        var info = AnniversaryMonthCardMgr.Inst.RewardTypeList[AnniversaryMonthCardMgr.MonthCardType.Silver][index];
        PreviewManager.Inst.ShowPreview(info);
    }


    void OnMonthCardGoldPreviewClicked(int index)
    {
        //预览奖励
        var info = AnniversaryMonthCardMgr.Inst.RewardTypeList[AnniversaryMonthCardMgr.MonthCardType.Gold][index];
        PreviewManager.Inst.ShowPreview(info);
    }


}
