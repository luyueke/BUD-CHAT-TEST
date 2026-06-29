using System;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class MayDayCarnival_TaskItem : MonoBehaviour {
    public GameObject Go_Claimed;
    public GameObject Go_Lock;
    public GameObject Go_Enable;
    public Image Img_Icon;
    public Text Txt_Progress;
    public Text Txt_RewardName;
    
    [HideInInspector]
    public int itemId;
    [HideInInspector]
    public CommonRewardItemData rewardData;
    protected Action<MayDayCarnival_TaskItem> onClaimClicked;

    public void Awake() {
        GetComponent<CButton>().onClick.AddListener(OnClaimClicked);
    }

    private void OnClaimClicked() {
        onClaimClicked?.Invoke(this);
    }

    public virtual void Init(int id, CommonRewardItemData data, Action<MayDayCarnival_TaskItem> callBack) {
        onClaimClicked = callBack;
        itemId = id;
        rewardData = data;
        rewardData.IconSp = Img_Icon.sprite;
        Txt_RewardName.SetLocalText(data.rewardName);
        
        if(rewardData.RewardAmount > 1)
            Txt_RewardName.SetLocalText(data.rewardName + "x" + rewardData.RewardAmount);
            
        Txt_Progress.text = "Day" + itemId;
    }
    
    public virtual void SetStatus(ClaimStatus status) {
        Go_Claimed.gameObject.SetActive(status == ClaimStatus.Claimed);
        Go_Lock.gameObject.SetActive(status == ClaimStatus.Lock);
        Go_Enable.gameObject.SetActive(status == ClaimStatus.Unlocked);
        Txt_Progress.gameObject.SetActive(status != ClaimStatus.Claimed);
    }
}
