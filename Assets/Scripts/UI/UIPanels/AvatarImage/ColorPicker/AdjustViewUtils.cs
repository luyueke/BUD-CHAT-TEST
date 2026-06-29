using System;
using System.Collections.Generic;
using EasySpreadsheet;
using Es;
using Game.Avatar;
using GameData.PgcData;
using Newtonsoft.Json;
using UnityEngine;

public class AdjustViewUtils
{
    public enum LeftRightType
    {
        LeftHand = 1,
        RightHand = 2,
        BothHand = 3
    }


    public static Vector3 GetCharacterWrapValueBySlider(CharacterWrap characterWrap, AvatarSubType resType,
        EAdjustItemType adjustItemType)
    {
        var partData = characterWrap.GetPartData(UniqueType.GetAvatar(resType));
        var configData = Es.DataTables.GetAvatarCommonData(partData.Id);
        switch (adjustItemType)
        {
            case EAdjustItemType.Size:
                return partData.Sca ?? configData.sDef;
            case EAdjustItemType.Spacing:
            case EAdjustItemType.Up_down:
            case EAdjustItemType.Front_back:
            case EAdjustItemType.Left_right:
                return partData.Pos ?? configData.pDef;
            case EAdjustItemType.Rotation:
            case EAdjustItemType.X_Rotation:
            case EAdjustItemType.Y_Rotation:
            case EAdjustItemType.Z_Rotation:
                return partData.Rot ?? configData.rDef;
        }

        return new Vector3();
    }

    public enum ChangeType
    {
        Move = 0,
        Rotate = 1,
        Scale = 2,
    }

    public enum AdjustType
    {
        //Doc:https://pointone.feishu.cn/docx/D2FUdZQv4oeWP8xtZp0cGv3wn3g?theme=FOLLOW_SYSTEM&contentTheme=DARK
        NotSupport = 0,
        Skin = 1, //皮肤向
        EyeBrow = 2, //眼镜眉毛
        Nose = 3, //鼻子
        Mouth = 4, //嘴巴
        Blush = 5, //腮红
        FacePaint = 6, //脸部彩绘
        EarRing = 7, //耳环
    }

    public static void AdjustType2AdjustItems(int adjustType, List<AdjustItemContext> adjustItemContexts)
    {
        adjustItemContexts.Clear();
        switch (adjustType)
        {
            case (int)AdjustType.Skin:
            {
                EAdjustItemType[] eAdjustItemTypes =
                {
                    EAdjustItemType.Size, EAdjustItemType.Up_down, EAdjustItemType.Left_right,
                    EAdjustItemType.Front_back,
                    EAdjustItemType.X_Rotation, EAdjustItemType.Y_Rotation, EAdjustItemType.Z_Rotation
                };
                foreach (var adjustItemType in eAdjustItemTypes)
                {
                    AdjustItemContextFactory.Create(adjustItemContexts, adjustItemType);
                }

                break;
            }
            case (int)AdjustType.EyeBrow:
            {
                EAdjustItemType[] eAdjustItemTypes =
                {
                    EAdjustItemType.Size, EAdjustItemType.Spacing, EAdjustItemType.Up_down,
                    EAdjustItemType.Front_back,
                    EAdjustItemType.Rotation
                };
                foreach (var adjustItemType in eAdjustItemTypes)
                {
                    AdjustItemContextFactory.Create(adjustItemContexts, adjustItemType);
                }

                break;
            }
            case (int)AdjustType.Nose:
            {
                EAdjustItemType[] eAdjustItemTypes =
                {
                    EAdjustItemType.Size, EAdjustItemType.Vertical, EAdjustItemType.HorizontalStretch,
                    EAdjustItemType.VerticalStretch
                };
                foreach (var adjustItemType in eAdjustItemTypes)
                {
                    AdjustItemContextFactory.Create(adjustItemContexts, adjustItemType);
                }

                break;
            }
            case (int)AdjustType.Mouth:
            {
                EAdjustItemType[] eAdjustItemTypes =
                {
                    EAdjustItemType.Size, EAdjustItemType.Up_down, EAdjustItemType.Left_right,
                    EAdjustItemType.Front_back,
                    EAdjustItemType.Rotation
                };
                foreach (var adjustItemType in eAdjustItemTypes)
                {
                    AdjustItemContextFactory.Create(adjustItemContexts, adjustItemType);
                }

                break;
            }
            case (int)AdjustType.Blush:
            {
                EAdjustItemType[] eAdjustItemTypes =
                {
                    EAdjustItemType.Size, EAdjustItemType.Spacing, EAdjustItemType.Up_down,
                    EAdjustItemType.Front_back
                };
                foreach (var adjustItemType in eAdjustItemTypes)
                {
                    AdjustItemContextFactory.Create(adjustItemContexts, adjustItemType);
                }

                break;
            }
            case (int)AdjustType.FacePaint:
            {
                EAdjustItemType[] eAdjustItemTypes =
                {
                    EAdjustItemType.Size, EAdjustItemType.Up_down
                };
                foreach (var adjustItemType in eAdjustItemTypes)
                {
                    AdjustItemContextFactory.Create(adjustItemContexts, adjustItemType);
                }

                break;
            }
            case (int)AdjustType.EarRing:
            {
                EAdjustItemType[] eAdjustItemTypes =
                {
                    EAdjustItemType.Size, EAdjustItemType.Spacing, EAdjustItemType.Up_down,
                    EAdjustItemType.Front_back,
                    EAdjustItemType.Rotation
                };
                foreach (var adjustItemType in eAdjustItemTypes)
                {
                    AdjustItemContextFactory.Create(adjustItemContexts, adjustItemType);
                }

                break;
            }
            default:
                break;
        }
    }


    public static Vector3 GetValueBySlider(Vector3 vector3, List<Vector3> limit, float progress,
        VecAxis vAxis = VecAxis.None)
    {
        Vector3 max = limit[1];
        Vector3 min = limit[0];
        var cur = new Vector3(vector3.x, vector3.y, vector3.z);
        switch (vAxis)
        {
            case VecAxis.X:
                cur.x = Mathf.Lerp(min.x, max.x, progress);
                break;
            case VecAxis.XY:
                cur.x = Mathf.Lerp(min.x, max.x, progress);
                cur.y = Mathf.Lerp(min.y, max.y, progress);
                break;
            case VecAxis.YZ:
                cur.y = Mathf.Lerp(min.y, max.y, progress);
                cur.z = Mathf.Lerp(min.z, max.z, progress);
                break;
            case VecAxis.XYZ:
                cur.y = Mathf.Lerp(min.y, max.y, progress);
                cur.x = Mathf.Lerp(min.x, max.x, progress);
                cur.z = Mathf.Lerp(min.z, max.z, progress);
                break;
            case VecAxis.XZ:
                cur.x = Mathf.Lerp(min.x, max.x, progress);
                cur.z = Mathf.Lerp(min.z, max.z, progress);
                break;
            case VecAxis.None:
                cur = Vector3.Lerp(min, max, progress);
                break;
            case VecAxis.Z:
                cur.z = Mathf.Lerp(min.z, max.z, progress);
                break;
            case VecAxis.Y:
                cur.y = Mathf.Lerp(min.y, max.y, progress);
                break;
        }

        cur.x = (float)Math.Round(cur.x, 4);
        cur.y = (float)Math.Round(cur.y, 4);
        cur.z = (float)Math.Round(cur.z, 4);
        return cur;
    }

    public static float GetSliderValue(List<Vector3> limit, Vec3 curVec, VecAxis vAxis = VecAxis.None)
    {
        Vector3 max = limit[1];
        Vector3 min = limit[0];
        Vector3 cur = curVec;
        switch (vAxis)
        {
            case VecAxis.X:
                max.y = 0;
                max.z = 0;
                min.y = 0;
                min.z = 0;
                cur.y = 0;
                cur.z = 0;
                break;
            case VecAxis.XY:
                max.z = 0;
                min.z = 0;
                cur.z = 0;
                break;
            case VecAxis.YZ:
                max.x = 0;
                min.x = 0;
                cur.x = 0;
                break;
            case VecAxis.Z:
                max.y = 0;
                max.x = 0;
                min.y = 0;
                min.x = 0;
                cur.y = 0;
                cur.x = 0;
                break;
            case VecAxis.Y:
                max.x = 0;
                max.z = 0;
                min.x = 0;
                min.z = 0;
                cur.x = 0;
                cur.z = 0;
                break;
        }

        float length = (max - min).magnitude;
        float curLength = (cur - min).magnitude;
        float progress = 0;
        if (length == 0)
        {
            LoggerUtils.LogError("分母不能为0" + max + min + cur);
            progress = 1;
        }
        else
        {
            progress = (float)Math.Round(curLength / length, 2);
            progress = Mathf.Clamp01(progress);
        }

        return progress;
    }

    public enum VecAxis
    {
        None,
        X,
        XY,
        YZ,
        XYZ,
        XZ,
        Z,
        Y
    }

    public static VecAxis GetVecAxis(EAdjustItemType itemType, AvatarSubType avatarSubType, int handBipType = 1)
    {
        if (avatarSubType == AvatarSubType.Eyes)
        {
            switch (itemType)
            {
                case EAdjustItemType.Spacing:
                    return VecAxis.Z;
                case EAdjustItemType.Front_back:
                    return VecAxis.Y;
                case EAdjustItemType.Up_down:
                    return VecAxis.X;
                default:
                    break;
            }

            return VecAxis.None;
        }
        else if (avatarSubType == AvatarSubType.Brow)
        {
            switch (itemType)
            {
                case EAdjustItemType.Up_down:
                    return VecAxis.X;
                case EAdjustItemType.Front_back:
                    return VecAxis.Y;
                case EAdjustItemType.Spacing:
                    return VecAxis.Z;
                default:
                    break;
            }

            return VecAxis.None;
        }
        else if (avatarSubType == AvatarSubType.Mouth)
        {
            switch (itemType)
            {
                case EAdjustItemType.Up_down:
                    return VecAxis.X;
                case EAdjustItemType.Front_back:
                    return VecAxis.Y;
                case EAdjustItemType.Left_right:
                    return VecAxis.Z;
                default:
                    break;
            }

            return VecAxis.None;
        }
        else if (avatarSubType == AvatarSubType.FacePaint)
        {
            return VecAxis.None;
        }
        else if (avatarSubType == AvatarSubType.Hats)
        {
            switch (itemType)
            {
                case EAdjustItemType.Up_down:
                    return VecAxis.X;
                case EAdjustItemType.Front_back:
                    return VecAxis.Y;
                case EAdjustItemType.Left_right:
                    return VecAxis.Z;
                case EAdjustItemType.X_Rotation:
                    return VecAxis.X;
                case EAdjustItemType.Y_Rotation:
                    return VecAxis.Y;
                case EAdjustItemType.Z_Rotation:
                    return VecAxis.Z;
                default:
                    break;
            }

            return VecAxis.None;
        }
        else if (avatarSubType == AvatarSubType.Glasses || avatarSubType == AvatarSubType.Visor)
        {
            switch (itemType)
            {
                case EAdjustItemType.Up_down:
                    return VecAxis.X;
                case EAdjustItemType.Front_back:
                    return VecAxis.Y;
                case EAdjustItemType.Left_right:
                    return VecAxis.Z;
                case EAdjustItemType.X_Rotation:
                    return VecAxis.X;
                case EAdjustItemType.Y_Rotation:
                    return VecAxis.Y;
                case EAdjustItemType.Z_Rotation:
                    return VecAxis.Z;
                default:
                    break;
            }

            return VecAxis.None;
        }
        else if (avatarSubType == AvatarSubType.Backpack)
        {
            switch (itemType)
            {
                case EAdjustItemType.Up_down:
                    return VecAxis.X;
                case EAdjustItemType.Front_back:
                    return VecAxis.Y;
                case EAdjustItemType.Left_right:
                    return VecAxis.Z;
                case EAdjustItemType.X_Rotation:
                    return VecAxis.X;
                case EAdjustItemType.Y_Rotation:
                    return VecAxis.Y;
                case EAdjustItemType.Z_Rotation:
                    return VecAxis.Z;
                default:
                    break;
            }

            return VecAxis.None;
        }
        else if (avatarSubType == AvatarSubType.Hand || avatarSubType == AvatarSubType.Glove)
        {
            switch (itemType)
            {
                case EAdjustItemType.Up_down:
                    return VecAxis.Z;
                case EAdjustItemType.Front_back:
                    return VecAxis.Y;
                case EAdjustItemType.Left_right:
                    return VecAxis.X;
                case EAdjustItemType.X_Rotation:
                    return VecAxis.X;
                case EAdjustItemType.Y_Rotation:
                    return VecAxis.Z;
                case EAdjustItemType.Z_Rotation:
                    return VecAxis.Y;
            }

            return VecAxis.None;
        }
        else if (avatarSubType == AvatarSubType.Effect)
        {
            switch (itemType)
            {
                case EAdjustItemType.Up_down:
                    return VecAxis.X;
                case EAdjustItemType.Front_back:
                    return VecAxis.Y;
                case EAdjustItemType.Left_right:
                    return VecAxis.Z;
                case EAdjustItemType.X_Rotation:
                    return VecAxis.X;
                case EAdjustItemType.Y_Rotation:
                    return VecAxis.Y;
                case EAdjustItemType.Z_Rotation:
                    return VecAxis.Z;
                default:
                    break;
            }

            return VecAxis.None;
        }
        else if (avatarSubType == AvatarSubType.Earring)
        {
            switch (itemType)
            {
                case EAdjustItemType.Up_down:
                    return VecAxis.Y;
                case EAdjustItemType.Front_back:
                    return VecAxis.XYZ;
                case EAdjustItemType.Spacing:
                    return VecAxis.XZ;
                case EAdjustItemType.X_Rotation:
                    return VecAxis.X;
                case EAdjustItemType.Y_Rotation:
                    return VecAxis.Y;
                case EAdjustItemType.Z_Rotation:
                    return VecAxis.Z;
                default:
                    return VecAxis.None;
                    break;
            }
        }
        else if (avatarSubType == AvatarSubType.Blush)
        {
            switch (itemType)
            {
                case EAdjustItemType.Spacing:
                    return VecAxis.Z;
                case EAdjustItemType.Up_down:
                    return VecAxis.X;
                case EAdjustItemType.Front_back:
                    return VecAxis.Y;
                default:
                    break;
            }

            return VecAxis.None;
        }
        else if (avatarSubType == AvatarSubType.Nose)
        {
            switch (itemType)
            {
                case EAdjustItemType.Vertical:
                    return VecAxis.XY;
                case EAdjustItemType.HorizontalStretch:
                    return VecAxis.XYZ;
                case EAdjustItemType.VerticalStretch:
                    return VecAxis.XYZ;
                default:
                    break;
            }

            return VecAxis.None;
        }
        else
        {
            //默认情况
            switch (itemType)
            {
                case EAdjustItemType.Up_down:
                    return VecAxis.X;
                case EAdjustItemType.Front_back:
                    return VecAxis.Y;
                case EAdjustItemType.Spacing:
                case EAdjustItemType.Left_right:
                    return VecAxis.Z;
                case EAdjustItemType.X_Rotation:
                    return VecAxis.X;
                case EAdjustItemType.Y_Rotation:
                    return VecAxis.Y;
                case EAdjustItemType.Z_Rotation:
                    return VecAxis.Z;
                default:
                    return VecAxis.None;
                    break;
            }
        }
    }

    /// <summary>
    /// 是否支持更改颜色
    /// </summary>
    /// <returns></returns>
    public static bool IsSupportChangeColor(AvatarMenuType avatarMenuType)
    {
        switch (avatarMenuType)
        {
            case AvatarMenuType.Hair:
            case AvatarMenuType.Eyebrow:
            case AvatarMenuType.Blush:
                return true;
            default:
                return false;
        }
    }

    public static string GetCurrentColor(CharacterWrap characterWrap, AvatarMenuType menuType)
    {
        var charterData = characterWrap.ChaData;
        if (charterData == null)
        {
            LoggerUtils.LogError("not found CharacterData, please check");
            return null;
        }

        var placeHolders = AvatarConfigTool.ConvertPlaceholderDatas(charterData);
        var partType = AvatarConfigTool.PartType(menuType);
        RolePlaceholderData placeHolderData = placeHolders.Find(x => x.SubType == partType);
        if (placeHolderData == null)
        {
            return null;
        }

        return placeHolderData.color;
    }

    public static bool GetIsSetColor(AvatarMenuType menuType, string itemId)
    {
        switch (menuType)
        {
            case AvatarMenuType.Headwear:
            case AvatarMenuType.Earrings:
            case AvatarMenuType.Glasses:
            case AvatarMenuType.Visor:
            {
                //部分帽子可以调整颜色
                var hatData = Es.DataTables.GetAvatarCommonData(itemId);
                //是否可调整颜色
                bool setColor = hatData.setColor;
                return setColor;
            }
        }

        return false;
    }

    public static string GetDefaultColor(AvatarMenuType menuType, string itemId)
    {
        switch (menuType)
        {
            case AvatarMenuType.Bag:
            case AvatarMenuType.Blush:
            case AvatarMenuType.Headwear:
            case AvatarMenuType.Glasses:
            case AvatarMenuType.Hair:
            case AvatarMenuType.Eyebrow:
            {
                var commonData = Es.DataTables.GetAvatarCommonData(itemId);
                string defaultColor = commonData.defaultColor;
                return defaultColor;
            }
        }

        return null;
    }
}