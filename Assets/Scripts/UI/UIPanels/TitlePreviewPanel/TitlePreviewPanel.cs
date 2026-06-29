using Basic.Utils;
using GameUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using static AccountUserInfo;




public class TitlePreviewPanel : BasePanel<TitlePreviewPanel>
{
    [SerializeField] private Text descText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text titleText;
    [SerializeField] private Image iconImage;

    UserTitle titleData;
    [SerializeField] CButton closeBtn;
    [SerializeField] CButton goBtn;

    public static string sptiteAlxs = "Assets/Loadable/UI/UIPanel/TitlePreviewPanel/TitleAlts.spriteatlas";

    private void Awake()
    {
        InitUI();
    }
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        titleData = (UserTitle)args[0];
        UpdateUI();
    }
    protected void InitUI()
    {
        descText = GameObjectEx.FindComponentByName<Text>(transform, "ReviewObj/Desc");
        timeText = GameObjectEx.FindComponentByName<Text>(transform, "ReviewObj/Time/TimeTxt");
        titleText = GameObjectEx.FindComponentByName<Text>(transform, "ReviewObj/TopBar/Title");
        iconImage = GameObjectEx.FindComponentByName<Image>(transform, "ReviewObj/TopBar/Icon");
        closeBtn = GameObjectEx.FindComponentByName<CButton>(transform, "ReviewObj/TopBar/CloseBtn");
        goBtn = GameObjectEx.FindComponentByName<CButton>(transform, "ReviewObj/GoBtn");
        goBtn.onClick.AddListener(OnGoClicked);
        closeBtn.onClick.AddListener(()=> {
            CloseSelf();
        });
    }
    protected void OnGoClicked()
    {
        if (titleData.titleId == 1 || titleData.titleId == 2 || titleData.titleId == 3)
        {
            var panel = UIManager.Inst.OpenPanel<MapTopListPanel>(PanelId.MapTopListPanel);
            panel.ruleView.SetActive(true);
        }
        else
        {
            OcCompetitionSystem.Inst.OpenPanel();
        }
    }

    public void UpdateUI()
    {
        DateTime dateTime = GameUtils.GetDataTimeStamp(titleData.claimTime);
        string startTime= dateTime.ToString("MM/dd", CultureInfo.InvariantCulture);
        dateTime = GameUtils.GetDataTimeStamp(titleData.endTime);
        string endTime = dateTime.ToString("MM/dd", CultureInfo.InvariantCulture);

        timeText.text = "称号有效期" + startTime + "-" + endTime;
        PrefileData();
    }

    void PrefileData()
    {
        string iconName;
        switch (titleData.titleId)
        {
            case 1:
                titleText.text = "紫梦守护者";
                descText.text = "“紫梦守护者”授予地图热度月榜第一名地图的前三名贡献者，象征着以行动诠释“守护”的真谛——用热爱成就创作，用支持编织梦境的璀璨星河！";
                iconName = "planet";
                break;
            case 2:
                titleText.text = "繁星守护者";
                descText.text = "“繁星守护者”授予地图热度月榜第二名地图的前三名贡献者，象征着以行动诠释“守护”的真谛——用热爱成就创作，用支持编织梦境的璀璨星河！";
                iconName = "star";
                break;
            case 3:
                titleText.text = "绮梦守护者";
                descText.text = "绮梦守护者”授予地图热度月榜第三名地图的前三名贡献者，象征着以行动诠释“守护”的真谛——用热爱成就创作，用支持编织梦境的璀璨星河！";
                iconName = "moon";
                break;
            case 4:
                titleText.text = "紫梦设计师";
                descText.text = "“紫梦设计师”授予每周设子搭配大赛第一名的设计师";
                iconName = "planet";
                break;
            case 5:
                titleText.text = "繁星设计师";
                descText.text = "“繁星设计师”授予每周设子搭配大赛第二到三名的设计师";
                iconName = "star";
                break;
            case 6:
                titleText.text = "绮梦设计师";
                descText.text = "绮梦设计师”授予每周设子搭配大赛第四到十名的设计师";
                iconName = "moon";
                break;
            default:
                titleText.text = "绮梦守护者";
                descText.text = "绮梦守护者”授予地图热度月榜第三名地图的前三名贡献者，象征着以行动诠释“守护”的真谛——用热爱成就创作，用支持编织梦境的璀璨星河！";
                iconName = "moon";
                break;
        }

        XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(sptiteAlxs, iconName, iconImage.gameObject, (sp) => {
            iconImage.sprite = sp;
        });
    }

    public static void  LoadImage(string iconName,Image img) {
        img.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(sptiteAlxs, iconName, img.gameObject);
    }

    public static Sprite LoadImage(string iconName, GameObject img)
    {
        return XAssetLoaderMgr.Inst.LoadSpriteInAltas(sptiteAlxs, iconName, img.gameObject);
    }
}
