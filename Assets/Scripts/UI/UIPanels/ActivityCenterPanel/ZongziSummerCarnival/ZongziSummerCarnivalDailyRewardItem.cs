using System;
using UnityEngine;
using UnityEngine.UI;

public class ZongziSummerCarnivalDailyRewardItem : CommonRewardItem {
    private readonly Color normalColor = new Color32(223, 113, 75, 255);
    private readonly Color highlightColor = new Color32(255, 228, 86, 255);
    [SerializeField]
    protected Text numText2;

    public override void SetStatus(ClaimStatus status) {
        base.SetStatus(status);
        bgImage.color = normalColor;
        if (status == ClaimStatus.Unlocked) {
            bgImage.color = highlightColor;
        }
    }

    public override void Init(int id, CommonRewardItemData data, Action<CommonRewardItem> callBack)
    {
        base.Init(id, data, callBack);
        switch (itemId)
        {
            case 11:
                numText2.text = "3天";
                numText.gameObject.SetActive(false);
                numText2.gameObject.SetActive(true);
                break;
            case 16:
                numText2.text = "永久";
                numText.gameObject.SetActive(false);
                numText2.gameObject.SetActive(true);
                break;
            default:
                numText.gameObject.SetActive(true);
                numText2.gameObject.SetActive(false);
                break;
        }
    }
}
