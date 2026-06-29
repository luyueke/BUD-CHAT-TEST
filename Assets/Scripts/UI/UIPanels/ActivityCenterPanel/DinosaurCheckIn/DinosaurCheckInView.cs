using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;

public class DinosaurCheckInView : ActivityBaseView {
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private Transform characterRoot;
    private ActivityInfo activityInfo;

    private Dictionary<int, DinosaurCheckInCardView> dinosaurCheckInCards =
        new Dictionary<int, DinosaurCheckInCardView>();

    private Dictionary<int, DinosaurCheckInDailyView> dinosaurCheckInCardDailyViewss =
        new Dictionary<int, DinosaurCheckInDailyView>();

    private DinosaurCheckInClaimView claimView;

    private CButton ruleBtn;

    private bool isSending = false;


    // 奖励是多种, 配置表不好配置，直接写在代码中
    private Dictionary<int, List<CommonRewardItemData>> rewardItemDataDic;


    public override void Init(ActivityInfo info) {
        base.Init(info);
        activityInfo = info;
        InitData();
        InitUI();
        InitAvatar();
    }

    private void InitUI() {
        var tmpCards = GetComponentsInChildren<DinosaurCheckInCardView>(true);
        foreach (var cardView in tmpCards) {
            cardView.SetCallBack(OnClaimClicked);
            dinosaurCheckInCards.Add(cardView.eventId, cardView);
        }

        var dailyViews = GetComponentsInChildren<DinosaurCheckInDailyView>(true);
        foreach (var dailyView in dailyViews) {
            dailyView.SetCallBack(OnClaimClicked);
            dinosaurCheckInCardDailyViewss.Add(dailyView.eventId, dailyView);
        }

        claimView = GetComponentInChildren<DinosaurCheckInClaimView>(true);
        claimView.SetCallBack(OnClaimClicked);

        ruleBtn = GameObjectEx.FindComponentByName<CButton>(transform, "Content/RuleBtn");
        ruleBtn.onClick.AddListener(OnRuleClicked);
    }

    private void InitData() {
        rewardItemDataDic =
            new Dictionary<int, List<CommonRewardItemData>>() {
                {
                    1, new List<CommonRewardItemData>() {
                        new CommonRewardItemData() {
                            rewardType = (int)BUDRewardType.RewardCoin,
                            RewardAmount = 1000,
                            rewardName = "金币"
                        }
                    }
                }, {
                    2, new List<CommonRewardItemData>() {
                        new CommonRewardItemData() {
                            rewardType = (int)BUDRewardType.RewardCoin,
                            RewardAmount = 200,
                            rewardName = "金币"
                        },
                        new CommonRewardItemData() {
                            rewardType = (int)BUDRewardType.RewardLuckyCoin,
                            RewardAmount = 2,
                            rewardName = "幸运币"
                        }
                    }
                }, {
                    3, new List<CommonRewardItemData>() {
                        new CommonRewardItemData() {
                            rewardType = (int)BUDRewardType.RewardCoin,
                            RewardAmount = 200,
                            rewardName = "金币"
                        },
                        new CommonRewardItemData() {
                            rewardType = (int)BUDRewardType.RewardLuckyCoin,
                            RewardAmount = 2,
                            rewardName = "幸运币"
                        },
                        new CommonRewardItemData() {
                            rewardType = (int)BUDRewardType.RewardPgcResource,
                            pgcId = "10900305",
                            RewardAmount = 1,
                            rewardName = "恐龙头饰"
                        },
                        new CommonRewardItemData() {
                            rewardType = (int)BUDRewardType.RewardPgcResource,
                            pgcId = "10400304",
                            RewardAmount = 1,
                            rewardName = "恐龙服装"
                        }
                    }
                }, {
                    5, new List<CommonRewardItemData>() {
                        new CommonRewardItemData() {
                            rewardType = (int)BUDRewardType.RewardCoin,
                            RewardAmount = 100,
                            rewardName = "金币"
                        }
                    }
                }, {
                    6, new List<CommonRewardItemData>() {
                        new CommonRewardItemData() {
                            rewardType = (int)BUDRewardType.RewardBadge,
                            RewardAmount = 10,
                            rewardName = "徽章"
                        }
                    }
                }, {
                    4, new List<CommonRewardItemData>() {
                        new CommonRewardItemData() {
                            rewardType = (int)BUDRewardType.RewardBadge,
                            RewardAmount = 50,
                            rewardName = "徽章"
                        }
                    }
                }
            };

        foreach (var keyValue in rewardItemDataDic) {
            foreach (var rewardItemData in keyValue.Value) {
                if (rewardItemData.rewardType == (int)BUDRewardType.RewardPgcResource) {
                    PgcUtils.GetIconSpriteByPgcIdAsync(rewardItemData.pgcId, gameObject, sp => {
                        rewardItemData.IconSp = sp;
                    });
                } else {
                    rewardItemData.IconSp =
                        PgcUtils.LoadRewardIcon((BUDRewardType)rewardItemData.rewardType, gameObject);
                }
            }
        }
    }

    private void InitAvatar() {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        if (rewardItemDataDic.TryGetValue(3, out var rewardItemDataList)) {
            foreach (var rewardItemData in rewardItemDataList) {
                if (rewardItemData.rewardType == (int)BUDRewardType.RewardPgcResource) {
                    var pgcConfig = PgcUtils.GetPgcConfigData(rewardItemData.pgcId);
                    if (pgcConfig.ResourceType != (int)ResourceType.Avatar) {
                        continue;
                    }

                    var config = DataTables.GetAvatarCommonData(rewardItemData.pgcId);
                    var classType = UniqueType.GetAvatar(rewardItemData.pgcId);
                    characterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType),
                        rewardItemData.pgcId);
                    characterWrapper.ChangeColor(classType, config.defaultColor);
                    characterWrapper.Move(classType, config.pDef);
                    characterWrapper.Rotate(classType, config.rDef);
                    characterWrapper.Scale(classType, config.sDef);
                    characterWrapper.HVScale(classType, config.vhSDef);
                    characterWrapper.SetLeftOrRight(classType, config.leftRightType);
                }
            }
        }

        characterWrapper.SetParent(characterRoot, true);
        avatarCameraController.RotateTarget = characterRoot;
        avatarCameraController.isMoveEnabled = false;
        avatarCameraController.isZoomEnabled = false;
    }

    private void OnRuleClicked() {
        UIManager.Inst.OpenPanel<ActivityRulePanel>(PanelId.ActivityRulePanel,
            "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/DinosaurCheckIn/Rule.json");
    }


    private void OnClaimClicked(int eventId) {
        var eventInfo = activityInfo.eventList.Find(tmp => tmp.eventId == eventId);
        if (eventInfo == null) {
            return;
        }

        if (eventInfo.eventStatus != (int)ClaimStatus.Unlocked && eventInfo.eventStatus != (int)ClaimStatus.ErrStatus) {
            return;
        }

        if (isSending) {
            return;
        }

        isSending = true;

        JObject jObject = new JObject() {
            ["activityId"] = activityInfo.activityId,
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) => {
                ActivityEventClaimResponse activityEventClaimResponse =
                    JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse);
            },
            (error) => {
                isSending = false;
            });
    }


    private void OnClaimSuccess(ActivityEventClaimResponse response) {
        if (this == null || gameObject == null) {
            return;
        }


        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null) {
            return;
        }

        eventInfo.eventStatus = response.eventInfo.eventStatus;
        if (rewardItemDataDic.TryGetValue(eventInfo.eventId, out var rewardItemDataList)) {
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(rewardItemDataList);
        }
        AccountDataManager.Inst.BalanceInfo.Refresh();
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);

    }


    public override void RefrashData(ActivityInfo info) {
        base.RefrashData(info);
        activityInfo = info;
        foreach (var eventInfo in info.eventList) {
            if (dinosaurCheckInCards.TryGetValue(eventInfo.eventId, out var cardView)) {
                cardView.SetStatus((ClaimStatus)eventInfo.eventStatus);
            }

            if (dinosaurCheckInCardDailyViewss.TryGetValue(eventInfo.eventId, out var dailyView)) {
                dailyView.SetStatus((ClaimStatus)eventInfo.eventStatus);
            }

            if (eventInfo.eventId == 4) {
                claimView.SetStatus((ClaimStatus)eventInfo.eventStatus);
                claimView.SetProgress(eventInfo.finishAmount);
            }
        }
    }
}
