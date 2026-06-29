using System;
using System.Collections.Generic;
using EventTracking;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;

public class LobbyInfoManager : GlobalInstance<LobbyInfoManager>
{
    public int TotalRecharge = -1;

    public int FirstRecharge = -1;
    public LobbyInfoRsp LobbyInfo
    {
        get
        {
            return _lobbyInfo;
        }
    }
    private LobbyInfoRsp _lobbyInfo = new LobbyInfoRsp();

    public void GetLobbyInfo(Action<LobbyInfoRsp> onSuccess = null, Action<string> onFail = null) 
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.lobbyInfo, HttpMethod.GET, "",
            (content) =>
            {
                if (string.IsNullOrEmpty(content))
                {
                    onFail?.Invoke("GetLobbyInfoSuccess But RspContent IsNull");
                    return;
                }
                
                LobbyInfoRsp rsp = JsonConvert.DeserializeObject<LobbyInfoRsp>(content);
                if (rsp == null)
                {
                    onFail?.Invoke("GetLobbyInfoSuccess But Rsp IsNull");
                    return;
                }

                _lobbyInfo = rsp;
                onSuccess?.Invoke(_lobbyInfo);
                SetEventData(_lobbyInfo);
            },
            (error) =>
            {
                onFail?.Invoke(error);
            });
    }
    public void SetEventData(LobbyInfoRsp _lobbyInfo)
    {
        //LoggerUtils.Log("设置充值属性");
        //Dictionary<string, object> superProperties = AnalyticsManager.Inst.GetSuperProperties();// new Dictionary<string, object>();
        //superProperties["charge_amount"] = _lobbyInfo.totalRecharge;//累计充值金额
        //superProperties["first_charge_amount"] = _lobbyInfo.firstRecharge;//首充金额
        //TotalRecharge = _lobbyInfo.totalRecharge;
        //FirstRecharge = _lobbyInfo.firstRecharge;
        //AnalyticsManager.Inst.SetSuperProperties(superProperties);//设置公共事件属性
        //var _userInfo = AccountDataManager.Inst.UserInfo;
        //if(_userInfo.isNewUser != 1)
        //{
        //    return;
        //}
        //if (TotalRecharge == -1 || FirstRecharge == -1
        //    || _lobbyInfo.totalRecharge != TotalRecharge
        //    || _lobbyInfo.firstRecharge != FirstRecharge) {

        //}
        
    }

    public ContestInfo GetContestInfo(BUDContestType type) 
    {
        if (LobbyInfo == null)
        {
            return null;
        }
        if (LobbyInfo.contestList == null)
        {
            return null;
        }
        foreach (var item in LobbyInfo.contestList)
        {
            if (item.CurrentContestType == type && (item.status == (int)ContestStatus.InProgress || item.status == (int)ContestStatus.Submission))
            {
                return item;
            }
        }
        return null;
    }

    public ContestInfo GetContestInfo(string type)
    {
        if (LobbyInfo == null)
        {
            return null;
        }
        foreach (var item in LobbyInfo.contestList)
        {
            if (item.contestId == type)
            {
                return item;
            }
        }
        return null;
    }
}

public enum GameTheme {
    Normal = 0, // 普通赛季主题
    Halloween = 1, // 万圣节限时 10月9日11点 - 11月4日11点 
}

public class LobbyInfoRsp
{
    public List<ContestInfo> contestList;
    public int enablePrivateOrder;
    public int enableNPCHalfPrice;
    public int hasNotPurchasedSkin;
    /// <summary>
    /// app 主题
    /// </summary>
    public int appTheme;
    public int totalRecharge;//累充金额
    public int firstRecharge;//首充金额
    public int showSeasonLuckyPack;//是否展示赛季预购的幸运礼包弹窗
    public List<HallRemoteBtn> carouselList;
    public GameTheme gameTheme
    {
        get
        {
            bool IsDefined = Enum.IsDefined(typeof(GameTheme), appTheme);
            if (IsDefined)
            {
                return (GameTheme)appTheme; 
            }

            return GameTheme.Normal;
        }
    }
}

public class HallRemoteBtn {

    public int id;
    public string iconUrl;
    public string name;
    public int skipType;
    public string skipData;
    public int sort;
}
