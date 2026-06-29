using System;
using System.Collections.Generic;
using Game.Audio;
using Game.Database;
using GameData.Gashapon;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;


public class LuckyStarExtraData {
    public int luckyStarNum;
    public int randomLuckyStarNum;
}

public class LuckyStarGiftRewardData {
    public int level;
    public int rewardType;
    public int amount;
    public string pgcId;
    public string rewardName;
    public bool isReplaced;
    public bool isCritical;
    public int bundleId;
}

public class LuckyStarGiftBoxRsp
{
    public List<LuckyStarGiftRewardData> rewardList;
    public ServerBagUpdateData backpackData;
    public int randomLuckyStarNum;
    public int luckyStarNum;
}


public class LuckyStarView : BaseGashaponView {
    private RawImage bgImage;
    private Dictionary<int, CommonRewardItem> rewardItems;
    private CButton twistBtn;
    private CButton twist10Btn;
    private CButton ruleBtn;
    private CButton previewBtn;
    private CButton giftBoxBtn;
    private CButton giftBtn;
    private Image giftProgressBar;
    private Text luckyStarNumText;
    private int curLuckyStarNum = 0;

    private GashaponInfoRsp curInfoRsp;
    private const int MaxLuckyStarNum = 300;

    private const string BGPath = "Assets/Loadable/UI/UIPanel/GashaponPanel/LuckyStar/LuckyStar_BG.png";

    public override void OnCreate(string id) {
        base.OnCreate(id);
        var items = GetComponentsInChildren<CommonRewardItem>(true);
        rewardItems = new Dictionary<int, CommonRewardItem>();
        for (int i = 0; i < items.Length; i++) {
            items[i].Init(i + 1, OnClaimRewardCallBack);
            rewardItems.Add(i + 1, items[i]);
        }

        twistBtn = GameObjectEx.FindComponentByName<CButton>(transform, "TwistBtn");
        twist10Btn = GameObjectEx.FindComponentByName<CButton>(transform, "Twist10Btn");
        ruleBtn = GameObjectEx.FindComponentByName<CButton>(transform, "RightTop/RuleBtn");
        previewBtn = GameObjectEx.FindComponentByName<CButton>(transform, "RightTop/PreviewBtn");
        giftBoxBtn = GameObjectEx.FindComponentByName<CButton>(transform, "BottomRight/GiftBox");
        luckyStarNumText = GameObjectEx.FindComponentByName<Text>(giftBoxBtn.transform, "LuckyStarText");
        giftProgressBar = GameObjectEx.FindComponentByName<Image>(giftBoxBtn.transform, "BarBg/Bar");
        giftBtn = GameObjectEx.FindComponentByName<CButton>(transform, "RightTop/GiftBtn");
        ruleBtn.onClick.AddListener(OnRuleClick);
        twistBtn.onClick.AddListener(OnTwistClick);
        twist10Btn.onClick.AddListener(OnTwist10Click);
        previewBtn.onClick.AddListener(OnPreviewClick);
        giftBoxBtn.onClick.AddListener(OnGiftBoxClick);
        giftBtn.onClick.AddListener(OnGiftBtnClick);
        giftBtn.gameObject.SetActive(false);
        foreach (var rewardIcon in GetComponentsInChildren<LuckStartRewardIcon>()) {
            rewardIcon.SetClickCallBack(OnPreviewClick);
        }
    }

    private void OnGiftBtnClick() {
        UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel, ActivityId.NewYearsTurntable2026.ToString());
    }


    public override void OnShow() {
        base.OnShow();
        foreach (var item in rewardItems) {
            item.Value.SetStatus(ClaimStatus.Lock);
        }
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp) {
        base.OnGashaponInfoUpdate(infoRsp);
        if (this == null || gameObject == null || infoRsp == null) {
            return;
        }

        curInfoRsp = infoRsp;
        if (infoRsp.taskList != null) {
            foreach (var taskInfo in infoRsp.taskList) {
                if (rewardItems.TryGetValue(taskInfo.eventId, out var rewardItem)) {
                    rewardItem.SetStatus((ClaimStatus)taskInfo.rewardStatus);
                }
            }
        }


        if (infoRsp.tenDrawDiscountedPrice == infoRsp.tenDrawPrice) {
            GameObjectEx.FindChildByName(twist10Btn.transform, "TagDiscount").gameObject.SetActive(false);
            GameObjectEx.FindChildByName(twist10Btn.transform, "Content/srcNum").gameObject.SetActive(false);
            GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "Content/num").SetText(infoRsp.tenDrawDiscountedPrice.ToString());
        } else {
            GameObjectEx.FindChildByName(twist10Btn.transform, "TagDiscount").gameObject.SetActive(true);
            GameObjectEx.FindChildByName(twist10Btn.transform, "Content/srcNum").gameObject.SetActive(true);
            GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "Content/srcNum")
                .SetText(infoRsp.tenDrawPrice.ToString());
            GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "Content/num")
                .SetText(infoRsp.tenDrawDiscountedPrice.ToString());
        }

        if (!string.IsNullOrEmpty(infoRsp.extraData)) {
            var luckyStarExtraData = JsonConvert.DeserializeObject<LuckyStarExtraData>(infoRsp.extraData);
            //if (luckyStarExtraData != null && luckyStarExtraData.randomLuckyStarNum > 0) {
            //    ShowRewardContainer(luckyStarExtraData.randomLuckyStarNum);
            //}
            if (GashaponDataManager.Inst.newYearLuckyStar_show > 0)
            {
                ShowRewardContainer(GashaponDataManager.Inst.newYearLuckyStar_show);
                GashaponDataManager.Inst.newYearLuckyStar_show = 0;
            }

            if (luckyStarExtraData != null) {
                luckyStarNumText.SetText($"<color=#FFD336>{luckyStarExtraData.luckyStarNum}</color>/{MaxLuckyStarNum}");
                curLuckyStarNum = luckyStarExtraData.luckyStarNum;
                giftProgressBar.fillAmount = (float)luckyStarExtraData.luckyStarNum / MaxLuckyStarNum;
            }

        }

    }

    private void OnGiftBoxClick() {


        if (curLuckyStarNum < MaxLuckyStarNum) {
            var previewContainer = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/GashaponPanel/LuckyStar/LuckyStarPreviewContainer.prefab")
                .Instantiate(panelTopContainer).GetComponent<LuckyStarPreviewContainer>();
            previewContainer.gameObject.SetActive(true);
            previewContainer.SetData(gashaponData);
            return;
        }

        try
        {
            JObject req = new JObject()
            {
                ["lotteryId"] = gashaponId,
                ["buttonType"] = (int)GashaponSpecialButton.LuckyStar,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GashaponSpecialButton, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
            {
                OnGiftBoxOpen(JsonConvert.DeserializeObject<LuckyStarGiftBoxRsp>(response));
            }, (fail) =>
            {
                LoggerUtils.LogError("开宝箱失败");
            });
        }
        catch (Exception e)
        {
            LoggerUtils.LogError("开宝箱失败 ：" + e.StackTrace);
        }
    }


    private void OnGiftBoxOpen(LuckyStarGiftBoxRsp rsp) {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();
        foreach (var rewardData in rsp.rewardList) {
            var itemData = new CommonRewardItemData() {
                rewardName = rewardData.rewardName,
                RewardAmount = rewardData.amount,
                rewardType = rewardData.rewardType,
                pgcId = rewardData.pgcId,
                isConverted = rewardData.isReplaced,
                isCrit = rewardData.isCritical,
            };
            rewardItemDatas.Add(itemData);
        }
        panel.ShowRewards(rewardItemDatas, true);
        panel.SetCloseAct(() => {
            if (rsp.randomLuckyStarNum > 0) {
                ShowRewardContainer(rsp.randomLuckyStarNum );
            }
        });
        AccountDataManager.Inst.BalanceInfo.Refresh();
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
    }


    private void OnRuleClick() {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, viewCfg.RulePath);
    }

    private void OnPreviewClick() {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam {
            bgPath = BGPath,
            title = gashaponData.Name,
            gashaponData = gashaponData,
            rewardCurrency = CurrencyType.PurpleDreamCoin,
            rulePath = viewCfg.RulePath
        });
        previewPanel.SetAnimPreviewBtnColor(new Color32(0,93,134, 255), new Color32(255,192,31,255), new Color32(38,190, 255, 255));


    }

    private void OnTwistClick() {
        if (curInfoRsp == null) {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }
        SendGashaponRequestOnce(gashaponData, curInfoRsp.singleDrawDiscountedPrice);
    }


    private void OnTwist10Click() {
        if (curInfoRsp == null) {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        SendGashaponRequestTenTimes(gashaponData, curInfoRsp.tenDrawDiscountedPrice);
    }

    private void OnClaimRewardCallBack(CommonRewardItem rewardItem) {
        GashaponDataManager.Inst.RequestClaimTaskReward(gashaponData.Id, rewardItem.itemId, OnClaimRewardSuccess);
    }

    private void OnClaimRewardSuccess(GashaponTaskRewardRsp rsp) {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();
        foreach (var taskInfo in rsp.taskList) {
            if (rewardItems.TryGetValue(taskInfo.eventId, out var taskItem)) {
                taskItem.SetStatus((ClaimStatus)taskInfo.rewardStatus);
            }
        }

        foreach (var rewardData in rsp.rewardList) {
            var rewardItemData = new CommonRewardItemData {
                rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardData.rewardType),
                RewardAmount = rewardData.rewardNum,
                rewardType = rewardData.rewardType,
            };
            rewardItemDatas.Add(rewardItemData);
        }

        panel.ShowRewards(rewardItemDatas, true);
        AccountDataManager.Inst.BalanceInfo.Refresh();
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp) {
        base.OnGashaOnceRsp(gashaponRsp);
        OnGashaRsp(gashaponRsp, true);
    }

    private void OnGashaRsp(GashaponRsp gashaponRsp, bool isOnce) {

        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
            new GashaponTwistAnimParam() {
                gashaponId = gashaponData.Id
            });
        if (isOnce) {
            panel.PlayOneTwistAnimation(gashaponRsp.rewardList, () => {
                OnGashaTwistAnimComplete(gashaponRsp);
            });
        } else {
            panel.PlayTenTwistAnimation(gashaponRsp.rewardList, () => {
                OnGashaTwistAnimComplete(gashaponRsp);
            });
        }

    }

    private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp) {
        UIManager.Inst.ClosePanel(PanelId.GashaponTwistAnimPanel);
        if (gashaponRsp == null || gashaponRsp.rewardList == null) {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponId, gashaponRsp);
    }

    public override void OnGashaTenRsp(GashaponRsp gashaponRsp) {
        base.OnGashaTenRsp(gashaponRsp);
        OnGashaRsp(gashaponRsp, false);
    }


    private void ShowRewardContainer(int count) {
        var rewardContainer = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/GashaponPanel/LuckyStar/RewardContainer.prefab")
            .Instantiate(panelTopContainer).GetComponent<LuckyStarRewardContainer>();
        rewardContainer.gameObject.SetActive(true);
        rewardContainer.SetRewardCount(count);
        AkSoundManager.Inst.PlayUIEffectSound("Play_UI_GetRewards_A3");
    }

}
