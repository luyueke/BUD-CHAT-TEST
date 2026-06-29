using System.Collections.Generic;
using Game.Event;
using Message;
using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AnniversarySummerItem : MonoBehaviour
{

    public GameObject normal_Go;
    public GameObject current_Go;
    public GameObject mask_Go;
    public GameObject completed_Go;

    public Text Txt_Title_Normal;
    public Text Txt_Title_Current;

    public Text Txt_Normal_State;
    public Text Txt_Current_State;


    public Button Btn_Claim;

    private TaskItemData _taskItemData;
    private ActivityEventInfo _eventInfo;
    public Transform rewardContent;
    bool _initReward = false;

    public Button btn_Preview1;
    public Button btn_Preview2;
    int dayIndex = 0;

    public void Awake()
    {
        Btn_Claim.onClick.AddListener(OnBtnClaimClick);
        btn_Preview1.onClick.AddListener(OnBtnPreview1Click);
        btn_Preview2.onClick.AddListener(OnBtnPreview2Click);
    }

    void InitUI()
    {
        Txt_Title_Normal.text = dayIndex.ToString();
        Txt_Title_Current.text = dayIndex.ToString();
        Txt_Normal_State.text = "";
        Txt_Current_State.text = "";
        Btn_Claim.gameObject.SetActive(false);
        normal_Go.SetActive(true);
        current_Go.SetActive(false);
        mask_Go.SetActive(false);
        completed_Go.SetActive(false);
    }

    public void InitData(TaskItemData taskInfoData, int day)
    {
        dayIndex = day;


        this._taskItemData = taskInfoData;
        if (this._taskItemData == null)
        {
            InitUI();
            return;
        }
        Txt_Title_Normal.text = day.ToString();
        Txt_Title_Current.text = day.ToString();
        Btn_Claim.gameObject.SetActive(day <= AnniversarySummerMgr.Inst.currentDay && taskInfoData.eventStatus == 2);

        normal_Go.SetActive(day != AnniversarySummerMgr.Inst.currentDay);
        current_Go.SetActive(day == AnniversarySummerMgr.Inst.currentDay);
        mask_Go.SetActive((day <= AnniversarySummerMgr.Inst.currentDay && taskInfoData.eventStatus == 0) ||
         (day <= AnniversarySummerMgr.Inst.currentDay && taskInfoData.eventStatus == 1) || (day <= AnniversarySummerMgr.Inst.currentDay && taskInfoData.eventStatus == 3));
        completed_Go.SetActive(day <= AnniversarySummerMgr.Inst.currentDay && taskInfoData.eventStatus == 3);

        var claimStatus = (SevenDaySignClaimStatus)taskInfoData.eventStatus;
        switch (claimStatus)
        {
            case SevenDaySignClaimStatus.Lock:
                Txt_Normal_State.text = "待领取";
                Txt_Current_State.text = "待领取";
                break;
            case SevenDaySignClaimStatus.Unlocked:
                Txt_Normal_State.text = "可领取";
                Txt_Current_State.text = "可领取";
                break;
            case SevenDaySignClaimStatus.Claimed:
                Txt_Normal_State.text = "已领取";
                Txt_Current_State.text = "已领取";
                break;
        }
    }


    private void OnBtnClaimClick()
    {
        SendClaimReq();
    }

    private void OnBtnPreview1Click()
    {
        var info = AnniversarySummerMgr.Inst.RewardTypeList[dayIndex][0];
        PreviewManager.Inst.ShowPreview(info);
    }

    private void OnBtnPreview2Click()
    {
        var info = AnniversarySummerMgr.Inst.RewardTypeList[dayIndex][1];
        PreviewManager.Inst.ShowPreview(info);
    }


    private void SendClaimReq()
    {
        AnniversarySummerMgr.Inst.ClaimTaskReward(this._taskItemData.eventId, (data) =>
        {
            OnClaimSuccess(data);
            AnniversarySummerMgr.Inst.GetTaskData();
        }, (error) =>
        {
            LoggerUtils.Log("AnniversarySummerItem SendClaimReq failed: " + error);
        });
    }

    private void OnClaimSuccess(TaskClaimRsp response)
    {
        // var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        // AccountDataManager.Inst.BalanceInfo.Refresh();
        // var rewardList = new List<CommonRewardItemData>();
        // CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
        // commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)response.rewardList[0].rewardType, gameObject);
        // commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)response.rewardList[0].rewardType);
        // commonRewardItemData.RewardAmount = response.rewardList[0].amount;
        // rewardList.Add(commonRewardItemData);

        // panel.ShowRewards(rewardList);
    }
}

public enum AnniversarySummerClaimStatus
{
    ErrStatus = 0,
    Lock = 1,
    Unlocked = 2,
    Claimed = 3,
    ReCheck = 4
}
