using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using Network.Http;
using Basic.Utils;
using EventTracking;
using System;
using Game.Event;
using UI.Base;


public class AnniversaryGiftView : BasePanel<AnniversaryGiftView>
{

    [Header("UI相关")]
    public CButton Btn_close;
    public CButton Btn_receive;
    public GameObject gray_Node;
    public GameObject receive_Node;
    public GameObject hadReceive_Go;
    public GameObject receive_Go;
    public Text Txt_countDown;
    public Button Btn_Preview1;
    public Button Btn_Preview2;
    public Button Btn_Preview3;

    private DateTime _activityCanReceiveTime = new DateTime(2025, 7, 29, 11, 0, 0);
    private BudTimer _timer;

    int _hour = -1;

    public GameObject boxPreviewGo;

    public override void OnCreate()
    {
        base.OnCreate();

        Btn_close.onClick.AddListener(OnCloseClicked);
        Btn_receive.onClick.AddListener(OnReceiveClicked);
        Btn_Preview1.onClick.AddListener(OnPreviewClick1);
        Btn_Preview2.onClick.AddListener(OnPreviewClick2);
        Btn_Preview3.onClick.AddListener(OnPreviewClick3);
        boxPreviewGo.SetActive(false);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        CheckTime();
        _timer = TimerManager.Inst.Run("AnniversaryGiftView", 0, 1, () =>
        {
            CheckTime();
        });
    }

    void CheckTime()
    {
        DateTime now = DateTime.Now;
        if (now < _activityCanReceiveTime)
        {
            if (now.Hour != _hour)
            {
                var day = (now - _activityCanReceiveTime).Duration().TotalDays;
                day = (int)day;
                var hour = (now - _activityCanReceiveTime).Duration().Hours % 24;
                _hour = now.Hour;
                Txt_countDown.text = $"{day}天{hour}小时后将收到周年赠礼";
                RefreshUI();
            }
        }
        else
        {
            TimerManager.Inst.Stop(_timer);
            _timer = null;
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        DateTime now = DateTime.Now;
        if (now < _activityCanReceiveTime)
        {
            gray_Node.SetActive(true);
            receive_Node.SetActive(false);
        }
        else
        {
            gray_Node.SetActive(false);
            receive_Node.SetActive(true);
            receive_Go.SetActive(true);
            hadReceive_Go.SetActive(false);
        }
        if (AnniversarySummerMgr.Inst.AnniversaryGiftClaimed)
        {
            receive_Go.SetActive(false);
            hadReceive_Go.SetActive(true);
        }
        else
        {
            receive_Go.SetActive(true);
            hadReceive_Go.SetActive(false);
        }
    }

    protected override void OnDestroy()
    {
        if (_timer != null)
        {
            TimerManager.Inst.Stop(_timer);
            _timer = null;
        }
        base.OnDestroy();
    }

    private void OnCloseClicked()
    {
        this.CloseSelf();
    }

    private void OnReceiveClicked()
    {
        if (!GlobalFuncExtensions.CheckCanClick())
        {
            return;
        }

        AnniversarySummerMgr.Inst.ClaimActivityReward(); 
    }

    void OnPreviewClick1(){
        var info = new RewardPreviewInfo(BUDRewardType.RewardPgcResource, CurrencyType.None, "40100509", "柔步轻拍舞", "");
        PreviewManager.Inst.ShowPreview(info);
    }
     void OnPreviewClick2(){
        var info = new RewardPreviewInfo(BUDRewardType.RewardChatBubbles, CurrencyType.None, "120100016", "", "");
        info.SetTitleAndDes("周年庆聊天气泡","通过周年庆周年赠礼活动获得，可前往个人资料使用");
        PreviewManager.Inst.ShowPreview(info);
    }
     void OnPreviewClick3(){
        boxPreviewGo.SetActive(true);

        // var info = new RewardPreviewInfo(BUDRewardType.RewardLuckyCoin, CurrencyType.LuckyCoin, "", "", "");
        // PreviewManager.Inst.ShowPreview(info);
    }





}
