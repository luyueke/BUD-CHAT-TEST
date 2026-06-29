using System;
using System.Collections.Generic;
using System.Linq;
using Basic.Utils;
using Game.Store;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;


public class SeasonPrePackView : MonoBehaviour
{
    /// <summary>
    /// 新赛季目标时间：2026年3月27日上午11点
    /// </summary>
    private static readonly DateTime SeasonTargetTime = new DateTime(2026, 5, 22, 11, 0, 0);

    [SerializeField] private Text djsTxt;

    public List<SeasonPrePackItem> rewardItems;

    private void Start()
    {
        InitItems();
    }

    private void OnEnable()
    {
        RefreshCountdown();
        InvokeRepeating(nameof(RefreshCountdown), 10f, 10f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RefreshCountdown));
    }


    private void RefreshCountdown()
    {
        if (djsTxt == null) return;

        var now = TcpTimeSystem.Inst.ServerDataTime;
   //     Debug.LogError("RefreshCountdown now="+now);
        if (now >= SeasonTargetTime)
        {
            djsTxt.text = "新赛季已开启";
            return;
        }

        var left = SeasonTargetTime - now;
        int days = left.Days;
        int hours = left.Hours;
        int minutes = left.Minutes;
        djsTxt.text = $"新赛季倒计时 {days}天{hours}小时{minutes}分";
    }

    private void InitItems()
    {
        for (int i = 0; i < rewardItems.Count; i++)
        {
            rewardItems[i].SetData(i);
        }
    }
}
