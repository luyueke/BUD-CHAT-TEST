using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using GameData.Base;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using JetBrains.Annotations;
using UGCAsset;
using UnityEngine;

/// <summary>
/// avatar 一级菜单
/// </summary>
public enum AvatarType
{
    /// <summary>
    /// 身体
    /// </summary>
    Body = 0,

    /// <summary>
    /// 脸绘
    /// </summary>
    Face = 1,
}

public enum AvatarMenuType
{
    PrefabImage = 0, // 设子形象
    Collect, // 收藏
    Hair, // 头发
    Skin, // 肤色
    Outfit, // 衣服
    Headwear, // 帽子
    Glasses, // 面部饰品 (眼镜)
    Visor, //面罩
    Earrings, // 耳环
    Shoe, // 鞋子
    Scarf, // 颈部饰品（围脖等）
    Bag, // 背部饰品 （背包、翅膀）
    Hand, // 手部饰品 （手持）
    Glove, //手套
    Effects, // 特效1
    Belt, // 腰带配饰

    Eye, // 眼睛
    Eyebrow, // 眉毛
    Nose, // 鼻子
    Mouth, // 嘴巴
    Blush, // 腮红
    FacePainting, // 面部彩绘
}

/// <summary>
/// avatar 二级菜单协议
/// </summary>
public interface AvatarCategoryItemProtocol
{
    /// <summary>
    /// 分类id, 用于区分当前分类。是用于后端取数据还是本地数据
    /// </summary>
    public string id { set; get; }

    /// <summary>
    /// 是否显示红点
    /// </summary>
    public bool isShowRedDot { set; get; }

    public string iconName(bool isSelect);
}

public class AvatarCategoryData : AvatarCategoryItemProtocol
{
    public string id { get; set; }
    public bool isShowRedDot { get; set; }

    public string iconName(bool isSelect)
    {
        return AvatarConfigTool.AvatarCategoryIconName(isSelect, (int)menuType);
    }

    /// <summary>
    /// 二级菜单类型
    /// </summary>
    private AvatarMenuType menuType = AvatarMenuType.PrefabImage;

    public AvatarMenuType CurrentMenuType
    {
        get { return menuType; }
    }

    public AvatarCategoryData(AvatarMenuType menu)
    {
        menuType = menu;
        id = System.Enum.GetName(typeof(AvatarMenuType), menu);
    }
}

class AvatarConfigTool
{
    /// <summary>
    /// 获取身体的所有值
    /// </summary>
    /// <param name="isNewuser"></param>
    /// <returns></returns>
    public static List<AvatarCategoryData> BodySubItems(bool isNewuser = false)
    {
        List<AvatarCategoryData> allItems = new List<AvatarCategoryData>();
        List<AvatarMenuType> items = new List<AvatarMenuType>
        {
            AvatarMenuType.PrefabImage,
            AvatarMenuType.Collect,
            AvatarMenuType.Hair,
            AvatarMenuType.Skin,
            AvatarMenuType.Outfit,
            AvatarMenuType.Headwear,
            AvatarMenuType.Glasses,
            AvatarMenuType.Visor,
            AvatarMenuType.Earrings,
            AvatarMenuType.Shoe,
            AvatarMenuType.Scarf,
            AvatarMenuType.Bag,
            AvatarMenuType.Hand,
            AvatarMenuType.Glove,
            AvatarMenuType.Effects,
            AvatarMenuType.Belt,
        };

        foreach (var item in items)
        {
            if (isNewuser)
            {
                if (item == AvatarMenuType.PrefabImage || item == AvatarMenuType.Collect)
                {
                    continue;
                }
            }

            var newItem = new AvatarCategoryData(item);
            allItems.Add(newItem);
        }

        return allItems;
    }

    public static List<AvatarCategoryData> FaceSubItems(bool isNewuser = false)
    {
        List<AvatarCategoryData> allItems = new List<AvatarCategoryData>();
        List<AvatarMenuType> items = new List<AvatarMenuType>
        {
            AvatarMenuType.Eye,
            AvatarMenuType.Eyebrow,
            AvatarMenuType.Nose,
            AvatarMenuType.Mouth,
            AvatarMenuType.Blush,
            AvatarMenuType.FacePainting
        };

        foreach (var item in items)
        {
            var newItem = new AvatarCategoryData(item);
            allItems.Add(newItem);
        }

        return allItems;
    }

    public static string AvatarCategoryIconName(bool isSelect, int subType)
    {
        List<string> keys = new List<string>
        {
            "my",
            "collect",
            "hair",
            "skin",
            "outfits",
            "headwear",
            "glasses",
            "visor",
            "ear",
            "shose",
            "scarf",
            "bag",
            "handholding",
            "glove",
            "effect",
            "waist",
            "eyes",
            "eyebrow",
            "nose",
            "mouse",
            "blush",
            "patterns"
        };

        if (subType < 0 || subType >= keys.Count)
        {
            LoggerUtils.LogError("Check keys count");
            return "";
        }

        var key = keys[subType];
        var selectKey = isSelect ? "normal" : "disable";
        return $"icn_lobby_{key}_{selectKey}";
    }

    public static bool IsShowColorBar(AvatarMenuType menuType)
    {
        switch (menuType)
        {
            case AvatarMenuType.Hair:
            case AvatarMenuType.Eyebrow:
            case AvatarMenuType.Blush:
                return true;
            default: return false;
        }
    }

    public static bool PartConvertEnable(AvatarMenuType menuType)
    {
        switch (menuType)
        {
            case AvatarMenuType.Collect:
            case AvatarMenuType.PrefabImage:
            case AvatarMenuType.Bag:
                return false;
            default: return true;
        }
    }

    // TODO: 补全数据
    public static AvatarSubType PartType(AvatarMenuType menuType)
    {
        switch (menuType)
        {
            case AvatarMenuType.Hair:
                return AvatarSubType.Hair;
            case AvatarMenuType.Outfit:
                return AvatarSubType.Clothes;
            case AvatarMenuType.Headwear:
                return AvatarSubType.Hats;
            case AvatarMenuType.Glasses:
                return AvatarSubType.Glasses;
            case AvatarMenuType.Visor:
                return AvatarSubType.Visor;
            case AvatarMenuType.Shoe:
                return AvatarSubType.Shoe;
            case AvatarMenuType.Scarf:
                return AvatarSubType.Scarf;
            case AvatarMenuType.Hand:
                return AvatarSubType.Hand;
            case AvatarMenuType.Glove:
                return AvatarSubType.Glove;
            case AvatarMenuType.Effects:
                return AvatarSubType.Effect;
            case AvatarMenuType.Bag:
                return AvatarSubType.Backpack;
            case AvatarMenuType.Eye:
                return AvatarSubType.Eyes;
            case AvatarMenuType.Eyebrow:
                return AvatarSubType.Brow;
            case AvatarMenuType.Belt:
                return AvatarSubType.Belt;
            case AvatarMenuType.Earrings:
                return AvatarSubType.Earring;
            case AvatarMenuType.Mouth:
                return AvatarSubType.Mouth;
            case AvatarMenuType.FacePainting:
                return AvatarSubType.FacePaint;
            case AvatarMenuType.Nose:
                return AvatarSubType.Nose;
            case AvatarMenuType.Blush:
                return AvatarSubType.Blush;
            default:
                Debug.LogError($"{menuType} not mapping");
                return AvatarSubType.Backpack;
        }
    }

    public static AvatarMenuType MenuType(AvatarSubType partType)
    {
        switch (partType)
        {
            case AvatarSubType.Scarf:
                return AvatarMenuType.Scarf;
            case AvatarSubType.Belt:
                return AvatarMenuType.Belt;
            case AvatarSubType.Brow:
                return AvatarMenuType.Eyebrow;
            case AvatarSubType.Clothes:
                return AvatarMenuType.Outfit;
            case AvatarSubType.Effect:
                return AvatarMenuType.Effects;
            case AvatarSubType.Eyes:
                return AvatarMenuType.Eye;
            case AvatarSubType.Glasses:
                return AvatarMenuType.Glasses;
            case AvatarSubType.Hair:
                return AvatarMenuType.Hair;
            case AvatarSubType.Hats:
                return AvatarMenuType.Headwear;
            case AvatarSubType.Glove:
                return AvatarMenuType.Hand;
            case AvatarSubType.Mouth:
                return AvatarMenuType.Mouth;
            case AvatarSubType.FacePaint:
                return AvatarMenuType.FacePainting;
            case AvatarSubType.Shoe:
                return AvatarMenuType.Shoe;
            case AvatarSubType.Backpack:
            case AvatarSubType.Cape:
            case AvatarSubType.Crossbody:
                return AvatarMenuType.Bag;
            case AvatarSubType.Earring:
                return AvatarMenuType.Earrings;
            case AvatarSubType.Nose:
                return AvatarMenuType.Nose;
            case AvatarSubType.Blush:
                return AvatarMenuType.Blush;
            default:
                Debug.LogError($"{partType} not mapping");
                return AvatarMenuType.Scarf;
        }
    }

    public static int[] PartTypes(AvatarMenuType menuType)
    {
        switch (menuType)
        {
            case AvatarMenuType.Hair:
                return new int[] { (int)AvatarSubType.Hair };
            case AvatarMenuType.Outfit:
                return new int[] { (int)AvatarSubType.Clothes};
            case AvatarMenuType.Headwear:
                return new int[] { (int)AvatarSubType.Hats };
            case AvatarMenuType.Glasses:
                return new int[] { (int)AvatarSubType.Glasses };
            case AvatarMenuType.Earrings:
                return new int[] { (int)AvatarSubType.Earring };
            case AvatarMenuType.Shoe:
                return new int[] { (int)AvatarSubType.Shoe };
            case AvatarMenuType.Scarf:
                return new int[] { (int)AvatarSubType.Scarf };
            case AvatarMenuType.Bag:
                return new int[] { (int)AvatarSubType.Backpack, (int)AvatarSubType.Crossbody, (int)AvatarSubType.Cape };
            case AvatarMenuType.Hand:
                return new int[] { (int)AvatarSubType.Hand };
            case AvatarMenuType.Effects:
                return new int[] { (int)AvatarSubType.Effect };
            case AvatarMenuType.Belt:
                return new int[] { (int)AvatarSubType.Belt };
            case AvatarMenuType.Eye:
                return new int[] { (int)AvatarSubType.Eyes };
            case AvatarMenuType.Eyebrow:
                return new int[] { (int)AvatarSubType.Brow };
            case AvatarMenuType.Nose:
                return new int[] { (int)AvatarSubType.Nose };
            case AvatarMenuType.Mouth:
                return new int[] { (int)AvatarSubType.Mouth };
            case AvatarMenuType.Blush:
                return new int[] { (int)AvatarSubType.Blush };
            case AvatarMenuType.FacePainting:
                return new int[] { (int)AvatarSubType.FacePaint };
            default:
                return null;
        }
    }

    public static ViewType CameraViewType(AvatarMenuType menuType)
    {
        switch (menuType)
        {
            case AvatarMenuType.PrefabImage:
            case AvatarMenuType.Collect:
            case AvatarMenuType.Outfit:
            case AvatarMenuType.Skin:
                return ViewType.ZoomWholeBody;
            case AvatarMenuType.Hair:
            case AvatarMenuType.Headwear:
            case AvatarMenuType.Glasses:
            case AvatarMenuType.Earrings:
            case AvatarMenuType.Scarf:
            case AvatarMenuType.Eye:
            case AvatarMenuType.Eyebrow:
            case AvatarMenuType.Nose:
            case AvatarMenuType.Mouth:
            case AvatarMenuType.Blush:
            case AvatarMenuType.FacePainting:
                return ViewType.ZoomUpperBody;
            case AvatarMenuType.Shoe:
                return ViewType.ZoomUnderBody;
        }

        return ViewType.ZoomWholeBody;
    }


    /// <summary>
    /// 从当前人物形象转换默认数据列表
    /// </summary>
    /// <param name="characterData"></param>
    /// <returns></returns>
    public static List<RolePlaceholderData> ConvertPlaceholderDatas(CharacterData characterData)
    {
        List<RolePlaceholderData> resultItems = new List<RolePlaceholderData>();
        foreach (var partData in characterData.partDatas)
        {
            var palceData = new RolePlaceholderData();
            palceData.id = partData.Id;
            palceData.color = partData.Cr;
            palceData.pDef = partData.Pos;
            palceData.rDef = partData.Rot;
            palceData.sDef = partData.Sca;
            palceData.vhSDef = partData.CSca;
            palceData.SubType = UniqueType.AvatarSubType(partData.Type);
            resultItems.Add(palceData);
        }
        return resultItems;
    }
}

/// <summary>
/// avatar 二级菜单协议
/// </summary>
public interface AvatarRoleItemProtocol
{
    /// <summary>
    /// 分类id, 用于区分当前分类。是用于后端取数据还是本地数据
    /// </summary>
    public string itemId { set; get; }

    /// <summary>
    /// UGC模板ID
    /// </summary>
    public string templateId { set; get; }

    /// <summary>
    /// UGC贴图url
    /// </summary>
    public string textureUrl { set; get; }

    /// <summary>
    /// UGC 元数据
    /// </summary>
    public string metadataUrl {
        set;
        get;
    }

    /// <summary>
    /// 是否显示红点
    /// </summary>
    public bool isShowRedDot { set; get; }

    /// <summary>
    /// 是否选中
    /// </summary>
    public bool isSelected { set; get; }

    /// <summary>
    /// 是否能显示Adjust
    /// </summary>
    public int adjustType { set; get; }

    /// <summary>
    /// 是否为已收藏
    /// </summary>
    public bool isCollected { set; get; }

    /// <summary>
    /// 图片路径
    /// </summary>
    public string iconPath { set; get; }

    /// <summary>
    /// 是否展示删除
    /// </summary>
    public bool isShowDelete { set; get; }

    /// <summary>
    /// 是否能否长按
    /// </summary>
    public bool isEnableLongPress { set; get; }

    public bool setColor { set; get; }

    /// <summary>
    /// 是否为调色按钮
    /// </summary>
    public bool isColorItem { set; get; }

    public bool isPGCItem { set; get; }

    public bool isPropSkin { set; get; }

}

public class AvatarRoleItemData : AvatarRoleItemProtocol
{
    public string itemId { get; set; }
    public string templateId { get; set; }
    public string textureUrl { get; set; }

    public string metadataUrl { get; set; }

    public bool isShowRedDot { get; set; }
    public bool isSelected { get; set; }
    public int adjustType { get; set; }
    public bool isCollected { get; set; }
    public string iconPath { get; set; }
    public bool isShowDelete { get; set; }
    public bool isEnableLongPress { get; set; }
    public bool setColor { get; set; }
    public bool isColorItem { set; get; }
    public bool isPGCItem { set; get; }

    public bool isPropSkin { set; get; }

    public AvatarRoleItemData(AvatarCommonData itemData)
    {
        itemId = itemData.PgcId;
        adjustType = itemData.adjustType;
        iconPath = itemData.Icon;
        isShowDelete = itemData.PgcId == "0";
        setColor = itemData.setColor;

        isShowRedDot = false;
        isSelected = false;
        isCollected = false;
        isEnableLongPress = true;
        isColorItem = false;
        isPGCItem = true;
        isPropSkin = false;
    }

    public AvatarRoleItemData(string colorHex)
    {
        itemId = colorHex;
        adjustType = 0;
        iconPath = colorHex;
        isShowDelete = false;
        setColor = true;

        isColorItem = true;

        isShowRedDot = false;
        isSelected = false;
        isCollected = false;
        isEnableLongPress = false;
    }

    public AvatarRoleItemData(UgcBaseInfo ugcItemData, BaseInteractInfo interactInfo)
    {
        itemId = ugcItemData.id;
        templateId = ugcItemData.templateId;
        adjustType = 0;
        iconPath = ugcItemData.cover;
        textureUrl = ugcItemData.textureUrl;
        metadataUrl = ugcItemData.metaDataUrl;

        isShowDelete = false;
        isShowRedDot = false;
        isSelected = false;
        isCollected = interactInfo.collected == 1;
        isEnableLongPress = true;
        isColorItem = false;
        isPGCItem = false;
        if (ugcItemData is SkinInfo skinInfo) {
            isPropSkin = skinInfo.isProp;
        } else {
            isPropSkin = false;
        }

    }
}


public class RolePlaceholderData
{
    public Vec3 pDef;
    public Vec3 rDef;
    public Vec3 sDef;
    public Vec3 vhSDef;
    [CanBeNull] public string color;
    public string id;
    public AvatarSubType SubType;
}

public class AvatarOcSetReq
{
    public AvatarOcInfo ocInfo;

    // 0： 设置； 1: 删除
    public int setType;
}

public class AvatarOcListRes
{
    public List<AvatarOcData> list;
    public string cookie;
    public int isEnd;
    public int limitedSlotCount;
    public string leftTime;
}

public class AvatarOcData
{
    public AvatarOcInfo ocInfo;
    public AvatarOcInteract interact;
    public bool isLimitedSlot;
}

public class AvatarOcInfo
{
    public string ocCover;
    public string ocId;
    public string avatarJson;
    public Int64 createTime;
}

public class AvatarOcInteract
{
    /// <summary>
    /// 1表示收藏
    /// </summary>
    public int isCollect;
}
