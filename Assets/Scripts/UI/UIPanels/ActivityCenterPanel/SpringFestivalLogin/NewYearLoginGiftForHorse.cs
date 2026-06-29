using Es;
using Game.Avatar;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewYearLoginGiftForHorse : ActivityBaseView
{
    [SerializeField] private RawImage bgImage;
    [SerializeField] private Button previewBtn;
    [SerializeField] private CButton ruleBtn;
    [SerializeField] private List<NewYearLoginGiftItem> items;
    [SerializeField] private Image pgcIcon;
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private Transform characterRoot;
    private ActivityInfo mActivityInfo;

    private void Awake()
    {
        pgcIcon.sprite = PgcUtils.GetIconSpriteByPgcId("40200521", gameObject);
    }

    private void InitAvatar()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);

        var pgcId = rewards[0][0].pgcId;
        var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
        var config = DataTables.GetAvatarCommonData(pgcId);
        var classType = UniqueType.GetAvatar(pgcId);
        characterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType), pgcId);
        characterWrapper.ChangeColor(classType, config.defaultColor);
        characterWrapper.Move(classType, config.pDef);
        characterWrapper.Rotate(classType, config.rDef);
        characterWrapper.Scale(classType, config.sDef);
        characterWrapper.HVScale(classType, config.vhSDef);
        characterWrapper.SetLeftOrRight(classType, config.leftRightType);

        characterWrapper.SetParent(characterRoot, true);
        avatarCameraController.RotateTarget = characterRoot;
        avatarCameraController.isMoveEnabled = false;
        avatarCameraController.isZoomEnabled = false;
    }

    private List<List<CommonRewardItemData>> rewards = new List<List<CommonRewardItemData>>()
    {
        new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                rewardType = (int)BUDRewardType.RewardPgcResource,
                pgcId = "40200521",
                rewardName = "趴下睡觉",
                RewardAmount = 1,
            },
            new CommonRewardItemData()
            {
                rewardType = (int)BUDRewardType.RewardYouYouCoin,
                rewardName = "优优币",
                RewardAmount = 66,
            }
        },
        new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                rewardType = (int)BUDRewardType.RewardLuckyCoin,
                rewardName = "幸运币",
                RewardAmount = 6,
            }
        },
        new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                rewardType = (int)BUDRewardType.RewardPurpleDreamCoin,
                rewardName = "紫梦币",
                RewardAmount = 6,
            }
        },
        new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                rewardType = (int)BUDRewardType.RewardYouYouCoin,
                rewardName = "优优币",
                RewardAmount = 6,
            }
        },
        new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                rewardType = (int)BUDRewardType.RewardPurpleDreamCoin,
                rewardName = "紫梦币",
                RewardAmount = 30,
            }
        },
        new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                rewardType = (int)BUDRewardType.RewardYouYouCoin,
                rewardName = "优优币",
                RewardAmount = 6,
            }
        },
        new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                rewardType = (int)BUDRewardType.RewardLuckyCoin,
                rewardName = "幸运币",
                RewardAmount = 66,
            }
        }
    };

    public override void Init(ActivityInfo info)
    {
        base.Init(info);
        mActivityInfo = info;
        InitUI();
        //InitAvatar();
    }

    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        info.rewardList = mActivityInfo.rewardList;
        mActivityInfo = info;

        for (int i = 0; i < items.Count; i++)
        {
            items[i].InitData(mActivityInfo.eventList[i], mActivityInfo.preRewardList[i], OnClaimCallBack);
        }
        SetTomorrow();
    }


    private void InitUI()
    {
        previewBtn.onClick.AddListener(OnPreviewBtnClick);
        ruleBtn.onClick.AddListener(OnRuleBtnClick);

        for (int i = 0; i < items.Count; i++)
        {
            items[i].InitData(mActivityInfo.eventList[i], mActivityInfo.preRewardList[i], OnClaimCallBack);
        }
        SetTomorrow();
    }

    private void SetTomorrow()
    {
        if(mActivityInfo?.eventList?.Count>0)
        {
            for (int i = 0; i < mActivityInfo.eventList.Count; i++)
            {
                if (mActivityInfo.eventList[i].eventStatus ==(int) ClaimStatus.Lock)
                {
                    items[i].SetTomorrow();
                    break;
                }
            }
        }
    }

    private void OnPreviewBtnClick()
    {
        if (mActivityInfo == null)
        {
            return;
        }
        var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        panel.SetEventPreview(mActivityInfo, "马年新禧至，登录领贺岁红包", bgImage.texture);
    }

    private void OnRuleBtnClick()
    {
        var rulePanel = UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel);
        rulePanel.SetPanelTitle("规则说明");
        rulePanel.AddDesc("");
        rulePanel.AddDesc("1. 大年初一到初七每日登录可以领取当日登录的丰厚奖励");
        rulePanel.AddDesc("2. 每天仅可领取当天奖励，逾期不可补领，登录后请及时领取");
    }

    private bool isSending = false;
    private void OnClaimCallBack(NewYearLoginGiftItem rewardItem)
    {
        var eventInfo = mActivityInfo.eventList.Find(tmp => tmp.eventId == rewardItem.EventId);
        if (eventInfo == null)
        {
            return;
        }

        if (isSending) return;
        isSending = true;

        JObject jObject = new JObject()
        {
            ["activityId"] = mActivityInfo.activityId,
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse, rewardItem);
                AccountDataManager.Inst.BalanceInfo.Refresh();
            },
            (error) =>
            {
                isSending = false;
            });
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response, NewYearLoginGiftItem rewardItem)
    {
        if (this == null || gameObject == null)
        {
            return;
        }
        var eventInfo = mActivityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }
        eventInfo.eventStatus = response.eventInfo.eventStatus;
        rewardItem.Refresh();
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        panel.ShowRewards(rewards[items.IndexOf(rewardItem)]);
        UpdateRedDot();
        RefrashData(mActivityInfo);
    }
}
