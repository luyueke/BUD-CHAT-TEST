using UnityEngine;

public class WarmHandInHandRewardItem : CommonRewardItem {
    private readonly Color normalColor = new Color32(255, 166, 84, 255);
    private readonly Color highlightColor = new Color32(255, 228, 86, 255);

    public override void SetStatus(ClaimStatus status) {
        base.SetStatus(status);
        bgImage.color = normalColor;
        if (status == ClaimStatus.Unlocked) {
            bgImage.color = highlightColor;
        }
    }
}
