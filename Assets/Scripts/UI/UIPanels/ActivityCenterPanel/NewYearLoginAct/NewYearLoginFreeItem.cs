using System;
using System.Collections.Generic;
using System.Linq;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewYearLoginFreeItem : MonoBehaviour {
    public Image Img_Icon;
    public Text Txt_Title;
    public GameObject LockMask;
    public GameObject Icon_Received;
    public Button Btn_Item;
    public Animator animator;
    public Image ImgEffect;
    public GameObject Icon_Lock;

    public SeasonPassItemInfo curData;
    public Sprite rewardSp;
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

        var pgcId = data.rewardInfo?.itemList?.First()?.pgcIdList?.First() ?? "";
        if (data.BudRewardType == BUDRewardType.RewardPgcResource && pgcId != "10900439") {
            PgcUtils.GetIconSpriteByPgcIdAsync(pgcId, gameObject, sp => {
                rewardSp = sp;
                Img_Icon.sprite = rewardSp;
            });
        } else {
            var iconName = data.IconName();
            if (!string.IsNullOrEmpty(iconName)) {
                rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, this.gameObject);
            }
            if (rewardSp == null) {
                LoggerUtils.LogError("rewardSp is null:" + rewardSp + "," + data.BudRewardType);
            }

            Img_Icon.sprite = rewardSp;
        }

        var amount = 0;
        if (data?.rewardInfo?.itemList?.Count > 0)
        {
            amount = data?.rewardInfo.itemList.First().amount ?? 0;
        }

        if (amount > 0)
        {
            Txt_Title.text = "x" + amount.ToString();
        }
        else
        {
            Txt_Title.text = "";
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
                Icon_Lock.SetActive(true);
                animator.enabled = false;
                LockMask.SetActive(false);
                Icon_Received.SetActive(false);
                ImgEffect.color = new Color(255, 255, 255, 0);
                break;
            case BudRewardStatus.Unlocked:
                Icon_Lock.SetActive(false);
                LockMask.SetActive(false);
                Icon_Received.SetActive(false);
                animator.enabled = true;
                animator.CrossFade("LoginGiftPanel_prompt",0.1f);
                break;
            case BudRewardStatus.Claimed:
                Icon_Lock.SetActive(false);
                animator.enabled = false;
                LockMask.SetActive(true);
                Icon_Received.SetActive(true);
                ImgEffect.color = new Color(255, 255, 255, 0);
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
