using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class BudBoxStudioPanel : BasePanel<BudBoxStudioPanel>
{
    [SerializeField] private Transform _trans_Bg;
    [SerializeField] private CButton backBtn;
    [SerializeField] private CButton AiCharacterStudioBtn;
    [SerializeField] private CButton BoxStudioBtn;
    [SerializeField] private CButton AiCharacterShopBtn;
    [SerializeField] private CButton BoxShopBtn;
    [SerializeField] private CButton ToneShopBtn;
    public override void OnCreate()
    {
        InitBG();
        AddListeners();
    }

    private void InitBG()
    {
        if (_trans_Bg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(_trans_Bg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "vehicle_icon_1", "vehicle_icon_2", "vehicle_icon_3"
        });
        item.gameObject.SetActive(true);
    }

    private void AddListeners()
    {
        backBtn.onClick.AddListener(() =>
        {

            CloseSelf();
        });

        AiCharacterStudioBtn.onClick.AddListener(() =>
        {
            
            UIManager.Inst.OpenPanel(PanelId.IncubationCabinDraftBox);
            //UIManager.Inst.OpenPanel(PanelId.IncubationCabinRolesMainPanel);
        });

        BoxStudioBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.BoxSceneStudioMainPanel);
        });

        AiCharacterShopBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AIPartnerShopPanel, AIPartnerTabSecond.AICharacter);
        });

        BoxShopBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AIPartnerShopPanel, AIPartnerTabSecond.PartnerBox);
        });

        ToneShopBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.IncubationToneShopPanel);
        });
    }
}
