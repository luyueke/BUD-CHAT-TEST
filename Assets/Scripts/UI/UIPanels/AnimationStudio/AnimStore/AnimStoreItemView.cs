using System;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AnimStoreItemView : MonoBehaviour
{
    [SerializeField] CButton Btn_View;
    [SerializeField] RemoteImageBehaviour coverImage; // 动作封面图
    [SerializeField] RemoteImageBehaviour pgcCoverImage; // pgc动作封面图（RawImage，已废弃，保留引用兼容旧 prefab）
    [SerializeField] Image pgcSpriteImage; // pgc动作封面图（Image，正确保留比例）
    [SerializeField] Image CurrencyIcon;
    [SerializeField] Image SelectBg;
    [SerializeField] Text assetName;   // 未拥有→价格/免费；已拥有→动作名称
    [SerializeField] private Color SelectColor;
    [SerializeField] Font numFont;
    [SerializeField] Font textFont;

    private RecommendItemData _curData;
    private Action<RecommendItemData> _onSelectAct;

    private void Awake()
    {
        Btn_View.onClick.AddListener(OnBtnViewClick);
    }

    public virtual void SetData(RecommendItemData data, Action<RecommendItemData> onSelect = null)
    {
        if (data == null) return;
        _curData = data;
        _onSelectAct = onSelect;

        var coverUrl = data.UgcInfo?.cover ?? "";
        bool isPgc = string.IsNullOrEmpty(coverUrl) && !string.IsNullOrEmpty(data.ugcId);

        coverImage?.gameObject.SetActive(!isPgc);
        pgcCoverImage?.gameObject.SetActive(false); // 旧 RawImage 路径已废弃
        pgcSpriteImage?.gameObject.SetActive(isPgc);

        if (!isPgc)
        {
            if (coverImage != null && !string.IsNullOrEmpty(coverUrl))
                coverImage.Load(coverUrl);
        }
        else if (pgcSpriteImage != null)
        {
            var sprite = PgcUtils.LoadEmoteIcon(data.ugcId, gameObject);
            pgcSpriteImage.sprite = sprite;
            pgcSpriteImage.preserveAspect = true;
        }

        AdjustPriceUI();
    }

    public virtual void SetSelectState(bool isSelected)
    {
        if (SelectBg != null)
            SelectBg.color = isSelected ? SelectColor : Color.white;
    }

    public void SetOwned()
    {
        if (_curData == null) return;
        if (_curData.interactInfo == null)
            _curData.interactInfo = new GameData.Base.BaseInteractInfo();
        _curData.interactInfo.consumed = 1;
        AdjustPriceUI();
    }

    private void OnBtnViewClick()
    {
        if (_curData == null) return;
        _onSelectAct?.Invoke(_curData);
    }

    private void AdjustPriceUI()
    {
        var paymentInfo = GetPaymentInfo(_curData);
        var isOwned = _curData?.interactInfo?.consumed == 1;

        if (isOwned)
        {
            if (CurrencyIcon != null) CurrencyIcon.gameObject.SetActive(false);
            if (assetName != null)
            {
                assetName.font = textFont;
                assetName.text = _curData.UgcInfo?.name ?? "";
            }
            return;
        }

        var currencyType = paymentInfo?.currencyType ?? CurrencyType.PinkCoin;
        var price = paymentInfo?.price ?? 0;

        if (CurrencyIcon != null)
        {
            CurrencyIcon.gameObject.SetActive(price > 0);
            if (price > 0)
                CurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);
        }

        if (assetName != null)
        {
            if (price > 0)
            {
                assetName.font = numFont;
                assetName.text = price.ToString();
            }
            else
            {
                assetName.font = textFont;
                assetName.text = "免费";
            }
        }
    }

    private static GameData.Base.PaymentInfo GetPaymentInfo(RecommendItemData data)
    {
        if (data == null) return null;
        if (data.UgcInfo is AnimInfo anim) return anim.paymentInfo;
        if (data.UgcInfo is PoseInfo pose) return pose.paymentInfo;
        return null;
    }
}
