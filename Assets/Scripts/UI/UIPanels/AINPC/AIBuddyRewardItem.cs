using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Es;
using UI.Manager;

public class AIBuddyRewardItem : MonoBehaviour
{
    [SerializeField] private Image Img_Icon;
    [SerializeField] private Text NumText;
    [SerializeField] private Text Txt_Title_CN;
    [SerializeField] private GameObject LockMask;
    [SerializeField] private GameObject Icon_Received;
    [SerializeField] private GameObject Icon_Lock;
    [SerializeField] private GameObject Icon_AvailableBg;
    [SerializeField] private Button Btn_Item;
    [SerializeField] private Animator animator;
    [SerializeField] private Image ImgEffect;
    [SerializeField] private Image ImgLine;
    [SerializeField] private GameObject ProgressGo;
    [SerializeField] private Image ImgCircal;
    [SerializeField] private Text LevelText;
    
    private ActivityEventInfo _itemData;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/AINPC/AIBuddyTask.spriteatlas";
    private Action<AIBuddyRewardItem,ActivityEventInfo> ClickAction;
    
    
    public void Init(ActivityEventInfo data)
    {
        this._itemData = data;
        var iconSprite = GetRewardSprite(_itemData);
        if (iconSprite != null)
        {
            Img_Icon.sprite = iconSprite;
        }

        var amount = _itemData.rewardNum;
        NumText.gameObject.SetActive(amount > 1);
        if (amount > 1)
        {
            NumText.SetText("x" + amount);
        }
        Txt_Title_CN.SetLocalText(_itemData.rewardName);
        LevelText.text = _itemData.eventId.ToString();
        SetState((BudRewardStatus)_itemData.eventStatus);
        Btn_Item.onClick.RemoveAllListeners();
        Btn_Item.onClick.AddListener(OnBtnClick);
    }

    public Sprite GetRewardSprite(ActivityEventInfo data)
    {
        var iconName = "reward_"+data.rewardType;
        if (!string.IsNullOrEmpty(data.pgcId))
        {
            iconName = "reward_"+data.pgcId;
        }
        var rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, gameObject);
        return rewardSp;
    }

    public ActivityEventInfo GetBindData()
    {
        return _itemData;
    }

    public void AddItemClickListener(Action<AIBuddyRewardItem,ActivityEventInfo> callback)
    {
        this.ClickAction += callback;
    }

    private void OnBtnClick()
    {
        if (_itemData == null || _itemData.eventStatus == (int)BudRewardStatus.Claimed)
        {
            return;
        }

        if (_itemData.eventStatus == (int)BudRewardStatus.Lock)
        {
            TipPanel.ShowToast("暂无奖励可领，快去提升伙伴的亲密度吧");
            return;
        }

        this.ClickAction?.Invoke(this,_itemData);
        
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
                break;
            case BudRewardStatus.Unlocked:
                Icon_Lock.SetActive(false);
                LockMask.SetActive(false);
                Icon_Received.SetActive(false);
                Icon_AvailableBg.SetActive(true);
                break;
            case BudRewardStatus.Claimed:
                Icon_Lock.SetActive(false);
                animator.enabled = false;
                LockMask.SetActive(true);
                Icon_Received.SetActive(true);
                Icon_AvailableBg.SetActive(false);
                break;
        }
        ImgLine.gameObject.SetActive(state!=BudRewardStatus.Lock);
        ImgCircal.gameObject.SetActive(state!=BudRewardStatus.Lock);
    }

    public void SetProgressVisible(bool value)
    {
        ProgressGo?.SetActive(value);
    }
}
