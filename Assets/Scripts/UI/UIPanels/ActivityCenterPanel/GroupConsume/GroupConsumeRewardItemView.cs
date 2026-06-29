using System;
using UnityEngine;

public class GroupConsumeRewardItemView : CommonRewardItem {
    public Transform On;

    private readonly Color normalColor = new Color32(255, 255, 255, 255);
    private readonly Color highlightColor = new Color32(0xCA, 0x64, 0x2D, 255);

    public override void Init(int id, CommonRewardItemData data, Action<CommonRewardItem> callBack)
    {
        iconImage.sprite = null;
        base.Init(id, data, callBack);
        onClaimClicked = callBack;
        itemId = id;
        rewardData = data;
    }

    public override void SetStatus(ClaimStatus status) {
        base.SetStatus(status);
        progressText.color = normalColor;
        if (status == ClaimStatus.Unlocked || status == ClaimStatus.Claimed)
        {
            progressText.color = highlightColor;
        }
        On.gameObject.SetActive(status == ClaimStatus.Unlocked);
    }



}
