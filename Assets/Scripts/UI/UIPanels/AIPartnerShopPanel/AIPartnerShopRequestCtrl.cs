
using System;
using System.Collections.Generic;
using Game.Store;
using GameData;
using GameData.Gashapon;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Game.Store.AssetsDataManager;

public class AIPartnerShopRequestCtrl : GlobalInstance<AIPartnerShopRequestCtrl>
{
    //请求获取伙伴section
    public void RequestGetPartnerSection(UgcType ugcType, Action<List<UgcSectionData>> onComplete, Action<string> onError)
    {
        var jb = new JObject
        {
            ["ugcType"] = (int)ugcType
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.sectionList, HttpMethod.GET, reqParam, (message) =>
        {
            SectionListRsp sectionListRsp = JsonConvert.DeserializeObject<SectionListRsp>(message);
            onComplete?.Invoke(sectionListRsp.list);
        }, (message) =>
        {
            onError?.Invoke(message);
            LoggerUtils.Log($"[Avatar] section fail message: {message}");
        });
    }

    // 请求获取指定分类下的伙伴商品列表（自动翻页，一次性返回所有数据）
    public void RequestGetPartnerSectionInfo(
        string sectionId,
        Action<List<CabinCharacterUgcInfo>> onComplete,
        Action<string> onError = null)
    {
        RequestGetPartnerSectionInfoAll(sectionId, "", new List<CabinCharacterUgcInfo>(), onComplete, onError);
    }

    private void RequestGetPartnerSectionInfoAll(
        string sectionId,
        string cookie,
        List<CabinCharacterUgcInfo> accumulated,
        Action<List<CabinCharacterUgcInfo>> onComplete,
        Action<string> onError)
    {
        var jb = new JObject
        {
            ["sectionId"]    = sectionId,
            ["cookie"]       = cookie,
            ["currencyType"] = (int)CurrencyType.None
        };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.sectionInfoV2, HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            message =>
            {
                var rsp = JsonConvert.DeserializeObject<Game.CommunityGame.SectionInfoRsp>(message);
                if (rsp?.list != null)
                {
                    foreach (var item in rsp.list)
                    {
                        if (string.IsNullOrEmpty(item.ugcData)) continue;
                        var info = JsonConvert.DeserializeObject<CabinCharacterUgcInfo>(item.ugcData);
                        if (info != null) accumulated.Add(info);
                    }
                }
                onComplete?.Invoke(accumulated);
                if (rsp?.IsEnd != 1 && !string.IsNullOrEmpty(rsp?.cookie))
                    RequestGetPartnerSectionInfoAll(sectionId, rsp.cookie, accumulated, onComplete, onError);
            },
            err => onError?.Invoke(err));
    }

    public void RequestGetBoxSectionInfo(
        string sectionId,
        Action<List<CharacterBoxInfo>> onComplete,
        Action<string> onError = null)
    {
        RequestGetBoxSectionInfoAll(sectionId, "", new List<CharacterBoxInfo>(), onComplete, onError);
    }

    private void RequestGetBoxSectionInfoAll(
        string sectionId, string cookie, List<CharacterBoxInfo> accumulated,
        Action<List<CharacterBoxInfo>> onComplete, Action<string> onError)
    {
        var jb = new JObject
        {
            ["sectionId"]    = sectionId,
            ["cookie"]       = cookie,
            ["currencyType"] = (int)CurrencyType.None
        };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.sectionInfoV2, HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            message =>
            {
                var rsp = JsonConvert.DeserializeObject<Game.CommunityGame.SectionInfoRsp>(message);
                if (rsp?.list != null)
                    foreach (var item in rsp.list)
                    {
                        if (string.IsNullOrEmpty(item.ugcData)) continue;
                        var info = JsonConvert.DeserializeObject<CharacterBoxInfo>(item.ugcData);
                        if (info == null) continue;
                        info.consumed = item.interactInfo?.consumed ?? 0;
                        accumulated.Add(info);
                    }
                onComplete?.Invoke(accumulated);
                if (rsp?.IsEnd != 1 && !string.IsNullOrEmpty(rsp?.cookie))
                    RequestGetBoxSectionInfoAll(sectionId, rsp.cookie, accumulated, onComplete, onError);
            },
            err => onError?.Invoke(err));
    }

    public void RequestSearchPartner(string keyword, Action<bool, List<CabinCharacterUgcInfo>> callback)
    {
        var req = new JObject
        {
            ["searchWord"] = keyword ?? "",
            ["searchScope"] = "0"//0: 全局搜索，1: 我的发布
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SearchCharacter, HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                var rsp = JsonConvert.DeserializeObject<PartnerSearchRsp>(rspStr);
                var result = new List<CabinCharacterUgcInfo>();
                if (rsp?.list != null)
                {
                    foreach (var item in rsp.list)
                    {
                        if (string.IsNullOrEmpty(item.ugcData)) continue;
                        var info = JsonConvert.DeserializeObject<CabinCharacterUgcInfo>(item.ugcData);
                        if (info != null) result.Add(info);
                    }
                }
                callback?.Invoke(true, result);
            },
            errRspStr =>
            {
                LoggerUtils.LogError("搜索AI伙伴失败: " + errRspStr);
                callback?.Invoke(false, null);
            });
    }

    private class PartnerSearchItem { public string ugcData; }
    private class PartnerSearchRsp  { public List<PartnerSearchItem> list; }

    public void RequestBuyPartner(List<string> packIds, Action onSuccess, Action<string> onError = null)
    {
        var req = new Req();
        req.ugcIds = packIds;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyUgcPay, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            _ => onSuccess?.Invoke(),
            err => onError?.Invoke(err));
    }

    public void RequestBuyPartner(string ugcId, Action onSuccess, Action<string> onError = null)
    {
        JObject req = new JObject()
        {
            ["ugcId"] = ugcId,
            ["useVoucher"] = false
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyUgcPay, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            _ => onSuccess?.Invoke(),
            err => onError?.Invoke(err));
    }

    public void RequestGetUserInfo(string uid, Action<AccountUserInfo> onComplete, Action<string> onError = null)
    {
        var req = new JObject { ["targetUid"] = uid };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.publicProfile, HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            rsp =>
            {
                var data = JsonConvert.DeserializeObject<UserInfoRsp>(rsp);
                onComplete?.Invoke(data?.userInfo);
            },
            err => onError?.Invoke(err));
    }

    private class UserInfoRsp { public AccountUserInfo userInfo; }
}
