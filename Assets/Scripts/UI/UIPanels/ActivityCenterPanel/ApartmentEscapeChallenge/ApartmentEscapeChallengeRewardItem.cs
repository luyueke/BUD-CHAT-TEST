using System;
using UnityEngine;
using UnityEngine.UI;

public class ApartmentEscapeChallengeRewardItem : CommonRewardItem {
    private readonly Color normalColor = new Color32(255, 166, 84, 255);
    private readonly Color highlightColor = new Color32(255, 228, 86, 255);
    
    private string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";

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
        if (data.rewardType == (int)BUDRewardType.RewardPgcResource)
        {
            Sprite rewardIcon = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "apartment_reward_icon", gameObject);
            iconImage.sprite = rewardIcon;
            rewardData.IconSp = rewardIcon;

            if (itemId  == 12)
            {
                numText2.text = "3天";
                numText.gameObject.SetActive(false);
                numText2.gameObject.SetActive(true);
            }
            else
            {
                numText.gameObject.SetActive(true);
                numText2.gameObject.SetActive(false);
            }
        }
    }
}
