using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Event;
using Message;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewbieV3TaskBottomItem : MonoBehaviour
{

    public Text title;
    public CButton goBtn;
    public Image finishIcon;
    public CButton claimBtn;
    public Text waitText;
    public GameObject rewardItem;
    public GameObject vipTips;
    public Slider slider;
    public Text finishNum;
    private TaskItemData _taskItemData;
    private RewardItem _rewardItem;
    private Action<TaskConfig> clickAction;
    private Action<TaskClaimRsp> claimAction;
    public Text fireText;
    TaskConfig _taskConfig;
    string _taskId;

    
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/BudNewBieTaskV3Panel/RewardIconAtlas.spriteatlas";
    private void Start()
    {
        goBtn.onClick.AddListener(() =>
        {
            if (_taskConfig.progress.Count > 0) {
                clickAction?.Invoke(_taskConfig);
            }
            else
            {
                goBtn.gameObject.SetActive(false);
            }
            
        });
        claimBtn?.onClick.AddListener(OnClaimBtnClick);
    }
    public void OnInitCreate(TaskConfig taskItem)
    {
        this._taskConfig = taskItem;
        title.text = taskItem.title;

    }

    public void SetData(TaskItemData taskItemData,Action<TaskConfig> clickAction , Action<TaskClaimRsp> claimAction, string taskId = "" , bool isClose = false)
    {
        if (taskItemData == null)
        {
            return;
        }
        this.clickAction = clickAction;
        this.claimAction = claimAction;
        _taskItemData = taskItemData;
        title.text = $"{_taskConfig.title}";
        finishNum.text = $"{taskItemData.finishAmount}/{_taskConfig.num}";
        slider.value = (float)taskItemData.finishAmount / _taskConfig.num;
        if (taskItemData.eventStatus == (int)EventStatus.Default)
        {
            finishIcon.gameObject.SetActive(false);
            claimBtn?.gameObject.SetActive(false);
        }
        else
        {
            claimBtn?.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.Claim);
            if (!isClose)
            {
                goBtn.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.UnClaim && _taskConfig.progress.Count > 0);
            }
            else
            {
                goBtn.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.UnClaim && _taskConfig.progress.Count > 0);
                waitText.gameObject.SetActive(_taskConfig.progress.Count <= 0);
            }
            
            finishIcon.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.Finish);
        }
        _taskId = taskId;
        if (_taskId == "")
        {
            claimBtn?.gameObject.SetActive(false);
        }
        if (rewardItem != null)
        {   
            if(_taskConfig.rewardNum!=null)
            foreach (var reward in _taskConfig.rewardNum) {
                var item = reward.Split(",");
                if(item[0] == "7")
                {
                    fireText.text = "活跃度+" + item[1];
                    continue;
                }
                // 克隆预制体
                GameObject cloneItem = GameObject.Instantiate(rewardItem, rewardItem.transform.parent);
                //设置icon图
                Image iconImage = cloneItem.transform.Find("Icon").GetComponent<Image>();
                if(item[0] == "1001")
                {
                        iconImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "icon_vip" , cloneItem);
                        iconImage.gameObject.GetComponent<Button>().onClick.AddListener(() => {
                            vipTips.gameObject.SetActive(true);
                        });
                    }
                else
                {
                    iconImage.sprite = PgcUtils.LoadCurrencyIcon((CurrencyType)int.Parse(item[0]), cloneItem);
                    //设置奖励预览
                    iconImage.gameObject.GetComponent<Button>().onClick.AddListener(()=> {
                        UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel, (CurrencyType)int.Parse(item[0]));
                    });
                }
                
                // 找到num对象并设置文本
                Text numText = cloneItem.transform.Find("num").GetComponent<Text>();
                if (numText != null)
                {
                    numText.text = item[1];
                }
                // 激活物体
                cloneItem.SetActive(true);
            }
        }
        var a = transform.Find("Center").GetComponent<RectTransform>();
        // 强制刷新布局
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(a);

    }

    void OnClaimBtnClick()
    {
        EventCenterDataManager.Inst.CliamReward(this._taskId, this._taskItemData.eventId, 1, 0, (claimRspData) =>
        {
            List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
           var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            List<CommonRewardItemData> items = new List<CommonRewardItemData>();
            this.claimAction?.Invoke(claimRspData);
            MessageHelper.Broadcast(MessageName.UpdateHallTask);
            foreach (var reward in rewardList)
            {
                CommonRewardItemData item;
                MessageHelper.Broadcast(MessageName.OnPlayerInfoAccountChange, (CurrencyType)reward.rewardType);
                if (reward.rewardType != 77)
                {//77是活跃度
                    item = new CommonRewardItemData()
                    {
                        RewardAmount = reward.amount,
                        rewardType = reward.rewardType,
                        rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardType)
                    };
                }
                else
                {
                    continue;
                }
                
                items.Add(item);
            }
            panel.ShowRewards(items);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            VipDataManager.Inst.UpdateVipStatus();
            ReddotManagerUtils.Inst.RefreshRedDot();
        });
    }

}
