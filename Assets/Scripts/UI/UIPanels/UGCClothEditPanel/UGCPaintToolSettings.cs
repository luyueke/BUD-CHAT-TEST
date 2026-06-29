using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UGCEditor
{
    public enum UGC_CLOTH_TYPE
    {
        CLOTH = 1,
        HAT = 2,
    }
    public enum EditorToolType
    {
        Painting,//画笔
        OilDrum,//油桶
        Scissors,//剪刀
        Text,
        Photo,
    }

    public enum BrushSize
    {
        Small = 1,
        Middle = 2,
        Big = 3
    }

    public enum UGCFillType
    {
        Fill = 0,
        FillAll = 1
    }

    public enum UGCDrawMode
    {
        Normal,//普通模式
        Mirror,//镜像模式
        Flip,//反转
        Copy,//复制
    }

    public enum UGCDrawSize
    {
        One_One = 1,
        Two_Two = 2,
        Three_Three = 3
    }



    public class UGCPaintToolSettings
    {
        private static UGCPaintToolSettings cur;
        public static UGCPaintToolSettings Current => cur ??= new UGCPaintToolSettings();
        
        public UGCDrawSize PaintingSize { get; set; } = UGCDrawSize.One_One;
        public Color UGCColor { get; set; }
        public UGCDrawMode PaintingMode { get; set; } = UGCDrawMode.Normal;

        public EditorToolType ToolType {set; get; } = EditorToolType.Painting;

        public UGCFillType oilDrumFileType { set; get; } = UGCFillType.Fill;

        public static void Dispose()
        {
            cur = null;
        }
        //     public Color pColor { private set; get; }
        //     // public CommonColorToggleItem PgcCurItem; //选中的PGC颜色
        //     // public CommonColorToggleItem UgcCurItem; //选中的UGC颜色
        //     public UGCDrawMode curMode { private set; get; }
        //     public BrushSize pBrushSize { private set; get; } = BrushSize.Small;
        //     public FillType pFillType { private set; get; } = FillType.Fill;
        //
        //     public void SetPaintType(PaintType pType)
        //     {
        //         pType = pType;
        //     }
        //
        //     public void SetCurMode(UGCDrawMode mode)
        //     {
        //         curMode = mode;
        //     }
        //
        //     public void SetColor(Color col)
        //     {
        //         pColor = col;
        //     }
        //
        //     public static void Release()
        //     {
        //         cur = null;
        //     }
        //
        //     public void SetBrushSize(BrushSize brushSize)
        //     {
        //         pBrushSize = brushSize;
        //     }
        //
        //     public void SetFillType(FillType fillType)
        //     {
        //         pFillType = fillType;
        //     }
    }

}