using System;
using UnityEngine;
using UnityEngine.UI;

public class DailyItem : MonoBehaviour
{
    [SerializeField] private Text dayText;            // 天数文本
    [SerializeField] private GameObject coverLayout;   // 已领取遮罩
    [SerializeField] private GameObject bgClaim;           // 可领取背景
    [SerializeField] private Button rootBtn;          // 根按钮
    [SerializeField] private GameObject lockObj;      // 锁定状态物体

    public ActivityEventInfo eventInfo;
    public ActivityRewardInfo rewardItem;

    public CurrencyType showID;

    /// <summary>
    /// 初始化奖励信息
    /// </summary>
    public void Init(ActivityResponse activity)
    {
        //foreach (var eventInfo in activity.eventList)
        {

        }
    }

    /// <summary>
    /// 设置图集路径
    /// </summary>
    public void SetAtlasPath(string path)
    {
        
    }

    /// <summary>
    /// 设置进度（如果需要）
    /// </summary>
    public void SetProgress(int progress)
    {
        // 根据需要实现进度显示
    }

    /// <summary>
    /// 初始化事件信息和领取回调
    /// </summary>
    /// 
    public void Init(Action<ActivityEventInfo, ActivityRewardInfo, CurrencyType> claimAction)
    {
        rootBtn.onClick.RemoveAllListeners();
        rootBtn.onClick.AddListener(() => {
            claimAction.Invoke(eventInfo, rewardItem , showID);
        });

        UpUI();
    }

    private void UpUI()
    {
        dayText.text = eventInfo.eventId.ToString();
        RefreshClaimStatus();
    }

    /// <summary>
    /// 刷新事件信息
    /// </summary>
    

    /// <summary>
    /// 刷新领取状态显示
    /// </summary>
    public void RefreshClaimStatus()
    {   
        rootBtn.interactable = true;
        switch (eventInfo.eventStatus)
        {   
            case (int)ClaimStatus.Claimed:  // 已领取
                coverLayout.SetActive(true);
                lockObj.SetActive(false);
                bgClaim.SetActive(false);
                break;
            case (int)ClaimStatus.Lock:    // 未解锁
                lockObj.SetActive(true);
                coverLayout.SetActive(false);
                bgClaim.SetActive(false); 
                break;
                
            case (int)ClaimStatus.Unlocked:  // 可领取
                bgClaim.SetActive(true);
                lockObj.SetActive(false);
                coverLayout.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// 设置特定天数的显示
    /// </summary>
    public void SetDay(int day)
    {
        //eventId = day;
        //dayText.text = $"第{day}天";
    }
}