using System.Collections.Generic;
using System.Linq;
using Basic.Utils;
using Es;
using Game.Avatar;
using Game.Event;
using Game.Store;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

//晴天娃娃特惠
public class SunnyDollGift : ActivityBaseView
{
    [SerializeField] private Texture PrevieBg;
    [SerializeField] private Sprite Icon;
    //二次确认
    [SerializeField] private Transform ConfirmGroup;
    [SerializeField] private Text ConfirmBuyBtnText;
    [SerializeField] private CButton ConfirmBuyBtn;
    //购买区域
    [SerializeField] private CButton GameBtn;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private CButton PreviewBtn;
    [SerializeField] private CButton SendBtn;
    [SerializeField] private Transform NoneBg;
    //标题
    [SerializeField] private CButton HelpBtn;
    [SerializeField] private Text LeftTime;
    //最低价
    [SerializeField] private Transform LowPrice;
    [SerializeField] private Text PriceText;
    [SerializeField] private Text OriginalPriceText;
    //任务区域
    [SerializeField] private Text TaskPrice;
    [SerializeField] private Text TaskDiscount;
    [SerializeField] private Transform ScrollContent;
    [SerializeField] private SunnyDollGiftItem ScrollItem;
    private List<SunnyDollGiftItem> ItemLs = new List<SunnyDollGiftItem>();
    //avatar
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private GameObject modelRoot; // 用于放置角色模型的根节点

    [HideInInspector] public ActivityInfo activityInfo;

    List<string> pgcIds = new List<string>() {
            "10400503",
            "10100118",
            "10900503",
    };
    public override void Init(ActivityInfo info)
    {
        base.Init(info);
        activityInfo = info;

        ScrollItem.gameObject.SetActive(false);

        GameBtn.onClick.AddListener(OnGameBtn);

        PreviewBtn.onClick.AddListener(OnPreviewBtn);

        BuyBtn.onClick.AddListener(OnBuyBtn);

        SendBtn.onClick.AddListener(OnSendBtn);

        HelpBtn.onClick.AddListener(OnHelpBtn);

        ConfirmBuyBtn.onClick.AddListener(OnConfirmBuyBtn);

        for (int i = 0; i < info.eventList.Count; i++)
        {
            if (i >= ItemLs.Count)
            {
                var obj = GameObject.Instantiate(ScrollItem, ScrollContent).GetComponent<SunnyDollGiftItem>();
                obj.gameObject.SetActive(true);
                ItemLs.Add(obj);
            }
            ItemLs[i].Init(info.eventList[i], this);
        }

        ConfirmGroup.gameObject.SetActive(false);

        InitCharacter();
    }

    private void InitCharacter()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, modelRoot.transform);
        avatarCameraController.RotateTarget = modelRoot.transform;
        foreach (var pgcId in pgcIds)
        {
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
        }
    }

    #region Button事件

    private void OnHelpBtn()
    {
        UIManager.Inst.OpenPanel<ActivityRulePanel>(PanelId.ActivityRulePanel, "Assets/Loadable/UI/ActivityCenterPanel/Jiejiele/Rule.json");
    }

    private void OnBuyBtn()
    {
        ConfirmGroup.gameObject.SetActive(!ConfirmGroup.gameObject.activeSelf);
    }

    private void OnConfirmBuyBtn()
    {
        JObject req = new JObject()
        {
            ["productType"] = 14,
            ["productId"] = "LanternFestivalFunGames",
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyProductPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
        {
            BuyProductResult rsp = JsonConvert.DeserializeObject<BuyProductResult>(response);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
            RefreshRedDot();
            ShowPackReward(rsp.rewardList);
        }, (_) =>
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
            RefreshRedDot();
        });
    }

    private void OnSendBtn()
    {
        var sendGiftPanel = UIManager.Inst.SwapPanel(PanelId.SendGiftPanel) as SendGiftPanel;
        sendGiftPanel?.JumpTo(SendGiftMainTabs.Tab.BUD, 10098);
        MessageHelper.Broadcast<string>(MessageName.SelectAssetItem, "133");

    }

    private void OnGameBtn()
    {
        UIManager.Inst.OpenPanel(PanelId.ConnectingGamePanel);
    }

    private void OnPreviewBtn()
    {
        //string spriteatlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";
        var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        //panel.SetEventPreview(pgcIds, "醒狮汤圆套装", "醒狮汤圆", "SunnyDollIcon", spriteatlasPath, "", "#81CC3C", PrevieBg,true);
        panel.SetEventPreview(pgcIds, "醒狮汤圆套装","醒狮汤圆", Icon, "#FD6E42", PrevieBg);
    }

    #endregion
    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        activityInfo = info;
        //LeftTime.text = activityInfo.leftTime;
        //当前价格
        var price = 600;
        foreach (var item in info.eventList)
        {
            if (item.eventStatus == (int)ClaimStatus.Claimed)
            {
                price -= item.rewardNum;
            }
        }
        PriceText.text = price.ToString();
        ConfirmBuyBtnText.text = PriceText.text;
        LowPrice.gameObject.SetActive(price <= 60);
        //已售罄
        //var own = AssetsDataManager.IsOwned("93");
        var own = true;
        foreach (var item in pgcIds)
        {
            if (!AssetsDataManager.IsOwned(item))
            {
                own = false;
                break;
            }
        }
        NoneBg.gameObject.SetActive(own);
        BuyBtn.gameObject.SetActive(!own);
        if (own)
        {
            ConfirmGroup.gameObject.SetActive(false);
        }
        //任务
        foreach (var item in ItemLs)
        {
            item.RefreshData(item._info);
        }
    }

    public void RefreshRedDot()
    {
        updateRedDotAction?.Invoke();
    }

    private void ShowPackReward(List<LimitPackageRewardData> rewardList)
    {
        if (rewardList != null && rewardList.Count > 0)
        {
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowPgcRewards(rewardList[0].pgcIdList, "醒狮汤圆套装");
        }

        ReddotManagerUtils.Inst.RefreshRedDot();
        MessageHelper.Broadcast(MessageName.OnPurchaseLimitedPackageSuccess);
    }
}