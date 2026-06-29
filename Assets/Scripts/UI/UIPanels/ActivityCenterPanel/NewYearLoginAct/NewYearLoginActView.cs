using System;
using System.Collections.Generic;
using Game.Event;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewYearLoginActView  : ActivityBaseView {


    [SerializeField]
    private NewYearLoginPaidItem newYearLoginPaidItemPrefab;
    [SerializeField]
    private NewYearLoginFreeItem newYearLoginFreeItemPrefab;

    [SerializeField] private RawImage bgImage;

    [SerializeField]
    private GameObject[] dayObjs;

    [SerializeField] private CButton buySeasonPass;

    [SerializeField] private Scrollbar scrollbar;

    [SerializeField] private NewYearLoginPurchaseView newYearLoginPurchaseView;

    private float[] barValues = new[] { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };


    private SeasonPassListRsp seasonPassRsp;
    private Dictionary<string, NewYearLoginPaidItem> seasonPassPaidItems = new Dictionary<string, NewYearLoginPaidItem>();
    private Dictionary<string, NewYearLoginFreeItem> seasonPassFreeItems = new Dictionary<string, NewYearLoginFreeItem>();




    private bool isClaiming;
    private ActivityInfo activityInfo;

    public override void Init(ActivityInfo info) {
        base.Init(info);
        activityInfo = info;
        InitUI();
        RequestSeasonPassData();
    }

    private void InitUI() {
        var previewBtn = GameObjectEx.FindComponentByName<CButton>(transform, "PreviewBtn");
        previewBtn.onClick.AddListener(OnPreviewBtnClick);
        buySeasonPass.onClick.AddListener(OnBuySeasonPassBtnClick);
        newYearLoginPurchaseView.SetPurchaseCallBack(OnPurchaseCallBack);
    }

    private void OnBuySeasonPassBtnClick() {

        if (seasonPassRsp.isPaid == 1) {
            return;
        }

        newYearLoginPurchaseView.transform.SetParent(mainPanel.transform,true);
        newYearLoginPurchaseView.gameObject.SetActive(true);
    }

    private void OnPurchaseCallBack(bool isSuccess) {
        newYearLoginPurchaseView.gameObject.SetActive(false);
        newYearLoginPurchaseView.transform.SetParent(transform,true);
        if (isSuccess) {
            RequestSeasonPassData();
        }
    }

    private void OnPreviewBtnClick()
    {

        if (activityInfo == null)
        {
            return;
        }

        //var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        //panel.SetEventPreview(new List<string>() {"10900318", "10500035"},"铆钉凯特琳套装", "Bundle_17",  XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "跨年登录领好礼，携手共赴金喜之约","#FFAE64", bgImage.texture);

        List<PaidPackRewardData> paidPackRewardDatas = new List<PaidPackRewardData>();
        paidPackRewardDatas.Add(new PaidPackRewardData()
        {
            pgcIds = new List<string>() { "10900499" },
            name = "	2026 新年头饰",
            isBundle = false
        });
        paidPackRewardDatas.Add(new PaidPackRewardData()
        {
            pgcIds = new List<string>() { "40300505" },
            name = "2026 倒计时",
            isBundle = false
        });
        var panel = UIManager.Inst.OpenPanel<PaidPackRewardPanel>(PanelId.PaidPackRewardPanel);
        panel.itemBgColorStr = "#FFEDA2";
        panel.SetEventPreview(paidPackRewardDatas, "", bgImage.texture);
    }

    private void RequestSeasonPassData() {
        var req = new JObject()
        {
            ["seasonPassType"] = (int)SeasonPassType.HorseYearLogin
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SeasonPassInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            onReceive: msg =>
            {
                seasonPassRsp = JsonConvert.DeserializeObject<SeasonPassListRsp>(msg);
                RefreshData();
            }, onFail: arg0 =>
            {
            }, retryCount:2);
    }

    private void RefreshData() {
        if (seasonPassRsp == null) {
            return;
        }
        var paidItems = seasonPassRsp.paidRewardList;
        if (paidItems != null) {
            for (int i = 0; i < paidItems.Count; i++) {
                if (i > 4)
                    break;
                if (!seasonPassPaidItems.TryGetValue(paidItems[i].rewardId, out var itemComp)) {
                    itemComp = Instantiate(newYearLoginPaidItemPrefab, newYearLoginPaidItemPrefab.transform.parent);
                    itemComp.gameObject.SetActive(true);
                    itemComp.Init(paidItems[i], info => {
                        OnClickPaidItem(info, true);
                    });
                    seasonPassPaidItems.Add(paidItems[i].rewardId, itemComp);
                }
                itemComp.curData = paidItems[i];
                itemComp.SetState(paidItems[i].BudRewardStatus);
            }
        }

        var normalItems = seasonPassRsp.rewardList;
        if (normalItems != null) {
            for (int i = 0; i < normalItems.Count; i++) {
                if (i > 4)
                    break;
                if (!seasonPassFreeItems.TryGetValue(normalItems[i].rewardId, out var itemComp)) {
                    itemComp = Instantiate(newYearLoginFreeItemPrefab, newYearLoginFreeItemPrefab.transform.parent);
                    itemComp.gameObject.SetActive(true);
                    itemComp.Init(normalItems[i], info => {
                        OnClickPaidItem(info, false);
                    });
                    seasonPassFreeItems.Add(normalItems[i].rewardId, itemComp);
                }
                itemComp.curData = normalItems[i];
                itemComp.SetState(normalItems[i].BudRewardStatus);
            }
        }
        newYearLoginPaidItemPrefab.gameObject.SetActive(false);
        newYearLoginFreeItemPrefab.gameObject.SetActive(false);
        var curData = GetCurDay();
        for (int i = 0; i < dayObjs.Length; i++) {
            dayObjs[i].SetActive(i < curData);
        }
        scrollbar.size = barValues[curData - 1];
        GameObjectEx.FindChildByName(buySeasonPass.transform, "Free").gameObject.SetActive(seasonPassRsp.isPaid != 1);
        GameObjectEx.FindChildByName(buySeasonPass.transform, "PaidBtn").gameObject.SetActive(seasonPassRsp.isPaid == 1);

    }

    private void OnClickPaidItem(SeasonPassItemInfo itemInfo, bool isPaid) {

        if (itemInfo.BudRewardStatus == BudRewardStatus.Lock) {
            //OnPreviewBtnClick();
            if (isPaid)
            {
                if (seasonPassRsp.isPaid == 1)
                {
                    TipPanel.ShowToast("累计登录天数不足！");
                }
                else
                {
                    TipPanel.ShowToast("获取福马鎏金卡可解锁奖励！");
                }
            }
            else
            {
                TipPanel.ShowToast("累计登录天数不足！");
            }
            return;
        }

        if (itemInfo.BudRewardStatus == BudRewardStatus.Claimed) {
            return;
        }

        if (isClaiming)
        {
            return;
        }

        isClaiming = true;
        var req = new JObject()
        {
            ["rewardId"] = itemInfo.rewardId,
            ["seasonPassType"] = (int)SeasonPassType.HorseYearLogin
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SeasonPassClaim,
            HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: msg =>
            {
                isClaiming = false;
                HandleClaimSuccess(itemInfo, isPaid);
            }, onFail: arg0 =>
            {
                isClaiming = false;
            });
    }


    private void HandleClaimSuccess(SeasonPassItemInfo data, bool isPaid = false) {
        if (data == null) {
            return;
        }

        if (data.BudRewardType != BUDRewardType.RewardPgcResource) {
            AccountDataManager.Inst.BalanceInfo.Refresh();
        }

        if (isPaid) {
            if (seasonPassPaidItems.TryGetValue(data.rewardId, out var itemView)) {
                itemView.SyncRewardState(data.rewardId);
            }
        } else {
            if (seasonPassFreeItems.TryGetValue(data.rewardId, out var itemView)) {
                itemView.SyncRewardState(data.rewardId);
            }
        }

        var commonRewardData = new List<CommonRewardItemData>() {

        };

        foreach (var rewardData in data.rewardInfo.itemList) {
            if (rewardData.BudRewardType == BUDRewardType.RewardPgcResource) {
                for (int i = 0; i < rewardData.pgcIdList.Count; i++) {
                    var commonRewardItemData = new CommonRewardItemData();
                    commonRewardItemData.rewardType = (int)rewardData.BudRewardType;
                    commonRewardItemData.RewardAmount = rewardData.amount;
                    commonRewardItemData.pgcId = rewardData.pgcIdList[i];
                    commonRewardItemData.rewardName = SeasonPassDataManager.Inst.GetPgcName(rewardData.pgcIdList[i]);//  GetPgcName(rewardData.pgcIdList[i]);
                    commonRewardData.Add(commonRewardItemData);
                }

            } 
            //else if (rewardData.BudRewardType == BUDRewardType.RewardAvatarFrame)
            //{
            //    var commonRewardItemData = new CommonRewardItemData();
            //    commonRewardItemData.rewardType = (int)rewardData.BudRewardType;
            //    commonRewardItemData.RewardAmount = rewardData.amount;
            //    commonRewardItemData.pgcId = "130100012";
            //    commonRewardItemData.rewardName = "心动头像框";
            //    commonRewardData.Add(commonRewardItemData);
            //}
            else {
                var commonRewardItemData = new CommonRewardItemData();
                commonRewardItemData.rewardType = (int)rewardData.BudRewardType;
                commonRewardItemData.RewardAmount = rewardData.amount;
                commonRewardItemData.rewardName = PgcUtils.GetRewardName(rewardData.BudRewardType);
                commonRewardData.Add(commonRewardItemData);
            }
        }

        var rewardPanel =  UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        rewardPanel.ShowRewards(commonRewardData);
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }

    //public string GetPgcName(string pgcId)
    //{
    //    switch (pgcId)
    //    {
    //        case "10900499":
    //            return "玫瑰针织帽";
    //        case "40300436":
    //            return "送熊熊";
    //        case "130100012":
    //            return "心动头像框";
    //    }

    //    return "";
    //}

    public int GetCurDay()
    {
        int curDay = 0;
        if (seasonPassRsp.rewardList == null)
        {
            return curDay;
        }

        foreach (var info in seasonPassRsp.rewardList)
        {
            if (info.BudRewardStatus != BudRewardStatus.Lock)
                curDay++;
        }
        curDay = Math.Max(1, curDay);
        curDay = Math.Min(curDay, 5);
        return curDay;
    }

}


public class NewYearLoginInfo {
    public int reddot;
}
