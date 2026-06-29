using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;

public class TheatreStudioCategoryPanel : BasePanel<TheatreStudioCategoryPanel>
{
    private Transform BG;
    private CButton Btn_Back;
    private CButton Btn_ActorStudio;
    private CButton ActorStudioBtn;
    //private CButton AudioMallBtn;
    private CButton ActorMallBtn;
    private CButton ScriptMallBtn;
    public override void OnCreate()
    {
        base.OnCreate();


        BG = transform.Find("BaseLayout2D/Bg");
        Btn_Back = transform.Find("BaseLayout2D/UIContainer/BackButton").GetComponent<CButton>();
        Btn_ActorStudio = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/TheatreStudioBtn").GetComponent<CButton>();
        ActorStudioBtn = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/ActorStudioBtn").GetComponent<CButton>();
        //AudioMallBtn = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/AudioMallBtn").GetComponent<CButton>();
        ActorMallBtn = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/ActorMallBtn").GetComponent<CButton>();
        ScriptMallBtn = transform.Find("BaseLayout2D/UIContainer/Content/BtnContent/ScriptMallBtn").GetComponent<CButton>();

        InitBG();

        Btn_Back.onClick.AddListener(CloseSelf);
        Btn_ActorStudio.onClick.AddListener((() =>
        {
            UIManager.Inst.OpenPanel(PanelId.TheatreEditorStudioPanel);
        }));
        ActorStudioBtn.onClick.AddListener((() =>
        {
            UIManager.Inst.OpenPanel(PanelId.TheatreActorEditorMainPanel);
        }));
        // AudioMallBtn.onClick.AddListener(() =>
        // {
        //     UIManager.Inst.OpenPanel<UgcAnimToneStorePanel>(PanelId.UgcAnimToneStorePanel);
        // });
        ActorMallBtn.onClick.AddListener(() =>
        {
            // var fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
            // if (fittingRoom)
            // {
            //     fittingRoom.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.AvatarCard, (int)GameData.PgcData.UgcTheatreSubType.AvatarCard));
            // }
            UIManager.Inst.OpenPanel(PanelId.ActorCardStorePanel);
        });
        ScriptMallBtn.onClick.AddListener(() =>
        {
            var fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
            if (fittingRoom)
            {
                fittingRoom.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.Theatre, (int)GameData.PgcData.UgcTheatreSubType.Theatre));
            }
        });
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
            "theatre_icon1", "theatre_icon2"
        });

        item.gameObject.SetActive(true);
    }
    
}
