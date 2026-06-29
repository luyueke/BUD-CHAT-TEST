//using System.Collections;
//using System.Collections.Generic;
//using Es;
//using Game.Avatar;
//using GameData.PgcData;
//using UI;
//using UI.BaseWidgets;
//using UI.UIPanels.FittingRoom;
//using UnityEngine;
//using UnityEngine.UI;

//public class VehicleAdjustView : UGCBaseStateView
//{
//    [SerializeField] private Transform characterRoot;

//    [SerializeField] private AdjustView adjustView;

//    [SerializeField] private AvatarCameraController cameraController;

//    [SerializeField] private RawImage avatarRawImage;
//    [SerializeField] private CButton btnReset;
//    private CharacterWrap characterWrap;
//    private CharacterData saveCharacterData;
//    private VehicleEditData vehicleEditData;
//    private AvatarCommonData avatarCommonData;
//    private Vector3 rDef;
//    private Vector3 pDef;
//    private Vector3 sDef;


//    public override void Awake()
//    {
//        base.Awake();
//        btnReset.onClick.AddListener(OnReset);
//    }

//    private void OnReset()
//    {
//        avatarCommonData.ResetDef(vehicleEditData.GetVehicleInfo());
//        adjustView.ResetAdjust();
//    }


//    public override void Show()
//    {
//        base.Show();
//        ShowCharacter();
//        var adjustList = AdjustTypeAdjustItems(avatarCommonData);
//        adjustView.SetAdjustItems(adjustList);
//        adjustView.ResetAdjust();
//        avatarRawImage.rectTransform.sizeDelta = new Vector2(1084.0f / 1124 * Screen.currentResolution.height, Screen.currentResolution.height);
//    }

//    protected override void SyncEditData()
//    {
//        base.SyncEditData();
//        vehicleEditData = editData as VehicleEditData;
//        if (vehicleEditData != null)
//        {
//            if (string.IsNullOrEmpty(vehicleEditData.GetInfo().metaDataUrl))
//            {
//                vehicleEditData.GetInfo().metaDataUrl =
//                    Es.DataTables.GetClothesTemplate(vehicleEditData.GetInfo().templateId).MetaDataUrl;
//            }

//            var skinInfo = vehicleEditData.GetSkinInfo();
//            if (skinInfo != null && skinInfo.skinDetailInfo != null)
//            {
//                if (skinInfo.skinDetailInfo.size == default || skinInfo.skinDetailInfo.size == Vector3.one)
//                {
//                    skinInfo.skinDetailInfo.size = vehicleEditData.size;
//                }
//            }

//            avatarCommonData = AvatarCommonData.From(vehicleEditData.skinActionDraftInfo.vehicleInfo);

//            pDef = avatarCommonData.pDef;
//            rDef = avatarCommonData.rDef;
//            sDef = avatarCommonData.sDef;
//        }
//    }

//    protected override void OnNextBtnClick()
//    {
//        vehicleEditData.skinActionDraftInfo.vehicleInfo.skinDetailInfo.pDef = pDef;
//        vehicleEditData.skinActionDraftInfo.vehicleInfo.skinDetailInfo.rDef = rDef;
//        vehicleEditData.skinActionDraftInfo.vehicleInfo.skinDetailInfo.sDef = sDef;
//        base.OnNextBtnClick();
//    }


//    public void ShowCharacter()
//    {
//        if (characterWrap != null)
//        {
//            saveCharacterData.ChangeSkinData(vehicleEditData.GetVehicleInfo());
//            characterWrap.ChangeUGCPart(vehicleEditData.GetVehicleInfo());
//            return;
//        }

//        saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
//        saveCharacterData.ChangeSkinData(vehicleEditData.GetVehicleInfo());
//        if (saveCharacterData == null)
//            saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1).Clone();
//        if (saveCharacterData != null)
//        {
//            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
//            characterWrap.SetParent(characterRoot, true);
//            cameraController.RotateTarget = characterRoot;
//            cameraController.SetCameraZoom(ViewType.ZoomWholeBody);
//        }
//    }

//    public List<RoleDataAdjust> AdjustTypeAdjustItems(AvatarCommonData configData)
//    {
//        var classType = UniqueType.GetUgcAvatar((AvatarSubType)vehicleEditData.GetVehicleInfo().subType);
//        switch (configData.adjustType)
//        {
//            case (int)AdjustType.Skin:
//                return new List<RoleDataAdjust>() {
//                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.LeftRight, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.XRotation, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.YRotation, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.ZRotation, configData, classType)
//                    };
//            case (int)AdjustType.EyeBrow:
//                return new List<RoleDataAdjust>() {
//                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.Spacing, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
//                    };
//            case (int)AdjustType.Nose:
//                return new List<RoleDataAdjust>() {
//                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.Vertical, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.HorizontalStretch, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.VerticalStretch, configData, classType)
//                    };
//            case (int)AdjustType.Mouth:
//                return new List<RoleDataAdjust>() {
//                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.LeftRight, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
//                    };
//            case (int)AdjustType.Blush:
//                return new List<RoleDataAdjust>() {
//                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.Spacing, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType)
//                    };
//            case (int)AdjustType.FacePaint:
//                return new List<RoleDataAdjust>() {
//                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType)
//                    };
//            case (int)AdjustType.EarRing:
//                return new List<RoleDataAdjust>() {
//                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.Spacing, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
//                        GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
//                    };
//            default:
//                return null;
//        }
//    }


//    public RoleDataAdjust GetRoleDataAdjust(AdjustViewItemType type, Es.AvatarCommonData configData,
//        int classType)
//    {
//        var partData = saveCharacterData.GetPartData(classType);
//        if (partData == null)
//        {
//            partData = new CharacterPartData();
//            saveCharacterData.partDatas.Add(partData);
//        }

//        var avatarSubType = (AvatarSubType)configData.SubType;
//        AdjustAxis axis;
//        switch (type)
//        {
//            case AdjustViewItemType.Size:
//                axis = avatarSubType switch
//                {
//                    _ => AdjustAxis.None
//                };
//                return new()
//                {
//                    AdjustType = AdjustViewItemType.Size,
//                    Getter = () => partData.Sca ?? configData.sDef,
//                    Setter = (v) => partData.Sca = v,
//                    Axis = () => axis,
//                    Limit = () => configData.scaLimit,
//                    Default = () => configData.sDef,
//                    Apply = () => ChangeSize(classType, partData.Sca)
//                };
//            case AdjustViewItemType.UpDown:
//                axis = avatarSubType switch
//                {
//                    AvatarSubType.Hair => AdjustAxis.Y,
//                    AvatarSubType.Earring => AdjustAxis.Y,
//                    AvatarSubType.Hand => AdjustAxis.Z,
//                    AvatarSubType.FacePaint => AdjustAxis.None,
//                    _ => AdjustAxis.X
//                };
//                return new()
//                {
//                    AdjustType = AdjustViewItemType.UpDown,
//                    Getter = () => partData.Pos ?? configData.pDef,
//                    Setter = (v) => partData.Pos = v,
//                    Axis = () => axis,
//                    Limit = () => configData.vLimit,
//                    Default = () => configData.pDef,
//                    Apply = () => ChangeMove(classType, partData.Pos)
//                };
//            case AdjustViewItemType.LeftRight:
//                axis = avatarSubType switch
//                {
//                    AvatarSubType.Hair => AdjustAxis.X,
//                    AvatarSubType.Hand => AdjustAxis.X,
//                    _ => AdjustAxis.Z
//                };
//                return new()
//                {
//                    AdjustType = AdjustViewItemType.LeftRight,
//                    Getter = () => partData.Pos ?? configData.pDef,
//                    Setter = (v) => partData.Pos = v,
//                    Axis = () => axis,
//                    Limit = () => configData.hLimit,
//                    Default = () => configData.pDef,
//                    Apply = () => ChangeMove(classType, partData.Pos)
//                };
//            case AdjustViewItemType.FrontBack:
//                axis = avatarSubType switch
//                {
//                    AvatarSubType.Hair => AdjustAxis.Z,
//                    AvatarSubType.Earring => AdjustAxis.Z,
//                    _ => AdjustAxis.Y
//                };
//                return new()
//                {
//                    AdjustType = AdjustViewItemType.FrontBack,
//                    Getter = () => partData.Pos ?? configData.pDef,
//                    Setter = (v) => partData.Pos = v,
//                    Axis = () => axis,
//                    Limit = () => configData.fLimit,
//                    Default = () => configData.pDef,
//                    Apply = () => ChangeMove(classType, partData.Pos)
//                };
//            case AdjustViewItemType.XRotation:
//                axis = avatarSubType switch
//                {
//                    _ => AdjustAxis.X
//                };
//                return new()
//                {
//                    AdjustType = AdjustViewItemType.XRotation,
//                    Getter = () => partData.Rot ?? configData.rDef,
//                    Setter = (v) => partData.Rot = CheckAngle(v, axis),
//                    Axis = () => axis,
//                    Limit = () => configData.xrotLimit,
//                    Default = () => configData.rDef,
//                    Apply = () => ChangeRotate(classType, partData.Rot)
//                };
//            case AdjustViewItemType.YRotation:
//                axis = avatarSubType switch
//                {
//                    _ => AdjustAxis.Z
//                };
//                return new()
//                {
//                    AdjustType = AdjustViewItemType.YRotation,
//                    Getter = () => partData.Rot ?? configData.rDef,
//                    Setter = (v) => partData.Rot = CheckAngle(v, axis),
//                    Axis = () => axis,
//                    Limit = () => avatarSubType is AvatarSubType.Hand or AvatarSubType.Visor
//                        ? configData.yrotLimit
//                        : configData.zrotLimit,
//                    Default = () => configData.rDef,
//                    Apply = () => ChangeRotate(classType, partData.Rot)
//                };
//            case AdjustViewItemType.ZRotation:
//                axis = avatarSubType switch
//                {
//                    _ => AdjustAxis.Y
//                };
//                return new()
//                {
//                    AdjustType = AdjustViewItemType.ZRotation,
//                    Getter = () => partData.Rot ?? configData.rDef,
//                    Setter = (v) => partData.Rot = CheckAngle(v, axis),
//                    Axis = () => axis,
//                    Limit = () => avatarSubType is AvatarSubType.Hand or AvatarSubType.Visor
//                        ? configData.zrotLimit
//                        : configData.yrotLimit,
//                    Default = () => configData.rDef,
//                    Apply = () => ChangeRotate(classType, partData.Rot)
//                };
//            case AdjustViewItemType.Spacing:
//                axis = avatarSubType switch
//                {
//                    AvatarSubType.Earring => AdjustAxis.X,
//                    _ => AdjustAxis.Z
//                };
//                return new()
//                {
//                    AdjustType = AdjustViewItemType.Spacing,
//                    Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
//                    Setter = (v) => partData.Pos = v,
//                    Axis = () => axis,
//                    Limit = () => configData.hLimit,
//                    Default = () => configData.pDef,
//                    Apply = () => ChangeMove(classType, partData.Pos)
//                };
//            case AdjustViewItemType.Vertical:
//                axis = avatarSubType switch
//                {
//                    _ => AdjustAxis.None
//                };
//                return new()
//                {
//                    AdjustType = AdjustViewItemType.Vertical,
//                    Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
//                    Setter = (v) => partData.Pos = v,
//                    Axis = () => axis,
//                    Limit = () => configData.vLimit,
//                    Default = () => configData.pDef,
//                    Apply = () => ChangeMove(classType, partData.Pos)
//                };
//            case AdjustViewItemType.Rotation:
//                axis = avatarSubType switch
//                {
//                    _ => AdjustAxis.None
//                };
//                return new()
//                {
//                    AdjustType = AdjustViewItemType.Rotation,
//                    Getter = () => partData.Rot != null ? partData.Rot : configData.rDef,
//                    Setter = (v) => partData.Rot = v,
//                    Axis = () => axis,
//                    Limit = () => configData.rotateLimit,
//                    Default = () => configData.rDef,
//                    Apply = () => ChangeRotate(classType, partData.Rot)
//                };
//        }

//        return new RoleDataAdjust();
//    }

//    void ChangeSize(int resType, Vec3 size)
//    {
//        characterWrap.Scale(resType, size);
//        sDef = size;
//    }

//    void ChangeMove(int resType, Vec3 pos)
//    {
//        characterWrap.Move(resType, pos);
//        pDef = pos;
//    }

//    void ChangeRotate(int resType, Vec3 rot)
//    {
//        characterWrap.Rotate(resType, rot);
//        rDef = rot;
//    }

//    Vector3 CheckAngle(Vector3 rot, AdjustAxis axis)
//    {
//        switch (axis)
//        {
//            case AdjustAxis.X:
//                if (Mathf.Abs(Mathf.RoundToInt(rot.x) % 45) <= 5)
//                {
//                    rot.x = (Mathf.RoundToInt(rot.x) / 45) * 45;
//                }
//                break;
//            case AdjustAxis.Y:
//                if (Mathf.Abs(Mathf.RoundToInt(rot.y) % 45) <= 5)
//                {
//                    rot.y = (Mathf.RoundToInt(rot.y) / 45) * 45;
//                }
//                break;
//            case AdjustAxis.Z:
//                if (Mathf.Abs(Mathf.RoundToInt(rot.z) % 45) <= 5)
//                {
//                    rot.z = (Mathf.RoundToInt(rot.z) / 45) * 45;
//                }
//                break;
//            default:
//                break;
//        }

//        return rot;
//    }
//}
