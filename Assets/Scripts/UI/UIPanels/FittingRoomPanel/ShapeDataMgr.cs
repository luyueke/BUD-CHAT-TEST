using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeDataMgr : GameInstance<ShapeDataMgr>
{
    public readonly List<ShapeData> shapeDataList = new List<ShapeData> 
    {
        new ShapeData(1001,"shape_0",ShapeSaleType.Free),
        new ShapeData(1002,"shape_1",ShapeSaleType.Free),
        new ShapeData(1003,"shape_2",ShapeSaleType.Free),
        new ShapeData(1004,"shape_3",ShapeSaleType.Vip),
        new ShapeData(1005,"shape_4",ShapeSaleType.Vip),
        new ShapeData(1006,"shape_5",ShapeSaleType.Vip),
        new ShapeData(1007,"shape_6",ShapeSaleType.Vip),
    };

    public readonly string shapeKey = "FirstOpenShapePanel" + AccountDataManager.Inst.UserInfo.uid;

    public readonly string firstSelectShapeKey = "firstSelectShapeKey" + AccountDataManager.Inst.UserInfo.uid;

    public ShapeData GetShapeData(int id)
    {
        for(int i = 0; i < shapeDataList.Count; i++)
        {
            if (shapeDataList[i].Id == id)
            {
                return shapeDataList[i];
            }
        }
        return null;
    }

}

public class ShapeData
{
    public int Id;
    public string SpriteName;
    public ShapeSaleType SaleType;


    public ShapeData(int id, string spriteName, ShapeSaleType type)
    {
        Id = id;
        SpriteName = spriteName;
        SaleType = type;
    }
}

public enum ShapeSaleType
{
    Free,
    Vip
}