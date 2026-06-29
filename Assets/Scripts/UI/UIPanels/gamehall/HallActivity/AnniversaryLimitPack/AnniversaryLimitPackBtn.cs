using System;
using System.Collections.Generic;
using Game.Event;
using Message;
using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AnniversaryLimitPackBtn : MonoBehaviour
{
    BudTimer _timer;

    public Text textName;

    private int _day = -1;
    public void Awake()
    {
        OnUpdateAnniversaryLimitPack();
        AnniversaryLimitPackMgr.Inst.GetActivityInfo();
        gameObject.SetActive(false);
        MessageHelper.AddListener(MessageName.UpdateAnniversaryLimitPack, OnUpdateAnniversaryLimitPack);
        RefreshUI();
        _timer = TimerManager.Inst.Run("AnniversaryLimitPackBtn", 0, 1, () =>
        {
            CheckTime();
        });
    }

    void CheckTime()
    {
        DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        if (now.Day != _day)
        {
            _day = now.Day;
            RefreshUI();
        }
        if (now > AnniversaryLimitPackMgr.DISCOUNT_END_TIME)
        {
            TimerManager.Inst.Stop(_timer);
            _timer = null;
        }
    }

    void RefreshUI()
    {
        if (textName != null)
        {
            if (AnniversaryLimitPackMgr.Inst.IsDuringDiscount())
            {
                textName.text = "限时秒杀";
            }
            else
            {
                textName.text = "萌萌哒套装";
            }
        }
    }

    public void OnDestroy()
    {
        TimerManager.Inst.Stop(_timer);
        _timer = null;
        MessageHelper.RemoveListener(MessageName.UpdateAnniversaryLimitPack, OnUpdateAnniversaryLimitPack);
    }

    public void OnClick()
    {
        //打开限时秒杀礼包界面
        AnniversaryLimitPackMgr.Inst.TrackAnalyticsData_Click();
        AnniversaryLimitPackMgr.Inst.OpenLimitPackWin();
    }

    void OnUpdateAnniversaryLimitPack()
    {
        gameObject.SetActive(AnniversaryLimitPackMgr.Inst.IsEntryOpen());
    }
}

