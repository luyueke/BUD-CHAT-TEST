using System;
using System.Collections.Generic;
using Game.Event;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class DailyTaskItem : MonoBehaviour {
    [SerializeField] protected CButton goButton;
    [SerializeField] protected CButton claimButton;
    [SerializeField] protected GameObject doneObj;
    [SerializeField] protected Text eventNameText;

    [SerializeField] protected List<GameObject> rewardObjList = new List<GameObject>();
    protected SeasonTaskEventInfo eventInfo;
    protected Action<int> onClaimCallBack;
    private string _strTargetAmount = "";
    private string _strEventName = "";


    public virtual void SetData(TaskItemData itemData)
    {
        doneObj.SetActive(false);
        claimButton.gameObject.SetActive(false);
        goButton.gameObject.SetActive(false);

        _strTargetAmount = "";
        if (eventInfo != null && eventInfo.targetAmount >= 1) {
            _strTargetAmount = $"({itemData.finishAmount}/{eventInfo.targetAmount})";
        }
        switch ((ClaimStatus)itemData.eventStatus) {
            case ClaimStatus.Claimed:
                doneObj.SetActive(true);
                break;
            case ClaimStatus.Unlocked:
                claimButton.gameObject.SetActive(true);
                break;
            case ClaimStatus.Lock:
                goButton.gameObject.SetActive(true);
                break;
        }

        eventNameText.SetLocalText($"{_strEventName} {_strTargetAmount}");
    }

    public virtual void Init(SeasonTaskEventInfo info, Action<int> callBack) {
        eventInfo = info;
        _strEventName = eventInfo.eventName;
        onClaimCallBack = callBack;
        int rewardIndex = 0;
        for (; rewardIndex < eventInfo.rewardList.Count; rewardIndex++) {
            var rewardInfo = eventInfo.rewardList[rewardIndex];
            rewardObjList[rewardIndex].SetActive(true);
            var rewardNumText = GameObjectEx.FindComponentByName<Text>(rewardObjList[rewardIndex], "Text");
            var rewardIconImage = GameObjectEx.FindComponentByName<Image>(rewardObjList[rewardIndex], "Icon");
            if (rewardInfo.rewardType == BUDRewardType.RewardPgcResource) {
                rewardNumText.SetLocalText(rewardInfo.rewardName);
                PgcUtils.GetIconSpriteByPgcIdAsync(rewardInfo.pgcId, gameObject, sp => {
                    rewardIconImage.sprite = sp;
                });
            } else {
                rewardIconImage.sprite = PgcUtils.LoadRewardIcon(rewardInfo.rewardType, gameObject);
                rewardNumText.SetLocalText($"x{rewardInfo.rewardAmount}");
            }
        }
        for (; rewardIndex < rewardObjList.Count; rewardIndex++) {
            rewardObjList[rewardIndex].SetActive(false);
        }
        if (eventInfo.targetAmount >= 1) {
            _strTargetAmount = $"(0/{eventInfo.targetAmount})";
        } else
        {
            _strTargetAmount = "";
        }

        doneObj.SetActive(false);
        claimButton.gameObject.SetActive(false);
        goButton.gameObject.SetActive(false);
        if (eventInfo.eventSkipType != EventCenterSkipType.ErrType) {
            goButton.SetLocalText("去完成");
        } else {
            goButton.SetLocalText("进行中");
        }
        eventNameText.SetLocalText($"{_strEventName} {_strTargetAmount}");
    }


    private void Awake() {
        claimButton.onClick.AddListener(OnClaimButtonClick);
        goButton.onClick.AddListener(OnGoButtonClick);
    }

    private void OnGoButtonClick() {
        EventCenterDataManager.Inst.SkipToTask(eventInfo.eventSkipType);
    }

    private void OnClaimButtonClick() {
        onClaimCallBack?.Invoke(eventInfo.eventId);
    }
}
