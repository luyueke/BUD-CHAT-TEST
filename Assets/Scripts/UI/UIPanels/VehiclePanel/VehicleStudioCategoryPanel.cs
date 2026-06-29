using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;

public class VehicleStudioCategoryPanel : BasePanel<VehicleStudioCategoryPanel>
{
    [SerializeField] private Transform _trans_Bg;
    [SerializeField] private CButton backBtn;
    [SerializeField] private CButton vehicleStudioBtn;
    [SerializeField] private CButton vehicleShopBtn;

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

        vehicleStudioBtn.onClick.AddListener(() => 
        {
            UIManager.Inst.OpenPanel(PanelId.VehicleStudioPanel);
        });

        vehicleShopBtn.onClick.AddListener(() =>
        {
            var fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
            if (fittingRoom)
            {
                fittingRoom.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.UgcVehicle, (int)GameData.PgcData.VehicleSubType.FittingRoomVehicle));
            }
        });
    }
}
