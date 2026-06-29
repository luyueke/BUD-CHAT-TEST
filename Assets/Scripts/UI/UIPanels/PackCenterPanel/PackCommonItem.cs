using System;
using System.Collections.Generic;
using Game.Event;
using UnityEngine;
using UnityEngine.UI;

public class PackCommonItem : MonoBehaviour
{
    [SerializeField] private Image lockImg;
    [SerializeField] private Button rootBtn;
    [SerializeField] private GameObject coverLayout;
    [SerializeField] private Text rewardNum1;
    [SerializeField] private Text rewardNum2;
    [SerializeField] private Text title;
    [SerializeField] private Text rewardName1;
    [SerializeField] private Text rewardName2;
    [SerializeField] private Image rewardIcon1;
    [SerializeField] private Image rewardIcon2;
    [SerializeField] private Image bgNormal;
    [SerializeField] private Image bgClaim;
    [SerializeField] Text progress;

    private TaskItemData _taskItemData;
    public RewardItem _rewardItem;
    private string _taskId;
    private string spriteatlasPath;
    private bool isBig = false;
    private bool isPaid = false;

    private Action<TaskItemData> _claimAction;
    private Action blockAction;
    
    private string rechargeAtlasPath = RechargePanel.RechargePanelAtlas;

    public void SetAtlasPath(string path)
    {
        spriteatlasPath = path;
    }

    private void Start()
    {
        rootBtn.onClick.AddListener(RootBtnClick);
        blockAction += ShowToast;
    }
    void ShowToast()
    {
        TipPanel.ShowToast("购买礼包后即可解锁奖励");
    }
    private void RootBtnClick()
    {
        if (_taskItemData == null)
        {
            return;
        }

        if (_taskItemData.eventStatus != (int)EventStatus.Claim && _taskItemData.eventStatus != (int)EventStatus.Finish)
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
                        IconSp = GetSpriteByName(_rewardItem.rewardIcon1),
                        RewardAmount = _rewardItem.rewardNum1 > 1 ? _rewardItem.rewardNum1 : 1,
                        rewardName = _rewardItem.rewardName1
                    },
                    new CommonRewardItemData()
                    {
                        IconSp = GetSpriteByName(_rewardItem.rewardIcon2),
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
                        IconSp = GetSpriteByName(_rewardItem.rewardIcon1),
                        RewardAmount =  _rewardItem.rewardNum1 > 1 ? _rewardItem.rewardNum1 : 1,
                        rewardName = _rewardItem.rewardName1
                    }
                });
            }
            ReddotManagerUtils.Inst.RefreshRedDot();
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
   
        Sprite sp1 = GetSpriteByName(rewardItem.rewardIcon1);
        rewardIcon1.sprite = sp1;
        if (!isBig)
        {
            rewardName2.SetLocalText(rewardItem.rewardName2);
            Sprite sp2 = GetSpriteByName(rewardItem.rewardIcon2);
            rewardIcon2.sprite = sp2;
        }
        else
        {
            rewardIcon1.SetNativeSize();
        }
    }

    private Sprite GetSpriteByName(string spriteName)
    {
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, spriteName, gameObject);
        if (sprite == null)//兜底，从总面板里获取icon
        {
            sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(rechargeAtlasPath, spriteName, gameObject);
        }

        return sprite;
    }

    public void SetData(string taskId, TaskItemData taskItemData, Action<TaskItemData> claimAction, Action blockAction)
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
        bgNormal.gameObject.SetActive(taskItemData.eventStatus != (int)EventStatus.Claim);
        bgClaim.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.Claim);
        if (taskItemData.eventStatus == (int)EventStatus.Claim)
        {
            rootBtn.transform.GetComponent<Animator>().enabled = true;
            rootBtn.transform.GetComponent<Animator>().CrossFade("LoginGiftPanel_prompt", 0.1f);
            rootBtn.gameObject.SetActive(false);
            rootBtn.gameObject.SetActive(true);
        }
        else
        {
            rootBtn.transform.GetComponent<Animator>().enabled = false;
            rootBtn.transform.GetComponent<Animator>().CrossFade("LoginGiftPanel_done", 0.1f);
            // rootBtn.gameObject.SetActive(false);
            // rootBtn.gameObject.SetActive(true);
        }

        if (taskItemData.eventStatus != (int)EventStatus.Finish)
        {
            progress.SetLocalText("当前进度：{0}" ,taskItemData.finishAmount + "/" + _rewardItem.progress);
        }
        else
        {
            progress.SetLocalText("当前进度：{0}" ,_rewardItem.progress + "/" + _rewardItem.progress);
        }
    }
}