using System;
using Game.Store;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class TimbreStoreItemView : MonoBehaviour
{
    [SerializeField] CButton Btn_View;
    [SerializeField] Image CurrencyIcon;
    [SerializeField] Image SelectBg;
    [SerializeField] Text assetName;
    [SerializeField] Text toneTypeName;
    [SerializeField] private Color SelectColor;
    [SerializeField] Font numFont;
    [SerializeField] Font textFont;
    
    private RecommendItemData _curData;
    private Action<RecommendItemData> _onSelectAct;

    private void Awake()
    {
        Btn_View.onClick.AddListener(OnBtnViewClick);
    }

    public void SetData(RecommendItemData data, Action<RecommendItemData> clickAction = null)
    {
        if (data == null)
            return;

        this._curData = data;
        this._onSelectAct = clickAction;
        AdjustPriceUI();
    }

    public void SetOwned()
    {
        if (_curData == null)
        {
            return;
        }
        _curData.interactInfo.consumed = 1;
        AdjustPriceUI();
    }

    private void AdjustPriceUI()
    {
        var fixedData = _curData?.UgcInfo as ToneInfo;
        if (fixedData == null)
        {
            return;
        }
        
        toneTypeName.text = fixedData.toneType == (int)ToneType.Fifteen ? "15音" : "22音";

        // 已拥有判断三路并行（与乐谱/姿势/演员卡商城一致）：自己创作 ∥ 服务端 consumed ∥ 本地背包已有。
        // 只看 consumed 会漏掉「已在背包但 sectionInfoV2 未标记 consumed」的音色，导致已拥有仍显示可购买。
        var isCreator = _curData?.creatorInfo?.uid == AccountDataManager.Inst?.Uid;
        var isOwned = isCreator
                      || _curData?.interactInfo?.consumed == 1
                      || (!string.IsNullOrEmpty(fixedData.id) && AssetsDataManager.IsOwned(fixedData.id));
        if (isOwned)
        {
            CurrencyIcon.gameObject.SetActive(false);
            assetName.font = textFont;
            assetName.SetLocalText("已拥有");
        }
        else
        {
            var currencyType = fixedData.paymentInfo?.currencyType ?? CurrencyType.PinkCoin;
            CurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
            var price = fixedData.paymentInfo?.price ?? 0;
            
            CurrencyIcon.gameObject.SetActive(price > 0);
            if (price > 0)
            {
                assetName.font = numFont;
                assetName.text = price.ToString();
            }
            else
            {
                assetName.SetLocalText("免费");
                assetName.font = textFont;
            }
        }
    }

    private void OnBtnViewClick()
    {
        if (_curData == null)
        {
            return;
        }
        this._onSelectAct?.Invoke(_curData);
    }

    public void SetSelectState(bool isSelected)
    {
        SelectBg.color = isSelected ? SelectColor : Color.white;
    }
}