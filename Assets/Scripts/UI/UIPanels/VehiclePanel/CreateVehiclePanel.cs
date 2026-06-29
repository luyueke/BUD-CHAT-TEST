using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.MusicalInstrument;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CreateVehiclePanel : BasePanel<CreateVehiclePanel>
{
    public Transform BG;
    public CButton Btn_Return;
    public LoadingButton Btn_Confirm;
    public Image Img_Confirm;
    public CreateAndPublishEditBox EditNameBox;
    public CButton Btn_ChangeType;
    public BUD_Text Txt_TypeTitle;
    public Transform SelectListTsf;
    public CButton Btn_Select_0;
    public CButton Btn_Select_1;

    private string _curName;
    private string unEnableColor = "#D9D9D9";
    private string enableColor = "#FFD400";
    private VehicleInfo _EmptyVehicleInfo;
    private const string tempSpriteatlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";

    public override void OnCreate()
    {
        base.OnCreate();
        InitDefaultData();
        InitBG();
        Btn_Return.onClick.AddListener(CloseSelf);
        Btn_ChangeType.onClick.AddListener(OnChangeTypeClick);
        Btn_Select_0.onClick.AddListener(OnSelectOneBtnClick);
        Btn_Select_1.onClick.AddListener(OnSelectTwoBtnClick);
        EditNameBox.SetAfterTextChangeAction(AfterTextChangeAction);
        EditNameBox.InitUI("给你的创作起个名字");
        Img_Confirm.color = DataUtil.DeSerializeColorCheckHash(unEnableColor);
        Btn_Confirm.SetClickAble(false);

        Btn_Confirm.onClick.RemoveAllListeners();
        Btn_Confirm.onClick.AddListener(CreateEmptyVehicle);

        Txt_TypeTitle.SetLocalText("单人载具");
    }

    private void InitDefaultData()
    {
        _EmptyVehicleInfo = VehicleUtils.GetDefaultVehicleSkinInfo();
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
                "vehicle_icon_1", "vehicle_icon_2", "vehicle_icon_3"
            });
        item.gameObject.SetActive(true);
    }

    private void OnChangeTypeClick()
    {
        SelectListTsf.gameObject.SetActive(!SelectListTsf.gameObject.activeSelf);
    }

    private void OnSelectOneBtnClick()
    {
        Txt_TypeTitle.SetLocalText("单人载具");
        SelectListTsf.gameObject.SetActive(false);
        _EmptyVehicleInfo.vehicleType = (int)VehicleSubType.SingleVehicle;
    }

    private void OnSelectTwoBtnClick()
    {
        Txt_TypeTitle.SetLocalText("双人载具");
        SelectListTsf.gameObject.SetActive(false);
        _EmptyVehicleInfo.vehicleType = (int)VehicleSubType.DoubleVehicle;
    }

    private void AfterTextChangeAction(string name)
    {
        _curName = name;
        Btn_Confirm.SetClickAble(!string.IsNullOrEmpty(_curName));

        var btnColor = string.IsNullOrEmpty(_curName) ? unEnableColor : enableColor;
        Img_Confirm.color = DataUtil.DeSerializeColorCheckHash(btnColor);
    }

    private void CreateEmptyVehicle()
    {
        Btn_Confirm.ShowLoading();
        _EmptyVehicleInfo.name = _curName;

        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tempSpriteatlasPath, "UGCVehicle_1", gameObject);
        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(new SkinInfo()
        {
            name = _curName
        }, null, LoadingType.Vehicle, s: sprite);

        GameController.StartVehicleActionGame(EnterGameModel.UgcVehicleEmpty, _EmptyVehicleInfo);
    }
}
