using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;


/// <summary>
/// 商业化上线线下配置管理器
/// </summary>
public class BusinessLiveManager : GlobalInstance<BusinessLiveManager>
{
    private BusinessLiveConfig curConfig = null;
    private Dictionary<string, string> gashaponLiveDict = new Dictionary<string, string>();//扭蛋
    private Dictionary<string, bool> gashaponNewDict = new Dictionary<string, bool>();//扭蛋
    private Dictionary<string, bool> gashaponBackDict = new Dictionary<string, bool>();//扭蛋
    private Dictionary<string, string> limitPackLiveDict = new Dictionary<string, string>();//限购礼包
    private Dictionary<string, string> paidPackLiveDict = new Dictionary<string, string>();//直氪礼包
    private Dictionary<string, string> seriesLiveDict = new Dictionary<string, string>();//系列
    private Dictionary<string, string> activityLiveDict = new Dictionary<string, string>();//活动
    private Dictionary<string, string> rechargeActivityLiveDict = new Dictionary<string, string>(); //热销活动
    private Action<BusinessLiveConfig> _configUpdateAction;

    private const string configPath = "Assets/Loadable/UI/UIPanel/GameHall/BusinessLiveLocalConfig.json";

    public void Init()
    {
        MessageHelper.AddListener(MessageName.BusinessLiveConfigUpdate, OnTcpBrocastConfigUpdate);
        LoadFromLocal();
        RequestBusinessConfig();
    }

    public override void Release()
    {
        base.Release();
        MessageHelper.RemoveListener(MessageName.BusinessLiveConfigUpdate, OnTcpBrocastConfigUpdate);
    }

    public void LoadFromLocal()
    {
        var UIRoot = GameObject.Find("Canvas");
        var textAsset = Loader.Load<TextAsset>(configPath, UIRoot);
        if (textAsset != null)
        {
            BusinessLiveConfig localData = JsonConvert.DeserializeObject<BusinessLiveConfig>(textAsset.text);
            if (localData != null)
            {
                LoggerUtils.Log("##LoadFromLocal Finish");
                InitData(localData);
            }
        }
    }

    //测试代码
    public void TestBrocastLocal()
    {
        _configUpdateAction?.Invoke(curConfig);
    }

    private void InitData(BusinessLiveConfig resData)
    {
        curConfig = resData;
        //扭蛋数据处理
        gashaponLiveDict.Clear();
        if (curConfig.store != null && curConfig.store.lotteryList != null && curConfig.store.lotteryList.Count > 0)
        {
            foreach (string gashaId in curConfig.store.lotteryList)
            {
                gashaponLiveDict[gashaId] = gashaId;
                LoggerUtils.Log($"Added series to live dict: {gashaId}");
            }
        }

        gashaponNewDict.Clear();
        if (curConfig.store != null && curConfig.store.newLotteryList != null && curConfig.store.newLotteryList.Count > 0)
        {
            foreach (string gashaId in curConfig.store.newLotteryList)
            {
                gashaponNewDict[gashaId] = true;
            }
        }
        gashaponBackDict.Clear();
        if (curConfig.store != null && curConfig.store.EncoreLotteryList != null && curConfig.store.EncoreLotteryList.Count > 0)
        {
            foreach (string gashaId in curConfig.store.EncoreLotteryList)
            {
                gashaponBackDict[gashaId] = true;
            }
        }


        //限购礼包数据处理
        limitPackLiveDict.Clear();
        if (curConfig.recharge != null && curConfig.recharge.limitPackageList != null)
        {
            AddLimitProduct(curConfig.recharge.limitPackageList);
        }

        seriesLiveDict.Clear();
        if (curConfig.avatar != null && curConfig.avatar.seriesList != null)
        {
            foreach (string sId in curConfig.avatar.seriesList)
            {
                seriesLiveDict[sId] = sId;
            }
        }

        paidPackLiveDict.Clear();
        if (curConfig.recharge != null && curConfig.recharge.paidPackageList != null)
        {
            foreach (string pId in curConfig.recharge.paidPackageList)
            {
                paidPackLiveDict[pId] = pId;
            }
        }

        rechargeActivityLiveDict.Clear();
        if (curConfig.recharge != null && curConfig.recharge.activityList != null)
        {
            foreach (string pId in curConfig.recharge.activityList)
            {
                rechargeActivityLiveDict[pId] = pId;
            }
        }

        activityLiveDict.Clear();
        if (curConfig.activity != null && curConfig.activity.activityList != null)
        {
            foreach (string aId in curConfig.activity.activityList)
            {
                activityLiveDict[aId] = aId;
            }
        }
    }

    private void AddLimitProduct(List<string> packageData)
    {
        if (packageData!= null)
        {
            foreach (string productId in packageData)
            {
                limitPackLiveDict[productId] = productId;
            }
        }
    }

    public BusinessLiveConfig GetBusinessConfig()
    {
        if (curConfig == null)
        {
            RequestBusinessConfig();
        }

        return curConfig;
    }

    /// <summary>
    /// 判断扭蛋是否上线
    /// </summary>
    /// <param name="gashaponType"></param>
    /// <returns></returns>
    public bool IsGashaponLive(int gashaponType) {
        if (gashaponType == 0)
        {
            return false;
        }
        var config = DataTables.GetGashaponViewConfig(gashaponType);
        if(config != null && config.GashaId == "lottery.bambooBasket" && gashaponLiveDict != null)
        {
            if(gashaponLiveDict.ContainsKey("lottery.bambooBasket"))
            {
                return true;
            }
        }
        if(config != null && config.GashaId == "lottery.VioletGashapon" && gashaponLiveDict != null)
        {
            if(gashaponLiveDict.ContainsKey("lottery.violetFragrance") || gashaponLiveDict.ContainsKey("lottery.nightButterflyDream"))
            {
                return true;
            }
        }
        if (config != null && config.GashaId == "lottery.PolkaDotBerry" && gashaponLiveDict != null)
        {
            if (gashaponLiveDict.ContainsKey("lottery.polkaDotBerry") || gashaponLiveDict.ContainsKey("lottery.seaSaltRabbit"))
            {
                return true;
            }
        }
        if (config != null && config.GashaId == "lottery.musicalPuddingSuit" && gashaponLiveDict != null)
        {
            if(gashaponLiveDict.ContainsKey("lottery.musicalPudding.prank") || gashaponLiveDict.ContainsKey("lottery.musicalPudding.umi") || gashaponLiveDict.ContainsKey("lottery.musicalPudding.didu"))
            {
                return true;
            }
        }
        if (config != null && config.GashaId == "lottery.wawaKindergartenSuit" && gashaponLiveDict != null)
        {
            if (gashaponLiveDict.ContainsKey("lottery.wawaKindergarten.pinkTail") || gashaponLiveDict.ContainsKey("lottery.wawaKindergarten.warmOrange"))
            {
                return true;
            }
        }
        //if(config != null && config.GashaId == "lottery.yuanxiaoJiejiele" && gashaponLiveDict != null)
        //{
        //    return true;
        //}
        return config != null && gashaponLiveDict != null && gashaponLiveDict.ContainsKey(config.GashaId);
    }

    public bool IsGashaponLive(string gashaId)
    {
        return gashaponLiveDict != null && gashaponLiveDict.ContainsKey(gashaId);
    }

    /// <summary>
    /// 判断扭蛋是否有New标识
    /// </summary>
    /// <param name="gashaponType"></param>
    /// <returns></returns>
    public bool IsGashaponNew(int gashaponType) {
        if (gashaponType == 0)
        {
            return false;
        }
        var config = DataTables.GetGashaponViewConfig(gashaponType);
        return config != null && gashaponNewDict != null && gashaponNewDict.ContainsKey(config.GashaId);
    }

    public bool IsGashaponNew(string gashaId)
    {
        return gashaponLiveDict != null && gashaponNewDict.ContainsKey(gashaId);
    }
    public bool IsGashaponBack(string gashaId)
    {
        return gashaponLiveDict != null && gashaponBackDict.ContainsKey(gashaId);
    }

    /// <summary>
    ///  判断限购礼包是否上线
    /// </summary>
    /// <param name="productId"> 产品id:productId</param>
    /// <returns></returns>
    public bool IsLimitProductLive(string productId)
    {
        return limitPackLiveDict.ContainsKey(productId);
    }


    /// <summary>
    /// 判断系列是否上线
    /// </summary>
    /// <param name="seriesId">系列id</param>
    /// <returns></returns>
    public bool IsSeriesLive(string seriesId)
    {
        var isLive = seriesLiveDict.ContainsKey(seriesId);
      //  LoggerUtils.LogError($"Series {seriesId} live status: {isLive}");
        return isLive;
    }


   /// <summary>
   /// 判断直氪礼包是否上线
   /// </summary>
   /// <param name="packageType">礼包枚举值</param>
   /// <returns></returns>
    public bool IsPaidPackLive(int packageType)
    {
        return paidPackLiveDict.ContainsKey(packageType.ToString());
    }

   /// <summary>
   /// 判断直氪礼包是否上线
   /// </summary>
   /// <param name="packageType">礼包枚举值</param>
   /// <returns></returns>
    public bool IsPaidPackLive(string packageType)
    {
        return paidPackLiveDict.ContainsKey(packageType);
    }

    /// <summary>
    /// 判断活动是否上线
    /// </summary>
    /// <param name="activityId"></param>
    /// <returns></returns>
    public bool IsActivityLive(string activityId)
    {
        return activityLiveDict.ContainsKey(activityId);
    }

    public bool IsRechargeActivityLive(string rechargeId)
    {
        return rechargeActivityLiveDict.ContainsKey(rechargeId);
    }

    //从后端获取配置
    public void RequestBusinessConfig(Action<BusinessLiveConfig> successAction = null, Action<string>failAction = null)
    {
        LoggerUtils.Log("##RequestBusinessConfig Start");
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BusinessConfig, HttpMethod.GET,"",
            onReceive: arg0 =>
            {
                BusinessLiveConfig resData = JsonConvert.DeserializeObject<BusinessLiveConfig>(arg0);
                if (resData != null)
                {
                    LoggerUtils.Log("##Server config loaded: " + JsonConvert.SerializeObject(resData.avatar?.seriesList));
                    //InitData(resData);
                    //successAction?.Invoke(resData);
                }
                if (resData == null)
                {
                    failAction?.Invoke("BusinessLiveConfig is null");
                    LoggerUtils.LogError("BusinessLiveConfig is null");
                    return;
                }
                //resData.store.lotteryList.Add("lottery.VioletGashapon");
                //resData.lottery.sections[0].list.Add(new LotteryConfig { endTime = "", lotteryId = "lottery.bambooBasket", startTime = "", tag = "新" });
                LoggerUtils.Log("##RequestBusinessConfig Success："+arg0);
                //resData.lottery.sections[0].list.Add(new LotteryConfig { endTime = "", lotteryId = "lottery.yuanxiaoJiejiele", startTime = "", tag = "新" });
                InitData(resData);
                successAction?.Invoke(resData);
                _configUpdateAction?.Invoke(resData);
            },
            onFail: arg0 =>
            {
                HttpResponseFailDataStruct failRes = JsonConvert.DeserializeObject<HttpResponseFailDataStruct>(arg0);
                if (failRes != null && !string.IsNullOrEmpty(failRes.rmsg))
                {
                    failAction?.Invoke(failRes.rmsg);
                    LoggerUtils.LogError("RequestBusinessConfig Fail:"+failRes.rmsg);
                }
                else
                {
                    failAction?.Invoke(arg0);
                    LoggerUtils.LogError("RequestBusinessConfig Fail:"+ arg0);
                }
            },
            retryCount:3);
    }


    public void AddConfigUpdateListener(Action<BusinessLiveConfig> callback)
    {
        _configUpdateAction += callback;
    }

    public void RemoveConfigUpdateListener(Action<BusinessLiveConfig> callback)
    {
        _configUpdateAction -= callback;
    }

    private void OnTcpBrocastConfigUpdate()
    {
        LoggerUtils.Log("###OnTcpBrocastConfigUpdate");
        RequestBusinessConfig();
    }
}


public class StoreLiveConfig
{
    public List<string> lotteryList;
    public List<string> newLotteryList;
    public List<string> EncoreLotteryList;
}


[Serializable]
public class RechargeLiveConfig
{
    public List<string> paidPackageList;
    public List<string> limitPackageList;
    public List<string> activityList;
}

[Serializable]
public class ActivityLiveConfig
{
    public List<string> activityList;
}

[Serializable]
public class AvatarLiveConfig
{
    public List<string> seriesList;
}

[Serializable]
public class LotterysConfig
{
    public List<SectionsConfig> sections;
}

[Serializable]
public class SectionsConfig
{
    public string name;
    public List<LotteryConfig> list;
}

[Serializable]
public class LotteryConfig
{
    public string lotteryId;
    public string startTime;
    public string endTime;
    public string tag;
}

[Serializable]
public class BusinessLiveConfig
{
    public StoreLiveConfig store;
    public RechargeLiveConfig recharge;
    public ActivityLiveConfig activity;
    public AvatarLiveConfig avatar;
    public LotterysConfig lottery;
}
