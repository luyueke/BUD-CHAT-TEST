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


public class SeasonPreBuyPreView : MonoBehaviour {

    public CButton closeBtn;

    private void Awake()
    {
        closeBtn.onClick.AddListener(OnClose);
    }

    private void OnClose()
    {
        gameObject.SetActive(false);
    }
}
