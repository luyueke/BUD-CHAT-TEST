using System;
using System.Collections.Generic;
using GameData.Base;
using GameData.UGCData;

public class ProfileMapUtils
{
    public static ResInfo GetResInfo(DraftListItem draftListItem)
    {
        ResInfo resInfo = new ResInfo();
        resInfo.interactInfo = draftListItem.interactInfo;
        resInfo.creator = draftListItem.creator;
        resInfo.ugcInfo = GetBaseInfo(draftListItem);
        return resInfo;
    }

    public static List<ResInfo> GetResInfos(List<DraftListItem> draftListItems)
    {
        List<ResInfo> resInfos = new List<ResInfo>();
        for (int i = 0; i < draftListItems.Count; i++)
        {
            ResInfo resInfo = new ResInfo();
            resInfo.interactInfo = draftListItems[i].interactInfo;
            resInfo.creator = draftListItems[i].creator;
            resInfo.ugcInfo = GetBaseInfo(draftListItems[i]);
            resInfos.Add(resInfo);
        }

        return resInfos;
    }

    private static UgcBaseInfo GetBaseInfo(DraftListItem draftListItem)
    {
        if (draftListItem.mapInfo != null)
        {
            return draftListItem.mapInfo;
        }
        else if (draftListItem.propInfo != null)
        {
            return draftListItem.propInfo;
        }
        else if (draftListItem.materialInfo != null)
        {
            return draftListItem.materialInfo;
        }
        else if (draftListItem.skinInfo != null)
        {
            return draftListItem.skinInfo;
        }

        return null;
    }

    /// <summary>
    /// 转换个人主页交互类型
    /// </summary>
    /// <param name="category"></param>
    /// <param name="subCategory"></param>
    /// <returns></returns>
    public static int GetProfileInteractType(Category category, SubCategory subCategory)
    {
        //只处理个人主页现有的类型
        switch (category)
        {
            case Category.Map:
                if (subCategory == SubCategory.Liked)
                {
                    return 2;
                }

                break;
            case Category.Avatar:
                if (subCategory == SubCategory.Liked)
                {
                    return 4;
                }
                else if (subCategory == SubCategory.Owned)
                {
                    return 3;
                }
                break;
            case Category.Prop:
                if (subCategory == SubCategory.Liked)
                {
                    return 7;
                } else if (subCategory == SubCategory.Owned)
                {
                    return 6;
                }
                break;
            case Category.Material:
                if (subCategory == SubCategory.Liked)
                {
                    return 10;
                } else if (subCategory == SubCategory.Owned)
                {
                    return 9;
                }
                break;
        }

        return 0;
    }


    public static string GetSubCategoryName(SubCategory subCategory)
    {
        switch (subCategory)
        {
            case SubCategory.Published:
                return "发布";
            case SubCategory.Liked:
                return "点赞";
            case SubCategory.Collected:
                return "收藏";
            case SubCategory.Owned:
                return "拥有";
            case SubCategory.Played:
                return "游玩";
        }

        return "";
    }
    
#if PACKAGE_TYPE_US
    public static string FormatNumber(double num)
    {
        if (num < 1000)
        {
            return num.ToString();
        }
        else if (num >= 1000 && num <= 999999)
        {
            var format = new System.Globalization.NumberFormatInfo();
            format.NumberDecimalDigits = 1;
            format.NumberDecimalSeparator = ".";
            format.NumberGroupSeparator = ",";
            double result = Math.Floor((double)num / 1000.0 * 10) / 10;
            return result.ToString("0.0", format) + "K";
        }
        else
        {
            var format = new System.Globalization.NumberFormatInfo();
            format.NumberDecimalDigits = 1;
            format.NumberDecimalSeparator = ".";
            format.NumberGroupSeparator = ",";
            double result = Math.Floor((double)num / 1000000.0 * 10) / 10;
            return result.ToString("0.0", format) + "M";
        }
    }
#else
    public static string FormatNumber(double number)
    {
        if (number >= 10000)
        {
            return (number / 10000).ToString("0.0") + " 万";
        }
        else
        {
            return number.ToString();
        }
    }
#endif
}