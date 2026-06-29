using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Es;
using UI.Manager;

public class CreatorWayItem : MonoBehaviour
{
    public Image Img_Icon;
    /// <summary>
    /// 显示数字
    /// </summary>
    public Text Txt_Title;
    /// <summary>
    /// 显示中文
    /// </summary>
    public Text Txt_Title_CN;
    public GameObject LockMask;
    public GameObject Icon_Received;
    public GameObject Icon_Lock;
    public GameObject Icon_AvailableBg;
    public Button Btn_Item;
    public Animator animator;
    public Image ImgEffect;
    public Image ImgLine;
    public Image ImgCircal;
    public Text LevelText;
    
    public CreatorCenterTaskLocalInfo info;
    public Sprite rewardSp;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/CreatorCenterPanel/CreatorCenterPanel.spriteatlas";
    private Action<CreatorCenterTaskLocalInfo> ClickAction;

    private void OnEnable()
    {
        // if (curData != null)
        // {
        //     SetState(curData.BudRewardStatus);
        // }
    }

    public void Init(CreatorCenterTaskLocalInfo info, Action<CreatorCenterTaskLocalInfo> ClickAction)
    {
        this.info = info;
        this.ClickAction = ClickAction;
        var iconName = info.rewardType.ToString();
        if (!string.IsNullOrEmpty(iconName))
        {
            rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, this.gameObject);
        }
        if (rewardSp == null) {
            LoggerUtils.LogError("rewardSp is null:" + rewardSp + "," + info.rewardType);
        }
        if (this.info.rewardType == (int)BUDRewardType.RewardAvatarFrame)
        {
             UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(info.pgcId,gameObject, (sp) =>
             {
                 Img_Icon.sprite = sp;
             });
        }
        else if (this.info.rewardType == (int)BUDRewardType.RewardChatBubbles)
        {
            UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(info.pgcId,gameObject, (sp) =>
            {
                Img_Icon.sprite = sp;
            });
        }
        else
        {
            Img_Icon.sprite = rewardSp;
        }
       
        var amount = info.rewardCount;
        Txt_Title.gameObject.SetActive(amount > 1);
        if (amount > 1)
        {
            Txt_Title.text = "x" + amount.ToString();
        }
        if (!string.IsNullOrEmpty(info.name))
        {
            Txt_Title_CN.text = info.name;
        }
        else
        {
            Txt_Title_CN.text = PgcUtils.GetRewardName((BUDRewardType)info.rewardType);
        }

        LevelText.text = info.taskId;
        SetState((BudRewardStatus)info.rewardStatus);
        Btn_Item.onClick.RemoveAllListeners();
        Btn_Item.onClick.AddListener(OnBtnClick);
    }

    private void OnBtnClick()
    {
        if (info == null||Icon_Lock.gameObject.activeSelf)
        {
            return;
        }
        this.ClickAction?.Invoke(info);
        
    }
    public void SetState(BudRewardStatus state)
    {
        switch (state)
        {
            case BudRewardStatus.Lock:
                Icon_Lock.SetActive(true);
                LockMask.SetActive(false);
                Icon_Received.SetActive(false);
                Icon_AvailableBg.SetActive(false);
                animator.enabled = false;
                // ImgEffect.color = new Color(255, 255, 255, 0);
                
                break;
            case BudRewardStatus.Unlocked:
                Icon_Lock.SetActive(false);
                LockMask.SetActive(false);
                Icon_Received.SetActive(false);
                Icon_AvailableBg.SetActive(true);
                // animator.enabled = true;
                // animator.CrossFade("LoginGiftPanel_prompt",0.1f);
                break;
            case BudRewardStatus.Claimed:
                Icon_Lock.SetActive(false);
                animator.enabled = false;
                LockMask.SetActive(true);
                Icon_Received.SetActive(true);
                Icon_AvailableBg.SetActive(false);
                // ImgEffect.color = new Color(255, 255, 255, 0);
                break;
        }
        ImgLine.gameObject.SetActive(state!=BudRewardStatus.Lock);
        ImgCircal.gameObject.SetActive(state!=BudRewardStatus.Lock);
    }

    // public void SyncData(List<SeasonPassItemInfo> infos)
    // {
    //     if(infos == null)
    //         return;
    //     var info = infos.Find(x => x.rewardId == curData.rewardId);
    //     if (info != null)
    //     {
    //         curData.rewardStatus = info.rewardStatus;
    //         SetState(curData.BudRewardStatus);
    //     }
    // }
    //
    // public void SyncRewardState(string rewardId)
    // {
    //     if (rewardId == curData.rewardId)
    //     {
    //         curData.rewardStatus = (int)BudRewardStatus.Claimed;
    //         SetState(curData.BudRewardStatus);
    //     }
    // }
}
