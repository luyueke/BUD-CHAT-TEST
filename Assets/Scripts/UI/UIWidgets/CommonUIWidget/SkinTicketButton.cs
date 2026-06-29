using Game.Database;
using Game.Store;
using GameUI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UI.BaseWidgets;

public class SkinTicketButton : CommonUIWidget
{
    private CButton Btn_Buy;
    private GoodsType _goodsType;
    private string _curId;
    private Action OnBuySuccess;
    private Action OnBuyFail;
    
    private void Awake()
    {
        Btn_Buy = this.GetComponent<CButton>();
        Btn_Buy.onClick.AddListener(OnBtnBuyClick);
    }

    /// <summary>
    /// 所需要的参数 args[0] CurrencyType args[1] GoodsType args[2] ugcId 
    /// </summary>
    /// <param name="args"></param>
    public override void SetData(params object[] args)
    {
        base.SetData(args);
        if (args.Length >= 2)
        {
            _goodsType = (GoodsType)args[0];
            _curId = (string)args[1];
        }
    }

    public void SetAction(Action succ = null, Action fail = null)
    {
        OnBuySuccess = succ;
        OnBuyFail = fail;
    }

    private void OnBtnBuyClick()
    {
        var panel = UIManager.Inst.OpenPanel<UseTicketConfirmPanel>(PanelId.UseTicketConfirmPanel, CurrencyType.Ticket);
        panel.SetConfimAction(() =>
        {
            Buy(_goodsType);
        });
    }

    private void Buy(GoodsType goodsType)
    {
        switch (goodsType)
        {
            case GoodsType.SingleUgc:
                JObject UgcReq = new JObject()
                {
                    ["ugcId"] = _curId,
                    ["useVoucher"] = true,
                };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyUgcPay, HttpMethod.POST, JsonConvert.SerializeObject(UgcReq), (_) =>
                {
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    OnBuySuccess?.Invoke();
                    UIManager.Inst.ClosePanel(PanelId.UseTicketConfirmPanel);
                    var rsp = JsonConvert.DeserializeObject<ServerBagUpdateDataRsp>(_);
                    TimeLimitGiftSystem.Inst.SetTriggerTime(rsp.popupType);
                }, (_) =>
                {
                    OnBuyFail?.Invoke();
                });
                break;
            
            case GoodsType.SinglePgc:
                JObject req = new JObject()
                {
                    ["pgcId"] = _curId,
                    ["useVoucher"] = true,
                };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyPgcPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
                {
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    OnBuySuccess?.Invoke();
                }, (_) =>
                {
                    OnBuyFail?.Invoke();
                });
                break;
        }
    }
}
