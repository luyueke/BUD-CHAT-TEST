using Game.Event;
using GameData.Manager;
using Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BreakIceClaimItem : MonoBehaviour
{
    public Button item1;
    public Button item2;
    public Text title;
    public Button claimBtn;
    public GameObject overObj;
    string spriteatlasPath = "Assets/Loadable/UI/UIPanel/BreakIcePanel/ICONS.spriteatlas";
    public RawImage bgRawTex;
    string _taskId;
    int _index = 1;
    void SetStatus(int statu)
    {
        switch (statu)
        {
            case (int)EventStatus.UnClaim:
                title.text = GetTitle(_index);
                title.color = new Color(0.255f, 0.510f, 0.588f, 1f);
                claimBtn.gameObject.SetActive(false);
                overObj.gameObject.SetActive(false);
                break;
            case (int)EventStatus.Claim:
                title.text = "领取";
                title.color = new Color(0.859f, 0.435f, 0.157f, 1f);
                claimBtn.gameObject.SetActive(true);
                overObj.gameObject.SetActive(false);
                break;
            case (int)EventStatus.Finish:
                title.text = "领取";
                title.color = new Color(0.859f, 0.435f, 0.157f, 1f);
                claimBtn.gameObject.SetActive(true);
                overObj.gameObject.SetActive(true);
                break;
        }
    }
    string GetTitle(int index)
    {
        switch (index)
        {
            case 1:
                return "第一天";
                break;
            case 2:
                return "第二天";
                break;
            case 3:
                return "第三天";
                break;

        }
        return "第一天";
    }

    void SetPriview(string taskId,int index)
    {
        switch (taskId)
        {
            case "NewbieDressUpTask":
            SetRewrads(index);
            break;
            case "NewbieDressUpTask18":
            SetRewrads18(index);
            break;
            case "NewbieDressUpTask30":
            SetRewrads30(index);
            break;
        }
        
    }

    private void SetRewrads(int index)
    {
        switch (index)
        {
            case 1:
                item1.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string> { "40100510" }, "热情招呼", "oi", spriteatlasPath, "周年庆累充福利", "#3586FF", "bg");
                });
                item2.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.PinkCoin);
                });
                break;
            case 2:
                item1.onClick.AddListener(() =>
                {
                    var bundleShowpanel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    bundleShowpanel.SetEventPreview(new List<string>() {
                    "10900493"}, "甜心蝴蝶结", "hdj", spriteatlasPath, "新年组队消费 领新年好礼", "#BF8DFF",
                    bgRawTex.texture);
                });
                item2.onClick.AddListener(() =>
                {
                    var bubblePreviewPanel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                    bubblePreviewPanel.PreviewChatBubble(ChatBubblesType.ChatBubblesSweetheartBow, "甜心蝴蝶结气泡", "通过购买【新手专享装扮礼包】获得，可前往个人资料使用");
                });
                break;
            case 3:
                item1.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                    panel.PreviewAvatarFrame(AvatarFrameType.AvaratFrameSweetheartBowe);
                });
                item2.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.YouYouCoin);
                });
                break;
        }
    }
    private void SetRewrads18(int index)
    {
        switch (index)
        {
            case 1:
                // S级昵称框x1
                item1.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel<ProfileNicknamePreviewPanel>(PanelId.ProfileNicknamePreviewPanel, 6);
                });
                // 粉币180
                item2.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.PinkCoin);
                });
                break;
            case 2:
                // 社区乐器兑换券
                item1.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.CommunityInstrumentTicket);
                });
                // 社区皮肤兑换券
                item2.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.CommunitySkinTicket);
                });
                break;
            case 3:
                // 自拍动作x1
                item1.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string> { "41000007" }, "侧面坐下", "oi", spriteatlasPath, "新手专享装扮礼包", "#3586FF", "bg");
                });
                // 幸运币x20
                item2.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.LuckyCoin);
                });
                break;
        }
    }
    private void SetRewrads30(int index)
    {
        switch (index)
        {
            case 1:
                // S级特效x1
                item1.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string> { "10500082" }, "甜梦缎带", "12", spriteatlasPath, "新手专享装扮礼包", "#3586FF", bgRawTex.texture);
                });
                // S级别动作
                item2.onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetEventPreview(new List<string> { "40100568" }, "跳芭蕾舞", "oi", spriteatlasPath, "新手专享装扮礼包", "#3586FF", "bg");
                    var breakIcePanel = UIManager.Inst.FindPanel(PanelId.BreakIcePanel);
                    if(breakIcePanel != null)
                    {
                        var pn = breakIcePanel as BreakIcePanel;
                        if(pn!=null)
                        {
                             pn.StopAudio();
                        }
                    }
                });
                break;
            case 2:
                // 粉币300
                item1.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.PinkCoin);
                });
                // 社区动作兑换券
                item2.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.CommunityAnimationTicket);
                });
                break;
            case 3:
                // 社区载具兑换券
                item1.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.CommunityVehicleTicket);
                });
                // 紫梦币x30
                item2.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.PurpleDreamCoin);
                });
                break;
        }
    }
    void ShowRewardClaim()
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> items = new List<CommonRewardItemData>();
        CommonRewardItemData rewardItem1;
        CommonRewardItemData rewardItem2;

        if (_taskId == "NewbieDressUpTask18")
        {
            switch (_index)
            {
                case 1:
                    rewardItem1 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item1.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "S级昵称框" };
                    rewardItem2 = new CommonRewardItemData() { RewardAmount = 180, IconSp = item2.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "粉币" };
                    break;
                case 2:
                    rewardItem1 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item1.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "社区乐器兑换券" };
                    rewardItem2 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item2.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "社区皮肤兑换券" };
                    break;
                default:
                    rewardItem1 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item1.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "自拍动作" };
                    rewardItem2 = new CommonRewardItemData() { RewardAmount = 20, IconSp = item2.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "幸运币" };
                    break;
            }
        }
        else if (_taskId == "NewbieDressUpTask30")
        {
            switch (_index)
            {
                case 1:
                    rewardItem1 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item1.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "S级特效" };
                    rewardItem2 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item2.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "S级别动作" };
                    break;
                case 2:
                    rewardItem1 = new CommonRewardItemData() { RewardAmount = 300, IconSp = item1.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "粉币" };
                    rewardItem2 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item2.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "社区动作兑换券" };
                    break;
                default:
                    rewardItem1 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item1.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "社区载具兑换券" };
                    rewardItem2 = new CommonRewardItemData() { RewardAmount = 30, IconSp = item2.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "紫梦币" };
                    break;
            }
        }
        else
        {
            switch (_index)
            {
                case 1:
                    rewardItem1 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item1.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "热情招呼" };
                    rewardItem2 = new CommonRewardItemData() { RewardAmount = 60, IconSp = item2.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "社区商品币" };
                    break;
                case 2:
                    rewardItem1 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item1.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "甜心蝴蝶结" };
                    rewardItem2 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item2.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "聊天气泡" };
                    break;
                default:
                    rewardItem1 = new CommonRewardItemData() { RewardAmount = 1, IconSp = item1.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "头像框" };
                    rewardItem2 = new CommonRewardItemData() { RewardAmount = 6, IconSp = item2.transform.Find("icon").GetComponent<Image>().sprite, rewardName = "优优币" };
                    break;
            }
        }

        
        items.Add(rewardItem1);
        items.Add(rewardItem2);

        SetStatus((int)EventStatus.Finish);
        panel.ShowRewards(items,true);
        IAPDataManager.Inst.GetTaskList(_taskId, (b, taskInfoResponse) =>
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            if (taskInfoResponse?.list == null || taskInfoResponse.list.Count <= 0) return;

            TaskInfoData tsTaskInfoData = taskInfoResponse.list[0];
            if (tsTaskInfoData?.eventList == null) return;

            var eventList = tsTaskInfoData.eventList;
            UIManager.Inst.FindPanel<GameHallPanel>(PanelId.GameHallPanel).
                    newBieBreakIceRedDot.gameObject.SetActive(false);
            foreach (var even in eventList)
            {
                if (even.eventStatus == (int)EventStatus.Claim)
                {   
                    UIManager.Inst.FindPanel<GameHallPanel>(PanelId.GameHallPanel).
                    newBieBreakIceRedDot.gameObject.SetActive(true);
                }
            }
        });

    }
    void OnClaimBtnClick()
    {
        EventCenterDataManager.Inst.CliamReward(this._taskId, this._index , 1, 0, (claimRspData) =>
        {
            List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
            ShowRewardClaim();
            MessageHelper.Broadcast(MessageName.UpdateHallTask);
            TokenDataManager.Inst.GetTokenData();
            AccountDataManager.Inst.BalanceInfo.Refresh();
            VipDataManager.Inst.UpdateVipStatus();
            ReddotManagerUtils.Inst.RefreshRedDot();
        });
    }
    public void SetData(TaskItemData serverData , int index,string taskId)
    {
        _index = index;
        _taskId = taskId;
        claimBtn.onClick.RemoveAllListeners();
        claimBtn.onClick.AddListener(OnClaimBtnClick);
        item1.onClick.RemoveAllListeners();
        item2.onClick.RemoveAllListeners();
        title.text = GetTitle(_index);
        SetStatus(serverData.eventStatus);
        SetPriview(taskId,index);
    }
}
