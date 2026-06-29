using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Avatar;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 特殊活动，扭蛋和活动均有
/// </summary>
public class ElfFrostView : BaseGashaponView
{
    [SerializeField] private Transform itemRoot;

    [SerializeField] private ElfFrostItem itemPrefab;

    [SerializeField] private Transform characterRoot;

    [SerializeField] private AvatarCameraController avatarCameraController;

    [SerializeField] private CButton previewBtn;

    [SerializeField] private CButton drawBtn;
    [SerializeField] private Text singleTip;
    [SerializeField] private Text ownAllTip;
    [SerializeField] private CButton ruleBtn;
    [SerializeField] private Text castText;
    [SerializeField] private CButton jumpBtn1;
    private List<ElfFrostItem> elfFrostItems = new List<ElfFrostItem>();

    //private ActivityInfo activityInfo;
    //private GashaponData gashaponData;
    //private GashaponInfoRsp gashaponInfoRsp;
    private PlayerAnimationCtrl animationCtrl;
    private string BgTexturePath => GashaponUtils.ViewBasePath + "ElfFrost/BG_Texture.png";
    private string RulePath => GashaponUtils.ViewBasePath + "ElfFrost/Rule.json";

    private string[] pgcids = new string[] { "11300217", "10800078", "10900310", "10400309", "11400039", "10500017", "11000004", "11000005", "11300001" };
    //public override void Init(ActivityInfo info) {
    //    base.Init(info);
    //    activityInfo = info;
    //    InitUI();
    //    InitData();
    //    InitAvatar();
    //}

    public override void OnCreate(string id)
    {
        base.OnCreate(id);
       InitSelfUI();
        //InitData();
        InitAvatar();

        jumpBtn1.gameObject.SetActive(BusinessLiveManager.Inst.IsActivityLive(((int)ActivityId.NewYearsTurntable2026).ToString()));
        jumpBtn1.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.ActivityCenterPanel, ActivityId.NewYearsTurntable2026.ToString());
        });
        // gashaponData.CurrencyType = CurrencyType.PurpleDreamCoin;
    }

    private GashaponRewardData GetRewardData(string pgcid)
    {
        foreach (var rewardInfo in gashaponData.RewardList)
        {
            if(rewardInfo.Id == pgcid)
            {
                return rewardInfo;
            }
        }
        return null;
    }

    private void InitSelfUI() {
        for(int i=0;i<pgcids.Length;i++)
        {
            var rewardInfo = GetRewardData(pgcids[i]);
            if(rewardInfo != null)
            {
                var item = Instantiate(itemPrefab, itemRoot);
                item.gameObject.SetActive(true);
                item.Init(rewardInfo, (pgcId) => {
                    OnPreviewClicked();
                });
                elfFrostItems.Add(item);
            }
        }

        itemPrefab.gameObject.SetActive(false);
        previewBtn.onClick.AddListener(OnPreviewClicked);
        drawBtn.onClick.AddListener(OnDrawClicked);
        ruleBtn.onClick.AddListener(OnRuleClicked);
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        base.OnGashaponInfoUpdate(infoRsp);
        RefreshUI();
    }

    //private void InitData() {
    //    JObject extra = JObject.FromObject(activityInfo.extra);
    //    string gashaponId = extra["gashaponId"]?.ToString();
    //    gashaponData = GashaponDataManager.Inst.gashaponData(gashaponId);
    //    RefreshData();
    //}


    private void InitAvatar() {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        foreach (var rewardInfo in gashaponData.RewardList) {
            //if (string.IsNullOrEmpty(rewardInfo.pgcId) || rewardInfo.extra == null) {
            //    continue;
            //}

            //JObject extra = JObject.FromObject(rewardInfo.extra);
            //if (extra["level"] != null) {
            //    var level = extra["level"].Value<int>();
            //    if (level != 1 && level != 2) {
            //        continue;
            //    }
            //}
            var level = (int)rewardInfo.Level;
            if (level != 1 && level != 2)
            {
                continue;
            }

            var pgcConfig = PgcUtils.GetPgcConfigData(rewardInfo.Id);
            if (pgcConfig.ResourceType != (int)ResourceType.Avatar) {
                continue;
            }
            var config = DataTables.GetAvatarCommonData(rewardInfo.Id);
            var classType = UniqueType.GetAvatar(rewardInfo.Id);
            characterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType), rewardInfo.Id);
            characterWrapper.ChangeColor(classType, config.defaultColor);
            characterWrapper.Move(classType, config.pDef);
            characterWrapper.Rotate(classType, config.rDef);
            characterWrapper.Scale(classType, config.sDef);
            characterWrapper.HVScale(classType, config.vhSDef);
            characterWrapper.SetLeftOrRight(classType, config.leftRightType);

        }

        characterWrapper.SetParent(characterRoot, true);
        animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        characterWrapper.Avatar?.GetComponentInChildren<CustomBodyTypeController>()?.ApplyBodyType(CustomBodyTypeController.BodyType.None);
        avatarCameraController.RotateTarget = characterRoot;
        avatarCameraController.isMoveEnabled = false;
        avatarCameraController.isZoomEnabled = false;
    }

    private void OnPreviewClicked() {
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam {
            bgPath = BgTexturePath,
            title = gashaponData.Name,
            gashaponData = gashaponData,
            rewardCurrency = CurrencyType.PurpleDreamCoin,
            rulePath = RulePath
        });
        previewPanel.SetAnimPreviewBtnColor(new Color32(0,93,134, 255), new Color32(255,192,31,255), new Color32(38,190, 255, 255));
    }

    private void OnDrawClicked() {
        if (gashaponData == null) {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)gashaponData.CurrencyType, gashaponInfoRsp.singleDrawPrice)) {
            if (gashaponData.CurrencyType == CurrencyType.GreenCoin) {
                TipPanel.ShowToast("当前创作者币余额不足");
                return;
            }

            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)gashaponData.CurrencyType,
                gashaponInfoRsp.singleDrawPrice);
            return;
        }

        var gId = gashaponData.Id;
        GashaponDataManager.Inst.RequestGashapon(gId, OneTimeGasha, OnGashaOnceRsp);
    }


    private void OnRuleClicked() {
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, RulePath);
    }


    private void RefreshData() {
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponData.Id, OnGetGashaponInfoSuccess);
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp) {
        if (!this || gashaponRsp == null || gashaponRsp.rewardList == null) {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel, new GashaponTwistAnimParam() {
            gashaponId = gashaponData.Id,
            bgPath = BgTexturePath
        });
        panel.PlayOneTwistAnimation(gashaponRsp.rewardList, () => {
            OnGashaTwistAnimComplete(gashaponRsp);
        });

        AccountDataManager.Inst.RefreshUserInfo();
    }

    private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp) {
        UIManager.Inst.ClosePanel(PanelId.GashaponTwistAnimPanel);
        if (gashaponRsp == null || gashaponRsp.rewardList == null) {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponData.Id, gashaponRsp);
        RefreshData();
    }

    private void OnGetGashaponInfoSuccess(GashaponInfoRsp rsp) {
        gashaponInfoRsp = rsp;
        drawBtn.gameObject.SetActive(true);
        var drawText = GameObjectEx.FindComponentByName<Text>(drawBtn.transform, "Content/Text");
        drawText.SetText(rsp.singleDrawPrice.ToString());
        drawText.SetPreferredSize();
        RefreshUI();
    }


    private void RefreshUI() {
        if (gashaponData == null) {
            return;
        }

        bool isOwnAll = gashaponData.RewardList.All(GashaponUtils.IsOwnedReward);
      //  Debug.LogError("elffrostview refreshui isownall=" + isOwnAll);
        if (isOwnAll) {
            drawBtn.gameObject.SetActive(false);
            ownAllTip.gameObject.SetActive(true);
            singleTip.gameObject.SetActive(false);
        } else {
            drawBtn.gameObject.SetActive(true);
            ownAllTip.gameObject.SetActive(false);
            singleTip.gameObject.SetActive(true);
        }
        castText.SetText(gashaponInfoRsp?.singleDrawPrice.ToString());
        foreach (var elfFrostItem in elfFrostItems) {
            elfFrostItem.RefreshOwnedMark();
        }
    }




}
