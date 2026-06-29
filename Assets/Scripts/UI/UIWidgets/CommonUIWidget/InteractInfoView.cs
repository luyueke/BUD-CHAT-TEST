using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using GameData.Base;
using UI.BaseWidgets;
using UnityEngine;

public class InteractInfoView : CommonUIWidget
{
    public CText Txt_VistNum;
    public CText Txt_LikeNum;
    public CText Txt_FavNum;

    private BaseInteractInfo _curInfo;

    /// <summary>
    /// 所需参数args[0] InteractInfo
    /// </summary>
    public override void SetData(params object[] args)
    {
        base.SetData(args);
        _curInfo = (BaseInteractInfo)args[0];

        var vistNumStr = GameUtils.ToBudCommonNumString(_curInfo.consumeAmount);
        Txt_VistNum.text = vistNumStr;
        RefreshLikeNum(_curInfo.likeAmount);
        RefreshFavNum(_curInfo.collectAmount);
    }

    public void RefreshLikeNum(int num)
    {
        var likeNumStr = GameUtils.ToBudCommonNumString(num);
        Txt_LikeNum.text = likeNumStr;
    }
    
    public void RefreshFavNum(int num)
    {
        var favNumStr = GameUtils.ToBudCommonNumString(num);
        Txt_FavNum.text = favNumStr;
    }
    
}
