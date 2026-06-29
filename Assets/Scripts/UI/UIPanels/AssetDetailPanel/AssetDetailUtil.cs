using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssetDetailUtil 
{
    static Dictionary<AssetDetailType, string> headUrlDict = new Dictionary<AssetDetailType, string>
    {
        { AssetDetailType.Prop, HttpUrlDefine.propInfo },
        { AssetDetailType.Skin, HttpUrlDefine.GetClothesInfo },
        { AssetDetailType.Mat, HttpUrlDefine.GetMaterialInfo },
        { AssetDetailType.Instrument, HttpUrlDefine.GetClothesInfo },
        { AssetDetailType.MusicScore, HttpUrlDefine.GetMusicScoreInfo },
        { AssetDetailType.UgcBundle, HttpUrlDefine.GetClothesInfo },
        { AssetDetailType.MusicTone, HttpUrlDefine.GetMusicTone },
        { AssetDetailType.UgcPose, HttpUrlDefine.GetPoseInfo },
        { AssetDetailType.UgcAnim, HttpUrlDefine.getAnimInfo },
        { AssetDetailType.UgcAnimMusic, HttpUrlDefine.getUgcAnimMusic },
        { AssetDetailType.AINpc, HttpUrlDefine.NpcInfo },
        { AssetDetailType.Actor, HttpUrlDefine.ActorInfo },
    };

    static Dictionary<string, DetailRsp> cacheInfoDic = new Dictionary<string, DetailRsp>();
    public static void GetInfo(int headUrl, string ID, Action<DetailRsp> aciton)
    {
        GetInfo((AssetDetailType)headUrl,ID,aciton);
    }
    public static void GetInfo(AssetDetailType headUrl,string ID,Action<DetailRsp> aciton)
    {
        if (cacheInfoDic.ContainsKey(ID))
        {
            aciton?.Invoke(cacheInfoDic[ID]);
            return;
        }
        if (headUrlDict.ContainsKey(headUrl))
        {
            JObject req = new JObject()
            {
                ["id"] = ID,
            };
            NetworkManager.Inst.SendHttpRequest(headUrlDict[headUrl], HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>{ OnGetInfoSuccess(ID,content, aciton); }, OnGetInfoFail);
        }
    }

    static void OnGetInfoSuccess(string ID,string content, Action<DetailRsp> aciton)
    {
        var rspData = JsonConvert.DeserializeObject<DetailRsp>(content);
        LoggerUtils.Log($"AssetDetail  : {ID} : {content}");
        if (rspData != null)
        {
            if (cacheInfoDic.ContainsKey(ID))
            {
                cacheInfoDic[ID] = rspData;
            }
            else
            {
                cacheInfoDic.Add(ID, rspData);
            }
            aciton?.Invoke(rspData);
        }
    }

    static void OnGetInfoFail(string error)
    {
        LoggerUtils.LogError($"AssetDetail  : {error}");
    }


    public static void GetBatchInfo(string ID, Action<DetailRsp> aciton)
    {
        JObject req = new JObject()
        {
            ["idList"] = ID,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
        {
            BatchDetailRsp rspData = JsonConvert.DeserializeObject<BatchDetailRsp>(content);
            if (rspData.skinList == null || rspData.skinList.Count == 0 || rspData.skinList[0].skinActionInfo == null) return;
            aciton?.Invoke(rspData.skinList[0]);
        },
        (error) =>
        {
            LoggerUtils.LogError($"GetBatchInfo  : {error}");
        });
    }
}