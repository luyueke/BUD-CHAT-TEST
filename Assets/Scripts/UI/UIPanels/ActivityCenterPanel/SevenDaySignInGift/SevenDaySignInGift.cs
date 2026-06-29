using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using Network.Http;
using Basic.Utils;
using EventTracking;

public class SevenDaySignInGift : ActivityBaseView
{   

    
    [Header("UI相关")] 
    public Text Txt_LeftTime;
    public CButton Btn_Rule;
    
    public Text Txt_BadgeProgress;
    public Image Img_BadgeProgress;
    
    private List<SevenDaySignInItem> CheckInItems;
    private ActivityInfo _info;
    
    public override void Init(ActivityInfo info)
    {   

        CheckInItems = this.GetComponentsInChildren<SevenDaySignInItem>().ToList();
        
        base.Init(info);
        Btn_Rule.onClick.AddListener(OnRuleClicked);
        this._info = info;
        Txt_LeftTime.text = _info.leftTime;
        InitContent(this._info.eventList);
     
    }

    private void OnRuleClicked() {
        UIManager.Inst.OpenPanel<ActivityRulePanel>(PanelId.ActivityRulePanel, "Assets/Loadable/UI/ActivityCenterPanel/SevenDaySignInGift/Rule.json");
    }
    
    public override void RefrashData(ActivityInfo info) {
        base.RefrashData(info);
        _info = info;

        InitContent(this._info.eventList);
    }

    private void InitContent(List<ActivityEventInfo> eventList)
    {
        for (int i = 0; i < eventList.Count; i++)
        {
            var data = eventList[i];
            if (i == 7)
            {
                InitBadgeProgress(data);
            }
            CheckInItems[i].InitData(data);
        }
    }

    private void InitBadgeProgress(ActivityEventInfo eventInfo)
    {
        Txt_BadgeProgress.text = eventInfo.finishAmount + "/50";
        float progress = eventInfo.finishAmount / 50.0f;
        Img_BadgeProgress.fillAmount = progress;
    }

    
    
}
