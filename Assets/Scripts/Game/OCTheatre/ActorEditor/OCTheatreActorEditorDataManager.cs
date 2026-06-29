using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Es;
using Game.Database;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Events;

public enum OCTAvatarExpressionType//立绘里的资料页签和图片vip页签
{
    materials = 1,
    image = 2,
}
public enum ExpressionType
{
    normal = 1,
    expression = 2,
    custom = 3,
}
public class OCTheatreActorEditorDataManager : GameInstance<OCTheatreActorEditorDataManager>
{
    string[] expressionStrings = {"疲倦", "疑惑", "紧张", "惊讶", "开心", "害羞", "生气","难过", "平静", "宠溺"};

    public OCTheatreAvatarInfo curActor;//演員


    public OCTheatreAvatarInfo GetCurActor()
    {
        return curActor;
    }
    private readonly Dictionary<string, OCTheatreAvatarInfo> _myPublishedActorDict = new();

    public void FetchMyPublishedActors(Action<List<OCTheatreAvatarInfo>> onSuccess, Action<string> onFail = null)
    {
        var reqParam = JsonConvert.SerializeObject(new JObject
        {
            ["cookie"] = "",
            ["uid"] = AccountDataManager.Inst.Uid,
        });
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActorPublishList, HttpMethod.GET, reqParam,
            content =>
            {
                var response = JsonConvert.DeserializeObject<MapListResponse>(content);
                var list = new List<OCTheatreAvatarInfo>();
                if (response?.list != null)
                {
                    foreach (var item in response.list)
                    {
                        if (item.actorInfo == null) continue;
                        _myPublishedActorDict[item.actorInfo.id] = item.actorInfo;
                        list.Add(item.actorInfo);
                    }
                }
                onSuccess?.Invoke(list);
            },
            error =>
            {
                UnityEngine.Debug.LogWarning($"[OCTheatreActorEditorDataManager] FetchMyPublishedActors failed: {error}");
                onFail?.Invoke(error);
            });
    }

    public void FetchMyBagActors(Action<List<OCTheatreAvatarInfo>> onSuccess, Action<string> onFail = null)
    {
        var actorKey = UniqueType.Get(ResourceType.AvatarCard, (int)UgcTheatreSubType.AvatarCard);
        var ownedItems = BagDatabase.Inst.SelectAll(actorKey);

        var idList = ownedItems != null
            ? string.Join(",", ownedItems.Where(i => !string.IsNullOrEmpty(i.Id)).Select(i => i.Id))
            : string.Empty;

        if (string.IsNullOrEmpty(idList))
        {
            onSuccess?.Invoke(new List<OCTheatreAvatarInfo>());
            return;
        }

        var jb = new JObject { ["idList"] = idList };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.ActorBatchInfo,
            HttpMethod.GET,
            JsonConvert.SerializeObject(jb),
            content =>
            {
                var rsp = JsonConvert.DeserializeObject<BatchActorDetailRsp>(content);
                var list = new List<OCTheatreAvatarInfo>();
                if (rsp?.actorList != null)
                {
                    foreach (var item in rsp.actorList)
                    {
                        var info = item?.actorInfo;
                        if (info == null) continue;
                        _myPublishedActorDict[info.id] = info;
                        if (OCTheatreDataManager.Inst != null)
                            OCTheatreDataManager.Inst.ocCharacterInfoDict[info.id] = info;
                        list.Add(info);
                    }
                }
                onSuccess?.Invoke(list);
            },
            error =>
            {
                UnityEngine.Debug.LogWarning($"[OCTheatreActorEditorDataManager] FetchMyBagActors failed: {error}");
                onFail?.Invoke(error);
            });
    }

    public void GetActorForId(string id, Action<OCTheatreAvatarInfo> callback)
    {
        if (string.IsNullOrEmpty(id)) { callback?.Invoke(null); return; }
        if (_myPublishedActorDict.TryGetValue(id, out var myInfo)) { callback?.Invoke(myInfo); return; }
        if (OCTheatreDataManager.Inst.ocCharacterInfoDict.TryGetValue(id, out var defInfo)) { callback?.Invoke(defInfo); return; }
        FetchMyPublishedActors(_ =>
        {
            _myPublishedActorDict.TryGetValue(id, out var fetched);
            if (fetched == null) OCTheatreDataManager.Inst.ocCharacterInfoDict.TryGetValue(id, out fetched);
            callback?.Invoke(fetched);
        });
    }
        

    public void SetCurActor(OCTheatreAvatarInfo actor)
    {
        curActor = actor;
        curActor.personalities ??= new List<string>();
        curActor.importantPersons ??= new List<string>();
        curActor.avatarClothes ??= new List<OTCAvatarClothes>();
        curActor.expressions ??= new List<OCTAvatarExpression>();
    }
    public void CreateNewActor()
    {
        curActor = new OCTheatreAvatarInfo
        {
            personalities = new List<string>(),
            importantPersons = new List<string>(),
            avatarClothes = new List<OTCAvatarClothes>(),
            expressions = new List<OCTAvatarExpression>(),
        };

        //初始化默认表情（资料页签）
        curActor.expressions.Add(new OCTAvatarExpression
        {
            expressionName = "常态",
            expressionURL = "",
            expressionType = (int)ExpressionType.normal,
            mType = (int)OCTAvatarExpressionType.materials,
        });
        for (int i = 0; i < expressionStrings.Length; i++)
        {
            curActor.expressions.Add(new OCTAvatarExpression
            {
                expressionName = expressionStrings[i],
                expressionURL = "",
                expressionType = (int)ExpressionType.expression,
                mType = (int)OCTAvatarExpressionType.materials,
            });
        }

        //初始化默认表情（图片vip页签）
        curActor.expressions.Add(new OCTAvatarExpression
        {
            expressionName = "常态",
            expressionURL = "",
            expressionType = (int)ExpressionType.normal,
            mType = (int)OCTAvatarExpressionType.image,
        });
        for (int i = 0; i < expressionStrings.Length; i++)
        {
            curActor.expressions.Add(new OCTAvatarExpression
            {
                expressionName = expressionStrings[i],
                expressionURL = "",
                expressionType = (int)ExpressionType.expression,
                mType = (int)OCTAvatarExpressionType.image,
            });
        }

    }
    public OCTheatreActorEditorDataManager()
    {
        CreateNewActor();
    }
    public List<OCTAvatarExpression> GetOCTAvatarExpression(OCTAvatarExpressionType m_type,ExpressionType expressionType)
     {
        List<OCTAvatarExpression> list = new List<OCTAvatarExpression>();
        list = curActor.expressions.FindAll((info) => info.mType == (int)m_type && info.expressionType == (int)expressionType);
        return list;
    }

    public void RemoveOCTAvatarExpression(OCTAvatarExpression expression)
    {
        if (expression == null) return;
        curActor.expressions.Remove(expression);
    }
    public void AddOCTAvatarExpression(OCTAvatarExpression expression)
    {
        if (expression == null) return;
        if (curActor.expressions.Contains(expression)) return;
        curActor.expressions.Add(expression);
    }

    public OTCAvatarClothes GetClothes(int id)
    {
        var actor = GetCurActor();
        return actor.avatarClothes.Find((clothes)=>clothes.clothesIndex == id);
    }
     public void SetClothes(OTCAvatarClothes _clothes)
    {
        var actor = GetCurActor();
        var clothes = actor.avatarClothes.Find((clothes)=>clothes.clothesIndex == _clothes.clothesIndex);
        if(clothes == null) return;
        clothes.clothesJson = _clothes.clothesJson;
        clothes.clothesName = _clothes.clothesName;
        clothes.clothesURL = _clothes.clothesURL;
    }
    public List<OTCAvatarClothes> GetClothesList()
    {
        var actor = GetCurActor();
        return actor.avatarClothes; 
    }
    public void RemoveClothes(int id)
    {
        var actor = GetCurActor();
        var clothes = actor.avatarClothes.Find(c => c.clothesIndex == id);
        if (clothes != null)
        {
            bool wasDef = clothes.isDef == 1;
            actor.avatarClothes.Remove(clothes);
            if (wasDef && actor.avatarClothes.Count > 0)
                actor.avatarClothes[0].isDef = 1;
        }
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
    }
    public void AddClothes(OTCAvatarClothes clothes)
    {
        var actor = GetCurActor();
        if (actor.avatarClothes.Count == 0)
            clothes.isDef = 1;
        actor.avatarClothes.Insert(0, clothes);
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
    }
    public void UpdateClothes(OTCAvatarClothes clothes)
    {
        var actor = GetCurActor();
        var existingClothes = actor.avatarClothes.Find(c => c.clothesIndex == clothes.clothesIndex);
        if(existingClothes != null)
        {
            existingClothes.clothesName = clothes.clothesName;
            existingClothes.clothesURL = clothes.clothesURL;
            existingClothes.clothesJson = clothes.clothesJson;
            // isDef 不覆盖，保留已有状态
        }
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
    }

    public OTCAvatarClothes GetDefaultClothes()
    {
        var actor = GetCurActor();
        return actor.avatarClothes.Find(c => c.isDef == 1) ?? (actor.avatarClothes.Count > 0 ? actor.avatarClothes[0] : null);
    }

    public void SetGender(int gender)
    {
        var actor = GetCurActor();
        actor.gender = gender;
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
    }

    public void SetActorName(string name)
    {
        var actor = GetCurActor();
        actor.name = name;
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
    }

    public void AddImportantPersons(string person,Action errAction)
    {
        var actor = GetCurActor();
        if(!actor.importantPersons.Contains(person))
        {
            actor.importantPersons.Add(person);
            MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
        }
        else
        {
            errAction?.Invoke();
        }
    }
    public void RemoveImportantPersons(string person)
    {
        var actor = GetCurActor();
        if(actor.importantPersons.Contains(person))
        {
            actor.importantPersons.Remove(person);
            MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
        }
    }
    public void SetBackgroundDes(string des)
    {
        var actor = GetCurActor();
        actor.backgroundDes = des;
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
    }
    public void AddPersonalities(string personality)
    {
        var actor = GetCurActor();
        if(!actor.personalities.Contains(personality))
        {
            actor.personalities.Add(personality);
            MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
        }
    }
    public void RemovePersonalities(string personality)
    {
        var actor = GetCurActor();
        if(actor.personalities.Contains(personality))
        {
            actor.personalities.Remove(personality);
            MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
        }
    }

    //衣柜设为默认
    public void SetDefault(OTCAvatarClothes _clothes)
    {
        var actor = GetCurActor();
        foreach (var c in actor.avatarClothes)
            c.isDef = 0;
        var clothes = actor.avatarClothes.Find(c => c.clothesIndex == _clothes.clothesIndex);
        if (clothes != null)
        {
            clothes.isDef = 1;
            MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, null);
        }
    }
    //角色介绍
    public void SetDesc(string desc)
    {
        var actor = GetCurActor();
        actor.desc = desc;
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
    }

    public void SetCover(string cover)
    {
        var actor = GetCurActor();
        actor.cover = cover;
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,null);
    }

    public void SetActorCarBg(string url)
    {
        var actor = GetCurActor();
        actor.bgUrl = url;
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate,actor);
    }
    public string GetActorCarBg()
    {
        var actor = GetCurActor();
        return actor.bgUrl;
    }
    public void SetExpressionURL(OCTAvatarExpression expression, string url)
    {
        if (!GetCurActor().expressions.Contains(expression)) return;
        expression.expressionURL = url;
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, null);
    }

    public void SetExpressionName(OCTAvatarExpression expression, string name)
    {
        // 直接按引用修改，避免同名多个 expression 时改错对象
        if (!GetCurActor().expressions.Contains(expression)) return;
        expression.expressionName = name;
        MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, null);
    }

    public void SetBarColor(string color)
    {
        var actor = GetCurActor();
        actor.barColor = color;
    }

    public void SetMainColor(string color)
    {
        var actor = GetCurActor();
        actor.mainColor = color;
    }

    public void SetTextColor(string color)
    {
        var actor = GetCurActor();
        actor.textColor = color;
    }

    public void SetBgColor(string color)
    {
        var actor = GetCurActor();
        actor.bgColor = color;
    }
    public void SetClothesName(OTCAvatarClothes clothes, string name)
    {
        var actor = GetCurActor();
        var c = actor.avatarClothes.Find(c => c.clothesIndex == clothes.clothesIndex);
        if (c != null)
        {
            c.clothesName = name;
            MessageHelper.Broadcast<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, null);
        }
    }
}
