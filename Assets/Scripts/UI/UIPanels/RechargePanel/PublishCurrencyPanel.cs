
using System;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;


public class PublishCurrencyPanel : BasePanel<PublishCurrencyPanel>
{

    public CButton closeBtn;
    public CButton pinkCoinBtn;
    public CButton gemBtn;
    public CButton confirmBtn;
    public GameObject cover;
    public GameObject pinkSelect;
    public GameObject gemSelect;

    private CurrencyType _curCurrencyType = CurrencyType.PinkCoin;
    private Action<CurrencyType> _action;

    public override void OnCreate()
    {
        bool enablePublishByGem = ContestDataManager.Inst.GetEnablePublishByGem();
        cover.gameObject.SetActive(!enablePublishByGem);
        
        closeBtn.onClick.AddListener((() =>
        {
            CloseSelf();
        }));
        
        pinkCoinBtn.onClick.AddListener((() =>
        {
            _curCurrencyType = CurrencyType.PinkCoin;
            gemSelect.gameObject.SetActive(false);
            pinkSelect.gameObject.SetActive(true);
        }));
        
        gemBtn.onClick.AddListener((() =>
        {
            if (!enablePublishByGem)
            {
                TipPanel.ShowToast("到达200粉丝解锁发布为钻石能力");
                return;
            }

            _curCurrencyType = CurrencyType.Gem;
            gemSelect.gameObject.SetActive(true);
            pinkSelect.gameObject.SetActive(false);
        }));
        
        confirmBtn.onClick.AddListener((() =>
        {
            PublishCurrencyManager.Inst.PublishCurrencyType = _curCurrencyType;
            if (this._action != null)
            {
                this._action.Invoke(_curCurrencyType);
            }
            CloseSelf();
        }));
    }

    public void SetCallback(Action<CurrencyType> action)
    {
        this._action = action;
    }
    
}