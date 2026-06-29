using System.Collections.Generic;

public static class AdjustItemContextFactory
{
    public static void Create(List<AdjustItemContext> items, EAdjustItemType itemType)
    {
        AdjustItemContext item= Create(itemType);
        if (items!=null)
        {
            items.Add(item);
        }
    }
    public static AdjustItemContext Create(EAdjustItemType itemType)
    {
        AdjustItemContext context = new AdjustItemContext();
        context.mItemType = itemType;
        context.mCurValue = 0;
        context.mTitle = "尺寸";
        switch (itemType)
        {
            case EAdjustItemType.Size:
                context.mTitle = "尺寸";
                break;
            case EAdjustItemType.Up_down:
                context.mTitle = "上下";
                break;
            case EAdjustItemType.Left_right:
                context.mTitle = "左右";
                break;
            case EAdjustItemType.Front_back:
                context.mTitle = "前后";
                break;
            case EAdjustItemType.Spacing:
                context.mTitle = "间距";
                break;
            case EAdjustItemType.Vertical:
                context.mTitle = "垂直";
                break;
            case EAdjustItemType.HorizontalStretch:
                context.mTitle = "水平拉伸";
                break;
            case EAdjustItemType.VerticalStretch:
                context.mTitle = "垂直拉伸";
                break;
            case EAdjustItemType.Rotation:
                context.mTitle = "旋转";
                break;
            //X轴旋转/Y轴旋转/Z轴旋转
            case EAdjustItemType.X_Rotation:
                context.mTitle = "X轴旋转";
                break;
            case EAdjustItemType.Y_Rotation:
                context.mTitle = "Y轴旋转";
                break;
            case EAdjustItemType.Z_Rotation:
                context.mTitle = "Z轴旋转";
                break;
            case EAdjustItemType.Hue:
                context.mTitle = "色调";
                break;
            case EAdjustItemType.Chroma:
                context.mTitle = "饱和度";
                break;
            case EAdjustItemType.Bright:
                context.mTitle = "亮度";
                break;
            default:
                break;
        }
        return context;
    }
}
