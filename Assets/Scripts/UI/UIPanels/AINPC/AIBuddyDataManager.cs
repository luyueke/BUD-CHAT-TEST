using System;
using System.Collections.Generic;
using GameData;
using GameData.Account;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public enum AIBuddyTitleLevel
{
    Lv0 = 0,
    Lv1 = 1,
    Lv2 = 2,
    Lv3 = 3,
    Lv4 = 4
}

//AIBuddy召唤特效
public enum AIBuddySummoningEffects
{
    None = 0,
    Portal = 1,//S7新增
}

public class LevelConfig
{
    public int Min;
    public int Max;
}

public class AIBuddyDataManager :  GlobalInstance<AIBuddyDataManager>
{
    public static string AIBuddyAtlas = "Assets/Loadable/UI/UIPanel/AINPC/AIBuddy.spriteatlas";
    private AIBuddyListRsp _aiBuddyListRsp;
    //亲密度数值要求表
    private Dictionary<int,LevelConfig> titleLevelDict = new Dictionary<int,LevelConfig>();
    private Dictionary<int,LevelConfig> intimacyLevelDict = new Dictionary<int, LevelConfig>();

    private Action<AIBuddyInfo> aiBuddyInfoUpdateListener;

    private bool isRequesting = false;
    private Action<AIBuddyListRsp> _pendingOnSuccess;
    private Action<string> _pendingOnFail;
    public AIBuddyDataManager()
    {
        titleLevelDict.Add((int)AIBuddyTitleLevel.Lv0,new LevelConfig{Min = 0,Max = 100});
        titleLevelDict.Add((int)AIBuddyTitleLevel.Lv1,new LevelConfig{Min = 100,Max = 1000});
        titleLevelDict.Add((int)AIBuddyTitleLevel.Lv2,new LevelConfig{Min = 1000,Max = 3000});
        titleLevelDict.Add((int)AIBuddyTitleLevel.Lv3,new LevelConfig{Min = 3000,Max = 6000});
        titleLevelDict.Add((int)AIBuddyTitleLevel.Lv4,new LevelConfig{Min = 6000,Max = 99999999});
        
        intimacyLevelDict.Add(0,new LevelConfig{Min = 0,Max = 100});
        intimacyLevelDict.Add(1,new LevelConfig{Min = 100,Max = 200});
        intimacyLevelDict.Add(2,new LevelConfig{Min = 200,Max = 500});
        intimacyLevelDict.Add(3,new LevelConfig{Min = 500,Max = 1000});
        intimacyLevelDict.Add(4,new LevelConfig{Min = 1000,Max = 1500});
        intimacyLevelDict.Add(5,new LevelConfig{Min = 1500,Max = 2000});
        intimacyLevelDict.Add(6,new LevelConfig{Min = 2000,Max = 3000});
        intimacyLevelDict.Add(7,new LevelConfig{Min = 3000,Max = 4000});
        intimacyLevelDict.Add(8,new LevelConfig{Min = 4000,Max = 5000});
        intimacyLevelDict.Add(9,new LevelConfig{Min = 5000,Max = 6000});
        intimacyLevelDict.Add(10,new LevelConfig{Min = 6000,Max = 99999999});
    }


    public bool IsOwnedBuddyByNpcId(string npcId)
    {
        if (_aiBuddyListRsp == null)
        {
            RequestAIBuddyList();
            return false;
        }

        var buddyInfo = GetAIBuddyByNpcId(npcId);

        return buddyInfo != null;
    }

    public AIBuddyInfo GetAIBuddyByNpcId(string npcId)
    {
        if (_aiBuddyListRsp.list != null && _aiBuddyListRsp.list.Count > 0)
        {
            foreach (var buddyInfo in _aiBuddyListRsp.list)
            {
                if (buddyInfo != null && buddyInfo.npc != null && buddyInfo.npc.id == npcId)
                {
                    return buddyInfo;
                }
            }
        }

        return null;
    }


    public AIBuddyListRsp GetLocalAIBuddyListRsp()
    {
        if (_aiBuddyListRsp == null)
        {
            _aiBuddyListRsp = new AIBuddyListRsp();
            _aiBuddyListRsp.totalSlot = 1;
            _aiBuddyListRsp.usedSlot = 0;
            _aiBuddyListRsp.list = new List<AIBuddyInfo>();
        }

        return _aiBuddyListRsp;
    }

    public void UpdateLocalAIBuddyListRsp(AIBuddyListRsp aiBuddyListRsp)
    {
        _aiBuddyListRsp = aiBuddyListRsp;
    }

    public void RequestAIBuddyList(Action<AIBuddyListRsp> onSuccess = null, Action<string> failCallback = null, string m_Cookie = null)
    {
        // 分页递归内部调用（有 cookie）直接继续；外部发起新请求时检查是否正在请求
        bool isNewRequest = string.IsNullOrEmpty(m_Cookie);
        if (isNewRequest)
        {
            if (isRequesting)
            {
                // 请求进行中，排队等候结果
                _pendingOnSuccess += onSuccess;
                _pendingOnFail += failCallback;
                return;
            }
            isRequesting = true;
            _pendingOnSuccess = null;
            _pendingOnFail = null;
            _aiBuddyListRsp = null;
        }

        var jb = new JObject();
        if (!string.IsNullOrEmpty(m_Cookie))
        {
            jb["cookie"] = m_Cookie;
        }

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIBuddyList,
            HttpMethod.GET, isNewRequest ? "" : JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                AIBuddyListRsp serverData = JsonConvert.DeserializeObject<AIBuddyListRsp>(arg0);
                if (serverData == null)
                {
                    isRequesting = false;
                    failCallback?.Invoke("deserialize failed");
                    var pf = _pendingOnFail; _pendingOnFail = null; _pendingOnSuccess = null;
                    pf?.Invoke("deserialize failed");
                    return;
                }
            //    UnityEngine.Debug.LogError($"AlBuddy List usedSlot={serverData.usedSlot},totalSlot={serverData.totalSlot},list={serverData.list.Count}");
                if (_aiBuddyListRsp == null)
                {
                    _aiBuddyListRsp = serverData;
                }
                else if (serverData.list != null)
                {
                    _aiBuddyListRsp.list.AddRange(serverData.list);
                }

                if (!string.IsNullOrEmpty(serverData.cookie) && serverData.isEnd == 0)
                {
                    RequestAIBuddyList(onSuccess, failCallback, serverData.cookie);
                }
                else
                {
                    isRequesting = false;
                    onSuccess?.Invoke(_aiBuddyListRsp);
                    var ps = _pendingOnSuccess; _pendingOnSuccess = null; _pendingOnFail = null;
                    ps?.Invoke(_aiBuddyListRsp);
                }
            }, onFail: arg0 =>
            {
                isRequesting = false;
                failCallback?.Invoke(arg0);
                var pf = _pendingOnFail; _pendingOnFail = null; _pendingOnSuccess = null;
                pf?.Invoke(arg0);
            }, retryCount: 3);
    }
    
    
    public void RequestAIBuddyInfo(string id,Action<AIBuddyInfoRsp> onSuccess = null, Action<string>failCallback = null)
    {
        JObject req = new JObject()
        {
            ["id"] = id,
        };
        
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIBuddyInfo,
            HttpMethod.GET,JsonConvert.SerializeObject(req),
            onReceive: arg0 =>
            {
                AIBuddyInfoRsp serverData = JsonConvert.DeserializeObject<AIBuddyInfoRsp>(arg0);
                if (serverData != null && serverData.info != null)
                {
                    if (_aiBuddyListRsp != null && _aiBuddyListRsp.list != null)
                    {
                        for (int i = 0;i < _aiBuddyListRsp.list.Count; i++)
                        {
                            var info = _aiBuddyListRsp.list[i];
                            if (info.id == serverData.info.id)
                            {
                                _aiBuddyListRsp.list[i] = serverData.info;
                                break;
                            }
                        }
                    }
                }
                onSuccess?.Invoke(serverData);
                MessageHelper.Broadcast(MessageName.OnAIBuddyInfoUpdated,serverData);
            }, onFail: arg0 =>
            {
                failCallback?.Invoke(arg0);
            },  retryCount:3);
    }

    //AIBuddy列表排序：交换位置
    public void RequestAIBuddyResort(string fromId,string toId,Action<bool>resultCallBack)
    {
        var jb = new JObject
        {
            ["aibuddyId"] = fromId,
            ["exchangeAIBuddyId"] = toId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIBuddyResort,
            HttpMethod.POST,JsonConvert.SerializeObject(jb),
            onReceive: arg0 =>
            {
                resultCallBack(true);
            }, onFail: arg0 =>
            {
                resultCallBack(false);
            },  retryCount:3);
    }

    /// <summary>
    /// 获取亲密度称号等级：0：路人甲 1：同行伙伴 2：知心好友 3：亲密挚友 4：灵魂伴侣
    /// </summary>
    /// <param name="intimacyValue"></param>
    /// <returns></returns>
    public int GetIntimacyTitleLevel(int intimacyValue)
    {
        int level = 0;
        foreach (var key in titleLevelDict.Keys)
        {
            var config = titleLevelDict[key];
            if (intimacyValue >= config.Min && intimacyValue < config.Max)
            {
                level = key;
                break;
            }
        }
        return level;
    }

    public bool IsMaxTitleLevel(int level)
    {
        return level >= (int)AIBuddyTitleLevel.Lv4;
    }

    public int GetTitleMaxByLevel(int level)
    {
        if(titleLevelDict.ContainsKey(level))
        {
            return titleLevelDict[level].Max;
        }

        return titleLevelDict[(int)AIBuddyTitleLevel.Lv4].Max;//返回最大值
    }

    public LevelConfig GetTitleLevelConfig(int level)
    {
        if (level >= (int)AIBuddyTitleLevel.Lv4)
        {
            return titleLevelDict[(int)AIBuddyTitleLevel.Lv4];
        }

        if (titleLevelDict.ContainsKey(level))
        {
            return titleLevelDict[level];
        }

        return null;
    }


    public int GetIntimacyLevel(int intimacyValue)
    {
        int level = 0;
        foreach (var key in intimacyLevelDict.Keys)
        {
            var config = intimacyLevelDict[key];
            if (intimacyValue >= config.Min && intimacyValue < config.Max)
            {
                level = key;
                break;
            }
        }
        return level;
    }
    
    public bool IsMaxIntimacyLevel(int level)
    {
        return level >= 10;
    }
    
    public LevelConfig GetIntimacyLevelConfig(int level)
    {
        if (IsMaxIntimacyLevel(level))
        {
            return intimacyLevelDict[10];
        }

        if (intimacyLevelDict.ContainsKey(level))
        {
            return intimacyLevelDict[level];
        }
        return null;
    } 

    public int GetLevelMaxByLevel(int level)
    {
        if(intimacyLevelDict.ContainsKey(level))
        {
            return intimacyLevelDict[level].Max;
        }

        return intimacyLevelDict[10].Max;//返回最大值
    }

    /// <summary>
    /// 获取当前特效id:"1" 、"2"等
    /// </summary>
    /// <param name="aiBuddyInfo"></param>
    /// <returns></returns>
    public string GetSummoningEffects(AIBuddyInfo aiBuddyInfo)
    {
        string result = "";
        if (aiBuddyInfo.summoningEffects == null || aiBuddyInfo.summoningEffects.Count <= 0)
        {
            return result;
        }

        string curEffect = aiBuddyInfo.summoningEffects[0];
        return curEffect;

    }

}
