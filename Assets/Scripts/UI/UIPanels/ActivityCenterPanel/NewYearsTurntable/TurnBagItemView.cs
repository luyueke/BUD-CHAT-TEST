using UnityEngine;
using UnityEngine.UI;

public class TurnBagItemView : MonoBehaviour {


    [SerializeField]
    private Text multipleText;
    [SerializeField]
    private Text rewardText;

    public void SetRewardInfo(TurnRewardInfo rewardInfo) {
        multipleText.SetLocalText(rewardInfo.multiple);
        rewardText.SetText($"x{rewardInfo.rewardAmount}");
    }
}
