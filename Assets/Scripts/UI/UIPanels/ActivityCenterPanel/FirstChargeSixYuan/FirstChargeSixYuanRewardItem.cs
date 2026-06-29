
using System;
using UnityEngine;
using UnityEngine.UI;

public class FirstChargeSixYuanRewardItem : MonoBehaviour {

   
    [SerializeField]
    private Image rewardIcon;
    [SerializeField] private Text title;
    [SerializeField] private Text rewardNum;
    [SerializeField] private GameObject coverLayout;
    [SerializeField] private Image bgNormal;
    [SerializeField] private Image bgClaim;
    [SerializeField] private Button rootBtn;
    [SerializeField] private GameObject lockObj;
    [SerializeField] private Text rewardTxt;

    private RewardItem _rewardItem;

    private string spriteatlasPath;

    public int eventId { get; private set; }
    
    public void OnInitCreate(RewardItem rewardItem)
    {
        _rewardItem = rewardItem;
        title.text = rewardItem.title;
        if (rewardItem.rewardId == "5")
        {
            rewardTxt.text = "1天体验卡";
            rewardTxt.gameObject.SetActive(true);
            rewardNum.gameObject.SetActive(false);
        }
        else
        {
            rewardNum.text = "x" +rewardItem.num;
            rewardTxt.gameObject.SetActive(false);
            rewardNum.gameObject.SetActive(true);
        }
        rewardIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1, gameObject);
    }
    
    public void SetAtlasPath(string path)
    {
        spriteatlasPath = path;
    }

    public void SetProgress(int progress)
    {
    }


    public void Init(ActivityEventInfo eventInfo, Action<ActivityEventInfo, RewardItem> claimAction)
    {
        eventId = eventInfo.eventId;

        var claimStatus = (ClaimStatus)eventInfo.eventStatus;
        RefreshClaimStatus(claimStatus);
        rootBtn.onClick.RemoveAllListeners();
        rootBtn.onClick.AddListener(() => {
            claimAction.Invoke(eventInfo, _rewardItem);
        });
    }


    public void Refresh(ActivityEventInfo eventInfo) {
        var claimStatus = (ClaimStatus)eventInfo.eventStatus;
        RefreshClaimStatus(claimStatus);
    }

    private void RefreshClaimStatus(ClaimStatus status) {
        switch (status) {
            case ClaimStatus.Claimed:
                coverLayout.SetActive(true);
                lockObj.SetActive(false);
                bgNormal.gameObject.SetActive(true);
                bgClaim.gameObject.SetActive(false);
                break;
            case ClaimStatus.Lock:
                coverLayout.SetActive(false);
                lockObj.SetActive(true);
                bgNormal.gameObject.SetActive(true);
                bgClaim.gameObject.SetActive(false);
                break;
            case ClaimStatus.Unlocked:
                coverLayout.SetActive(false);
                lockObj.SetActive(false);
                bgNormal.gameObject.SetActive(false);
                bgClaim.gameObject.SetActive(true);
                break;
            default:
                coverLayout.SetActive(false);
                lockObj.SetActive(true);
                bgNormal.gameObject.SetActive(true);
                bgClaim.gameObject.SetActive(false);
                break;
        }
    }
}
