using System;
using GameData.BaseInfo;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using GameData.Base;

public class TimbreCommunityItemView : MonoBehaviour
{
    [SerializeField] private GameObject assetRoot;
    [SerializeField] private GameObject storeRoot;
    [SerializeField] private GameObject publishRoot;
    [SerializeField] private GameObject AppealRoot;

    [Header("节点")] 
    [SerializeField] private Image CurrencyIcon;
    [SerializeField] private Text assetName;
    [SerializeField] private Text toneTypeName;

    private Action<ToneFixedInfo> onItemSelected;
    private ToneFixedInfo mData;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnItemClick);
    }

    private void OnItemClick()
    {
        if (mData == null)
        {
            return;
        }
        
        var isInAppeal = mData.toneInfo?.auditInfo?.auditResult == (int)AuditResult.Appealing;
        if (mData.style == ToneFixedInfo.ToneFixedStyle.Normal && isInAppeal)
        {
            TipPanel.ShowToast("该作品在申诉中，请耐心等待");
            return;
        }
        
        onItemSelected?.Invoke(mData);
    }

    private void ResetAllUI()
    {
        assetRoot.SetActive(false);
        storeRoot.SetActive(false);
        publishRoot.SetActive(false);
        AppealRoot.SetActive(false);
    }
    
    public void UpdateViews(ToneFixedInfo goodsData, Action<ToneFixedInfo> action)
    {
        if (goodsData == null)
        {
            return;
        }
        mData = goodsData;
        onItemSelected = action;
        ResetAllUI();
        
        if (goodsData.style == ToneFixedInfo.ToneFixedStyle.Store)
        {
            storeRoot.SetActive(true);
            return;
        }
        else if (goodsData.style == ToneFixedInfo.ToneFixedStyle.Publish)
        {
            publishRoot.SetActive(true);
            return;
        }

        var isInAppeal = goodsData.toneInfo?.auditInfo?.auditResult == (int)AuditResult.Appealing;
        assetRoot.SetActive(true);
        AppealRoot.SetActive(isInAppeal);
        CurrencyType currencyType = goodsData?.toneInfo?.paymentInfo?.currencyType ?? CurrencyType.PinkCoin;
        CurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
        assetName.text = goodsData.toneInfo?.name;
        toneTypeName.SetLocalText(goodsData.toneInfo.toneType == (int)ToneType.Fifteen ? "15音" : "22音");
    }
    
}