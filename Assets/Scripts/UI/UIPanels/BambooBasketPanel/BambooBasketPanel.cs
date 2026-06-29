using System.Collections.Generic;
using Game.Audio;
using Game.Avatar;
using GameData;
using GameData.Gashapon;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class BambooBasketPanel : BaseGashaponView
{
    [SerializeField] protected string bundleViewBgColor = "#FFFFFF";
    [SerializeField] private GameObject playerImageView;
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;
    [SerializeField] private CButton TwistBtn;
    [SerializeField] private Text Txt_Price;
    [SerializeField] private Text Txt_Tips;
    [SerializeField] private CButton InfoBtn;
    [SerializeField] private CButton PreviewBtn;
    [SerializeField] private CButton ItemBtn;
    [SerializeField] private List<Image> RewardItemBgs;
    [SerializeField] private CButton PreVideoBtn;
    private string _pgcId = "40900006";
    private bool _isInit;
    private List<string> _rewardIds = new List<string>(){ "10400474", "11300375", "10900312", "11000284", "11000285", 
    "12000038", "11000286", "10700024", "RewardCommunityCoin_30"};

    internal CharacterWrap characterWrap;
    internal CharacterWrap otherCharacterWrap;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;
    private const string videoPath = "Assets/Loadable/Demand3D/ResVideo/bamboo/bamboo.mp4";

    public override void OnCreate(string id)
    {
        base.OnCreate(id);

        InitWrapper();

        TwistBtn.onClick.AddListener(OnTwistClick);
        InfoBtn.onClick.AddListener(OnInfoClick);
        PreviewBtn.onClick.AddListener(OnPreviewBtnClick);
        ItemBtn.onClick.AddListener(OnPreviewBtnClick);
        PreVideoBtn?.onClick.AddListener(OnPriviewVideoBtnClick);
        ShowSpecialAnim();
        _isInit = true;
    }
    private void OnPriviewVideoBtnClick()
    {
        UIManager.Inst.OpenPanel<VideoPreviewPanel>(PanelId.VideoPreviewPanel, videoPath);
    }
    private void OnEnable()
    {
        if (_isInit) ShowSpecialAnim();
    }

    private void OnDisable()
    {
        StopAllEmoteSound();
    }

    private void InitWrapper()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData != null && characterWrap == null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrap.SetParent(characterRoot, true);
            animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;
            animationCtrl.gameObject.SetActive(true);

            otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(AccountDataManager.Inst.UserInfo.otherAvatarInfo);
            otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherCharacterWrap.Avatar.SetActive(false);
        }
    }

    private void ShowSpecialAnim()
    {
        if (characterWrap == null) return;
        characterWrap.Avatar.SetActive(true);
        otherCharacterWrap.Avatar.SetActive(true);
        animationCtrl.PlayLinkEmoteForUICharacter(_pgcId, SpecialAnim.Run, otherAnimationCtrl);
        avatarCameraController.ZoomCustom(new Vector3(0, 90, 0), 1.2f);
    }

    private void StopAllEmoteSound()
    {
        if (animationCtrl != null && animationCtrl.gameObject != null)
            AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
        if (otherAnimationCtrl != null && otherAnimationCtrl.gameObject != null)
            AkSoundManager.Inst.StopAll(otherAnimationCtrl.gameObject);
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        base.OnGashaponInfoUpdate(infoRsp);
        Txt_Price.SetText(infoRsp.singleDrawDiscountedPrice.ToString());
        RefreshRewardDrawnStatus(infoRsp);
        if (infoRsp.luckyProgressInfo.start == infoRsp.luckyProgressInfo.end)
        {
            Txt_Tips.SetText("恭喜！你已集齐竹韵青篓奖池所有商品！");
            TwistBtn.gameObject.SetActive(false);
        }
        else
        {
            TwistBtn.gameObject.SetActive(true);
        }
    }

    private void RefreshRewardDrawnStatus(GashaponInfoRsp infoRsp)
    {
        if (infoRsp?.rewardPool == null || gashaponData?.RewardList == null) return;
        foreach (var bg in RewardItemBgs)
            bg.enabled = false;

        foreach (var drawnInfo in infoRsp.rewardPool)
        {
            if (drawnInfo.everDrawn <= 0) continue;

            var rewardData = gashaponData.RewardList.Find(r => r.RewardId == drawnInfo.rewardId);
            var pgcId = rewardData != null ? rewardData.Id : drawnInfo.pgcId;
            int index = _rewardIds.IndexOf(pgcId);
            if (index >= 0 && index < RewardItemBgs.Count)
            {
                RewardItemBgs[index].enabled = true;
            }
        }
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

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        base.OnGashaOnceRsp(gashaponRsp);
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var animPanel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
            new GashaponTwistAnimParam
            {
                gashaponId = gashaponData.Id,
                bgPath = viewCfg?.BgPath
            });
        animPanel.PlayOneTwistAnimation(gashaponRsp.rewardList, () =>
        {
            UIManager.Inst.ClosePanel(PanelId.GashaponTwistAnimPanel);
            GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
            if (gashaponRsp?.rewardList != null)
            {
                var rewardPanel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
                rewardPanel.ShowRewards(gashaponData.Id, gashaponRsp);
            }
        });
    }

    private void OnInfoClick()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, viewCfg.RulePath);
    }

    private void OnPreviewBtnClick()
    {
        StopAllEmoteSound();
        characterWrap?.Avatar.SetActive(false);
        otherCharacterWrap?.Avatar.SetActive(false);

        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = viewCfg.BgPath,
            title = gashaponData.Name,
            gashaponData = gashaponData,
            gashaponInfoRsp = gashaponInfoRsp,
            rewardCurrency = CurrencyType.PurpleDreamCoin,
            rulePath = viewCfg.RulePath,
            onBackCallBack = ShowSpecialAnim,
        });
        if (previewPanel != null)
            previewPanel.SetBundleViewBgClolr(bundleViewBgColor);
    }
}
