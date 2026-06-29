using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsManagers;
using GameData.Base;
using GameData.BaseInfo;
using GameData.MapData;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class ThreeDPreviewPanel : BasePanel<ThreeDPreviewPanel>
{
    private Transform BG;
    private CButton Btn_Back;
    private PreviewInfoPanel PreviewInfoPanel;
    private ModelPreview ModelPreview;
    private VehiclePreview VehiclePreview;
    private AvatarPreview AvatarPreview;
    private BundlePreview BundlePreview;
    private PetPreview PetPreview;
    private MatPreview MatPreview;
    private AnimIkPreview AnimIkPreview;
    private MusicScorePreview MusicScorePreview;
    private AmbientLightSetting _srcLightSetting;
    private bool _srcHallLightVisible;
    private bool _srcGameSceneLightVisible;
    private bool _srcPreviewSceneLightVisible;
    private AssetDetailType _curOriAssetDetailType;


    public override void OnCreate()
    {
        base.OnCreate();
        BG = GameObjectEx.FindChildByName(this.transform, "BG");
        Btn_Back = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
        PreviewInfoPanel = GameObjectEx.FindChildByName(this.transform, "PreviewInfoPanel").GetComponent<PreviewInfoPanel>();
        ModelPreview = GameObjectEx.FindChildByName(this.transform, "ModelPreview").GetComponent<ModelPreview>();
        VehiclePreview = GameObjectEx.FindChildByName(this.transform, "VehiclePreview").GetComponent<VehiclePreview>();
        AvatarPreview = GameObjectEx.FindChildByName(this.transform, "AvatarPreview").GetComponent<AvatarPreview>();
        BundlePreview = GameObjectEx.FindChildByName(this.transform, "BundlePreview").GetComponent<BundlePreview>();
        PetPreview = GameObjectEx.FindChildByName(this.transform, "PetPreview").GetComponent<PetPreview>();
        MatPreview = GameObjectEx.FindChildByName(this.transform, "MatPreview").GetComponent<MatPreview>();
        MusicScorePreview= GameObjectEx.FindChildByName(this.transform, "MusicScorePreview").GetComponent<MusicScorePreview>();
        AnimIkPreview = GameObjectEx.FindChildByName(this.transform, "AnimIkPreview").GetComponent<AnimIkPreview>();
        
        Btn_Back.onClick.AddListener(CloseSelf);
    }
    
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _curOriAssetDetailType = (AssetDetailType)args[0];
        InputReceiver.Inst.enabled = false;
        _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
        _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
        _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
        _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();
        InitBG();
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
        switch (_curOriAssetDetailType)
        {
            case AssetDetailType.Pet:
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "pet_icon_1", "pet_icon_2", "pet_icon_3", "pet_icon_4"
                });
                break;
            
            case AssetDetailType.Prop:
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "store_icon1", "store_icon2", "store_icon3", "store_icon4", "store_icon5", "store_icon6"
                });
                break;
            
            case AssetDetailType.Instrument:
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "music_icon_1", "music_icon_2", "music_icon_3",
                });
                break;
            
            case AssetDetailType.UgcPose:
            case AssetDetailType.UgcAnim:
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "animStudio_icon1", "animStudio_icon2", "animStudio_icon3",
                });
                break;
            
            default:
            case AssetDetailType.Skin:
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "avatar_icon_1", "avatar_icon_2", "avatar_icon_3", "avatar_icon_4"
                });
                break;
        }
        item.gameObject.SetActive(true);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        InputReceiver.Inst.enabled = true;
        AmbientLightManager.Inst.CloseUILight(_srcLightSetting);
        AmbientLightManager.Inst.RevertHallLight(_srcHallLightVisible);
        AmbientLightManager.Inst.RevertGameSceneLight(_srcGameSceneLightVisible);
        AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);
    }
    
    public void PreviewPet(SkinInfo skinInfo)
    {
        PetPreview.StartPreview(skinInfo);
        PreviewInfoPanel.SetData(skinInfo);
    }
    
    public void PreviewAvatar(SkinInfo skinInfo)
    {
        AvatarPreview.StartPreview(skinInfo);
        PreviewInfoPanel.SetData(skinInfo);
    }

    public void PreviewBundle(SkinInfo skinInfo)
    {
        BundlePreview.StartPreview(skinInfo, PreviewInfoPanel.SetData);
    }

    public void PreviewModel(UgcBaseInfo baseInfo)
    {
        ModelPreview.StartPreview(baseInfo);
        PreviewInfoPanel.SetData(baseInfo);
    }

    public void PreviewVehicle(VehicleInfo baseInfo)
    {
        if (baseInfo is VehicleInfo vehicleInfo)
        {
            GameVehicleManager.Inst.AddPropData(vehicleInfo);
        }
        VehiclePreview.StartPreview(baseInfo);
        PreviewInfoPanel.SetData(baseInfo);
    }

    public void PreviewMat(MaterialInfo baseInfo)
    {
        MatPreview.StartPreview(baseInfo);
        PreviewInfoPanel.SetData(baseInfo);
    }

    public void PreviewMusicScore(MusicScoreInfo baseInfo)
    {
        MusicScorePreview.StartPreview(baseInfo);
        PreviewInfoPanel.SetData(baseInfo);
    }

    public void PreviewAnim(AnimInfo baseInfo)
    {
        AnimIkPreview.StartPreviewAnim(baseInfo);
        PreviewInfoPanel.SetData(baseInfo);
    }
    
    public void PreviewPose(PoseInfo baseInfo)
    {
        AnimIkPreview.StartPreviewPose(baseInfo);
        PreviewInfoPanel.SetData(baseInfo);
    }

    public void PreviewCharacterBox(CharacterBoxInfo info)
    {
        ModelPreview.StartPreviewBox(info);
        var baseInfo = new UgcBaseInfo
        {
            id = info.id,
            name = info.name,
            desc = info.desc,
            cover = info.cover,
            creator = info.creator,
            designCode = info.designCode,
        };
        PreviewInfoPanel.SetData(baseInfo);
    }
}
