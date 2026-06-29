using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class CumulativeRechargeItem : MonoBehaviour
{
    [Header("上方")]
    public GameObject Go_Normal_Bg;
    public GameObject Go_Enable_Bg;
    public GameObject Go_Claimed;
    public GameObject Go_Lock;
    public GameObject Go_OptionTip;
    public GameObject IsOption;//二选一显示
    public Image Img_Icon;
    public Text Txt_RewardName;
    public Text Txt_RewardNum;
    public CButton Btn_Preview;
    public GameObject Go_Price_Enable;
    public GameObject Go_Price_Unable;
    public Animator Effect_animator;
    public Image Img_Effect;

    [Header("下方")] 
    public Text Txt_CumulativePrice;
    public CButton Btn_Claim;

    private RechargeLevelData _curData;

    private void Awake()
    {
        Btn_Claim.onClick.AddListener(OnBtnClaimClick);
        Btn_Preview.onClick.AddListener(OnBtnPreviewClick);
    }

    public void InitData(RechargeLevelData data)
    {
        this._curData = data;
        Txt_RewardName.text = data.itemName;
        Txt_CumulativePrice.text = data.rechargeNum.ToString() + "元";
        Go_OptionTip.SetActive(data.isOptionalReward == 1);
        RefreshClaimStatus((ClaimStatus)data.rewardStatus);

        if ((BUDRewardType)data.rewardType == BUDRewardType.RewardPinkCoin)
        {
            Txt_RewardNum.gameObject.SetActive(true);
        }
     
        IsOption.SetActive(data.id == 12);//称号二选一 
        
    }

    private void OnBtnClaimClick()
    {
        if (_curData.isOptionalReward == 0)
        {
            var rewardId = _curData.id;
            var pgcId = _curData.pgcId;
            ClaimSingleItem(rewardId, pgcId);
        }
        else
        {
            if (_curData.isOptionalReward == 1)
            {
                GoOptionalClaimRewardPage();
            }
            else
            {
                GoOfficalClaimRewardPage();
            }
        }
    }

    private void ClaimSingleItem(int rewardId, int pgcId)
    {
        JObject jb = new JObject
        {
            ["rewardId"] = rewardId,
            ["pgcId"] = pgcId
        };
        var rewardType = (BUDRewardType)_curData.rewardType;
        Sprite sp = null;
        
        if (rewardType == BUDRewardType.RewardPgcResource) {
            sp = PgcUtils.GetIconSpriteByPgcId(pgcId.ToString(), gameObject);
        } else
        {
            sp = PgcUtils.LoadRewardIcon(rewardType, gameObject);
        }
        
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimRechargeBenifits,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                var rewardItemDatas = new List<CommonRewardItemData>();
                var itemData = new CommonRewardItemData()
                {
                    IconSp = sp,
                    RewardAmount = 1,
                    rewardName = this._curData.itemName
                };

                if ((BUDRewardType)_curData.rewardType == BUDRewardType.RewardPinkCoin)
                    itemData.RewardAmount = 200;
                
                rewardItemDatas.Add(itemData);
                
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(rewardItemDatas);
                
                MessageHelper.Broadcast(MessageName.RefreshCumData);
                
                //领取成功后，设置为不可领的状态
                RefreshClaimStatus(ClaimStatus.Claimed);
            }, onFail: arg0 =>
            {
            });
    }

    private void GoOptionalClaimRewardPage()
    {
        var panel = UIManager.Inst.OpenPanel<OptionalClaimRewardPanel>(PanelId.OptionalClaimRewardPanel);
        panel.SetPreviewData(_curData);
    }

    private void GoOfficalClaimRewardPage()
    {
        var panel = UIManager.Inst.OpenPanel<OfficalClaimRewardPanel>(PanelId.OfficalClaimRewardPanel);
        panel.SetPreviewData(_curData);
    }

    private void OnBtnPreviewClick()
    {
        if (_curData.isOptionalReward == 1)
        {
            GoOptionalClaimRewardPage();
        }
        else
        {
            GoOfficalClaimRewardPage();
        }
    }

    public void SetIcon(int index)
    {
        var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(RechargePanel.CumulativeRechargePanelAtlas, "Cumulative_Icon_" + index, this.gameObject);
        if (sp)
        {
            Img_Icon.sprite = sp;
        }
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
                Effect_animator.CrossFade("LoginGiftPanel_done",0.1f);
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
                Effect_animator.CrossFade("LoginGiftPanel_prompt",0.1f);
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
                Effect_animator.CrossFade("LoginGiftPanel_done",0.1f);
                Effect_animator.enabled = false;
                break;
        }
        Go_Price_Enable.SetActive(status != ClaimStatus.Lock);
        Go_Price_Unable.SetActive(status == ClaimStatus.Lock);
    }

}
