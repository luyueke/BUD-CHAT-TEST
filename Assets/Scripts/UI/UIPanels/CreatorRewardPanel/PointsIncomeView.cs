using System;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.CreaterRewardPanel
{
    public class PointsIncomeView : MonoBehaviour
    {
        [SerializeField] internal Button backButton;
        [SerializeField] internal Text pointsBalance;
        [SerializeField] internal Text totalPoints;
        [SerializeField] internal Text exchangePoints;
        [SerializeField] internal CButton exchangePinkCoin;
        [SerializeField] internal CButton exchangeBadge;
        [SerializeField] internal CButton exchangeCoin;


        private Action<bool> _backAction;
        private bool isInit = false;
        private int allConsume;

        public void OnInitCreated(Action<bool> backAction)
        {
            if (isInit)
            {
                return;
            }

            isInit = true;
            this._backAction = backAction;
            backButton.onClick.AddListener(() => { _backAction?.Invoke(true); });
            exchangePinkCoin.onClick.AddListener(() =>
            {
                ExchangeCoinPanel exchangeCoinPanel =
                    UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                exchangeCoinPanel.SetData(CurrencyType.PinkCoin, CurrencyType.Points,0, () =>
                {
                    GetCreatorConsume();
                });
            });
            exchangeBadge.onClick.AddListener(() =>
            {
                ExchangeCoinPanel exchangeCoinPanel =
                    UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                exchangeCoinPanel.SetData(CurrencyType.Badge, CurrencyType.Points, 0, () =>
                {
                    GetCreatorConsume();
                });
            });
            exchangeCoin.onClick.AddListener(() =>
            {
                ExchangeCoinPanel exchangeCoinPanel =
                    UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                exchangeCoinPanel.SetData(CurrencyType.Coin, CurrencyType.Points, 0, () =>
                {
                    GetCreatorConsume();
                });
            });

            GetCreatorConsume();
        }

        private void GetCreatorConsume()
        {
            JObject req = new JObject()
            {
                ["currencyType"] = 7
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.creatorConsume, HttpMethod.GET,
                JsonConvert.SerializeObject(req),
                onReceive: arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<CreatorConsumeData>(arg0);
                    allConsume = data.allConsume;
                    SetConsume();
                },
                onFail: arg0 => { }
            );
        }

        private void SetConsume()
        {
            var num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Points);
            pointsBalance.text = MaxNum(num);
            totalPoints.text = MaxNum(num + allConsume);
            exchangePoints.text = MaxNum(allConsume);
        }

        private string MaxNum(int num)
        {
            if (num < 1000000)
            {
                return num.ToString();
            }
            else
            {
                return "999999";
            }
        }

        private void Awake()
        {
        }
    }
}