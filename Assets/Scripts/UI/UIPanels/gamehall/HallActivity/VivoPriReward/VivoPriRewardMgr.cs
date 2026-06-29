
using UnityEngine;
using Message;
using System;
using Game.Event;
using Newtonsoft.Json;
using System.Collections.Generic;
using UI.Manager;
using GameData.Manager;
using Network;
using Network.Http;


public class VivoPriRewardPackMgr
{

    private static VivoPriRewardPackMgr _instance;
    public static VivoPriRewardPackMgr Inst
    {
        get
        {
            return _instance ?? (_instance = new VivoPriRewardPackMgr());
        }
    }



    public VivoPriRewardView view;

    BudRewardStatus _budRewardStatus = BudRewardStatus.ErrRewardStatus;

    public bool IsVivoStartFromGameCenter = false;

    VivoPriRewardPackMgr()
    {
        if(!IsVivoSupport()){
            return;
        }
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.vivoStartFromGameCenter, OnVivoStartFromGameCenter);
        Debug.Log(" VivoPriRewardPackMgr 获取IsVivoStartFromGameCenter");
        MobileInterface.Instance.IsVivoStartFromGameCenter();
    }

    void OnVivoStartFromGameCenter(string message)
    {
        Debug.Log(" VivoPriRewardPackMgr 返回OnVivoStartFromGameCenter message=" + message);
        IsVivoStartFromGameCenter = message == "1";
        MessageHelper.Broadcast(MessageName.UpdateVivoPriState);
    }
    /// <summary>
    /// 领取vivo 特权奖励
    /// </summary>
    public void ReceiveVivoPriReward()
    {
        Debug.Log(" VivoPriRewardPackMgr 领取vivo 特权奖励");
        EventCenterDataManager.Inst.CliamReward("VIVOPrivilegedBenefits", 1, 1, 0, (claimRspData) =>
        {
            Debug.Log("VivoPriRewardPackMgr 领取vivo 特权奖励成功:" + "  " + JsonConvert.SerializeObject(claimRspData));
            List<TaskClaimRewardData> rewardList = new();
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            List<CommonRewardItemData> items = new List<CommonRewardItemData>();
            {
                CommonRewardItemData item;
                item = new CommonRewardItemData()
                {
                    RewardAmount = 100,
                    rewardType = (int)BUDRewardType.RewardCoin,
                    rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardCoin),
                };
                items.Add(item);

                item = new CommonRewardItemData()
                {
                    RewardAmount = 5,
                    rewardType = (int)BUDRewardType.RewardBadge,
                    rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardBadge),
                };
                items.Add(item);
            }
            panel.ShowRewards(items);
            panel.ShowCommonBtn("确定", null);

            _budRewardStatus = BudRewardStatus.Claimed;
            MessageHelper.Broadcast(MessageName.UpdateVivoPriState);
            TokenDataManager.Inst.GetTokenData();
            AccountDataManager.Inst.BalanceInfo.Refresh();
        });
    }

    /// <summary>
    /// 获取vivo 特权信息
    /// </summary>
    public void GetVivoPriRewardInfo()
    {
        var reqParam = new GetTaskListReq()
        {
            idList = new List<string>() { "VIVOPrivilegedBenefits" }
        };
        Debug.Log("VivoPriRewardPackMgr请求活动信息: " + JsonConvert.SerializeObject(reqParam));
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, JsonConvert.SerializeObject(reqParam), (content) =>
        {
            Debug.Log("VivoPriRewardPackMgr请求活动信息成功 success: " + content);
            var rsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
            if (rsp.list.Count == 0)
            {
                return;
            }
            var taskInfo = rsp.list[0];
            if (taskInfo.eventList.Count == 1)
            {
                var eventInfo = taskInfo.eventList[0];
                _budRewardStatus = (BudRewardStatus)eventInfo.eventStatus;
            }
            MessageHelper.Broadcast(MessageName.UpdateVivoPriState);
        }, (error) =>
        {
            LoggerUtils.Log("VivoPriRewardPackMgr GetTaskData failed: " + error);
        });
    }



    public void JumpToVivoGameCenter()
    {
        Debug.Log(" VivoPriRewardPackMgr 跳转到vivo游戏中心");
        //跳转到vivo游戏中心
        MobileInterface.Instance.JumpToVivoGameCenter();
    }

    /// <summary>
    /// 获取奖励状态
    /// </summary>
    /// <returns></returns>
    public BudRewardStatus GetBudRewardStatus()
    {
        return _budRewardStatus;
    }


    public bool IsVivoSupport()
    {
        return DeviceInfoManager.Inst.CheckVersion_1_0_14() && IAPDataManager.Inst.channelId == (int)IAPDataManager.ChannelIdEnum.Vivo;
    }

    public void OpenVivoPriRewardWin()
    {
        UIManager.Inst.OpenPanel<VivoPriRewardView>(PanelId.VivoPriRewardView);
    }


    /// <summary>
    /// 是否有红点
    /// </summary>
    /// <returns></returns> 
    public bool CheckHasRedDot()
    {
        var budRewardStatus = GetBudRewardStatus();
        return budRewardStatus != BudRewardStatus.Claimed;
    }
}
