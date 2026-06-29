using System;
using System.Collections.Generic;
using Basic.Extensions;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AnniversaryCumulativeItem : MonoBehaviour
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


    [Header("下方")]
    public Text Txt_CumulativePrice_Enanle;
    public Text Txt_CumulativePrice_Unanle;
    public CButton Btn_Claim;

    private RechargeLevelData _curData;
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/AnniversaryCumulativeView/icons.spriteatlas";

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
            Img_Icon.SetNativeSize();
            var btn = Img_Icon.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError("Btn为空！");
            }
            btn.onClick.AddListener(() =>
            {
                ShowPreview(rewardItem, 1);
            });
            Txt_RewardNum.text = "x" + rewardItem.rewardNum1;
        }
        else
        {
            Txt_RewardName.fontSize = 25;
            rewardObj1.SetActive(false);
            rewardObj2.SetActive(true);
            Reward2_Img_Icon.sprite =
                XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1, gameObject);
            Reward2_Img_Icon.GetComponent<Button>().onClick.AddListener(() =>
            {
                ShowPreview(rewardItem, 1);
            });
            Reward2_Img_Icon2.sprite =
                XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon2, gameObject);
            Reward2_Img_Icon2.GetComponent<Button>().onClick.AddListener(() =>
            {
                ShowPreview(rewardItem, 2);
            });
            Reward2_Img_Icon.SetNativeSize();
            Reward2_Img_Icon2.SetNativeSize();
            Reward2_RewardNum.text = "x" + rewardItem.rewardNum1;
        }

        Txt_CumulativePrice_Enanle.text = rewardItem.progress + "元";
        Txt_CumulativePrice_Unanle.text = rewardItem.progress + "元";
        previewImage.gameObject.SetActive(rewardItem.hasPreview == 1);
    }

    void ShowPreview(RewardItem rewardItem, int index)
    {
        switch (rewardItem.rewardId)
        {
            case "1":
            case "2":
            case "4"://单个货币
                UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, (CurrencyType)rewardItem.rewardType1);
                break;
            case "3"://货币 + 动作
                if(index == 1)
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, (CurrencyType)rewardItem.rewardType1);
                }
                else
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string> { "40300502" }, "电击枪", "djq", spriteatlasPath, "周年庆累充福利", "#3586FF", "bg");
                }
                break;
            case "5"://货币 + 头像框
                if (index == 1)
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, (CurrencyType)rewardItem.rewardType1);
                }
                else
                {
                    var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                    panel.PreviewAvatarFrame(AvatarFrameType.AvatarFrameScarletEleg);
                }
                break;
            case "6"://货币 + 聊天气泡
                if (index == 1)
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, (CurrencyType)rewardItem.rewardType1);
                }
                else
                {
                    var bubblePreviewPanel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                    bubblePreviewPanel.PreviewChatBubble(ChatBubblesType.ChatBubblesScarletElegy, "猩红挽歌气泡", "通过周年庆累充活动获得，可前往个人资料使用");
                }
                break;


            case "7"://主页皮肤
                UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, ProfileTheme.ScarletElegy);
                break;
        }

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
                Img_Effect.color = new Color(255, 255, 255, 0);
                Effect_animator.CrossFade("LoginGiftPanel_done", 0.1f);
                Effect_animator.enabled = false;
                break;
        }

        Go_Price_Enable.SetActive(status != ClaimStatus.Lock);
        Go_Price_Unable.SetActive(status == ClaimStatus.Lock);
    }
}