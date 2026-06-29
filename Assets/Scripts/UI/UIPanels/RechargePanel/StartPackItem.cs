using System;
using System.Collections.Generic;
using Game.Event;
using UnityEngine;
using UnityEngine.UI;

public class StartPackItem : MonoBehaviour
{
    public Image lockImg;
    public Button rootBtn;
    public GameObject coverLayout;
    public Text rewardNum1;
    [SerializeField] private Text rewardNum2;
    public Text title;
    public Text rewardName1;
    public Text rewardName2;
    public Image rewardIcon1;
    public Image rewardIcon2;
    public Image bg;
    public Text progress;

    private TaskItemData _taskItemData;
    private RewardItem _rewardItem;
    private string _taskId;
    private string spriteatlasPath = RechargePanel.RechargePanelAtlas;
    private bool isBig = false;
    private bool isPaid = false;

    private Action<TaskItemData> _claimAction;
    private Action blockAction;


    private void Start()
    {
        rootBtn.onClick.AddListener(RootBtnClick);
    }

    private void RootBtnClick()
    {
        if (_taskItemData == null)
        {
            return;
        }

        if (_taskItemData.eventStatus != (int)EventStatus.Claim)
        {
            this.blockAction?.Invoke();
            return;
        }

        EventCenterDataManager.Inst.CliamReward(this._taskId, this._taskItemData.eventId, 1, 0,(claimRspData) =>
        {
            Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck);
            if (!isBig)
            {
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(new List<CommonRewardItemData>()
                {
                    new CommonRewardItemData()
                    {
                        IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, _rewardItem.rewardIcon1,
                            gameObject),
                        RewardAmount = _rewardItem.rewardNum1 > 1 ? _rewardItem.rewardNum1 : 1,
                        rewardName = _rewardItem.rewardName1
                    },
                    new CommonRewardItemData()
                    {
                        IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, _rewardItem.rewardIcon2,
                            gameObject),
                        RewardAmount = _rewardItem.rewardNum2 > 1 ? _rewardItem.rewardNum2 : 1,
                        rewardName = _rewardItem.rewardName2
                    }
                });
            }
            else
            {
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(new List<CommonRewardItemData>()
                {
                    new CommonRewardItemData()
                    {
                        IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, _rewardItem.rewardIcon1,
                            gameObject),
                        RewardAmount =  _rewardItem.rewardNum1 > 1 ? _rewardItem.rewardNum1 : 1,
                        rewardName = _rewardItem.rewardName1
                    }
                });
            }

            AccountDataManager.Inst.BalanceInfo.Refresh();
            _claimAction.Invoke(_taskItemData);
        });
    }

    public void OnInitCreate(RewardItem rewardItem, bool isBig)
    {
        this.isBig = isBig;
        _rewardItem = rewardItem;
        if(!string.IsNullOrEmpty(rewardItem.title) && rewardItem.title.Contains("{0}"))
        {
            title.SetLocalText(rewardItem.title,rewardItem.progress);
        }
        else
        {
            title.SetLocalText(rewardItem.title);
        }

        rewardName1.SetLocalText(rewardItem.rewardName1);
        if (rewardNum1 != null)
        {
            rewardNum1.text = rewardItem.rewardNum1 > 1 ? "x" + rewardItem.rewardNum1 : "";
        }

        if (rewardNum2 != null && rewardItem.rewardNum2 > 1)
        {
            rewardNum2.text = "x" + rewardItem.rewardNum2;
        }

        Sprite sp1 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon1, gameObject);
        rewardIcon1.sprite = sp1;
        rewardIcon1.SetNativeSize();
        if (!isBig)
        {
            rewardName2.SetLocalText(rewardItem.rewardName2);
            Sprite sp2 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardItem.rewardIcon2, gameObject);
            rewardIcon2.sprite = sp2;
            rewardIcon2.SetNativeSize();
        }
    }

    public void SetData(string taskId, TaskItemData taskItemData, Action<TaskItemData> claimAction, Action blockAction,PaidPackageType paidPackageType,bool isOver = false)
    {
        this._claimAction = claimAction;
        this.blockAction = blockAction;
        if (taskItemData == null)
        {
            return;
        }

        _taskId = taskId;
        _taskItemData = taskItemData;
        lockImg.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.UnClaim);
        coverLayout.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.Finish);
        Sprite bgSp = null;
        
        if (paidPackageType == PaidPackageType.WeirdCorePack)
        {
            bgSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(RechargePanel.S4ShiYuanPackPanelAtlas,
                taskItemData.eventStatus == (int)EventStatus.Claim ? "common_active" : "s4_weirdcore_inactive",
                gameObject);
        } 
        else if (paidPackageType == PaidPackageType.Y2KPack)
        {
            bgSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(RechargePanel.S4ShiYuanPackPanelAtlas,
                taskItemData.eventStatus == (int)EventStatus.Claim ? "common_active" : "s4_y2k_inative",
                gameObject);
        } 
        else if (paidPackageType == PaidPackageType.LimitedTimeCurrencyPack)
        {
            bgSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(RechargePanel.S4ShiYuanPackPanelAtlas,
                taskItemData.eventStatus == (int)EventStatus.Claim ? "common_active" : "s4_limit_inative",
                gameObject);
        }
        else if (paidPackageType == PaidPackageType.WasabiPack)
        {
            bgSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(RechargePanel.S11ShiYuanPackPanelAtlas,
                taskItemData.eventStatus == (int)EventStatus.Claim ? "common_active" : "s11_limit_inative",
                gameObject);
        }
        bg.sprite = bgSp;
        if (taskItemData.eventStatus == (int)EventStatus.Claim)
        {
            rootBtn.transform.GetComponent<Animator>().enabled = true;
            rootBtn.transform.GetComponent<Animator>().CrossFade("LoginGiftPanel_prompt", 0.1f);
        }
        else
        {
            rootBtn.transform.GetComponent<Animator>().CrossFade("LoginGiftPanel_done", 0.1f);
        }

        progress.SetLocalText("当前进度：{0}" ,taskItemData.finishAmount + "/" + _rewardItem.progress);
        if (isOver)
        {
            rootBtn.interactable = false;
        }
    }
}