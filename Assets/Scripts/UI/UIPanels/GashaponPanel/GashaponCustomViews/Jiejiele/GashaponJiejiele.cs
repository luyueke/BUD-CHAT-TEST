using Es;
using Game.Avatar;
using GameData.PgcData;
using Message;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class GashaponJiejiele : NewDefaultGashaponView
{
    [SerializeField] private CButton jumpBtn1;

    [SerializeField] private AvatarCameraController avatarCameraController2;
    [SerializeField] private GameObject modelRoot2; // 用于放置角色模型的根节点
    [HideInInspector] private PlayerAnimationCtrl animationCtrl;

    public Text tipsText;
    public List<GashaponJiejieleItem> leftItems;
    protected override void InitBG()
    {

    }

    private void InitCharacter()
    {
        List<string> pgcIds = new List<string>() {
        "10900505"};
        //var saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, modelRoot2.transform);
        avatarCameraController2.RotateTarget = modelRoot2.transform;
        foreach (var pgcId in pgcIds)
        {
            //var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
            var config = DataTables.GetAvatarCommonData(pgcId);
            var classType = UniqueType.GetAvatar(pgcId);
            characterWrapper.ChangePart(classType, pgcId);
            characterWrapper.ChangeColor(classType, config.defaultColor);
            characterWrapper.Move(classType, config.pDef);
            characterWrapper.Rotate(classType, config.rDef);
            characterWrapper.Scale(classType, config.sDef);
            characterWrapper.HVScale(classType, config.vhSDef);
            characterWrapper.SetLeftOrRight(classType, config.leftRightType);
        }
        characterWrapper.Avatar?.GetComponentInChildren<CustomBodyTypeController>()?.ApplyBodyType(CustomBodyTypeController.BodyType.None);
        animationCtrl = characterWrapper.Avatar?.GetComponentInChildren<PlayerAnimationCtrl>();
        //animationCtrl?.PlaySpecialAnimForUICharacter(GameData.SpecialAnim.Run);
    }

    public override void OnCreate(string id)
    {
        base.OnCreate(id);
        jumpBtn1.onClick.AddListener(OnJump);
        var items = GetComponentsInChildren<CommonRewardItem>(true);
        bgPath = GashaponDataManager.Inst.GetGashaponView(id).BgPath;
        //exchageCurrency = CurrencyType.CollectionTicket;

        InitCharacter();

        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }

    void OnJump() {
        UIManager.Inst.OpenPanel(PanelId.JiejieleGiftPanel);
    }

    private void OnClaimRewardCallBack(CommonRewardItem rewardItem)
    {
        GashaponDataManager.Inst.RequestClaimTaskReward(gashaponData.Id, rewardItem.itemId, OnClaimRewardSuccess);
    }

    private void OnClaimRewardSuccess(GashaponTaskRewardRsp rsp)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();

        foreach (var rewardData in rsp.rewardList)
        {
            var rewardItemData = new CommonRewardItemData
            {
                rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardData.rewardType),
                RewardAmount = rewardData.rewardNum,
                rewardType = rewardData.rewardType,
            };
            rewardItemDatas.Add(rewardItemData);
        }

        panel.ShowRewards(rewardItemDatas, true);
        MessageHelper.Broadcast(MessageName.ReddotNotice);
        AccountDataManager.Inst.BalanceInfo.Refresh();
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
    }

    public override void OnShow()
    {
        base.OnShow();
        InitItems();
    }

    void InitItems()
    {
        twistBtn.gameObject.SetActive(false);
        tipsText.text = "恭喜！你已集齐狮灯滚滚奖池所有商品！";
        foreach (var K in leftItems)
        {
            bool ishave = false;
            K.Init(out ishave, OnPriviewBtnClick);

            if (!ishave)
            {
                twistBtn.gameObject.SetActive(true);
                tipsText.text = "每次抽取都不会重复奖励，9次之内必解锁狮灯滚滚";
            }
        }
    }

    protected override void OnGashaponInfoUpdate(GameData.Gashapon.GashaponInfoRsp infoRsp)
    {
        base.OnGashaponInfoUpdate(infoRsp);

        singleText.SetText(infoRsp.singleDrawPrice.ToString());

        //if (infoRsp.singleDrawDiscountedPrice == infoRsp.singleDrawPrice)
        //{
        //    GameObjectEx.FindChildByName(twistBtn.transform, "TagDiscount").gameObject.SetActive(false);
        //    GameObjectEx.FindChildByName(twistBtn.transform, "Content/srcNum").gameObject.SetActive(false);
        //    GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "Content/num").SetText(infoRsp.singleDrawDiscountedPrice.ToString());
        //}
        //else
        //{
        //    GameObjectEx.FindChildByName(twistBtn.transform, "TagDiscount").gameObject.SetActive(true);
        //    GameObjectEx.FindChildByName(twistBtn.transform, "Content/srcNum").gameObject.SetActive(true);
        //    GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "Content/srcNum")
        //        .SetText(infoRsp.singleDrawPrice.ToString());
        //    GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "Content/num")
        //        .SetText(infoRsp.singleDrawDiscountedPrice.ToString());
        //}

        //int curNum = infoRsp.luckyProgressInfo?.start ?? 0;
        //if (curNum == 0)
        //{
        //    GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("首次五折");
        //}
        //else
        //{
        //    GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("九折优惠");
        //}

        InitItems();
    }
}
