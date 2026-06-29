using Network.Http;
using Network;
using System;
using Newtonsoft.Json;
using GameData.Base.Common;

/// <summary>
/// UGC通用请求
/// </summary>
public class UGCCommonReq : GlobalInstance<UGCCommonReq>
{
    
    public enum LikeType
    {
        Like = 1,
        UnLike = 2
    }
    
    /// <summary>
    /// 购买UGC商品
    /// </summary>
    /// <param name="ugcId"></param>
    /// <param name="callBack"></param>
    public void UGCBuyReq(string ugcId, Action<bool> callBack)
    {
        var req = new SetTypeReqeust
        {
            id = ugcId,
            setType = 1
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCBuy, HttpMethod.POST, JsonConvert.SerializeObject(req), response =>
        {
            callBack?.Invoke(true);
        }, fail =>
        {
            LoggerUtils.LogError($"购买UGC商品失败 [{ugcId}]:" + fail);
            callBack?.Invoke(false);
        });
    }

    /// <summary>
    /// 点赞UGC商品
    /// </summary>
    /// <param name="ugcId"></param>
    /// <param name="like">1 点赞，2 取消</param>
    /// <param name="callBack"></param>
    public void UGCLikeReq(string ugcId, LikeType like, Action<bool> callBack)
    {
        var req = new SetTypeReqeust
        {
            id = ugcId,
            setType = (int)like
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCLike, HttpMethod.POST, JsonConvert.SerializeObject(req), response =>
        {
            callBack?.Invoke(true);
        }, fail =>
        {
            LoggerUtils.LogError($"点赞UGC商品失败 [{ugcId}]:" + fail);
            callBack?.Invoke(false);
        });
    }

}
