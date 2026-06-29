using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPassFreeItem : MonoBehaviour
{
    public Image Img_Icon;
    public Text Txt_Title;
    public Text Txt_Title_CN;
    public GameObject claimImg;
    public GameObject LockMask;
    public GameObject Icon_Received;
    public Button Btn_Item;
    public Animator animator;

    public SeasonPassItemInfo curData;
    private Sprite rewardSp;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";

    private Action<SeasonPassItemInfo> ClickAction;
    
    private void OnEnable()
    {
        if (curData != null)
        {
            SetState(curData.BudRewardStatus);
        }
    }

    public void Init(SeasonPassItemInfo data, Action<SeasonPassItemInfo> ClickAction)
    {
        this.curData = data;
        this.ClickAction = ClickAction;

        Txt_Title.text = "";
        Txt_Title_CN.text = "";
        
        var iconName = data.IconName();
        if (!string.IsNullOrEmpty(iconName))
        {
            rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, this.gameObject);
            Img_Icon.sprite = rewardSp;
        }
        var pgcId = data.rewardInfo?.itemList?.First()?.pgcIdList?.First() ?? "";
        var amount = 0;
        if (data?.rewardInfo?.itemList?.Count > 0)
        {
            amount = data?.rewardInfo.itemList.First().amount ?? 0;
        }

        if (amount > 0)
        {
            Txt_Title.text = "x" + amount.ToString();
        }
        else if(pgcId == "10400470")
        {
            Txt_Title_CN.text = "蜜桃泳衣";
        }
        else if(pgcId == "11000245")
        {
            Txt_Title_CN.text = "医生针管";
        }
        else if (pgcId == "10100116")
        {
            Txt_Title_CN.text = "亚比雪人项圈";
        }
        else if (pgcId == "10200070")
        {
            Txt_Title_CN.text = "焦糖布丁喵尾巴";
        }
        else if (pgcId == "10900514")
        {
            Txt_Title_CN.text = "天使小羊头套";
        }
        else
        {
            Txt_Title.text = "x1";
        }
        
        SetState(data.BudRewardStatus);
        Btn_Item.onClick.RemoveAllListeners();
        Btn_Item.onClick.AddListener(OnBtnClick);
    }
    
    private void OnBtnClick()
    {
        if (curData == null)
        {
            return;
        }
        this.ClickAction?.Invoke(curData);
    }

    public void SetState(BudRewardStatus state)
    {
        switch (state)
        {
            case BudRewardStatus.ErrRewardStatus:
            case BudRewardStatus.Lock:
                animator.enabled = false;
                LockMask.SetActive(false);
                Icon_Received.SetActive(false);
                claimImg.SetActive(false);
                break;
            case BudRewardStatus.Unlocked:
                LockMask.SetActive(false);
                Icon_Received.SetActive(false);
                claimImg.SetActive(true);
                animator.enabled = true;
                animator.CrossFade("LoginGiftPanel_prompt",0.1f);
                break;
            case BudRewardStatus.Claimed:
                animator.enabled = false;
                LockMask.SetActive(true);
                Icon_Received.SetActive(true);
                claimImg.SetActive(false);
                break;
        }
    }

    public void SyncData(List<SeasonPassItemInfo> infos)
    {
        if(infos == null)
            return;
        var info = infos.Find(x => x.rewardId == curData.rewardId);
        if (info != null)
        {
            curData.rewardStatus = info.rewardStatus;
            SetState(curData.BudRewardStatus);
        }
    }
    
    public void SyncRewardState(string rewardId)
    {
        if (rewardId != curData.rewardId)
        {
            return;
        }
        curData.rewardStatus = (int)BudRewardStatus.Claimed;
        SetState(curData.BudRewardStatus);
    }
}
