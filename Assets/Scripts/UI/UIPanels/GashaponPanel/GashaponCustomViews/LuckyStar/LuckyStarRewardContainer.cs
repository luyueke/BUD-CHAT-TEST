using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class LuckyStarRewardContainer : MonoBehaviour {

    private Text rewardNumText;

    private void Awake() {
        GetComponent<CButton>().onClick.AddListener(OnCloseClick);
        rewardNumText = GameObjectEx.FindComponentByName<Text>(transform, "Num");
    }


    public void SetRewardCount(int rewardCount) {
        rewardNumText.SetText($"x{rewardCount}");
    }

    private void OnCloseClick() {
        Destroy(gameObject);
    }
}
