using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AIYandereLeaderboardPanel : BasePanel<AIYandereLeaderboardPanel>
{
    public Transform BG;
    public CButton Btn_Close;
    public Toggle Tog_TheBest;
    public Toggle Tog_TheFast;
    public LeaderboardWidget LeaderboardWidget;

    public override void OnCreate()
    {
        base.OnCreate();

        InitBG();
        
        Btn_Close.onClick.AddListener(CloseSelf);
        Tog_TheBest.onValueChanged.AddListener(OnTogTheBestClick);
        Tog_TheFast.onValueChanged.AddListener(OnTogTheFastClick);

        Tog_TheBest.isOn = true;
        LeaderboardWidget.GetData(RankType.YandereTheBest);
    }
    
    private void InitBG()
    {
        if (BG == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFD68A", atlasPath, new List<string>()
        {
            "icon_yandere_1","icon_yandere_2","icon_yandere_3"
        });
        item.gameObject.SetActive(true);
    }

    private void OnTogTheBestClick(bool isOn)
    {
        if (isOn)
        {
            LeaderboardWidget.GetData(RankType.YandereTheBest);
        }
    }

    private void OnTogTheFastClick(bool isOn)
    {
        if (isOn)
        {
            LeaderboardWidget.GetData(RankType.YandereTheFast);
        }
    }
    
}
