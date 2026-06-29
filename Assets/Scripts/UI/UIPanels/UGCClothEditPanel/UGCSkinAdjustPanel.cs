using UI;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using UI.UIPanels.FittingRoom;

/// <summary>
/// Author:
/// Desc:
/// Date:24-07-23 19:41:31
/// </summary>
public class UGCSkinAdjustPanel : BasePanel<UGCSkinAdjustPanel>
{
    [SerializeField] protected CButton backBtn;

    [SerializeField] protected CButton nextBtn;

    [SerializeField] private Transform characterRoot;

    [SerializeField] private AdjustView adjustView;

    [SerializeField] private AvatarCameraController cameraController;

    public Action SyncAdjustAct;
    private BaseAvatarWrapper characterWrap;
    private CharacterData saveCharacterData;
    private SkinEditData skinEditData;
    private Transform charcaterParent;
    private Vector3 curScale;
    private Vector3 curPosition;
    private Vector3 curRotate;

    public override void OnCreate()
    {
        base.OnCreate();
        backBtn.onClick.AddListener(OnBackBtnClick);
        nextBtn.onClick.AddListener(OnNextBtnClick);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        characterWrap.Avatar.transform.SetParent(charcaterParent);
        characterWrap.Avatar.SetActive(false);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        characterWrap = args[0] as BaseAvatarWrapper;
        skinEditData = args[1] as SkinEditData;
        SyncEditData();
        curPosition = skinEditData.GetSkinInfo().skinDetailInfo.pDef;
        curScale = skinEditData.GetSkinInfo().skinDetailInfo.sDef;
        curRotate = skinEditData.GetSkinInfo().skinDetailInfo.rDef;
        charcaterParent = characterWrap.Avatar.transform.parent;
        characterWrap.Avatar.transform.SetParent(characterRoot);
        characterWrap.Avatar.transform.localPosition = Vector3.zero;
        characterWrap.Avatar.transform.localScale = Vector3.one;
        characterWrap.Avatar.SetActive(true);
        // ShowCharacter();
        var adjustList = AdjustTypeAdjustItems();
        adjustView.SetAdjustItems(adjustList);
        adjustView.ResetAdjust();
    }

    protected void SyncEditData()
    {
        var skinInfo = skinEditData.GetInfo().ToSkinInfo();
        if (skinEditData != null &&skinInfo!=null&& string.IsNullOrEmpty(skinInfo.metaDataUrl))
        {
            skinInfo.metaDataUrl =skinInfo.skinType == (int)SkinType.Avatar? 
                Es.DataTables.GetClothesTemplate(skinInfo.templateId).MetaDataUrl
                : Es.DataTables.GetPetClothesTemplate(skinInfo.templateId).MetaDataUrl;
               
        }
    }

    protected void OnBackBtnClick()
    {
        CloseSelf();
    }

    protected void OnNextBtnClick()
    {
        SaveAdjustData();
        CloseSelf();
    }
    
    

    public List<RoleDataAdjust> AdjustTypeAdjustItems()
    {
        int classType = 0;
        if (skinEditData.GetSkinInfo().skinType == (int)SkinType.Avatar)
        {
            classType = UniqueType.GetUgcAvatar((AvatarSubType) skinEditData.GetSkinInfo().subType);
        }
        else
        {
            classType = UniqueType.GetUGCPetAvatar((AvatarSubType) skinEditData.GetSkinInfo().subType);
        }
        var configData = AvatarCommonData.SkinFrom(skinEditData.GetSkinInfo());
        switch (configData.adjustType)
        {
            case (int) AdjustType.EyeBrow:
                return new List<RoleDataAdjust>()
                {
                    GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                    GetRoleDataAdjust(AdjustViewItemType.Spacing, configData, classType),
                    GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                    GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
                    GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
                };
            case (int) AdjustType.Mouth:
                return new List<RoleDataAdjust>()
                {
                    GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                    GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                    GetRoleDataAdjust(AdjustViewItemType.LeftRight, configData, classType),
                    GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
                    GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
                };
            case (int) AdjustType.FacePaint:
                return new List<RoleDataAdjust>()
                {
                    GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                    GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType)
                };
            default:
                return null;
        }
    }


    public RoleDataAdjust GetRoleDataAdjust(AdjustViewItemType type, Es.AvatarCommonData configData,
        int classType)
    {
        // var partData = saveCharacterData.GetPartData(classType);
        // if (partData == null)
        // {
        //     partData = new CharacterPartData();
        //     saveCharacterData.partDatas.Add(partData);
        // }

        var avatarSubType = (AvatarSubType) configData.SubType;
        AdjustAxis axis;
        switch (type)
        {
            case AdjustViewItemType.Size:
                axis = avatarSubType switch
                {
                    _ => AdjustAxis.None
                };
                return new()
                {
                    AdjustType = AdjustViewItemType.Size,
                    Getter = () => curScale,
                    Setter = (v) => curScale = v,
                    Axis = () => axis,
                    Limit = () => configData.scaLimit,
                    Default = () => configData.sDef,
                    Apply = () => ChangeSize(classType, curScale)
                };
            case AdjustViewItemType.UpDown:
                axis = avatarSubType switch
                {
                    AvatarSubType.Hair => AdjustAxis.Y,
                    AvatarSubType.Earring => AdjustAxis.Y,
                    AvatarSubType.Hand => AdjustAxis.Z,
                    AvatarSubType.FacePaint => AdjustAxis.None,
                    _ => AdjustAxis.X
                };
                return new()
                {
                    AdjustType = AdjustViewItemType.UpDown,
                    Getter = () => curPosition,
                    Setter = (v) => curPosition = v,
                    Axis = () => axis,
                    Limit = () => configData.vLimit,
                    Default = () => configData.pDef,
                    Apply = () => ChangeMove(classType, curPosition)
                };
            case AdjustViewItemType.LeftRight:
                axis = avatarSubType switch
                {
                    AvatarSubType.Hair => AdjustAxis.X,
                    AvatarSubType.Hand => AdjustAxis.X,
                    _ => AdjustAxis.Z
                };
                return new()
                {
                    AdjustType = AdjustViewItemType.LeftRight,
                    Getter = () => curPosition,
                    Setter = (v) => curPosition = v,
                    Axis = () => axis,
                    Limit = () => configData.hLimit,
                    Default = () => configData.pDef,
                    Apply = () => ChangeMove(classType, curPosition)
                };
            case AdjustViewItemType.FrontBack:
                axis = avatarSubType switch
                {
                    AvatarSubType.Hair => AdjustAxis.Z,
                    AvatarSubType.Earring => AdjustAxis.Z,
                    _ => AdjustAxis.Y
                };
                return new()
                {
                    AdjustType = AdjustViewItemType.FrontBack,
                    Getter = () => curPosition,
                    Setter = (v) => curPosition = v,
                    Axis = () => axis,
                    Limit = () => configData.fLimit,
                    Default = () => configData.pDef,
                    Apply = () => ChangeMove(classType, curPosition)
                };
            case AdjustViewItemType.XRotation:
                axis = avatarSubType switch
                {
                    _ => AdjustAxis.X
                };
                return new()
                {
                    AdjustType = AdjustViewItemType.XRotation,
                    Getter = () => curRotate,
                    Setter = (v) => curRotate = v,
                    Axis = () => axis,
                    Limit = () => configData.xrotLimit,
                    Default = () => configData.rDef,
                    Apply = () => ChangeRotate(classType, curRotate)
                };
            case AdjustViewItemType.YRotation:
                axis = avatarSubType switch
                {
                    _ => AdjustAxis.Z
                };
                return new()
                {
                    AdjustType = AdjustViewItemType.YRotation,
                    Getter = () => curRotate,
                    Setter = (v) => curRotate = v,
                    Axis = () => axis,
                    Limit = () => avatarSubType is AvatarSubType.Hand or AvatarSubType.Visor
                        ? configData.yrotLimit
                        : configData.zrotLimit,
                    Default = () => configData.rDef,
                    Apply = () => ChangeRotate(classType, curRotate)
                };
            case AdjustViewItemType.ZRotation:
                axis = avatarSubType switch
                {
                    _ => AdjustAxis.Y
                };
                return new()
                {
                    AdjustType = AdjustViewItemType.ZRotation,
                    Getter = () => curRotate,
                    Setter = (v) => curRotate = v,
                    Axis = () => axis,
                    Limit = () => avatarSubType is AvatarSubType.Hand or AvatarSubType.Visor
                        ? configData.zrotLimit
                        : configData.yrotLimit,
                    Default = () => configData.rDef,
                    Apply = () => ChangeRotate(classType, curRotate)
                };
            case AdjustViewItemType.Spacing:
                axis = avatarSubType switch
                {
                    AvatarSubType.Earring => AdjustAxis.X,
                    _ => AdjustAxis.Z
                };
                return new()
                {
                    AdjustType = AdjustViewItemType.Spacing,
                    Getter = () => curPosition,
                    Setter = (v) => curPosition = v,
                    Axis = () => axis,
                    Limit = () => configData.hLimit,
                    Default = () => configData.pDef,
                    Apply = () => ChangeMove(classType, curPosition)
                };
            case AdjustViewItemType.Rotation:
                axis = avatarSubType switch
                {
                    _ => AdjustAxis.None
                };
                return new()
                {
                    AdjustType = AdjustViewItemType.Rotation,
                    Getter = () => curRotate,
                    Setter = (v) => curRotate = v,
                    Axis = () => axis,
                    Limit = () => configData.rotateLimit,
                    Default = () => configData.rDef,
                    Apply = () => ChangeRotate(classType, curRotate)
                };
        }

        return new RoleDataAdjust();
    }




    void ChangeSize(int resType, Vec3 size)
    {
        characterWrap.Scale(resType, size);
    }
    
    void ChangeMove(int resType, Vector3 pos)
    {
        characterWrap.Move(resType, pos);
    }


    
    void ChangeRotate(int resType, Vec3 rot)
    {
        characterWrap.Rotate(resType, rot);
    }

    private void SaveAdjustData()
    {
        var info = skinEditData.GetSkinInfo().skinDetailInfo;
        info.sDef = curScale;
        info.pDef = curPosition;
        info.rDef = curRotate;
        SyncAdjustAct?.Invoke();
    }
}