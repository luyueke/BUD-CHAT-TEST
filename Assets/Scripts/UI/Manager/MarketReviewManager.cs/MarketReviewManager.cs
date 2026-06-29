using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json.Linq;
using UnityEngine;
/// <summary>
/// 商店评分管理器
/// </summary>
public class MarketReviewManager : GlobalInstance<MarketReviewManager>
{
    #region NewBieSevenDayV3Task 累计活跃度首次满100时弹出
    bool canCheckMarketPointPanelByNewBieSevenDayV3Task = false; //是否需要检测累计活跃度  初始值超过100，不必要检测
    bool canShowMarketPointPanelByNewBieSevenDayV3Task = false;
    int oldNewBieSevenDayV3TaskNum = -1;

    int _isShowedPopupRating = 0; //是否弹出过评分弹窗 1:弹出过 0:未弹出
    public MarketReviewManager()
    {
        AccountDataManager.Inst.AddShowedPopupRatingChangeListener(OnShowedPopupRatingChange);
    }

    public void OnShowedPopupRatingChange(int showedPopupRating)
    {
        _isShowedPopupRating = showedPopupRating;
    }

    /// <summary>
    /// 记录累计活跃度，当第一次超过100后，记录可以弹出。在关闭相应奖励界面后弹出市场评分弹窗
    /// </summary>
    /// <param name="num"></param>
    public void CheckNewBieSevenDayV3TaskData(int num)
    {
        if (oldNewBieSevenDayV3TaskNum == -1)
        {
            if (num > 30)
            {
                canCheckMarketPointPanelByNewBieSevenDayV3Task = false;
                return;
            }
            canCheckMarketPointPanelByNewBieSevenDayV3Task = true;
            oldNewBieSevenDayV3TaskNum = num;
        }
        if (!canCheckMarketPointPanelByNewBieSevenDayV3Task)
        {
            return;
        }
        if (num >= 30)
        {
            canShowMarketPointPanelByNewBieSevenDayV3Task = true;
        }
    }


    public void CheckTaskV3IsShowMarketPointPanel()
    {
        if (canShowMarketPointPanelByNewBieSevenDayV3Task)
        {
            canCheckMarketPointPanelByNewBieSevenDayV3Task = false;
            canShowMarketPointPanelByNewBieSevenDayV3Task = false;
            ShowMarketPointPanel();
        }
    }
    #endregion

    public void CheckExitGameIsShowMarketPointPanel()
    {
        var playCnt = PlayerPrefs.GetInt("CheckExitGameIsShowMarketPointPanel", 0);
        playCnt++;
        PlayerPrefs.SetInt("CheckExitGameIsShowMarketPointPanel", playCnt);
        PlayerPrefs.Save();
        GetUgcInteractAmount();
        // if (playCnt == 3)
        // {
        // ShowMarketPointPanel();
        // }
    }

    public void CheckSaveOcListIsShowMarketPointPanel(int totalCount)
    {
        if (totalCount == 2)
        {
            ShowMarketPointPanel();
        }
    }


    public void ShowMarketPointPanel()
    {
        #if UNITY_EDITOR
        return;
        #endif
        // var hadEverShowMarketPointPanel = PlayerPrefs.GetInt("HadEverShowMarketPointPanel", 0);
        // if (hadEverShowMarketPointPanel == 1)
        // {
        //     return;
        // }
        // PlayerPrefs.SetInt("HadEverShowMarketPointPanel", 1);
        // PlayerPrefs.Save();
        if (_isShowedPopupRating == 1)
        {
            //弹出过了
            return;
        }
        string versionStr = DeviceInfoManager.Inst.DeviceBaseData.version;
        try
        {
            //需要大于1.0.16 版本号，否则不弹出
            Version version = new Version(versionStr);
            if (version.Major < 1 || (version.Major == 1 && version.Minor < 0) || (version.Major == 1 && version.Minor == 0 && version.Build <= 16))
            {
                return;
            }
        }
        catch (System.Exception e)
        {
            LoggerUtils.LogError("版本号解析失败:" + e.Message);
            return;
        }
        SetPopupRating();
        UIManager.Inst.OpenPanelTakeAni<MarketReviewPanel>(PanelId.MarketReviewPanel);
    }

    public void OpenAppStoreRating()
    {
#if UNITY_IOS
        // iOS: 使用深度链接打开应用商店
        // var appStore = "itms-apps://itunes.apple.com/app/apple-store/id6450975322";
        // #if PACKAGE_TYPE_US
        // appStore = "itms-apps://itunes.apple.com/app/apple-store/id1590291415";
        // #endif
        // Application.OpenURL(appStore);
        UnityEngine.iOS.Device.RequestStoreReview();
        // LoggerUtils.Log($"打开iOS应用商店: {appStore}");
#elif UNITY_ANDROID
        if (IAPDataManager.Inst.channelId == (int)IAPDataManager.ChannelIdEnum.Xiaomi)
        {
            try
            {
                // 尝试多种小米应用商店深度链接格式
                string packageName = "cn.budapp.biyoudideshijie.mi";
                string.Format("https://app.mi.com/details?id={0}", packageName);
                string url = string.Format("https://app.mi.com/details?id={0}", packageName);
                Application.OpenURL(url);
                LoggerUtils.Log($"尝试打开小米应用商店: {url}");
                return;
            }
            catch (Exception e)
            {
                LoggerUtils.LogError($"打开小米应用商店失败: {e.Message}");
            }
            return;
        }
        // Android: 使用原生接口打开应用商店
        try
        {
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openNativeMarketReview, "");
            LoggerUtils.Log("打开Android应用商店");
        }
        catch (Exception e)
        {
            LoggerUtils.LogError($"打开应用商店失败: {e.Message}");
        }
#else
        LoggerUtils.Log("当前平台不支持应用商店评分");
#endif
    }


    public void GetUgcInteractAmount()
    {
        var req = new JObject()
        {
            ["interactType"] = 1,
        };
        NetworkManager.Inst.SendHttpRequest<UgcInteractAmountRsp>(HttpUrlDefine.UgcInteractAmount, HttpMethod.GET,
          req,
          rsp =>
          {
              if (rsp == null)
              {
                  return;
              }
              LoggerUtils.Log("获取进入地图数据成功:" + rsp.amount);
              if (rsp.amount == 1)
              {
                  ShowMarketPointPanel();
              }
          }, errRsp =>
          {
              LoggerUtils.LogError("获取数据失败:" + errRsp.rmsg);
          });
    }

    /// <summary>
    /// 上报商城评分弹窗打开
    /// </summary>
    public void SetPopupRating()
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PopupRatingSet, HttpMethod.POST,
          "",
          rsp =>
          {
              _isShowedPopupRating = 1;
          },
          errRsp =>
          {
              LoggerUtils.LogError("设置数据失败:");
          });
    }
}


public class UgcInteractAmountRsp
{
    public int amount;
}

