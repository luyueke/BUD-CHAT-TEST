using System;
using System.Collections.Generic;
using Game.Event;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewBieV3AccumulativeTaskItem : MonoBehaviour
{
    public Button rootBtn;
    public Text rewardNum;
    public Text lowNum;
    public Text overLowNum;
    public GameObject overObj;
    public GameObject claimBg;
    public GameObject defaultBg;
    public GameObject selectBg;
    public Image iconImage;
    public TaskItemData _taskItemData;
    public int _curCount;
    public RawImage bgRawTex;
    private Action<int> _clickAction;
    public RewardPreviewPanel bundleShowpanel;
    private Action<TaskClaimRsp> _claimAction;
    Action<int> _selectAction;
    string _taskId;
    List<int> width = new List<int> {0, 203, 185, 220, 203, 190, 210, 210 };
    string spriteatlasPath = "Assets/Loadable/UI/UIPanel/BudNewBieTaskV3Panel/RewardIconAtlas.spriteatlas";
    private void Start()
    {
        rootBtn.onClick.AddListener(RootBtnClick);
    }
    private void RootBtnClick()
    {
        _selectAction.Invoke(_curCount);
        if (_taskItemData == null)
        {
            return;
        }

        if (_taskItemData.eventStatus != (int)EventStatus.Claim)
        {
            ShowPreview(_curCount);
            return;
        }
        else
        {   
            EventCenterDataManager.Inst.CliamReward(this._taskId, this._taskItemData.eventId, 1, 0, (claimRspData) =>
            {
                if (_curCount == 1)
                {
                    EventTracking.LoadEvent.ReportPopupStatus("1", "guide_task_reward1");
                }
                if (_curCount == 2)
                {
                    EventTracking.LoadEvent.ReportPopupStatus("1", "guide_task_reward2");
                }
                
                _claimAction.Invoke(claimRspData);
                List<CommonRewardItemData> items = new List<CommonRewardItemData>();
                CommonRewardItemData item = new CommonRewardItemData()
                {
                    IconSp = GetRewardIcon(_curCount),
                    RewardAmount = GetRewardNum(_curCount),
                    rewardName =  GetRewardName(_curCount),
                };
                items.Add(item);
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(items,true);

                AccountDataManager.Inst.BalanceInfo.Refresh();
                VipDataManager.Inst.UpdateVipStatus();
                ReddotManagerUtils.Inst.RefreshRedDot();
            });
        }
    }
    public void SetSelect(bool isShow)
    {   
        if(defaultBg!=null)
        defaultBg?.SetActive(!isShow);
        if(selectBg!= null)
        selectBg?.SetActive(isShow);
    }
    public void SetData(string taskId , Action<TaskClaimRsp> claimAction, TaskItemData taskItemData, Action<int> clickAction, int curCount,Action<int> selectAction)
    {
        _taskId = taskId;
        _curCount = curCount;
        _selectAction = selectAction;
        _claimAction = claimAction;
        _taskItemData = taskItemData;
        this._clickAction = clickAction;
        RectTransform rectTransform = GetComponent<RectTransform>();
        // 只修改宽度，保持原有高度
        float currentHeight = rectTransform.sizeDelta.y;
        rectTransform.sizeDelta = new Vector2(width[curCount], currentHeight);
        if (taskItemData == null)
        {
            return;
        }
        lowNum.text = taskItemData.finishAmount.ToString();
        overLowNum.text = taskItemData.finishAmount.ToString();
        rewardNum.text = GetRewardNum(curCount) == 1 ? "" : GetRewardNum(curCount).ToString();
        SetStatus(taskItemData.eventStatus);
        iconImage.sprite = GetRewardIcon(curCount);
        if(curCount == 1 || curCount==3 || curCount== 5 || curCount == 7)
        {
            iconImage.SetNativeSize();
        }
        if (curCount == 3 || curCount == 7)
        {
            iconImage.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }
        if (curCount == 2)
        {
            iconImage.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        }
    }
    void ShowPreview(int id)
    {
        switch (id)
        {
            case 1:
                bundleShowpanel.gameObject.SetActive(true);
                bundleShowpanel.SetEventPreview(new List<string>() {
                    "10900492"}, "童心甜梦睡帽", "Bundle_103", XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "新年组队消费 领新年好礼", "#BF8DFF",
                bgRawTex.texture);
             
                return;
            case 2:
                UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel, CurrencyType.Badge);
                return;
            case 3:
                bundleShowpanel.gameObject.SetActive(true);
                bundleShowpanel.SetEventPreview(new List<string>() {
                    "11300354"}, "童心甜梦拖鞋", "Bundle_103", XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "新年组队消费 领新年好礼", "#BF8DFF",
                bgRawTex.texture);
     
                return;
            case 4:
                UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel, CurrencyType.PinkCoin);
                return;
            case 5:
                bundleShowpanel.gameObject.SetActive(true);
                bundleShowpanel.SetEventPreview(new List<string>() {
                    "11000261"}, "童心甜梦抱枕", "Bundle_103", XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "新年组队消费 领新年好礼", "#BF8DFF",
                bgRawTex.texture);

                return;
            case 6:
                UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel, CurrencyType.LuckyCoin);
                return;
            case 7:
                bundleShowpanel.gameObject.SetActive(true);
                bundleShowpanel.SetEventPreview(new List<string>() {
                    "10400492"}, "童心甜梦睡裙", "Bundle_103", XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "新年组队消费 领新年好礼", "#BF8DFF",
                bgRawTex.texture);
               
                return;

        }
    }
    void SetStatus(int statu)
    {
        switch (statu)
        {
            case (int)EventStatus.UnClaim:
                defaultBg.SetActive(true);
                overObj.SetActive(false);
                selectBg.SetActive(false);
                claimBg.SetActive(false);
                break;
            case (int)EventStatus.Claim:
                defaultBg.SetActive(false);
                overObj.SetActive(false);
                selectBg.SetActive(false);
                claimBg.SetActive(true);
                break;
            case (int)EventStatus.Finish:
                overObj.SetActive(true);
                selectBg.SetActive(false);
                claimBg.SetActive(false);
                break;
        }
    }

    int GetRewardNum(int id)
    {
        switch (id)
        {
            case 2:
                return 50;
            case 4:
                return 5;
            case 6:
                return 20;
            default:
                return 1;
        }
    }
    Sprite GetRewardIcon(int id)
    {
        switch (id)
        {
            case 2:
                return PgcUtils.LoadCurrencyIcon((int)CurrencyType.Badge, iconImage.gameObject);
            case 4:
                return PgcUtils.LoadCurrencyIcon((int)CurrencyType.PinkCoin , iconImage.gameObject);
            case 6:
                return PgcUtils.LoadCurrencyIcon((int)CurrencyType.LuckyCoin, iconImage.gameObject);
            default:
                return XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, GetRewardIconName(id), gameObject);
        }
    }
    string GetRewardIconName(int id)
    {
        switch (id)
        {   
             
            case 1:
                return "Reward1";
            case 3:
                return "Reward3";
            case 5:
                return "Reward5";
            case 7:
                return "Reward7";
            default:
                return "";
        }
    }
    string GetRewardName(int id)
    {
        switch (id)
        {
            case 1:
                return "童心甜梦睡帽";
            case 2:
                return "徽章";
            case 3:
                return "童心甜梦拖鞋";
            case 4:
                return "社区商品币";
            case 5:
                return "童心甜梦抱枕";
            case 6:
                return "幸运币";
            case 7:
                return "童心甜梦睡袍";
            default:
                return "";
        }
    }
}
