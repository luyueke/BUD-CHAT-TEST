using System;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class NewbieTaskBottomItem : MonoBehaviour
{

    public Text title;
    public CButton finishBtn;
    public Image finishIcon;
    
    private TaskItemData _taskItemData;
    private RewardItem _rewardItem;
    private Action<RewardItem> clickAction;

    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/EventCenter/EventCenter.spriteatlas";

    private void Start()
    {
        finishBtn.onClick.AddListener(() =>
        {
            clickAction?.Invoke(_rewardItem);
        });
    }
    
    public void OnInitCreate(RewardItem rewardItem)
    {
        this._rewardItem = rewardItem;
        title.text = rewardItem.title;
        
    }
    
    public void SetData(TaskItemData taskItemData,Action<RewardItem> clickAction)
    {
        if (taskItemData == null)
        {
            return;
        }

        this.clickAction = clickAction;
    
        _taskItemData = taskItemData;
        title.text = $"{_rewardItem.title}({taskItemData.finishAmount}/{_rewardItem.num})";

        if (taskItemData.eventStatus == (int)EventStatus.Default)
        {
            finishBtn.gameObject.SetActive(false);
            finishIcon.gameObject.SetActive(false);
        }
        else
        {
            finishBtn.gameObject.SetActive(taskItemData.eventStatus == (int)EventStatus.UnClaim);
            finishIcon.gameObject.SetActive(taskItemData.eventStatus != (int)EventStatus.UnClaim);
        }
    }
}
