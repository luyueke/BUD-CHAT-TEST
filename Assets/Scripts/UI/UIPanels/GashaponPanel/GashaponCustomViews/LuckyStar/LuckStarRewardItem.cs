using UnityEngine;

public class LuckStarRewardItem : CommonRewardItem {

    [SerializeField]
    private Sprite normalSprite;

    [SerializeField]
    private Sprite unlockSprite;

    public override void SetStatus(ClaimStatus status) {
        base.SetStatus(status);
        bgImage.sprite = normalSprite;
        if (status == ClaimStatus.Unlocked) {
            bgImage.sprite = unlockSprite;
        }
    }
}
