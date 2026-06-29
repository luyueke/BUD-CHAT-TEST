using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class PlantTaskRechargeGroupItem : MonoBehaviour
{
    public int EventID;

    public Image Icon;

    public Text Count;

    public Image Icon2;

    public Text Count2;

    public CButton Lock;

    public GameObject Geted;

    public CButton Get;


    ActivityEventInfo data;
    void Awake()
    {
        Get.onClick.AddListener(OnBtn);
        Lock.onClick.AddListener(OnRecharge);
    }

    private void OnRecharge()
    {
        var panel = UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel, RechargeId.GemPack);
        panel.OnTabClick(RechargeId.GemPack);
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

        var rsp = PlantTreeSystem.Inst.RechargeInfo;
        foreach (var item in rsp.eventList)
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
        JObject jObject = new JObject()
        {
            ["activityId"] = ActivityId.TreePlantingDayDailyRecharge.ToString(),
            ["eventId"] = data.eventId,
            ["isAll"] = 0,
            ["expireClaim"] = 0
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                isSending = false;

                var rewardItemDatas = new List<CommonRewardItemData>();

                var itemData = new CommonRewardItemData()
                {
                    IconSp = Icon.sprite,
                    RewardAmount = int.Parse(Count.text.Remove(0, 1)),
                    //rewardSpecial = Name,
                };
                rewardItemDatas.Add(itemData);

                var itemData2 = new CommonRewardItemData()
                {
                    IconSp = Icon2.sprite,
                    RewardAmount = int.Parse(Count2.text.Remove(0, 1)),
                    //rewardSpecial = Name,
                };
                rewardItemDatas.Add(itemData2);

                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(rewardItemDatas);
                AccountDataManager.Inst.BalanceInfo.Refresh();
                PlantTreeSystem.Inst.ActivityReq();
            },
            (error) =>
            {
                LoggerUtils.LogError($"发送奖励失败！！，失败原因{error}");
                isSending = false;
            });


    }
}