using System;
using UnityEngine;

public class FireworkWelcomeSpringRewardItem : CommonRewardItem {
    private readonly Color normalColor = new Color32(255, 146, 45, 255);
    private readonly Color highlightColor = new Color32(255, 228, 86, 255);

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
    }
}
