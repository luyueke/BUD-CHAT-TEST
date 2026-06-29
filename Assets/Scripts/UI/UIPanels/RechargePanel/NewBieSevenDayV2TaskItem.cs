using System;
using System.Collections.Generic;
using EventTracking;
using Game.Event;
using Game.Store;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewBieSevenDayV2TaskItem : MonoBehaviour
{
    public GameObject overLayout;
    public GameObject nextLayout;
    public Button goBtn;
    public Button signedBtn;
    public Text newGemCnt;
    private TaskItemData _taskItemData;
    private RewardItem _rewardItem;
    private string _taskId;
    private string spriteatlasPath = RechargePanel.RechargePanelAtlas;
    SevenDaySigneView _curView;
    private Action<TaskItemData> _claimAction;
    public int _signNumber;
    BasePanel _CloseView;
    private void Start()
    {
        if (goBtn != null)
        {
            goBtn.onClick.RemoveAllListeners();
            goBtn.onClick.AddListener(() =>
            {
                GoBtnClick();
            });
        }

        if (signedBtn != null)
        {
            signedBtn.onClick.RemoveAllListeners();
            signedBtn.onClick.AddListener(() =>
            {
                SignedBtnClick();
            });
        }

    }
    void SignInCallBack()
    {
        _claimAction.Invoke(_taskItemData);
    }
    private void SignedBtnClick()//补签
    {
        if (_signNumber < 2)//前两次免费
        {
            JObject req = new JObject()
            {
                ["productType"] = 15,
                ["productId"] = this._taskItemData.eventId.ToString(),
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyProductPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
            {
                // 补签成功后直接领取奖励，不依赖状态检查
                ClaimRewardDirectly();
                AccountDataManager.Inst.BalanceInfo.Refresh();
            }, (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, 0);
            });
            return;
        }
        _curView.SetData(_signNumber, this._taskItemData.eventId, () =>
        {
            // 补签成功后直接领取奖励
            ClaimRewardDirectly();
        });
        _curView.gameObject.SetActive(true);
    }

    /// <summary>
    /// 直接领取奖励，不依赖状态检查
    /// </summary>
    private void ClaimRewardDirectly()
    {
        if (_taskItemData == null)
        {
            return;
        }
        if (_taskItemData.eventId == 2)
        {
            var panel = UIManager.Inst.OpenPanel<NewbieV2OptionalRewardPanel>(PanelId.NewbieV2OptionalRewardPanel);
            panel.SetPreviewData(this._taskId, this._taskItemData.eventId, new List<string> { "10400486", "10400485" },
                _ =>
                {
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
                    ReddotManagerUtils.Inst.RefreshRedDot();
                    _claimAction.Invoke(_taskItemData);
                });
            panel.SetStatus(_taskItemData.eventStatus);
            return;
        }

        EventCenterDataManager.Inst.CliamReward(this._taskId, this._taskItemData.eventId, 1, 0, (claimRspData) =>
        {
            string key = "firstClickSevenDayClaim" + AccountDataManager.Inst.Uid;
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                LoadEvent.ReportPopupStatus("11", "guide_ID");
            }
            List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
            bool isCoin = false;
            List<CommonRewardItemData> items = new List<CommonRewardItemData>();
            foreach (var reward in rewardList)
            {
                CommonRewardItemData item;
                if (reward.rewardType == (int)BUDRewardType.RewardAvatarFrame)//头像框
                {
                    item = new CommonRewardItemData()
                    {
                        IconSp = UserUIWidgetManager.Inst.GetHeadCycleSp(27, this.gameObject),
                        rewardName = "萌新同学头像框"
                    };
                }
                else if (reward.rewardType == (int)BUDRewardType.RewardPgcResource)//PGC道具
                {
                    item = new CommonRewardItemData()
                    {
                        IconSp = PgcUtils.GetIconSpriteByPgcId("40100499", gameObject),
                        rewardName = "得意摇摆"
                    };
                }
                else //货币按正常处理方式
                {
                    item = new CommonRewardItemData
                    {
                        RewardAmount = reward.amount,
                        rewardType = reward.rewardType,
                        rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardType)
                    };
                    if (reward.rewardType == (int)BUDRewardType.RewardCoin)
                    {
                        MessageHelper.Broadcast(MessageName.GetCurrencyAnimation, GetComponent<RectTransform>(), CurrencyType.Coin, 6);
                        isCoin = true;
                    }
                }
                items.Add(item);
            }
            if (!isCoin || UIManager.Inst.FindPanel(PanelId.FittingRoomPanel) == null)
            {
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(items);
                panel.SetCloseAct(() => {
                    if (_taskItemData.eventId == 1)
                    {
                        MarketReviewManager.Inst.ShowMarketPointPanel();
                    }
                });
            }
            else{
                if (_taskItemData.eventId == 1)
                {
                    MarketReviewManager.Inst.ShowMarketPointPanel();
                }
            }
            AccountDataManager.Inst.BalanceInfo.Refresh();
            EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
            ReddotManagerUtils.Inst.RefreshRedDot();
            _claimAction.Invoke(_taskItemData);
        });
    }

    private void GoBtnClick()
    {
        if (_taskItemData == null)
        {
            return;
        }

        if (_taskItemData.eventStatus != (int)EventStatus.Claim)
        {
            return;
        }

        if (_taskItemData.eventId == 2)
        {
            var panel = UIManager.Inst.OpenPanel<NewbieV2OptionalRewardPanel>(PanelId.NewbieV2OptionalRewardPanel);
            panel.SetPreviewData(this._taskId, this._taskItemData.eventId, new List<string> { "10400486", "10400485" },
                _ =>
                {
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
                    ReddotManagerUtils.Inst.RefreshRedDot();
                    _claimAction.Invoke(_taskItemData);
                });
            return;
        }


        EventCenterDataManager.Inst.CliamReward(this._taskId, this._taskItemData.eventId, 1, 0, (claimRspData) =>
        {
            string key = "firstClickSevenDayClaim" + AccountDataManager.Inst.Uid;
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                LoadEvent.ReportPopupStatus("11", "guide_ID");
            }

            List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
            bool isCoin = false;
            List<CommonRewardItemData> items = new List<CommonRewardItemData>();
            foreach (var reward in rewardList)
            {
                CommonRewardItemData item;
                if (reward.rewardType == (int)BUDRewardType.RewardAvatarFrame)//头像框
                {
                    item = new CommonRewardItemData()
                    {
                        IconSp = UserUIWidgetManager.Inst.GetHeadCycleSp(27, this.gameObject),
                        rewardName = "萌新同学头像框"
                    };
                }
                else if (reward.rewardType == (int)BUDRewardType.RewardPgcResource)//PGC道具
                {
                    item = new CommonRewardItemData()
                    {
                        pgcId = "40100499",
                        rewardType = (int)BUDRewardType.RewardPgcResource,
                        //IconSp = PgcUtils.GetIconSpriteByPgcId("40100499", gameObject),
                        rewardName = "得意摇摆"
                    };
                }
                else //货币按正常处理方式
                {
                    item = new CommonRewardItemData
                    {
                        RewardAmount = reward.amount,
                        rewardType = reward.rewardType,
                        rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardType)
                    };
                    if (reward.rewardType == (int)BUDRewardType.RewardCoin)
                    {
                        MessageHelper.Broadcast(MessageName.GetCurrencyAnimation, GetComponent<RectTransform>(), CurrencyType.Coin, 6);
                        isCoin = true;
                    }
                }
                items.Add(item);
            }
            if (!isCoin || UIManager.Inst.FindPanel(PanelId.FittingRoomPanel) == null)
            {
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(items);
                panel.SetCloseAct(() =>
                {
                    if (_taskItemData.eventId == 1)
                    {
                        MarketReviewManager.Inst.ShowMarketPointPanel();
                    }
                });
            }
            else
            {
                if (_taskItemData.eventId == 1)
                {
                    MarketReviewManager.Inst.ShowMarketPointPanel();
                }
            }

            AccountDataManager.Inst.BalanceInfo.Refresh();
            EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
            ReddotManagerUtils.Inst.RefreshRedDot();
            _claimAction.Invoke(_taskItemData);
            //},()=> {
            //    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            //    List<CommonRewardItemData> items = new List<CommonRewardItemData>();
            //    CommonRewardItemData item;
            //    item = new CommonRewardItemData()
            //    {
            //        pgcId = "40100499",
            //        rewardType = (int)BUDRewardType.RewardPgcResource,
            //      //  IconSp = PgcUtils.GetIconSpriteByPgcId("40100499", gameObject),
            //        rewardName = "得意摇摆"
            //    };
            //    items.Add(item);
            //    panel.ShowRewards(items);
        });
    }


    public void SetData(SevenDaySigneView view, string taskId, TaskItemData taskItemData, Action<TaskItemData> claimAction, int signNumber, BasePanel CloseView, bool isNext = false)
    {
        this._signNumber = signNumber;
        this._claimAction = claimAction;
        _CloseView = CloseView;
        if (taskItemData == null)
        {
            return;
        }
        _curView = view;
        _taskId = taskId;
        _taskItemData = taskItemData;
        //_taskItemData.eventStatus = (int)EventStatus.Claim;
        setStatus(taskItemData.eventStatus);

        if (isNext)
        {
            nextLayout.transform.Find("Txt").GetComponent<Text>().SetText("明日可领");
        }
        else
        {
            nextLayout.transform.Find("Txt").GetComponent<Text>().SetText("待签到");
        }
    }

    void setStatus(int status)
    {

        switch (status)
        {
            case (int)EventStatus.UnClaim:
                overLayout.gameObject.SetActive(false);
                nextLayout.gameObject.SetActive(true);
                goBtn.gameObject.SetActive(false);
                signedBtn.gameObject.SetActive(false);
                break;
            case (int)EventStatus.Claim:
                overLayout.gameObject.SetActive(false);
                nextLayout.gameObject.SetActive(false);
                signedBtn.gameObject.SetActive(false);
                goBtn.gameObject.SetActive(true);
                goBtn.transform.Find("Txt").GetComponent<Text>().SetText("签到");
                goBtn.interactable = true;
                break;
            case (int)EventStatus.Finish:
                overLayout.gameObject.SetActive(true);
                nextLayout.gameObject.SetActive(false);
                signedBtn.gameObject.SetActive(false);
                goBtn.gameObject.SetActive(true);
                goBtn.transform.Find("Txt").GetComponent<Text>().SetText("已签到");
                goBtn.interactable = false;
                break;
            case (int)EventStatus.Signed:
                overLayout.gameObject.SetActive(false);
                nextLayout.gameObject.SetActive(false);
                signedBtn.gameObject.SetActive(true);
                goBtn.gameObject.SetActive(false);
                break;
        }
    }

    //预览效果：
    public void ShowCurrencyPreview(int currencyType)
    {
        UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel, (CurrencyType)currencyType);
    }
    public void ShowAnimaPreview()
    {
        var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        var icons = new List<string>
        {
            "pinktask_bg1", "pinktask_bg2",
        };
        panel.SetEventPreviewBg(new List<string> { "40100499" }, "得意摇摆", spriteatlasPath, "#a645f6", icons);
    }
    public void ShowAvatarPreview()
    {
        var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
        panel.PreviewAvatarFrame(AvatarFrameType.AvatarFrameS10NewBie);
    }
    public void ShowClorthPreview()
    {
        var panel = UIManager.Inst.OpenPanel<NewbieV2OptionalRewardPanel>(PanelId.NewbieV2OptionalRewardPanel);
        panel.SetPreviewData(this._taskId, this._taskItemData.eventId, new List<string> { "10400486", "10400485" },
            _ =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
                ReddotManagerUtils.Inst.RefreshRedDot();
                _claimAction.Invoke(_taskItemData);
            });
        panel.SetStatus(_taskItemData.eventStatus);
    }

}
