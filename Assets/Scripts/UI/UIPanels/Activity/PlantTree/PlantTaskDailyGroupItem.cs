using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class PlantTaskDailyGroupItem : MonoBehaviour
{
    public int EventID;

    public Image Icon;

    public Text Count;

    public Text Progress;

    public GameObject Lock;

    public GameObject Geted;

    public CButton Get;

    TaskItemData data;
    void Awake()
    {
        Get.onClick.AddListener(OnBtn);
    }
    private void OnEnable()
    {
        isSending = false;
    }
    public void SetData() 
    {
        Lock.gameObject.SetActive(false);
        Get.gameObject.SetActive(false);
        Geted.gameObject.SetActive(false);

        var rsp = PlantTreeSystem.Inst.DailyInfo;
        foreach (var item in rsp.list[0].eventList)
        {
            if (item.eventId == EventID)
            {
                data = item;
                break;
            }
        }

        if (data == null)
        {
            return;
        }
        Progress.text = data.finishAmount.ToString();
        var state = (ClaimStatus)data.eventStatus;
        switch (state)
        {
            case ClaimStatus.ErrStatus:
                break;
            case ClaimStatus.Lock:
                Lock.gameObject.SetActive(true);
                break;
            case ClaimStatus.Unlocked:
                Get.gameObject.SetActive(true);
                break;
            case ClaimStatus.Claimed:
                Geted.gameObject.SetActive(true);
                break;
            case ClaimStatus.Expired:
                break;
        }
    }

    bool isSending;
    void OnBtn()
    {
        if (data == null)
        {
            return;
        }

        // 发送领取奖励请求
        if (isSending) return;

        isSending = true;
        JObject jb = new JObject
        {
            ["taskId"] = TASK_ID.TreePlantingDayDailyTask.ToString(),
            ["eventId"] = data.eventId,
            ["claimType"] = 1,
            ["rewardIndex"] = 1
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimTaskRewards, HttpMethod.POST, reqParam, (content) =>
        {
            if (string.IsNullOrEmpty(content))
                return;
            isSending = false;

            var rewardItemDatas = new List<CommonRewardItemData>();
            ActivityEventClaimResponse reward = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
            if(reward.rewardList?.Count == 0)
            {
                return;
            }
            var itemData = new CommonRewardItemData()
            {
                IconSp = Icon.sprite,
                RewardAmount = reward.rewardList[0].amount,// int.Parse(Count.text.Remove(0, 1)),
                rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardList[0].rewardType),
                //rewardSpecial = Name,
            };
            rewardItemDatas.Add(itemData);

            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(rewardItemDatas);

            PlantTreeSystem.Inst.ActivityReq();
            AccountDataManager.Inst.BalanceInfo.Refresh();

        }, (error) =>
        {
            LoggerUtils.LogError($"发送奖励失败！！，失败原因{error}");
            isSending = false;
        });

    }
}