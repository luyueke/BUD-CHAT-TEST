using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventTracking;
public class ActivityBaseView : MonoBehaviour
{
    protected ActivityCenterPanel mainPanel;
    public Action updateRedDotAction;

    public virtual string Path { get { return "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas"; } }
    public void SetMainPanel(ActivityCenterPanel panel)
    {
        mainPanel = panel;
    }
    public virtual void Init(ActivityInfo info)//本地数据只有任务基本信息，只在创建时调用一次,!!不一定包含后端数据(应该在此方法中初始化出所有的任务ui等)
    {
        EventTracking.LoadEvent.ReportPopupStatus(info.activityId);
    }
    protected virtual void OnViewShow()
    {
      
    }
    public virtual void RefrashData(ActivityInfo info)//后端数据，包括任务完成信息，领取状态等(应该在此方法中刷新任务领取状态货币余额等)
    {
    }
    protected virtual void OnViewHide()
    {
      
    }
    public void Show()
    {
        gameObject.SetActive(true);
        OnViewShow();
    }
    
    public void Hide()
    {
        OnViewHide();
        gameObject.SetActive(false);
    }
    public bool IsShow()
    {
        return gameObject.activeSelf;
    }
    public void InitBg(Transform bgParent,string bgColor,List<string> iconNames)
    {
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(bgParent);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem(bgColor, Path, iconNames);
        item.gameObject.SetActive(true);
      
    }
    //用于更新tab红点(需要记录红点的任务在领取奖励后调用)
    protected void UpdateRedDot()
    {
        updateRedDotAction?.Invoke();
    }
}
