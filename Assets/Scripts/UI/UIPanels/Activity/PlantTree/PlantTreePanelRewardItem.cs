using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class PlantTreePanelRewardItem : MonoBehaviour
{
    public int EventID;
    public Image Icon;
    public GameObject Mask;
    public GameObject Lock;
    public Text IconCount;
    public Text Count;
    public Button GetButton;


    ActivityEventInfo data;
    void Awake()
    {
        GetButton.onClick.AddListener(OnBtn);
    }

    private void OnEnable()
    {
        isSending = false;
    }

    public void SetData()
    {
        //GetButton.gameObject.SetActive(false);
        Mask.gameObject.SetActive(false);
        Lock.gameObject.SetActive(false);

        var rsp = PlantTreeSystem.Inst.WaterInfo;
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
        //Debug.LogError($" planttreepanel data={JsonConvert.SerializeObject(data)}");
        var state = (ClaimStatus)data.eventStatus;
        switch (state)
        {
            case ClaimStatus.ErrStatus:
                break;
            case ClaimStatus.Lock:
                Lock.gameObject.SetActive(true);
                break;
            case ClaimStatus.Unlocked:
                GetButton.gameObject.SetActive(true);
                break;
            case ClaimStatus.Claimed:
                GetButton.gameObject.SetActive(false);
                Mask.gameObject.SetActive(true);
                break;
            case ClaimStatus.Expired:
                break;
            default:
                break;
        }
    }

    private string GetRewardName()
    {
        string rewardName = "";
        if (data?.eventId > 0)
        {
            switch (data.eventId)
            {
                case 1:
                    rewardName = "金币"; break;
                case 2:
                    rewardName = "徽章"; break;
                case 3:
                    rewardName = "幸运币"; break;
                case 4:
                    rewardName = "优优币"; break;
                case 5:
                    rewardName = "春日森屿头像框"; break;
            }
        }
        return rewardName;
    }

    private CurrencyType GetRewardType()
    {
        CurrencyType rewardType = CurrencyType.None;
        if (data?.eventId > 0)
        {
            switch (data.eventId)
            {
                case 1:
                    rewardType = CurrencyType.Coin; break;
                case 2:
                    rewardType = CurrencyType.Badge; break;
                case 3:
                    rewardType = CurrencyType.LuckyCoin; break;
                case 4:
                    rewardType = CurrencyType.YouYouCoin; break;
                case 5:
                    break;
            }
        }
        return rewardType;
    }

    bool isSending;
    void OnBtn()
    {
        if (data == null)
        {
            return;
        }
        
        if (data.eventStatus == (int)BudRewardStatus.Lock)
        {
            if (data.eventId == 5)
            {
                var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                panel.PreviewAvatarFrame(AvatarFrameType.AvatarFramePlantTree);
                return;
            }
            UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, GetRewardType());
            return;
        }

        // 发送领取奖励请求
        if (isSending) return;

        isSending = true;
        JObject jObject = new JObject()
        {
            ["activityId"] = ActivityId.TreePlantingDayWateringActivity.ToString(),
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

                ShowReward();
                AccountDataManager.Inst.BalanceInfo.Refresh();
                PlantTreeSystem.Inst.ActivityReq();
            },
            (error) =>
            {
                LoggerUtils.LogError($"发送奖励失败！！，失败原因{error}");
                isSending = false;
            });


    }

    private void ShowReward()
    {
        var rewardItemDatas = new List<CommonRewardItemData>();
        var itemData = new CommonRewardItemData()
        {
            IconSp = Icon.sprite,
            RewardAmount = int.Parse(IconCount.text.Remove(0, 1)),
            rewardName = GetRewardName(),
        };
        rewardItemDatas.Add(itemData);

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardItemDatas);
    }
}