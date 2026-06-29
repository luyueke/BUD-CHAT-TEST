using Network.Http;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Message;
using UnityEditor;

public class AIParkStoreManager: GlobalInstance<AIParkStoreManager>
{
    /// <summary>
    /// 商店配置信息
    /// </summary>
    private readonly string _configPath = "Assets/Loadable/UI/UIPanel/AIHospitalGame/AIHospitalConfig/AIHospital_StoreData.json";

    /// <summary>
    /// 本地缓存 前缀
    /// </summary>
    private string _localKey = "s9AIStore_";

    private string _notfirstSet = "notfirstSet";

    private Dictionary<int, AIHospitalStoreItemData> _allItems = new Dictionary<int, AIHospitalStoreItemData>();

    private Dictionary<int, bool> _itemTipsStateDic = new Dictionary<int, bool>();

    private Dictionary<int,RedDotCondition> _checkList = new Dictionary<int, RedDotCondition>();

    private Dictionary<int, List<string>> _clothesConfig;

    private class RedDotCondition
    {
        public CurrencyType currencyType;
        public int currencyNum;
    }

    public void Init()
    {
        LoadConfig();
        AddListener();
        InitClothesConfig();
    }

    private void AddListener()
    {
        MessageHelper.AddListener(MessageName.LoginSuccess,SyncServerConfig);
        MessageHelper.AddListener<CurrencyType>(MessageName.OnPlayerInfoAccountChange, OnPlayerInfoAccountChange);
    }

    private void RemoveListener()
    {
        MessageHelper.RemoveListener(MessageName.LoginSuccess,SyncServerConfig);
        MessageHelper.AddListener<CurrencyType>(MessageName.OnPlayerInfoAccountChange, OnPlayerInfoAccountChange);
    }

    #region 内部数据加载
    private void LoadConfig()
    {
        //todo 读取配置路径的文件序列化_allItems,添加完成后打印出来看看
        _allItems = new Dictionary<int, AIHospitalStoreItemData>();
        GameObject obj = new GameObject();
        var jsonContent = Loader.Load<TextAsset>(_configPath, obj);
        //todo 读取jsonContent.text 

        try
        {
            JObject jsonData = JsonConvert.DeserializeObject<JObject>(jsonContent.text);
            if (jsonData["items"] is JArray data)
            {
                foreach (var item in data)
                {
                    _allItems.Add(item["ID"].ToObject<int>(), item.ToObject<AIHospitalStoreItemData>());
                }
                if (PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + _localKey + _notfirstSet, 0) == 0)
                {
                    foreach (var item in _allItems)
                    {
                        LoggerUtils.Log($"商城配置：{item.Key} - {item.Value}");
                        PlayerPrefs.SetInt(AccountDataManager.Inst.Uid + _localKey + _notfirstSet, 1);
                        SetItemTipsState(item.Key, item.Value.nTips == 1);
                    }
                }
                else
                {
                    LoadLocalCache();
                }

                LoggerUtils.Log($"商城配置加载完成，数量：{_allItems.Count}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }

    }

    private void LoadLocalCache()
    {
        //todo读取本地缓存的关注信息，红点
        foreach (var item in _allItems)
        {
            bool value = PlayerPrefs.GetInt(AccountDataManager.Inst.Uid + _localKey + item.Key, 0) == 1;
            SetItemTipsState(item.Key, value);
        }
    }

    /// <summary>
    /// 从服务器同步信息
    /// </summary>
    private void SyncServerConfig()
    {
        //todo 从服务器同步信息     
        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>
        {
            ActivityId.AbandonedHospital.ToString()
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
        {
            ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
            if (activityResponse.list != null)
            {
                foreach (var item in activityResponse.list)
                {
                    if (item.activityId == ActivityId.AbandonedHospital.ToString())
                    {
                        foreach (var reward in item.rewardList)
                        {
                            if (_allItems.TryGetValue(reward.rewardId, out AIHospitalStoreItemData goodsItem))
                            {
                                //LoggerUtils.Log($"服务器商品数据更新 id = {reward.rewardId} name = {goodsItem.strName},剩余数量= {reward.redeemLeftCount}");
                                goodsItem.nRemainCostTimes = reward.redeemLeftCount;
                                goodsItem.rewardStatus = reward.rewardStatus == 1;
                            }
                        }
                    }
                }
            }
            else
            {
                LoggerUtils.Log("拉取服务器商店活动数据失败");
            }
        }, (err) => { LoggerUtils.Log("拉取活动数据失败", err); }, null, 0, 3);
    }

    private void WritelocalCache()
    {
        //todo 遍历_itemTipsStateDic，如果值为true则写入本地
        foreach (var kvp in _itemTipsStateDic)
        {
            if (kvp.Value)
            {
                PlayerPrefs.SetInt(AccountDataManager.Inst.Uid+_localKey + kvp.Key, 1);
            }
            else
                PlayerPrefs.SetInt(AccountDataManager.Inst.Uid + _localKey + kvp.Key, 0);
        }
    }


    private void RegisterRedDot(AIHospitalStoreItemData data)
    {
        //todo 注册红点
        if (!_checkList.TryGetValue(data.ID,out RedDotCondition value))
        {
            bool isGiftBox = data.nGoodsType == (int)EGoodsType.GiftBox;
            LoggerUtils.Log($"_checkList add {data.ID}");
            _checkList.Add(data.ID,new RedDotCondition()
            {
                currencyType = (CurrencyType)data.nCostCurrencyType,
                currencyNum = data.nCostAmount,
            });
        }
    }

    private void UnRegisterRedDot(AIHospitalStoreItemData data)
    {
        //todo 取消注册红点
        //LoggerUtils.Log($"_checkList remove {data.ID}");
        _checkList.Remove(data.ID);
    }

    private void OnPlayerInfoAccountChange(CurrencyType bType)
    {
        var balanceInfo = AccountDataManager.Inst.BalanceInfo;
        foreach (var item in _checkList)
        {
            if (item.Value.currencyType == bType)
            {
                if (balanceInfo.GetAccountCount(bType)>item.Value.currencyNum)
                {
                    LoggerUtils.Log($"当前货币类型{bType}足够 count = {balanceInfo.GetAccountCount(bType)},所需 = {item.Value.currencyNum}，刷新商店红点");
                    MessageHelper.Broadcast(MessageName.OnS9StoreTips);
                    break;
                }
            }
        }
    }
    #endregion

    public override void Release()
    {
        WritelocalCache();
        RemoveListener();
        base.Release();
    }


    #region 提供外部访问的方法
    public void SetItemTipsState(int id, bool value)
    {
        if (!_allItems.TryGetValue(id, out AIHospitalStoreItemData item))
        {
            return;
        }
        //else if (item.nRemainCostTimes < 0)
        //{
        //    LoggerUtils.Log($"商品数量不足 没必要再关注了{id}");
        //    value = false;
        //}
        if (_itemTipsStateDic.TryGetValue(id, out bool state))
        {
            _itemTipsStateDic[id] = value;
        }
        else
        {
            _itemTipsStateDic.Add(id, value);
        }

        if (value)
        {
            RegisterRedDot(item);
        }
        else
            UnRegisterRedDot(item);
    }

    public bool GetItemTipsState(int id)
    {
        if (_itemTipsStateDic.TryGetValue(id, out bool value))
        {
            return value;
        }
        return false;
    }

    public void UpdateItemRemainCount(int id,int remainCount)
    {
        if (_allItems.TryGetValue(id,out AIHospitalStoreItemData data))
        {
            data.nRemainCostTimes = remainCount;
            //if (remainCount<=0)
            //{
            //    SetItemTipsState(id,false);
            //}
        }
    }

    public bool OnCheckRedDotState()
    {
        bool bShow = false;
        var balanceInfo = AccountDataManager.Inst.BalanceInfo;
        foreach (var item in _checkList)
        {
            bool bFree = item.Value.currencyType == CurrencyType.Free;
            bool sallOut = false;
            if (_allItems.TryGetValue(item.Key, out AIHospitalStoreItemData data))
            {
                sallOut = data.nRemainCostTimes <= 0;
            } 
            if ((bFree||balanceInfo.GetAccountCount(item.Value.currencyType) > item.Value.currencyNum)&&!sallOut)
            {
                bShow = true;
                break;
            }
        }
        return bShow;
    }

    public List<string> GetClothesData(int id)
    {
        if (_clothesConfig.TryGetValue(id, out List<string> data))  
        {
            return data;
        }
        else
        {   
            LoggerUtils.LogError($"获取时装数据失败，id = {id}");
            return null;
        }   
    }

    public Dictionary<int, AIHospitalStoreItemData> GetAllStoreItem()
    {
        return _allItems;
    }
    #endregion

    private void InitClothesConfig()
    {
        _clothesConfig = new Dictionary<int, List<string>>
        {
            {
                1, new List<string>
                {
                    "11500032",
                    "11700032",
                    "10600120",
                    "10100107",
                    "11300343",
                    "12000032",
                    "10400467",
                    "10500068",
                    "10900468"
                }
            },
            {
                2, new List<string>
                {
                    "11500033",
                    "10600121",
                    "10200058",
                    "11300344",
                    "10400468",
                    "10900476",
                    "10500069",
                    "11000241"
                }
            },
            {
                3, new List<string>
                {
                    "10600118",
                    "10100105",
                    "10200057",
                    "11300341",
                    "11000249",
                    "10400465",
                    "11700031",
                    "10900474",
                    "10500070"
                }
            },
            {
                4, new List<string>
                {
                    "10600119",
                    "10100106",
                    "11300342",
                    "11000250",
                    "10400466",
                    "10900475",
                    "10500071"
                }
            }
        };
    }

    public static string HeadFramePgcID = "130100020";
}


