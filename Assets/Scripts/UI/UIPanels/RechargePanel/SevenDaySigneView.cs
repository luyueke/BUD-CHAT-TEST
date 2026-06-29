using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class SevenDaySigneView : MonoBehaviour
{
    int _signNumber;
    int _eventId;
    public Text needGem;
    Action _claimAction;
    public CButton signBtn;
    public CButton closeBtn;
    public CButton destroyBtn;
    private void Start()
    {
        signBtn.onClick.AddListener(() =>
        {
            SignedBtnClick();
        });
        closeBtn.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
        destroyBtn.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
    }


    public void  SetData(int signNumber, int eventId, Action claimAction)
    {
        _signNumber = signNumber;
        int needGemNumber = (_signNumber - 1) * 5;
        if (_signNumber < 2)//前两次免费
        {
            needGemNumber = 0;
        }
        needGem.text = needGemNumber.ToString();
        _eventId = eventId;
        _claimAction = claimAction;
    }



    private void SignedBtnClick()//补签
    {
        int needGem = (_signNumber - 1) * 5;

        if (_signNumber < 2)//前两次免费  0或1
        {
            needGem = 0;
        }
       // Debug.LogError( "Gem="+AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem));

        if (AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem) < needGem)
        {

            UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needGem);
        }
        else
    
        {
            JObject req = new JObject()
            {
                ["productType"] = 15,
                ["productId"] = _eventId.ToString(),
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyProductPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                _claimAction.Invoke();
                
            }, (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
            });

        }

    }
}
