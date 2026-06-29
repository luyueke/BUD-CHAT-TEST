using System.Collections;
using System.Collections.Generic;
using Game.Store;
using GameData.Gashapon;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class BaseGashaponView : MonoBehaviour {
    protected string gashaponId;
    protected GashaponStoreHandler dataHandler;
    protected GashaponData gashaponData;
    protected Transform panelTopContainer;
    protected GashaponInfoRsp gashaponInfoRsp;

    public const int OneTimeGasha = 1;
    public const int TemTimeGasha = 10;

    [SerializeField] private Text timeText;

    /// <summary>
    /// 设置Panel顶部容器
    /// </summary>
    public void SetPanelTopContainer(Transform container) {
        panelTopContainer = container;
    }

    public virtual void SetActivityTime(string startTime,string endTime)
    {
        if(timeText != null)
        {
            if(string.IsNullOrEmpty(endTime))
            {
                timeText.gameObject.SetActive(false);
            }else
            {
                timeText.gameObject.SetActive(true);
                timeText.text = string.Format("活动时间：{0}-{1}", startTime, endTime);
            }
         
        }
  
    }

    public virtual void OnCreate(string id) {
        gashaponId = id;
        dataHandler = AssetsDataManager.GetData<GashaponStoreHandler>();
        dataHandler.AddDataChange(gameObject, OnDataChange);
        gashaponData = GashaponDataManager.Inst.gashaponData(id);
    }

    /// <summary>
    /// 在View Activity 置为 true 之后，调用该方法
    /// </summary>
    public virtual void OnShow() {
        if(gashaponId == "lottery.musicalNoteSuit" || gashaponId == "lottery.VioletGashapon" || gashaponId == "lottery.musicalPuddingSuit" || gashaponId == "lottery.babyShrimpSuit")//这里三合一扭蛋，特殊处理一下
        {
            return;
        }
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
    }

    /// <summary>
    /// 在View Activity 置为 false 之前，调用该方法
    /// </summary>
    public virtual void OnHide() {
    }


    public virtual void OnDataChange(AssetsData[] changes) {
    }

    public virtual void OnGashaOnceRsp(GashaponRsp gashaponRsp) {
        OnGashaponInfoUpdate(gashaponRsp.lotteryInfo);
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
        //刷新UserInfo
        AccountDataManager.Inst.RefreshUserInfo();
    }

    public virtual void OnGashaTenRsp(GashaponRsp gashaponRsp) {
        OnGashaponInfoUpdate(gashaponRsp.lotteryInfo);
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
        //刷新UserInfo
        AccountDataManager.Inst.RefreshUserInfo();
    }

    protected virtual void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp) {
        gashaponInfoRsp = infoRsp;
    }

    //供老的扭蛋使用
    protected virtual void SendGashaponRequestOnce(GashaponData data) {
        if (data == null) {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)data.CurrencyType, data.SinglePrice)) {
            if (data.CurrencyType == CurrencyType.GreenCoin) {
                TipPanel.ShowToast("当前创作者币余额不足");
                return;
            }

            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)data.CurrencyType, data.SinglePrice);
            return;
        }

        var gId = data.Id;
        GashaponDataManager.Inst.RequestGashapon(gId, OneTimeGasha, OnGashaOnceRsp);
    }

    protected virtual void SendGashaponRequestOnce(GashaponData data, int singlePrice) {
        if (data == null) {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)data.CurrencyType, singlePrice)) {
            if (data.CurrencyType == CurrencyType.GreenCoin) {
                TipPanel.ShowToast("当前创作者币余额不足");
                return;
            }

            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)data.CurrencyType, singlePrice);
            return;
        }

        var gId = data.Id;
        GashaponDataManager.Inst.RequestGashapon(gId, OneTimeGasha, OnGashaOnceRsp);
    }

    //供老的扭蛋使用
    protected void SendGashaponRequestTenTimes(GashaponData data) {
        if (data == null) {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        int temPrice = GashaponUtils.GetRealTenPrice(data);
        if (!GashaponUtils.CurrencyIsEnough((int)data.CurrencyType, temPrice)) {
            if (data.CurrencyType == CurrencyType.GreenCoin) {
                TipPanel.ShowToast("当前创作者币余额不足");
                return;
            }

            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)data.CurrencyType, temPrice);
            return;
        }

        var gId = data.Id;
        GashaponDataManager.Inst.RequestGashapon(gId, TemTimeGasha, OnGashaTenRsp);
    }

    protected void SendGashaponRequestTenTimes(GashaponData data, int realPrice) {
        if (data == null) {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)data.CurrencyType, realPrice)) {
            if (data.CurrencyType == CurrencyType.GreenCoin) {
                TipPanel.ShowToast("当前创作者币余额不足");
                return;
            }

            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)data.CurrencyType, realPrice);
            return;
        }

        var gId = data.Id;
        GashaponDataManager.Inst.RequestGashapon(gId, TemTimeGasha, OnGashaTenRsp);
    }
}



