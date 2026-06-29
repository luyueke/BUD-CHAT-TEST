using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AICompanionPanel : BasePanel<AICompanionPanel>
{
    private Transform BG;
    private CButton Btn_Back;
    private CButton BtnStore;
    private CButton BtnSet;
    private CButton BtnCharactor;
    private CButton BtnChat;
    private CButton BtnAd;
    private CButton BtnNiuDan;

    public override void OnCreate()
    {
        base.OnCreate();


        BG = transform.Find("BaseLayout2D/Bg");
        Btn_Back = transform.Find("BaseLayout2D/UIContainer/BackButton").GetComponent<CButton>();
        BtnStore = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/BtnStore").GetComponent<CButton>();
        BtnSet = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/BtnSet").GetComponent<CButton>();
        BtnCharactor = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/BtnCharactor").GetComponent<CButton>();
        BtnChat = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/BtnChat").GetComponent<CButton>();
        BtnAd = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/BtnBoxAd").GetComponent<CButton>();
        BtnNiuDan = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/BtnNiuDan").GetComponent<CButton>();
        InitBG();

        Btn_Back.onClick.AddListener(CloseSelf);
        BtnStore.onClick.AddListener((() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AIPartnerShopPanel);
        }));
        BtnSet.onClick.AddListener((() =>
        {
            CabinBoxManager.Inst.OpenBoxEntrance();
        }));
        BtnCharactor.onClick.AddListener((() =>
        {
            UIManager.Inst.OpenPanel(PanelId.IncubationCabinRolesMainPanel);
        }));
        BtnChat.onClick.AddListener((() =>
        {
            ScreenOrientationHelper.Inst.Switch(
                ScreenOrientation.Portrait,
                onComplete: () => UIManager.Inst.OpenPanel(PanelId.AICompanionChatPanel, gameObject),
                onPreRotate: () => gameObject.SetActive(false));
        }));
        BtnAd.onClick.AddListener((() =>
        {
            //广告
            TipPanel.ShowToast("敬请期待");
        }));

        BtnNiuDan.onClick.AddListener((() =>
        {
            //扭蛋
            TipPanel.ShowToast("敬请期待");
        }));
        AmbientLightManager.Inst.ShowPreviewDirLight();
        AmbientLightManager.Inst.HideHallLight();
    }

    protected override void OnDestroy()
    {
        AmbientLightManager.Inst.HidePreviewDirLight();
        AmbientLightManager.Inst.RevertHallLight(true);
        CabinBoxManager.Inst.DisconnectMqtt();
        base.OnDestroy();
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
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "animStudio_icon1", "animStudio_icon2", "animStudio_icon3"
        });
        item.gameObject.SetActive(true);
    }

}
