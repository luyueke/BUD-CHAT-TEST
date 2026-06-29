using System.Collections.Generic;
using System.Linq;
using Basic.Extensions;
using Game.Store;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class WitchLunaView : BaseGashaponView
{
    [SerializeField] private CButton backBtn;

    [SerializeField] private GashaponCharacterPreview characterPreview;

    [SerializeField] private MagicRewardItem rewardItemPrefab;
    [SerializeField] private MagicRewardItem bundleRewardItem;

    [SerializeField] private List<MagicEventRewardItem> eventRewardItems;

    [SerializeField] private GameObject discountMarkObj;
    [SerializeField] private Text originalPriceText;
    [SerializeField] private Text priceText;
    [SerializeField] private GameObject previewObj;
    [SerializeField] private CButton rechargeBtn;
    [SerializeField] private Text ownAllTip;
    [SerializeField] private Text rechargeTip;

    [SerializeField] private CButton ruleBtn;

    public string curgashaponId { get; private set; }

    private GashaponData gashaponData;
    private MagicLotteryItem[] lotteryItems;
    private string curBundleId;
    private List<MagicRewardItem> rewardItems = new List<MagicRewardItem>();
    private GashaponInfoRsp gashaponInfoRsp;
    private bool isSending = false;
    private string BgTexturePath => GashaponUtils.ViewBasePath + "WitchLuna/WitchLuna_bg.png";
    private string RulePath = "Assets/Loadable/UI/UIPanel/WitchLuna/Rule.json";

    public override void OnCreate(string gashaponId)
    {
        base.OnCreate(gashaponId);
        InitUI();
        curgashaponId = gashaponId;
        RefreshData(gashaponId);
    }
    private void OnGetGashaponInfoSuccess(GashaponInfoRsp rsp)
    {
        gashaponInfoRsp = rsp;
        discountMarkObj.SetActive(false);
        originalPriceText.gameObject.SetActive(false);
        if (rsp.singleDrawDiscountedPrice != rsp.singleDrawPrice)
        {
            discountMarkObj.SetActive(true);
            originalPriceText.gameObject.SetActive(true);
            originalPriceText.SetText(rsp.singleDrawPrice.ToString());
            priceText.SetText(rsp.singleDrawDiscountedPrice.ToString());
        }
        else
        {
            priceText.SetText(rsp.singleDrawDiscountedPrice.ToString());
        }

        if (rsp.taskList == null)
        {
            foreach (var eventRewardItem in eventRewardItems)
            {
                eventRewardItem.SetStatus(BudRewardStatus.Lock);
            }
        }
        else
        {
            foreach (var taskInfo in rsp.taskList)
            {
                var rewardItem = eventRewardItems.Find(tmp => tmp.eventId == taskInfo.eventId);
                rewardItem.SetStatus((BudRewardStatus)taskInfo.rewardStatus);
            }
        }

        if (rsp.rewardPool != null)
        {
            foreach (var drawnInfo in rsp.rewardPool)
            {
                var rewardItem = rewardItems.Find(tmp => tmp.GetRewardData().RewardId == drawnInfo.rewardId);
                if (rewardItem != null)
                {
                    rewardItem.SetDrawnStatus(drawnInfo.everDrawn == 1);
                }
            }
        }


        if (rsp.singleDrawPrice == 0)
        {
            rechargeTip.gameObject.SetActive(false);
            rechargeBtn.gameObject.SetActive(false);
            ownAllTip.gameObject.SetActive(true);
            ownAllTip.SetLocalText("恭喜！你已集齐{0}奖池所有商品！", gashaponData.Name);
        }
        else
        {
            rechargeTip.gameObject.SetActive(true);
            rechargeBtn.gameObject.SetActive(true);
            rechargeTip.SetLocalText("奖励不会重复，12抽内必得{0}", PgcUtils.GetBundleName(curBundleId));
            ownAllTip.gameObject.SetActive(false);
        }
    }
    private void InitUI()
    {
        rewardItemPrefab.gameObject.SetActive(false);
        rechargeBtn.onClick.AddListener(OnRechargeBtnClick);
        lotteryItems = GetComponentsInChildren<MagicLotteryItem>(true);
        foreach (var lotteryItem in lotteryItems)
        {
            lotteryItem.SetOnSelectCallBack(RefreshData);
        }
        foreach (var eventRewardItem in eventRewardItems)
        {
            eventRewardItem.SetClaimCallBack(OnEventRewardItemClick);
            eventRewardItem.SetPreviewCallBack(OnEventRewardPreviewClick);
        }
        ruleBtn.onClick.AddListener(OnRuleBtnClick);
        characterPreview.avatarCameraController.isMoveEnabled = false;
        characterPreview.avatarCameraController.isZoomEnabled = false;
        characterPreview.SetCameraColor(DataUtil.DeSerializeColorByHex("#66666600"));
    }
    private void OnEnable()
    {   
        if(curgashaponId!=null)
            RefreshData(curgashaponId);
    }
    private void OnRuleBtnClick()
    {
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, RulePath);
    }


    private void OnEventRewardItemClick(int eventId)
    {

        GashaponDataManager.Inst.RequestClaimTaskReward(gashaponData.Id, eventId, OnClaimRewardSuccess);
    }


    private void OnRechargeBtnClick()
    {
        SendGashaponRequestOnce(gashaponData);
    }

    protected override void SendGashaponRequestOnce(GashaponData data)
    {
        if (gashaponData == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)gashaponData.CurrencyType, gashaponInfoRsp.singleDrawDiscountedPrice))
        {
            if (gashaponData.CurrencyType == CurrencyType.GreenCoin)
            {
                TipPanel.ShowToast("当前创作者币余额不足");
                return;
            }
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)gashaponData.CurrencyType, gashaponInfoRsp.singleDrawDiscountedPrice);
            return;
        }
        var gId = gashaponData.Id;
        GashaponDataManager.Inst.RequestGashapon(gId, OneTimeGasha, OnGashaOnceRsp);
    }
    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        if (!this || gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
            new GashaponTwistAnimParam()
            {
                gashaponId = gashaponData.Id
            });

        panel.PlayOneTwistAnimation(gashaponRsp.rewardList, () =>
        {
            OnGashaTwistAnimComplete(gashaponRsp);
        });
    }

    private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp)
    {
        UIManager.Inst.ClosePanel(PanelId.GashaponTwistAnimPanel);
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponData.Id, gashaponRsp);
        RefreshData(gashaponData.Id);

    }

    private void OnEventRewardPreviewClick(int eventId)
    {
        previewObj.SetActive(true);
    }


    private void OnClaimRewardSuccess(GashaponTaskRewardRsp rsp)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();
        foreach (var taskInfo in rsp.taskList)
        {
            var taskItem = eventRewardItems.Find(tmp => tmp.eventId == taskInfo.eventId);
            if (taskItem.GetStatus() != (BudRewardStatus)taskInfo.rewardStatus && (BudRewardStatus)taskInfo.rewardStatus == BudRewardStatus.Claimed)
            {
                taskItem.SetStatus((BudRewardStatus)taskInfo.rewardStatus);
                if (taskItem.eventId == 3)
                {
                    AccountDataManager.Inst.SendSetProfileThemeReqeust((int)ProfileTheme.MagicTrial);
                }
                rewardItemDatas.Add(taskItem.GetRewardData());
            }
        }
        panel.ShowRewards(rewardItemDatas, true);
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }


    private void RefreshUI()
    {
        foreach (var lotteryItem in lotteryItems)
        {
            if (lotteryItem.lotteryName == gashaponData.Id)
            {
                lotteryItem.SetIsOnWithoutNotify(true);
            }
        }

        if (!string.IsNullOrEmpty(curBundleId))
        {
            var bundleRewardData =
                GashaponDataManager.Inst.GetBundleRewardDataById(gashaponData.RewardList, curBundleId);
            bundleRewardItem.gameObject.SetActive(true);
            bundleRewardItem.SetData(bundleRewardData, ShowPreview);
            this.SetFrameCallBack(1, () => {
                ShowCharacter(bundleRewardData);
            });
        }
        else
        {
            bundleRewardItem.gameObject.SetActive(false);
        }


        var singleRewardDataList = GashaponDataManager.Inst.RemoveBundleRewardData(gashaponData.RewardList);
        foreach (var rewardItem in rewardItems)
        {
            rewardItem.gameObject.SetActive(false);
        }

        for (int i = 0; i < singleRewardDataList.Count; i++)
        {
            MagicRewardItem rewardItem = null;
            if (i >= rewardItems.Count)
            {
                rewardItem = Instantiate(rewardItemPrefab, rewardItemPrefab.transform.parent);
                rewardItems.Add(rewardItem);
            }
            else
            {
                rewardItem = rewardItems[i];
            }

            rewardItem.gameObject.SetActive(true);
            rewardItem.SetData(singleRewardDataList[i], ShowPreview);
        }

    }


    public void ShowPreview()
    {
        characterPreview.gameObject.SetActive(false);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            title = gashaponData.Name,
            gashaponData = gashaponData,
            gashaponInfoRsp = gashaponInfoRsp,
            rewardCurrency = CurrencyType.YouYouCoin,
            rulePath = RulePath
        });
        previewPanel.SetBundleViewBgClolr("#6453BB");
    }


    private void ShowCharacter(GashaponRewardData bundleRewardData)
    {
        characterPreview.StartPreview(bundleRewardData, null);
    }



    private void RefreshData(string gashaponId)
    {
        gashaponData = dataHandler.GetGashaponData(gashaponId);
        //Debug.Log($"[GASHAPON DEBUG] Gashapon ID: {gashaponData.Id}, Name: {gashaponData.Name}, CurrencyType in Memory: {gashaponData.CurrencyType} (int value: {(int)gashaponData.CurrencyType})");
        curBundleId = gashaponData.RewardList.FirstOrDefault(tmp => !string.IsNullOrEmpty(tmp.BundleId))?.BundleId;
        RefreshUI();
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGetGashaponInfoSuccess);
    }
}