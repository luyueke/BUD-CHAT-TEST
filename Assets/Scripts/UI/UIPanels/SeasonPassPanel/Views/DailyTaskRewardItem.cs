using UnityEngine;

namespace SeasonPass {

    public class DailyTaskRewardItem : CommonRewardItem {

        [SerializeField]
        private Sprite normalSprite;

        [SerializeField]
        private Sprite unlockSprite;

        [SerializeField]
        private GameObject lineImage;

        public override void SetStatus(ClaimStatus status) {
            base.SetStatus(status);
            if(lineImage!=null)
            lineImage.SetActive(status == ClaimStatus.Unlocked);
            if (status == ClaimStatus.Unlocked) {
                bgImage.color = new Color32(255, 232, 151, 255);
            }
            else
            {
                bgImage.color = new Color32(255, 147, 174, 255);
            }
        }
    }
}

