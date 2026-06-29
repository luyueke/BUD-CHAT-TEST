using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Avatar;
using GameData.Gashapon;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class ZongXiaFuYaoView : NewDefaultGashaponView {
    [SerializeField]
    public Text needNumText;
    [SerializeField]
    public Text progressValueText;
    [SerializeField]
    public RectTransform progressValueTrans;
    [SerializeField]
    public Text rewardText;
    [SerializeField]
    public CButton homePageSkinBtn;
    [SerializeField]
    public CButton avatarBtn;
    [SerializeField]
    public CButton bubBtn;
    [SerializeField]
    public Text tipsDoneTxt;
    [SerializeField]
    public Text tipsTxt;
    [SerializeField]
    public GameObject bottom;
    private PlayerAnimationCtrl otherAnimationCtrl;
    private CharacterWrap otherCharacterWrap;
    [SerializeField] private AvatarCameraController avatarCameraController;
    public GameObject modelRoot; // 用于放置角色模型的根节点

    [SerializeField] public GameObject tipObj;

    [SerializeField] private CButton jumpBtn1;

    private List<Tuple<int, string>> luckInfos = new List<Tuple<int, string>>() {
        new Tuple<int, string>(20, "双人动作"),
        new Tuple<int, string>(50, "金奖特效"),
        new Tuple<int, string>(80, "紫奖套装"),
        new Tuple<int, string>(140, "金奖套装"),
    };



    protected override void InitUI() {
        base.InitUI();
        //InitOtherCharacter();
        homePageSkinBtn.onClick.AddListener(ShowHomepageSkin);
        avatarBtn.onClick.AddListener(ShowPreview);

        jumpBtn1.gameObject.SetActive(BusinessLiveManager.Inst.IsActivityLive(((int)ActivityId.NewYearsTurntable2026).ToString()));
        jumpBtn1.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.ActivityCenterPanel, ActivityId.NewYearsTurntable2026.ToString());
        });
    }

    private void Start()
    {
        InitCharacter();
    }

    protected override void InitBG() {
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(bgRootNode);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomTextureBg("Assets/Loadable/UI/UIPanel/ZongXiaFuYao/Bg.png");
        item.gameObject.SetActive(true);
    }
    //展示头像框
    public void ShowPreview()
    {
        var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
        panel.PreviewAvatarFrame(AvatarFrameType.AvatarFrameS9DragonBoatLottery);
    }
    //展示主页皮肤
    private void ShowHomepageSkin()
    {
        UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, ProfileTheme.ZongXiaFuYao);
    }
    private void InitOtherCharacter()
    {
        if (modelRoot == null)
        {
            LoggerUtils.LogError("modelRoot未设置");
            return;
        }

        // 获取玩家角色数据
        CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (avatarInfo == null)
        {
            LoggerUtils.LogError("无法获取玩家角色数据");
            return;
        }

        // 创建玩家角色
        otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
        otherCharacterWrap.SetParent(modelRoot.transform, true);
        otherCharacterWrap.Avatar.gameObject.name = "PlayerAvatarPreview";
        LoggerUtils.LogError("已创造玩家角色！");
        // 获取动画控制器
        otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
        if (otherAnimationCtrl == null)
        {
            LoggerUtils.LogError("无法获取PlayerAnimationCtrl组件");
            return;
        }
    }
    private void InitCharacter()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, modelRoot.transform);
        avatarCameraController.RotateTarget = modelRoot.transform;
        var pgcIds = new List<string>() {
            "10600123",
            "10500075",
            "11300348",
            "11400079",
            "11200041",
            "11700033",
            "10900479",
            "10400475",
        };
        foreach (var pgcId in pgcIds)
        {
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
    }
    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp) {
        base.OnGashaponInfoUpdate(infoRsp);

        int curNum = infoRsp.luckyProgressInfo?.start ?? 0;
        curNum %= 140;

        int lastNum = 0;
        int nextNum = 10;

        var nextRewardName = "大奖";

        for (int i = 0; i < luckInfos.Count; i++) {
            var luckInfo = luckInfos.ElementAt(i);
            if (luckInfo.Item1 > curNum) {
                nextNum = luckInfo.Item1;
                nextRewardName = luckInfo.Item2;
                break;
            }
            lastNum = luckInfo.Item1;
        }

        singleText.SetText(infoRsp.singleDrawDiscountedPrice.ToString());
        var fixedLuck = curNum - lastNum;
        needNumText.SetText((infoRsp.luckyProgressInfo.end - infoRsp.luckyProgressInfo.start).ToString());
        needNumText.SetPreferredSize();
        rewardText.SetLocalText($"次必得大奖");
        rewardText.SetPreferredSize();
        progressValueText.SetLocalText("抽取进度: {0}/{1}", infoRsp.luckyProgressInfo.start, infoRsp.luckyProgressInfo.end);
        progressValueTrans.sizeDelta = new Vector2((infoRsp.luckyProgressInfo.start * 1.0f / (infoRsp.luckyProgressInfo.end)) * 796, 30);
        
        //更新按钮显示、集齐完商品则隐藏
        if(infoRsp.luckyProgressInfo.start == infoRsp.luckyProgressInfo.end)
        {
            twistBtn.gameObject.SetActive(false);
            twist10Btn.gameObject.SetActive(false);
            tipsTxt.gameObject.SetActive(false);
            tipsDoneTxt.gameObject.SetActive(true);
            bottom.SetActive(false);
        }
        else
        {
            twistBtn.gameObject.SetActive(true);
            twist10Btn.gameObject.SetActive(true);
            tipsTxt.gameObject.SetActive(true);
            tipsDoneTxt.gameObject.SetActive(false);
            bottom.SetActive(true);
        }

        //更新按钮价格，设置折扣价格和原价
        if (infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice)
        {
            singleDiscountTag.SetActive(true);
            srcSingleText.gameObject.SetActive(true);
            srcSingleText.SetText(infoRsp.singleDrawPrice.ToString());
        }
        else
        {
            singleDiscountTag.SetActive(false);
            srcSingleText.gameObject.SetActive(false);
        }

        tenText.SetText(infoRsp.tenDrawDiscountedPrice.ToString());
        if (infoRsp.tenDrawDiscountedPrice != infoRsp.tenDrawPrice)
        {
            tenDiscountTag.SetActive(true);
            srcTenText.gameObject.SetActive(true);
            srcTenText.SetText(infoRsp.tenDrawPrice.ToString());
        }
        else
        {
            tenDiscountTag.SetActive(false);
            srcTenText.gameObject.SetActive(false);
        }

        if (infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice)
        {
            float DiscountRate =  (float)infoRsp.singleDrawDiscountedPrice/ (float)infoRsp.singleDrawPrice;
            if(DiscountRate > 0.6f)
            {
                GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时75折");
                GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时75折");
            }else if (DiscountRate > 0.45f)
            {
                GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时5折");
                GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时5折");
            }
            else
            {
                GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时4折");
                GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时4折");
            }
        }
    }
}
