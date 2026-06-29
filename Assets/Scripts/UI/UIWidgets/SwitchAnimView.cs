using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;

public class SwitchAnimView : SpecialAnimContainer {
    
    private GoodsData curGoodsData;
    private string pgcId;
    
    public void SetGoodsData(GoodsData goodsData)
    {
        curGoodsData = goodsData;
        pgcId = goodsData.Id;
    }

    public void SetPgcId(string id)
    {
        pgcId = id;
    }


    public GoodsData GetGoodsData()
    {
        return curGoodsData;
    }

    public string GetPgcId()
    {
        return pgcId;
    }

    public override void OnDisable()
    {
        //覆盖父类，不执行disable时的回调
    }
}
