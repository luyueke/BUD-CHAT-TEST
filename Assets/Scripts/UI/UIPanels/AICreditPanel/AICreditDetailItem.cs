using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AI能量明细 限时赠送item（一行：礼包名 + 剩余能量 + 过期描述）
/// </summary>
public class AICreditDetailItem : MonoBehaviour
{
    public Text packNameTxt; // "xx礼包" / desc
    public Text amountTxt;   // "28,000 一天后过期" 等

    public void Init(AICreditDetailExpiringItem item)
    {
        if (packNameTxt != null) packNameTxt.text = item.desc ?? string.Empty;
        if (amountTxt   != null) amountTxt.text   = $"{item.amount:N0}" + AICreditManager.FormatDuration(item.duration);
    }

    public void Init(AICreditGiftPackInfo info)
    {
        if (packNameTxt != null) packNameTxt.text = info.packName ?? string.Empty;
        if (amountTxt   != null) amountTxt.text   = $"{info.remainAmount:N0}" + AICreditManager.FormatExpiry(info.expireTimestamp);
    }
}
