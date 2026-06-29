using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class BundleOnlineRewardItemView : MonoBehaviour
{
    [SerializeField] private Color selectColor;
    [SerializeField] private Color normalColor;
    [SerializeField] private Image bgImage;
    [SerializeField] private Button actionBtn;
    [SerializeField] private Image iconImg;
    [SerializeField] private Text priceNum;

    [SerializeField] private GameObject priceObj;
    [SerializeField] private GameObject ownedObj;
    
    private ActivityRewardInfo rewardInfo;
    private Action<ActivityRewardInfo> clickAction;
    
    private void Awake()
    {
        actionBtn?.onClick.AddListener(OnClickItem);
    }

    public int RewardId
    {
        get
        {
            return rewardInfo?.rewardId ?? -1;
        }
    }
    
    public void SetData(ActivityRewardInfo rewardInfo, Action<ActivityRewardInfo> action)
    {
        this.rewardInfo = rewardInfo;
        clickAction = action;
        
        bool isOwned = false;
        var budRewardType = rewardInfo.budRewardType;
        if (budRewardType == (int)BUDRewardType.RewardPgcResource)
        {
            var pgcId = rewardInfo.pgcId;
            if (!string.IsNullOrEmpty(pgcId))
            {
                iconImg.sprite = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject);
                isOwned = AssetsDataManager.IsOwned(pgcId);
            }
        } 
        else if (budRewardType == (int)BUDRewardType.RewardPinkCoin)
        {
            iconImg.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.PinkCoin, gameObject);
            iconImg.SetNativeSize();
            isOwned = rewardInfo.rewardStatus == 1;
        }
        
        priceNum.text = rewardInfo.spendNum.ToString();
        priceObj.SetActive(!isOwned);
        ownedObj.SetActive(isOwned);
    }

    public void SetSelect(bool isSelect)
    {
        bgImage.color = isSelect ? selectColor : normalColor;
    }
    
    private void OnClickItem()
    {
        if (rewardInfo == null)
        {
            return;
        }

        clickAction?.Invoke(rewardInfo);
    }

    public void SetOwnedUI()
    {
        priceObj.SetActive(false);
        ownedObj.SetActive(true);
    }
}
