using System;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;


public class SendCoinPanel : BasePanel<SendCoinPanel>
{
    [SerializeField] private CButton closeBtn;
    [SerializeField] private CButton publishBtn;
    [SerializeField] private Toggle priceToggle20;
    [SerializeField] private Toggle priceToggle30;
    [SerializeField] private Toggle priceToggle50;
    [SerializeField] private Toggle priceToggle55;
    [SerializeField] private Toggle priceToggle75;
    [SerializeField] private CButton tipsBtn;
    [SerializeField] private AccountWidget accountWidget;
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private CButton priceEditBtn;

    private Dictionary<int, Toggle> priceToggleDic;


    private Action<int> callback;

    private string toUid;
    private string ugcId;
    private KeyBoardInfo priceKeyBoardInfo;
    private int price;


    public override void OnCreate()
    {
        base.OnCreate();
        priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);
        closeBtn.onClick.AddListener(() => { CloseSelf(); });
        publishBtn.onClick.AddListener(OnPublishBtnClicked);
        tipsBtn.onClick.AddListener(() =>
        {
            int energyCoinNum = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.EnergyCoin);
            var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
            // var ownedStr = $"当前拥有：{energyCoinNum}";
            string desc = "通过钻石兑换或者抽取金币/徽章盲盒获取，可用于对作者进行投币支持，投币的50%将作为作者的创作者币奖励";
            panel.UpdateUI(iconSprite, "创作能量币", energyCoinNum.ToString(), desc);
        });

        priceToggle20.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnPriceToggleClick(20);
            }
        });
        priceToggle30.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnPriceToggleClick(30);
            }
        });
        priceToggle50.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnPriceToggleClick(50);
            }
        });
        priceToggle55.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnPriceToggleClick(55);
            }
        });
        priceToggle75.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnPriceToggleClick(75);
            }
        });

        priceToggleDic = new Dictionary<int, Toggle>()
        {
            { 20, priceToggle20 },
            { 30, priceToggle30 },
            { 50, priceToggle50 },
            { 55, priceToggle55 },
            { 75, priceToggle75 }
        };

        priceKeyBoardInfo = new KeyBoardInfo()
        {
            type = 0,
            placeHolder = "最低价是 10",
            inputMode = 1,
            maxLength = 4,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = "请输入一个介于10和9999之间的整数。",
            defaultText = "10",
            returnKeyType = (int)ReturnType.Return
        };
        OnPriceToggleClick(20);

    }


    private void OnPriceEditBtnClick()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetPriceFromNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(priceKeyBoardInfo));
    }

    private void OnGetPriceFromNative(string price)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (int.TryParse(price, out var value))
        {
            OnPriceToggleClick(value);
        }
    }

    private void OnPriceToggleClick(int value)
    {
        if ((value < 10 || value > 9999))
        {
            TipPanel.ShowToast("请输入一个介于10和9999之间的整数。");
            return;
        }
        this.price = value;

        bool isCustom = false;
        Toggle findToggle;

        isCustom = !priceToggleDic.TryGetValue(value,
            out var toggle);
        findToggle = toggle;
        if (isCustom)
        {
            priceToggle20.group.SetAllTogglesOff(false);
            priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(true);
            priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(false);
            priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(true);
            priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>().text =
                value.ToString();
            priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite =
                PgcUtils.LoadCurrencyIcon(CurrencyType.EnergyCoin, gameObject);
        }
        else
        {
            findToggle.SetIsOnWithoutNotify(true);
            priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
            priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
            priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
        }
    }

    private void OnPublishBtnClicked()
    {
        if (this.price <= 0)
        {
            return;
        }
        if(string.IsNullOrEmpty(toUid)  && string.IsNullOrEmpty(ugcId))
        {
            Debug.LogError("打赏接口传入数据为空");
            CloseSelf();
            return;
        }
        var energyCoinCount = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.EnergyCoin);
        if (this.price > energyCoinCount)
        {
            ExchangeCoinPanel badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
            badgePanel.SetData(CurrencyType.EnergyCoin, CurrencyType.Gem, this.price);
            return;
        }
        RewardCoinReq rewardCoinReq = new RewardCoinReq()
        {
            toUid = toUid,
            ugcId = ugcId,
            count = this.price
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.RewardCoin,
            HttpMethod.POST,
            JsonConvert.SerializeObject(rewardCoinReq),
            (message =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                TipPanel.ShowToast("投币成功，感谢你的支持");
                this.callback?.Invoke(this.price);
                CloseSelf();
            }),
            (arg0 =>
            {
                TipPanel.ShowToast(arg0);
            }));
    }

    public void SetCallback(Action<int> callback)
    {
        this.callback = callback;
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        this.toUid = args[0] as string;
        if (args.Length > 1) {
            this.ugcId = args[1] as string;
        }
    }

    public class RewardCoinReq
    {
        public string toUid;
        public string ugcId;
        public int count;
    }
}
