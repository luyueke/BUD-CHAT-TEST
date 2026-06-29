using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Message;

public class HalloweenLimitPackBtn : MonoBehaviour
{
    BudTimer _timer;

    private DateTime _nowTime;

    private bool _openStatus;
    
    public void Awake()
    {
        _openStatus = true;
        HalloweenLimitPackMgr.Inst.GetActivityInfo();
        MessageHelper.AddListener(MessageName.UpadateHalloweenLimitPack, OnUpdateHalloweenLimitPack);
        MessageHelper.AddListener(MessageName.TcpTimeUpdate, OnShow);
    }

    void OnShow()
    {
        _timer = TimerManager.Inst.Run("HalloweenLimitPackBtn", 0, 1, () =>
        {
            CheckTime();
        });
    }
    
    void CheckTime()
    {
        _nowTime = TcpTimeSystem.Inst.ServerDataTime;
        if (_nowTime > HalloweenLimitPackMgr.DISCOUNT_END_TIME || _nowTime < HalloweenLimitPackMgr.DISCOUNT_START_TIME)
        {
            TimerManager.Inst.Stop(_timer);
            _timer = null;
            _openStatus = false;
            OnUpdateHalloweenLimitPack();
            //todo如果这里打开了活动界面，要进行关闭
        }
    }
    
    public void OnDestroy()
    {
        TimerManager.Inst.Stop(_timer);
        _timer = null;
        MessageHelper.RemoveListener(MessageName.UpadateHalloweenLimitPack, OnUpdateHalloweenLimitPack);
        MessageHelper.RemoveListener(MessageName.TcpTimeUpdate, OnShow);
    }
    
    
    void OnUpdateHalloweenLimitPack()
    {
        if (!_openStatus)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(HalloweenLimitPackMgr.Inst.IsEntryOpen());
    }
    
    public void OnClick()
    {
        //打开万圣节限定界面
        HalloweenLimitPackMgr.Inst.OpenLimitPackWin();
    }
}
