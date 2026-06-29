using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MsgUtils 
{
    #region npc数据
    static Dictionary<string, DetailRsp> npcCacheDic = new Dictionary<string, DetailRsp>();

    public static void GetNpcInfoLs(List<string> IDs, Action<List<DetailRsp>> aciton)
    {
        if (IDs == null || IDs.Count <= 0)
        {
            return;
        }

        var needreq = false;
        foreach (var item in IDs)
        {
            if (!npcCacheDic.ContainsKey(item))
            {
                needreq = true;
                break;
            }
        }

        if (!needreq) {
            var ls = new List<DetailRsp>();
            foreach (var item in IDs)
            {
                ls.Add(npcCacheDic[item]);
            }
            aciton?.Invoke(ls);
            return;
        }

        string id = string.Empty;
        foreach (string s in IDs) {
            id += (s + ",");
        }
        id.Remove(id.Length -1 ,1);
        JObject req = new JObject()
        {
            ["idList"] = id,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.NpcBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req),
             (content) =>
             {
                 var rspData = JsonConvert.DeserializeObject<NpcBatchInfo>(content);
                 LoggerUtils.Log($"GetNpcInfoLs : {content}");
                 if (rspData != null)
                 {
                     foreach (var item in rspData.npcList)
                     {
                         if (npcCacheDic.ContainsKey(item.npc.id))
                         {
                             npcCacheDic[item.npc.id] = item;
                         }
                         else
                         {
                             npcCacheDic.Add(item.npc.id, item);
                         }
                     }
                     var ls = new List<DetailRsp>();
                     foreach (var item in IDs)
                     {
                         ls.Add(npcCacheDic[item]);
                     }
                     aciton?.Invoke(ls);
                 }
             },
            (error) => { LoggerUtils.LogError($"GetNpcInfoLs  : {error}"); });
    }

    public static void GetNpcInfo(string ID, Action<DetailRsp> aciton)
    {
        if (npcCacheDic.ContainsKey(ID))
        {
            aciton?.Invoke(npcCacheDic[ID]);
            return;
        }
        JObject req = new JObject()
        {
            ["idList"] = ID,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.NpcBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), 
            (content) => 
            {
                var rspData = JsonConvert.DeserializeObject<NpcBatchInfo>(content);
                LoggerUtils.Log($"GetNpcInfo : {content}");
                if (rspData != null)
                {
                    foreach (var item in rspData.npcList)
                    {
                        if (npcCacheDic.ContainsKey(item.npc.id))
                        {
                            npcCacheDic[item.npc.id] = item;
                        }
                        else
                        {
                            npcCacheDic.Add(item.npc.id, item);
                        }
                    }
                    aciton?.Invoke(npcCacheDic[ID]);
                }
            }, 
            (error) => { LoggerUtils.LogError($"GetNpcInfo  : {error}"); });
    }

    public class NpcBatchInfo {
        public List<DetailRsp> npcList;
    }
    #endregion
}