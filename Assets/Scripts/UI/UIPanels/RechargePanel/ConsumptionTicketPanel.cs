using Basic.Utils;
using EventTracking;
using Game.Database;
using Game.Event;
using Game.Store;
using GameData.Manager;
using GameUI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class ConsumptionTicketConfig
{
    public CurrencyType type;
    public string pgcId;
    public Action ClaimCallBack;
}
public class ConsumptionTicketPanel : BasePanel<ConsumptionTicketPanel>
{
    public Image icon;
    public Text des;
    public Button closeBtn;
    public Button configBtn;
    public HorizontalLayoutGroup layout;
    ConsumptionTicketConfig _config;
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if(args!=null && args.Length > 0)
        {
            _config = (ConsumptionTicketConfig)args[0];
        }
        UpdateView();
    }
    public void UpdateView()
    {
        configBtn.onClick.AddListener(ConfigBtnClick);
        closeBtn.onClick.AddListener(CloseBtnClick);
        TokenDataManager.Inst.GetTokenData((tokenData) => {
            icon.sprite = PgcUtils.LoadCurrencyIcon((int)_config.type, icon.gameObject);
            des.text = $"当前剩余{tokenData.GetToken(_config.type)}张{PgcUtils.GetTokenName(_config.type)}";
            layout.spacing = 1;

        }, (fil) => {
            icon.sprite = PgcUtils.LoadCurrencyIcon((int)_config.type, icon.gameObject);
        });
    }
    void ConfigBtnClick()
    {
        BuyUgc(_config.pgcId);
    }
    void CloseBtnClick()
    {
        CloseSelf();
    }
    public void BuyUgc(string ugcId)
    {

        JObject req = new JObject()
        {
            ["ugcId"] = ugcId,
            ["useVoucher"] = true
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyUgcPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            TokenDataManager.Inst.GetTokenData((TokenData)=> {
                _config.ClaimCallBack?.Invoke();
                CloseSelf();
            });
            var rsp = JsonConvert.DeserializeObject<ServerBagUpdateDataRsp>(_);
            TimeLimitGiftSystem.Inst.SetTriggerTime(rsp.popupType);
        }, (_) =>
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            _config.ClaimCallBack?.Invoke();
            CloseSelf();
        });
    }
    

}