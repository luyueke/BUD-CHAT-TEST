using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;
using GameData.Gashapon;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.UIPanels.GashaponPanel;
using UnityEngine;



/// <summary>
/// 启用，使用 BaseGashaponView
/// </summary>
[Obsolete("弃用，使用 BaseGashaponVie替代")]
public class GashaponBaseView : MonoBehaviour
{
    protected GashaponCustomPanel mainPanel;
    protected GashaponStoreHandler dataHandler;

    public const int OneTimeGasha = 1;
    public const int TemTimeGasha = 10;

    public void SetMainPanel(GashaponCustomPanel panel)
    {
        mainPanel = panel;
    }

    public virtual void OnCreate(string gashaponId)
    {
        dataHandler = AssetsDataManager.GetData<GashaponStoreHandler>();
        dataHandler.AddDataChange(gameObject, OnDataChange);
    }


    public virtual void OnDataChange(AssetsData[] changes) {}

    public virtual void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
    }

    public virtual void OnGashaTenRsp(GashaponRsp gashaponRsp)
    {
    }

    //供老的扭蛋使用
    protected virtual void SendGashaponRequestOnce(GashaponData gashaponData)
    {
        if (gashaponData == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        if(!GashaponUtils.CurrencyIsEnough((int)gashaponData.CurrencyType,gashaponData.SinglePrice))
        {
            if (gashaponData.CurrencyType == CurrencyType.GreenCoin)
            {
                TipPanel.ShowToast("当前创作者币余额不足");
                return;
            }
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)gashaponData.CurrencyType,gashaponData.SinglePrice);
            return;
        }
        var gId = gashaponData.Id;
        GashaponDataManager.Inst.RequestGashapon(gId, OneTimeGasha, OnGashaOnceRsp);
    }

    protected virtual void SendGashaponRequestOnce(GashaponData gashaponData,int singlePrice)
    {
        if (gashaponData == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        if(!GashaponUtils.CurrencyIsEnough((int)gashaponData.CurrencyType,singlePrice))
        {
            if (gashaponData.CurrencyType == CurrencyType.GreenCoin)
            {
                TipPanel.ShowToast("当前创作者币余额不足");
                return;
            }
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)gashaponData.CurrencyType,singlePrice);
            return;
        }
        var gId = gashaponData.Id;
        GashaponDataManager.Inst.RequestGashapon(gId, OneTimeGasha, OnGashaOnceRsp);
    }

    //供老的扭蛋使用
    protected void SendGashaponRequestTenTimes(GashaponData gashaponData)
    {
        if (gashaponData == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        int temPrice = GashaponUtils.GetRealTenPrice(gashaponData);
        if(!GashaponUtils.CurrencyIsEnough((int)gashaponData.CurrencyType,temPrice))
        {
            if (gashaponData.CurrencyType == CurrencyType.GreenCoin)
            {
                TipPanel.ShowToast("当前创作者币余额不足");
                return;
            }
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)gashaponData.CurrencyType,temPrice);
            return;
        }
        var gId = gashaponData.Id;
        GashaponDataManager.Inst.RequestGashapon(gId, TemTimeGasha, OnGashaTenRsp);
    }

    protected void SendGashaponRequestTenTimes(GashaponData gashaponData,int realPrice)
    {
        if (gashaponData == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        if(!GashaponUtils.CurrencyIsEnough((int)gashaponData.CurrencyType,realPrice))
        {
            if (gashaponData.CurrencyType == CurrencyType.GreenCoin)
            {
                TipPanel.ShowToast("当前创作者币余额不足");
                return;
            }
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)gashaponData.CurrencyType,realPrice);
            return;
        }
        var gId = gashaponData.Id;
        GashaponDataManager.Inst.RequestGashapon(gId, TemTimeGasha, OnGashaTenRsp);
    }

}
