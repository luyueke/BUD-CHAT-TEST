using System;
using System.Collections.Generic;
using Game.Event;
using Message;
using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AnniversaryLimitPackView : BasePanel<AnniversaryLimitPackView>
{
    public CButton btn_close;
    public CButton btn_buy;
    public Button[] previewBtns;
    public GameObject discount_go;
    public GameObject normal_go;

    private BudTimer _timer;
    private int _day = -1;


    protected override void Awake()
    {
        base.Awake();
        AnniversaryLimitPackMgr.Inst.TrackAnalyticsData_Show();

        btn_close.onClick.AddListener(CloseSelf);
        btn_buy.onClick.AddListener(OnBuy);
        MessageHelper.AddListener(MessageName.UpdateAnniversaryLimitPack, RefreshUI);

        CheckTime();
        _timer = TimerManager.Inst.Run("AnniversaryLimitPackView", 0, 1, () =>
        {
            CheckTime();
        });
        RefreshUI();

    }

    void CheckTime()
    {
        DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        if (now < AnniversaryLimitPackMgr.DISCOUNT_END_TIME)
        {
            if (now.Day != _day)
            {
                _day = now.Day;
                AnniversaryLimitPackMgr.Inst.GetActivityInfo();
            }
        }
        else
        {
            TimerManager.Inst.Stop(_timer);
            _timer = null;
        }
    }


    void RefreshUI()
    {
        try
        {
            var isDuringDiscount = AnniversaryLimitPackMgr.Inst.IsDuringDiscount();
            var hasBuy = AnniversaryLimitPackMgr.Inst.CheckHasBuy();
            btn_buy.gameObject.SetActive(!hasBuy);
            discount_go?.SetActive(isDuringDiscount && !hasBuy);
            normal_go?.SetActive(!isDuringDiscount && !hasBuy);
        }
        catch (System.Exception e)
        {
            Debug.LogError("refresh ui error" + e.Message);
        }

    }

    void OnBuy()
    {
        AnniversaryLimitPackMgr.Inst.BuyPackage();
    }

    protected override void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.UpdateAnniversaryLimitPack, RefreshUI);
        TimerManager.Inst.Stop(_timer);
        _timer = null;
        base.OnDestroy();
    }



}

