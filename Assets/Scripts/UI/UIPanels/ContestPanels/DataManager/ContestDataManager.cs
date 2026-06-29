using System;
using System.Collections.Generic;
using System.Linq;
using GameData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ContestDataManager : GlobalInstance<ContestDataManager>
{
    private LobbyInfoRsp _lobbyInfoRes;
    
    private Dictionary<string, List<string>> luckyDrawCache = new Dictionary<string, List<string>>();

    /// <summary>
    /// 获取当前活动ID
    /// </summary>
    /// <returns>当前活动ID，如果没有活动返回空字符串</returns>
    public string GetCurrentContestId()
    {
        if (_lobbyInfoRes?.contestList == null || _lobbyInfoRes.contestList.Count == 0)
        {
            return string.Empty;
        }

        // 优先返回进行中的活动
        var inProgressContest = _lobbyInfoRes.contestList.Find(x => x.status == (int)ContestStatus.InProgress);
        if (inProgressContest != null)
        {
            return inProgressContest.contestId;
        }

        // 如果没有进行中的活动，返回第一个活动的ID
        return _lobbyInfoRes.contestList[0].contestId;
    }

    public List<string> GetLuckyDrawCache(string contestId)
    {
        if (luckyDrawCache.ContainsKey(contestId))
        {
            return luckyDrawCache[contestId];
        }
        return null;
    }
    
    public LobbyInfoRsp CurrentLobbyInfo
    {
        get { return _lobbyInfoRes; }
    }

    public bool GetEnablePublishByGem()
    {
        // return _lobbyInfoRes.enablePublishByGem;
        return false;
    }

    /// <summary>
    /// 获取可参与的活动
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public List<ContestInfo> canJoinContests(BUDContestType type)
    {
        var contests = _lobbyInfoRes?.contestList;
        if (contests == null || contests.Count == 0)
        {
            return new List<ContestInfo>();
        }
        return contests.FindAll(x => type == x.CurrentContestType && x.CurrentContestType != BUDContestType.Unknown && x.status == (int)ContestStatus.InProgress);
    }

    /// <summary>
    /// 是否有活跃活动
    /// </summary>
    public bool HasActiveContest(List<BUDContestType>types = null)
    {
        var contests = _lobbyInfoRes?.contestList;
        if (contests == null || contests.Count == 0)
        {
            return false;
        }

        if (types != null && types.Count > 0)
        {
            contests = contests.FindAll(x => types.Contains(x.CurrentContestType) && x.CurrentContestType != BUDContestType.Unknown);
        }
            
        foreach (var kv in contests)
        {
            if (kv.status == (int)ContestStatus.InProgress || kv.status == (int)ContestStatus.Completed)
            {
                return true;
            }
        }

        return false;
    }

    #region Tools

    public ContestInfo GetContestInfo(string contestId)
    {
        if (string.IsNullOrEmpty(contestId))
        {
            return null;
        }

        return _lobbyInfoRes?.contestList?.Find(e => e.contestId == contestId);
    }

    public void JoinSkinContest(string templateId, string creationId, bool isPet = false,int ugcStyle = 0)
    {
        if (SkinContest != null && SkinContest.IsCanJoinSkin(templateId) && SkinContest.IsPetSkinContest == isPet)
        {
            JoinContest(creationId, new List<string>() { SkinContest.contestId }, b =>
            {
                if (b)
                {
                    var panel = UIManager.Inst.OpenPanel<JoinInContestPanel>(PanelId.JoinInContestPanel, new List<ContestInfo>() {SkinContest}, creationId);
                    panel.ShowJoinSuccess(SkinContest);
                }
                
                SkinContest = null;
            });
            return;
        }
        //isAnimeItemOnly为0，代表不区分二次元
        var contests = _lobbyInfoRes?.contestList.FindAll(c => c.status == 1 && c.IsCanJoinSkin(templateId) && c.IsPetSkinContest == isPet && c.isAnimeItemOnly <= ugcStyle);
        if (contests != null && contests.Count > 0)
        {
            UIManager.Inst.OpenPanel(PanelId.JoinInContestPanel, contests, creationId);
        }
    }

    public void JoinBundleContest(string creationId, bool isPet)
    {
        if (string.IsNullOrEmpty(creationId))
        {
            return;
        }
        
        if (SkinContest != null && SkinContest.IsPetSkinContest == isPet)
        {
            JoinContest(creationId, new List<string>() { SkinContest.contestId }, b =>
            {
                if (b)
                {
                    var panel = UIManager.Inst.OpenPanel<JoinInContestPanel>(PanelId.JoinInContestPanel, new List<ContestInfo>() {SkinContest}, creationId);
                    panel.ShowJoinSuccess(SkinContest);
                }
                
                SkinContest = null;
            });
            return;
        }

        List<ContestInfo> contests;
        if (isPet)
        { 
            contests = _lobbyInfoRes?.contestList.FindAll(c => c.status == 1 && c.CurrentContestType == BUDContestType.PetBundle);
        }
        else
        {
            contests = _lobbyInfoRes?.contestList.FindAll(c => c.status == 1 && c.CurrentContestType == BUDContestType.Bundle);
        }
        
        if (contests != null && contests.Count > 0)
        {
            UIManager.Inst.OpenPanel(PanelId.JoinInContestPanel, contests, creationId);
        }
    }

    /// <summary>
    /// 衣服大赛进入编辑器后记录
    /// </summary>
    public ContestInfo SkinContest;

    #endregion


    #region Request

    public void GetLuckyDrawInfo(List<ContestInfo> contestList)
    {
        if (contestList == null || contestList.Count == 0)
        {
            return;
        }

        contestList.ForEach(info =>
        {
            var contestId = info.contestId;
            if (info.status == 2 && info.hasLuckyDraw == 1 && !string.IsNullOrEmpty(contestId)) {
                GetLuckDraw(info.contestId, (b, data) =>
                {
                    if (b)
                    {
                        luckyDrawCache[contestId] = data.userNameList;
                    }
                });
            }
        });
    }

    /// <summary>
    /// 获取当前配置
    /// </summary>
    /// <param name="resultAction"></param>
    public void RefreshLobbyInfo(Action<bool, LobbyInfoRsp> resultAction)
    {
        LobbyInfoManager.Inst.GetLobbyInfo((response) =>
        {
            _lobbyInfoRes = response;
            GetLuckyDrawInfo(response.contestList);
            resultAction?.Invoke(true, response);
        }, (error) =>
        {
            resultAction?.Invoke(false, null);
        });
    }

    /// <summary>
    /// 获取当前活动详情
    /// </summary>
    /// <param name="contestId"></param>
    /// <param name="resultAction"></param>
    public void RefreshContestInfo(string contestId, Action<bool, ContestInfo> resultAction)
    {
        if (string.IsNullOrEmpty(contestId))
        {
            resultAction?.Invoke(false, null);
            return;
        }

        var jb = new JObject
        {
            ["contestId"] = contestId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ContestInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            (content) =>
            {
                ContextReqData response = JsonConvert.DeserializeObject<ContextReqData>(content);

                resultAction?.Invoke(true, response.contestInfo);
                syncContestData(response.contestInfo);
            },
            (error) => { resultAction?.Invoke(false, null); });
    }


    public void GetLuckDraw(string contestId, Action<bool, ContestLuckyDrawRespData?> resultAction)
    {
        if (string.IsNullOrEmpty(contestId))
        {
            resultAction?.Invoke(false, null);
            return;
        }
        JObject req = new JObject() { ["contestId"] = contestId, };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ContestLucky, 
            HttpMethod.GET,
            JsonConvert.SerializeObject(req),
            (content) =>
            {
                ContestLuckyDrawRespData luckyDrawData = JsonConvert.DeserializeObject<ContestLuckyDrawRespData>(content);
                resultAction?.Invoke(true, luckyDrawData);
            }, (error) =>
            {
                resultAction?.Invoke(false, null);
            });
    }
    
    
    
    public void JoinContest(string creationId, List<string> contestIds, Action<bool> resultAction)
    {
        var ids = new JArray();
        foreach (var VARIABLE in contestIds)
        {
            ids.Add(VARIABLE);
        }

        var jb = new JObject()
        {
            ["creationId"] = creationId,
            ["contestIds"] = ids
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ContestJoin,
            HttpMethod.POST, JsonConvert.SerializeObject(jb),
            (msg) =>
            {
                resultAction?.Invoke(true);
                MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
            },
            (error) => { resultAction?.Invoke(false); });
    }

    private void syncContestData(ContestInfo info)
    {
        if (info == null)
        {
            return;
        }

        var fixedList = new List<ContestInfo>();
        
        var lists = _lobbyInfoRes?.contestList;
        foreach (var element in lists)
        {
            if (element.contestId != info.contestId)
            {
                fixedList.Add(element);
            }
        }

        fixedList.Add(info);

        _lobbyInfoRes.contestList = fixedList;
    }

    #endregion
}