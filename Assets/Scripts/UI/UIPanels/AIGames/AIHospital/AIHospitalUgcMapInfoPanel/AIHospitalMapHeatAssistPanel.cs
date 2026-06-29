using Network.Http;
using Network;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AIHospitalMapHeatAssistPanel : BasePanel<AIHospitalMapHeatAssistPanel>
{
    [SerializeField] private CButton _closeBtn;
    [SerializeField] private CButton _assistBtn;
    [SerializeField] private CButton _customNumBtn;
    [SerializeField] private Toggle[] _toggles;
    [SerializeField] private Text _customNumText;
    [SerializeField] private Text _addHeatText;
    [SerializeField] private Text _costNumText;
    [SerializeField] private ToggleGroup _toggleGroup;
    [SerializeField]private int _currentIndex = 0;
    [SerializeField] private List<int> _toggleDefaultValue=new List<int>()
    {
        30,60,120,240,300
    };

    private string _toUid = "";
    private string _mapID = "";
    private int _heatAmount = 0;

    private Action<int> callback;

    private class RewardCoinReq
    {
        public string ugcId;
        public int count;
    }

    /// <summary>
    /// 钻石换热度汇率
    /// </summary>
    private const int _currencyExchangeRate = 6;

    public override void OnCreate()
    {
        base.OnCreate();
        AddListener();
    }


    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length == 3)
        {
            _toUid = (string)args[0];
            _mapID = (string)args[1];
            _heatAmount = (int)args[2];
            _addHeatText.text = _heatAmount.ToString() + "+" + _toggleDefaultValue[0];
            _costNumText.text = (_toggleDefaultValue[0] / _currencyExchangeRate).ToString();
        }
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        RemoveListener();
    }

    public void AddListener()
    {
        _closeBtn.onClick.AddListener(OnCloseBtnClick);
        _assistBtn.onClick.AddListener(OnAssistBtnClick);
        _customNumBtn.onClick.AddListener(OnCustomNumClick);
        foreach (var toggle in _toggles)
        {
            int index = Array.IndexOf(_toggles, toggle);
            toggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(isOn, index));
            toggle.GetComponentInChildren<Text>().text = _toggleDefaultValue[index].ToString();
        }
    }

    public void RemoveListener()
    {
        _closeBtn.onClick.RemoveListener(OnCloseBtnClick);
        _assistBtn.onClick.RemoveListener(OnAssistBtnClick);
        _customNumBtn.onClick.RemoveListener(OnCustomNumClick);
        foreach (var toggle in _toggles)
        {
            toggle.onValueChanged.RemoveAllListeners();
        }
    }

    public void UpdateHeatText()
    {
        //int curMapHeat = 100;
        //todo 当前文本显示当前热度 "+" 即将获得的热度
        int increseNum = _currentIndex >= 0 ? _toggleDefaultValue[_currentIndex] : int.Parse(_customNumText.text);
        _addHeatText.text =  _heatAmount+ "+" + increseNum;
        UpdateCostNumText(increseNum);
    }



    public void UpdateCostNumText(int heatNum)
    {
        _costNumText.text = (heatNum/_currencyExchangeRate).ToString();
    }

    public void OnCustomNumClick()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("输入自定义数值"),
            inputMode = 2,
            maxLength = 4,
            inputFlag = 0,
            textSecurity = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = "30",
            returnKeyType = (int)ReturnType.Return,
            source = (int)KeyboardSource.Other
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }

    public void OnKeyboard(string value)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(value))
        {
            return;
        }
        if (InputCheck(value))
        {
            _customNumText.text = value;
            UpdateHeatText();
            _toggleGroup.SetAllTogglesOff(false);
            _currentIndex = -1;
        }
    }

    public bool InputCheck(string value)
    {
        int maxNum = 9999;
        //todo value需要是正整数，>0 且是6的倍数，且不允许大于maxNum
        if (int.TryParse(value, out int num))
        {
            //todo 拆分三个条件并且给出tips     
            if (num <= 0)
            {
                TipPanel.ShowToast("自定义输入的数量必须大于0");
                return false;
            }
            if (num % _currencyExchangeRate != 0)
            {
                TipPanel.ShowToast("自定义输入的数量必须是6的倍数");
                return false;
            }
            if (num > maxNum)   
            {
                TipPanel.ShowToast("自定义输入的数量超过了当前已有钻石数量");
                return false;
            }
            if (_toggleDefaultValue.Contains(num))
            {
                var index = _toggleDefaultValue.IndexOf(num);
                _toggles[index].isOn = true;
                return false;
            }
            return true;
        }
        TipPanel.ShowToast("自定义输入的数量必须是数字");
        return false;   
    }


    public void OnCloseBtnClick()
    {
        CloseSelf();
    }

    public void OnAssistBtnClick()
    {
        int costNum = (_currentIndex >= 0 ? _toggleDefaultValue[_currentIndex]:int.Parse(_customNumText.text))/_currencyExchangeRate;
        if (costNum <= 0)
        {
            return;
        }
        if (string.IsNullOrEmpty(_mapID))
        {
            Debug.LogError("打赏接口传入数据为空");
            CloseSelf();
            return;
        }
        var energyCoinCount = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
        if (costNum > energyCoinCount)
        {
            ExchangeCoinPanel badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
            badgePanel.SetData(CurrencyType.EnergyCoin, CurrencyType.Gem, costNum);
            return;
        }
        RewardCoinReq rewardCoinReq = new RewardCoinReq()
        {
            ugcId = _mapID,
            count = costNum
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.RewardCoin,
            HttpMethod.POST,
            JsonConvert.SerializeObject(rewardCoinReq),
            (message =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                TipPanel.ShowToast("打赏热度成功，感谢你的支持");
                this.callback?.Invoke(costNum*_currencyExchangeRate);
                CloseSelf();
            }),
            (arg0 =>
            {
                TipPanel.ShowToast(arg0);
            }));
    }

    public void OnToggleValueChanged(bool isOn,int index)
    {
         _currentIndex = index;
        if (isOn)
        {
            _customNumText.gameObject.SetActive(true);
        }
        else
        {
            _customNumText.gameObject.SetActive(false);
        }
        UpdateHeatText();
    }

    public  void SetCallback(Action<int> callback)
    {
        this.callback = callback;
    }
}
