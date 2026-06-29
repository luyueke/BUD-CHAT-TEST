using Basic.Extensions;
using Game.Event;
using System;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class SeasonCumulativeItem : MonoBehaviour
{
    [Header("上方")] public GameObject Go_Normal_Bg;
    public GameObject Go_Enable_Bg;
    public GameObject Go_Claimed;
    public GameObject Go_Lock;
    public GameObject Go_OptionTip;
    public Image Img_Icon;
    public Text Txt_RewardName;
    public Text Txt_RewardNum;
    public CButton Btn_Preview;
    public GameObject Go_Price_Enable;
    public GameObject Go_Price_Unable;
    public Animator Effect_animator;
    public Image Img_Effect;
    public Image previewImage;
    public GameObject rewardObj1;
    public GameObject rewardObj2;
    public Image Reward2_Img_Icon;
    public Image Reward2_Img_Icon2;
    public Text Reward2_RewardNum;


    [Header("下方")] public Text Txt_CumulativePrice;
    public CButton Btn_Claim;

    private RechargeLevelData _curData;
    private string spriteatlasPath = RechargePanel.CumulativeRechargePanelAtlas;

    private RewardItem _rewardItem;


    public int eventId { get; private set; }


    private void Awake()
    {
    }

    public void Refresh(ActivityEventInfo eventInfo)
    {
        var claimStatus = (ClaimStatus)eventInfo.eventStatus;
        RefreshClaimStatus(claimStatus);
    }

    public void OnInitCreate(RewardItem rewardItem)
    {
        _rewardItem = rewardItem;
        Txt_RewardName.text = rewardItem.title;
        if (rewardItem.rewardIcon2.IsNullOrEmpty())
        {
            rewardObj1.SetActive(true);
            rewardObj2.SetActive(false);
            Img_Icon.sprite =
                XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1, gameObject);
            Txt_RewardNum.text = "x" + rewardItem.rewardNum1;
        }
        else
        {
            rewardObj1.SetActive(false);
            rewardObj2.SetActive(true);
            Reward2_Img_Icon.sprite =
                XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1, gameObject);
            Reward2_Img_Icon2.sprite =
                XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon2, gameObject);
            Reward2_RewardNum.text = "x" + rewardItem.rewardNum1;
            //if(rewardItem.rewardIcon2 == "season_4") //特殊处理下，防止变形
            {
                Reward2_Img_Icon2.SetNativeSize();
            }

        }

        Txt_CumulativePrice.text = rewardItem.progress + "元";
        previewImage.gameObject.SetActive(rewardItem.hasPreview == 1);

        Btn_Preview.onClick.RemoveAllListeners();
        Btn_Preview.onClick.AddListener((() =>
        {

            if (_rewardItem.hasPreview != 1) return;
            RewarPreview(_rewardItem.rewardId);
        }));
    }

    public void Init(ActivityEventInfo eventInfo, Action<ActivityEventInfo, RewardItem> claimAction)
    {
        
        eventId = eventInfo.eventId;

        var claimStatus = (ClaimStatus)eventInfo.eventStatus;
        RefreshClaimStatus(claimStatus);
        Btn_Claim.onClick.RemoveAllListeners();
        Btn_Claim.onClick.AddListener(() => { claimAction.Invoke(eventInfo, _rewardItem); });
    }
    

    private void RefreshClaimStatus(ClaimStatus status)
    {
        switch (status)
        {
            case ClaimStatus.Claimed:
                Go_Normal_Bg.SetActive(true);
                Go_Enable_Bg.SetActive(false);
                Go_Claimed.SetActive(true);
                Go_Lock.SetActive(false);
                Btn_Claim.gameObject.SetActive(false);
                Btn_Preview.gameObject.SetActive(false);
                Img_Effect.color = new Color(255, 255, 255, 0);
                Effect_animator.CrossFade("LoginGiftPanel_done", 0.1f);
                Effect_animator.enabled = false;
                Go_OptionTip.gameObject.SetActive(false);
                break;

            case ClaimStatus.Unlocked:
                Go_Normal_Bg.SetActive(false);
                Go_Enable_Bg.SetActive(true);
                Go_Claimed.SetActive(false);
                Go_Lock.SetActive(false);
                Btn_Claim.gameObject.SetActive(true);
                Btn_Preview.gameObject.SetActive(false);
                Effect_animator.enabled = true;
                Effect_animator.CrossFade("LoginGiftPanel_prompt", 0.1f);
                break;

            default:
            case ClaimStatus.Lock:
                Go_Normal_Bg.SetActive(true);
                Go_Enable_Bg.SetActive(false);
                Go_Claimed.SetActive(false);
                Go_Lock.SetActive(true);
                Btn_Claim.gameObject.SetActive(false);
                Btn_Preview.gameObject.SetActive(true);
                Img_Effect.color = new Color(255, 255, 255, 0);
                Effect_animator.CrossFade("LoginGiftPanel_done", 0.1f);
                Effect_animator.enabled = false;
                break;
        }

        Go_Price_Enable.SetActive(status != ClaimStatus.Lock);
        Go_Price_Unable.SetActive(status == ClaimStatus.Lock);
    }

    void RewarPreview(string id) {
        if(SeasonPassDataManager.Inst.CurrentSeasonPassType == SeasonPassType.S14SeasonPass)
        {
            switch (id)
            {
                case "1":
                    var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                    panel.UpdateUI(PgcUtils.LoadRewardIcon(BUDRewardType.RewardVipFreeTrail, panel.gameObject), "VIP体验卡1天", null, "特殊权益，领取后VIP月卡有效期增加1天");
                    break;
                case "2":
                    PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""));
                    break;
                case "3":
                    HeadCycleData headData3 = UserUIWidgetManager.Inst.GetHeadCycleDataById(49);
                    if (headData3 != null)
                    {
                        PreviewManager.Inst.ShowAvatarFramePreview(headData3.Id);
                    }
                    break;
                case "4":
                    PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""));
                    break;
                case "5":
                    GameChatBubbleData chatData5 = UserUIWidgetManager.Inst.GetChatDataByID(29);
                    if (chatData5 != null)
                    {
                        var tem5 = new RewardPreviewInfo(BUDRewardType.RewardChatBubbles, CurrencyType.None, chatData5.PgcId, chatData5.Name, "");
                        tem5.SetTitleAndDes(chatData5.Name, chatData5.Desc);
                        PreviewManager.Inst.ShowPreview(tem5);
                    }
                    break;
                case "6":
                    UIManager.Inst.OpenPanel<ProfileTitlePreviewPanel>(PanelId.ProfileTitlePreviewPanel, 29);
                    break;
                case "7":
                    var parentView = GetComponentInParent<SeasonCumulativeView>();
                    var theme = (parentView != null && parentView.CurRechargeId == RechargeId.S14SeasonRecharge)
                        ? ProfileTheme.S14Season
                        : ProfileTheme.S15Season;
                    UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, theme);
                    break;


            }
        }
        else if(SeasonPassDataManager.Inst.CurrentSeasonPassType == SeasonPassType.S15SeasonPass)
        {
             switch (id)
            {
                case "1":
                    var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                    panel.UpdateUI(PgcUtils.LoadRewardIcon(BUDRewardType.RewardVipFreeTrail, panel.gameObject), "VIP体验卡1天", null, "特殊权益，领取后VIP月卡有效期增加1天");
                    break;
                case "2":
                    PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""));
                    break;
                case "3":
                    HeadCycleData headData3 = UserUIWidgetManager.Inst.GetHeadCycleDataById(49);
                    if (headData3 != null)
                    {
                        PreviewManager.Inst.ShowAvatarFramePreview(headData3.Id);
                    }
                    break;
                case "4":
                    PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""));
                    break;
                case "5":
                    GameChatBubbleData chatData5 = UserUIWidgetManager.Inst.GetChatDataByID(29);
                    if (chatData5 != null)
                    {
                        var tem5 = new RewardPreviewInfo(BUDRewardType.RewardChatBubbles, CurrencyType.None, chatData5.PgcId, chatData5.Name, "");
                        tem5.SetTitleAndDes(chatData5.Name, chatData5.Desc);
                        PreviewManager.Inst.ShowPreview(tem5);
                    }
                    break;
                case "6":
                    UIManager.Inst.OpenPanel<ProfileTitlePreviewPanel>(PanelId.ProfileTitlePreviewPanel, 29);
                    break;
                case "7":
                    UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, ProfileTheme.S15Season);
                    break;
            }
        }
    }
}