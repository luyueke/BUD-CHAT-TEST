using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using Network.Http;
using Basic.Utils;
using EventTracking;
using System;
using Game.Event;
using UI.Base;



public class AnniversaryCelePackSubView1Item : MonoBehaviour
{

    [Header("UI相关")]

    public CButton Btn_receive;
    public GameObject complete_Go;
    public Text txt_title;
    public Text txt_receive;

    private TaskItemData _taskItemData;

    public Button[] btn_previews;

    int _eventId;
    public void Awake()
    {
        Btn_receive.onClick.AddListener(OnReceiveClicked);
        if (btn_previews != null)
        {
            for (int i = 0; i < btn_previews.Length; i++)
            {
                var idx = i;
                btn_previews[i].onClick.AddListener(() => { OnPreviewClicked(idx); });
            }
        }

    }

    void OnPreviewClicked(int index)
    {
        try
        {
            var info = AnniversaryCelePackMgr.Inst.RewardTypeList[AnniversaryCelePackMgr.TaskId_1][_eventId][index];
            PreviewManager.Inst.ShowPreview(info);
        }
        catch (System.Exception e)
        {
            LoggerUtils.Log("AnniversaryCelePackSubView1Item OnPreviewClicked failed: " + e.Message);
        }
    }

    void OnEnable()
    {
        RefreshUI(_taskItemData,_eventId);
    }

    public void RefreshUI(TaskItemData taskItemData,int eventId)
    {
        this._taskItemData = taskItemData;
        _eventId = eventId;
        if (taskItemData == null)
        {
            if (txt_receive != null)
            {
                txt_receive.text = "";
            }
            complete_Go.SetActive(false);
            Btn_receive.gameObject.SetActive(false);
            return;
        }
        if (taskItemData.eventStatus == 2)
        {
            complete_Go.SetActive(false);
            Btn_receive.gameObject.SetActive(true);
            txt_receive.text = "可领取";
        }
        else if (taskItemData.eventStatus == 3)
        {
            complete_Go.SetActive(true);
            Btn_receive.gameObject.SetActive(false);
            txt_receive.text = "已领取";
        }
        else
        {
            complete_Go.SetActive(false);
            Btn_receive.gameObject.SetActive(false);
            txt_receive.text = "待领取";
        }
    }

    // void OnClaimSuccess(TaskClaimRsp response)
    // {
    //     RefreshUI();
    //     AccountDataManager.Inst.BalanceInfo.Refresh();
    //     var rewardList = new List<CommonRewardItemData>();
    //     CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
    //     commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)_taskItemData.rewardType, gameObject);
    //     commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)_taskItemData.rewardType);
    //     commonRewardItemData.RewardAmount = response.claimAmount;
    //     rewardList.Add(commonRewardItemData);

    //     panel.ShowRewards(rewardList);
    // }

    void OnReceiveClicked()
    {
        AnniversaryCelePackMgr.Inst.ClaimTaskReward(AnniversaryCelePackMgr.TaskId_1, _taskItemData.eventId, null, null);
    }

}
