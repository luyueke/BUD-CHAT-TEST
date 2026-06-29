using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Avatar;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;


public class SpringSmallView : NewDefaultGashaponView
{
    [SerializeField] private Transform itemRoot;

    [SerializeField] private SpringSmallItem itemPrefab;

    [SerializeField] private Transform characterRoot;

    [SerializeField] private AvatarCameraController avatarCameraController;

    [SerializeField] private CButton previewBtn;
    [SerializeField] private CButton springLimitBtn;

    [SerializeField] private Text singleTip;
    [SerializeField] private Text ownAllTip;
    [SerializeField] private CButton ruleBtn;
    [SerializeField] private Text singleDiscountTagTxt;


    private List<SpringSmallItem> springSmallItems = new List<SpringSmallItem>();

    private PlayerAnimationCtrl animationCtrl;
    private List<RewardItem> _rewardItems = new List<RewardItem>();


    private bool isInit = false;

    protected override void InitUI()
    {
        GenerateContent();
        SetupUI();
        InitData();
        InitAvatar();
    }


    private void SetupUI()
    {
        twistBtn = GameObjectEx.FindChildByName(transform, "TwistBtn").GetComponent<CButton>();
        singleIcon = GameObjectEx.FindChildByName(twistBtn.transform, "icon").GetComponent<Image>();
        singleText = GameObjectEx.FindChildByName(twistBtn.transform, "num").GetComponent<Text>();
        srcSingleText = GameObjectEx.FindChildByName(twistBtn.transform, "srcNum").GetComponent<Text>();
        singleDiscountTag = GameObjectEx.FindChildByName(twistBtn.transform, "TagDiscount").gameObject;
        // singleDiscountTagTxt = GameObjectEx.FindChildByName(twistBtn.transform, "TagDiscountTxt").GetComponent<Text>();
        twistBtn.onClick.AddListener(OnTwistClick);

        itemPrefab.gameObject.SetActive(false);
        previewBtn.onClick.AddListener(OnPriviewBtnClick);
        springLimitBtn.onClick.AddListener(OnSpringLimitClicked);
        ruleBtn.onClick.AddListener(OnRuleClicked);
        UpdateBtnUI();
    }
    
    private void GenerateContent()
    {
        if (isInit)
        {
            return;
        }

        isInit = true;
        string jsonPath =
            "Assets/Loadable/UI/UIPanel/GashaponPanel/SpringSmall/SpringSmallData.json";
        var ugcAsset =
            Loader.Load<TextAsset>(
                jsonPath, this.gameObject);
        _rewardItems = JsonConvert.DeserializeObject<List<RewardItem>>(ugcAsset.text);

        for (var i = 0; i < _rewardItems.Count; i++)
        {
            var item = Instantiate(itemPrefab, itemRoot);
            item.gameObject.SetActive(true);
            item.Init(_rewardItems[i], (pgcId) => { OnPriviewBtnClick(); });
            springSmallItems.Add(item);
        }
    }


    private void InitData()
    {
        RefreshUI(gashaponInfoRsp);
    }


    private void InitAvatar()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        foreach (var rewardInfo in gashaponData.RewardList)
        {
            List<AssetsData> pgcDatas = rewardInfo.PgcDatas;
            if (pgcDatas == null || pgcDatas.Count <= 0)
            {
                continue;
            }

            string pgcId = pgcDatas.First().Id;

            var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
            if (pgcConfig == null)
            {
                continue;
            }

            if (pgcConfig.ResourceType != (int)ResourceType.Avatar)
            {
                continue;
            }

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

        characterWrapper.SetParent(characterRoot, true);
        animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        avatarCameraController.RotateTarget = characterRoot;
        avatarCameraController.isMoveEnabled = false;
        avatarCameraController.isZoomEnabled = false;
    }

    private void OnSpringLimitClicked()
    {
        UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.SpringLimited);
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        gashaponInfoRsp = infoRsp;
        twistBtn.gameObject.SetActive(true);
        singleText.SetText(infoRsp.singleDrawDiscountedPrice.ToString());
        //更新按钮价格
        if (infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice) {
            singleDiscountTag.SetActive(true);
        
            // 计算折扣，格式化为保留最多一位小数
            float discount = (float)infoRsp.singleDrawDiscountedPrice / infoRsp.singleDrawPrice * 10;
            string discountText = discount % 1 == 0 
                ? $"{(int)discount}折"         // 如果是整数，显示整数折扣
                : $"{discount:F1}折";         // 否则保留一位小数
            singleDiscountTagTxt.text = discountText; // 格式化保留一位小数
            srcSingleText.gameObject.SetActive(true);
            srcSingleText.SetText(infoRsp.singleDrawPrice.ToString());
        } else {
            singleDiscountTag.SetActive(false);
            srcSingleText.gameObject.SetActive(false);
        }

        RefreshUI(gashaponInfoRsp);
    }

    private void UpdateBtnUI()
    {
        if (gashaponData == null) return;
        var iconSprite = PgcUtils.LoadCurrencyIcon((int)gashaponData.CurrencyType, this.gameObject);
        if (iconSprite != null)
        {
            singleIcon.sprite = iconSprite;
        }

        singleText.SetText(gashaponData.SinglePrice.ToString());
    }

    protected override void OnPriviewBtnClick()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel,
            new GashaponPreviewParam
            {
                bgPath = viewCfg.BgPath,
                title = gashaponData.Name,
                gashaponData = gashaponData,
                gashaponInfoRsp = gashaponInfoRsp,
                rewardCurrency = gashaponData.CurrencyType,
                rulePath = viewCfg.RulePath,
            });
        previewPanel.SetBundleViewBgClolr(bundleViewBgColor);
    }

    private void OnTwistClick()
    {
        if (gashaponInfoRsp == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        SendGashaponRequestOnce(gashaponData, gashaponInfoRsp.singleDrawDiscountedPrice);
    }


    private void OnRuleClicked()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var rulePath = viewCfg.RulePath;
        if (string.IsNullOrEmpty(rulePath))
        {
            rulePath = "Assets/Loadable/UI/UIPanel/GashaponRulePanel/Rules/DefaultRule.json";
        }

        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }
    
    private void RefreshUI(GashaponInfoRsp gashaponInfoRsp)
    {
        if (gashaponData == null || gashaponInfoRsp?.rewardPool == null)
        {
            return;
        }
    
        // 判断是否全部拥有
        bool isOwnAll = _rewardItems.All(_rewardItem =>
        {
            var drawnInfo = gashaponInfoRsp.rewardPool.Find(tmp => tmp.rewardId.ToString() == _rewardItem.rewardId);
            return drawnInfo != null && drawnInfo.everDrawn == 1;
        });
    
        // 更新按钮和提示显示
        twistBtn.gameObject.SetActive(!isOwnAll);
        ownAllTip.gameObject.SetActive(isOwnAll);
        singleTip.gameObject.SetActive(!isOwnAll);
    
        // 刷新 springSmallItems 的拥有标记
        foreach (var springSmallItem in springSmallItems)
        {
            springSmallItem.RefreshOwnedMark(gashaponInfoRsp);
        }
    }
}