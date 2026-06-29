using System;
using System.Collections.Generic;
using EventTracking;
using Game.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;


public class SeasonPassCoinPanel : BasePanel<SeasonPassCoinPanel>
{
    public CButton closeBtn;
    public CButton convertBtn;
    public Text gemTxt;
    public Text roseCoinTxt;
    public Text convertNum;
    public Text buyNum;
    public CButton addBtn;
    public CButton maxBtn;
    public RectTransform bugLayout;
    public Image rewardIcon;
    public Image fromCurrencyIcon;
    public Image buyIcon;
    public Text title;
    public Text exchangeNum;
    public CButton decreaseBtn;
    private int currenCoverNum = 0;

    private bool isConverting = false;
    private float coverRate = 10;
    private CurrencyType _exchangeType;
    private CurrencyType _fromCurrencyType;
    private string spriteatlasPath = RechargePanel.RechargePanelAtlas;

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(() => { CloseSelf(); });
    }

    private int GetNearNum(int needNum)
    {
        // 若 兑换比例 < 1，例如 10:1 则返回当前值, 若比例>1, 则兑换最接近的值
        if (coverRate <= 1)
        {
            return needNum;
        }
        else
        {
            return Mathf.RoundToInt(Mathf.CeilToInt((needNum / coverRate)) * coverRate);
        }
    }

    public void SetData(CurrencyType exchangeType, CurrencyType fromCurrencyType = CurrencyType.Gem, int needNum = 0, Action onSuccess = null)
    {
        this._exchangeType = exchangeType;
        this._fromCurrencyType = fromCurrencyType;

        if (_fromCurrencyType == CurrencyType.Points)
        {
            fromCurrencyIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "points1_big", gameObject);
            buyIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "points1_small", gameObject);
        }

        switch (exchangeType)
        {
            case CurrencyType.Coin:
                {
                    Sprite rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "coin1_big", gameObject);
                    rewardIcon.sprite = rewardSp;
                    title.SetLocalText(fromCurrencyType == CurrencyType.Points ? "用积分兑换金币" : "用BUD钻兑换金币");
                    exchangeNum.text = "1:10";
                    coverRate = 10;
                    break;
                }
            case CurrencyType.Badge:
                {
                    Sprite rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "badge1", gameObject);
                    rewardIcon.sprite = rewardSp;
                    title.SetLocalText(fromCurrencyType == CurrencyType.Points ? "用积分兑换徽章" : "用BUD钻兑换徽章");
                    exchangeNum.text = "1:1";
                    coverRate = 1;
                    break;
                }
            case CurrencyType.PinkCoin:
                {
                    Sprite rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "pink_coin_big", gameObject);
                    rewardIcon.sprite = rewardSp;
                    title.SetLocalText(fromCurrencyType == CurrencyType.Points ? "用积分兑换社区商品币" : "用BUD钻兑换社区商品币");
                    exchangeNum.text = "1:1";
                    coverRate = 1;
                    break;
                }
            case CurrencyType.EnergyCoin:
                {
                    Sprite rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "icn_common_creator_big", gameObject);
                    rewardIcon.sprite = rewardSp;
                    title.SetLocalText("用BUD钻兑换创作能量币");
                    exchangeNum.text = "1:1";
                    coverRate = 1;
                    break;
                }
            case CurrencyType.LuckyCoin:
            case CurrencyType.MagicCoin:
            case CurrencyType.YouYouCoin:
            case CurrencyType.PurpleDreamCoin:
            case CurrencyType.ChristmasCoin:
                {
                    string spriteName = PgcUtils.CurrencyIconPath[exchangeType];
                    Sprite rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, spriteName, gameObject);
                    rewardIcon.sprite = rewardSp;
                    string coinName = PgcUtils.GetTokenName(exchangeType);
                    string coinStr = LocalizationManager.Inst.GetLocalizedText(coinName);
                    title.SetLocalText("用BUD钻兑换{0}", coinStr);
                    exchangeNum.text = "10:1";
                    coverRate = 0.1f;
                    break;
                }
            case CurrencyType.GiftTicket:
                {
                    string spriteName = PgcUtils.CurrencyIconPath[exchangeType];
                    Sprite rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, spriteName, gameObject);
                    rewardIcon.sprite = rewardSp;
                    string coinName = PgcUtils.GetTokenName(exchangeType);
                    string coinStr = LocalizationManager.Inst.GetLocalizedText(coinName);
                    title.SetLocalText("用BUD钻兑换{0}", coinStr);
                    exchangeNum.text = "1:1";
                    coverRate = 1f;
                    break;
                }
            case CurrencyType.SeasonPassCoin:
                {
                    string spriteName = PgcUtils.CurrencyIconPath[exchangeType];
                    Sprite rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, spriteName, gameObject);
                    rewardIcon.sprite = rewardSp;
                    string coinName = PgcUtils.GetTokenName(exchangeType);
                    string coinStr = LocalizationManager.Inst.GetLocalizedText(coinName);
                    title.SetLocalText("用BUD钻兑换{0}", coinStr);
                    exchangeNum.text = "2:1";
                    coverRate = 0.5f;
                    break;
                }
        }

        addBtn.onClick.AddListener(() =>
        {
            currenCoverNum += 10;
            gemTxt.text = "x" + currenCoverNum;
            convertNum.text = (currenCoverNum * coverRate).ToString();
            roseCoinTxt.text = "x" + currenCoverNum * coverRate;
            buyNum.text = currenCoverNum.ToString();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bugLayout);
        });
        decreaseBtn.onClick.AddListener(() =>
        {
            if (currenCoverNum <= 0)
            {
                return;
            }

            currenCoverNum -= 10;
            currenCoverNum = Math.Max(0, currenCoverNum);
            gemTxt.text = "x" + currenCoverNum;
            convertNum.text = (currenCoverNum * coverRate).ToString();
            roseCoinTxt.text = "x" + currenCoverNum * coverRate;
            buyNum.text = currenCoverNum.ToString();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bugLayout);
        });
        maxBtn.onClick.AddListener(() =>
        {
            var maxCoin = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
            if (_fromCurrencyType == CurrencyType.Points)
            {
                maxCoin = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Points);
            }

            var roseCoin = Mathf.FloorToInt(maxCoin * coverRate);
            maxCoin = Mathf.RoundToInt(roseCoin / coverRate);


            currenCoverNum = maxCoin;
            gemTxt.text = "x" + currenCoverNum;
            convertNum.text = (currenCoverNum * coverRate).ToString();
            roseCoinTxt.text = "x" + currenCoverNum * coverRate;
            buyNum.text = currenCoverNum.ToString();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bugLayout);
        });
        convertBtn.onClick.AddListener(() =>
        {
            int playerGemCoin = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
            //上报UGC商城埋点
            if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
            {
                LoadEvent.ReportPopupStatus("DiamondRedeemClicked", "ClickUGCDiamondRedeem");
            }
            if (currenCoverNum < 1)
            {
                return;
            }

            if (currenCoverNum > playerGemCoin)
            {
                int needNum = currenCoverNum - playerGemCoin;
                UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                CloseSelf();
                return;
            }
            //上报新玩家行为埋点
            if (SignInPanel.isNewPlayer)
            {
                LoadEvent.ReportPopupStatus("1", "trigger_pay");
            }
            PurchaseBadge(onSuccess);
        });
        SetDefaultValue(needNum);
    }

    private void SetDefaultValue(int needNum)
    {
        if (needNum > 0)
        {
            int nearNum = GetNearNum(needNum);
            currenCoverNum = Mathf.RoundToInt(nearNum / coverRate);
        }
        else
        {
            currenCoverNum += 10;
        }

        gemTxt.text = "x" + currenCoverNum;
        convertNum.text = (currenCoverNum * coverRate).ToString();
        roseCoinTxt.text = "x" + currenCoverNum * coverRate;
        buyNum.text = currenCoverNum.ToString();
        LayoutRebuilder.ForceRebuildLayoutImmediate(bugLayout);
    }

    public void PurchaseBadge(Action onSuccess = null, Action<string> onFail = null)
    {
        if (isConverting)
        {
            return;
        }

        isConverting = true;

        ExchangeReq exchangeReq = new ExchangeReq()
        {
            fromCurrency = (int)_fromCurrencyType,
            toCurrency = (int)_exchangeType,
            exchangeNum = currenCoverNum
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.exchangePay,
            HttpMethod.POST,
            JsonConvert.SerializeObject(exchangeReq),
            (message =>
            {
                isConverting = false;
                AccountDataManager.Inst.BalanceInfo.Refresh();
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(new List<TaskRewardData>()
                {
                    new TaskRewardData()
                    {
                        num = Mathf.RoundToInt(currenCoverNum * coverRate),
                        rewardType = _exchangeType switch
                        {
                            CurrencyType.Coin => (int)BUDRewardType.RewardCoin,
                            CurrencyType.Badge => (int)BUDRewardType.RewardBadge,
                            CurrencyType.PinkCoin => (int)BUDRewardType.RewardPinkCoin,
                            CurrencyType.EnergyCoin => (int)BUDRewardType.RewardEnergyCoin,
                            CurrencyType.LuckyCoin => (int)BUDRewardType.RewardLuckyCoin,
                            CurrencyType.ChristmasCoin => (int)BUDRewardType.RewardChristmasCoin,
                            CurrencyType.MagicCoin => (int)BUDRewardType.RewardMagicCoin,
                            CurrencyType.ChristmasTicket => (int)BUDRewardType.RewardChristmasTicket,
                            CurrencyType.CoinTicket => (int)BUDRewardType.RewardCoinTicket,
                            CurrencyType.PurpleDreamCoin => (int)BUDRewardType.RewardPurpleDreamCoin,
                            CurrencyType.YouYouCoin => (int)BUDRewardType.RewardYouYouCoin,
                            CurrencyType.GiftTicket => (int)BUDRewardType.RewardGiftTicket,
                            CurrencyType.SeasonPassCoin => (int)BUDRewardType.RewardSeasonPassCoin,
                            _ => (int)BUDRewardType.ErrRewardType
                        }

                    }
                });
                onSuccess?.Invoke();
                CloseSelf();
            }),
            (failData =>
            {
                isConverting = false;
                onFail?.Invoke(failData);
                HttpResponseRawData rsp = JsonConvert.DeserializeObject<HttpResponseRawData>(failData);
                if (rsp != null && !string.IsNullOrEmpty(rsp.rmsg))
                {
                    TipPanel.ShowToast(rsp.rmsg);
                }
            }));
    }

    #region Event Coin

    private Sprite eventCoinSprite;

    public void SetPianoCoinEventUI(Sprite coinSprite, String activityId, Action<int> onSuccess = null, string coinName = "钢琴活动币", float coinRate = 1)
    {
        eventCoinSprite = coinSprite;
        rewardIcon.sprite = coinSprite;
        string coinStr = LocalizationManager.Inst.GetLocalizedText(coinName);
        title.SetLocalText("用BUD钻兑换{0}", coinStr);
        if (coinRate > 1)
        {
            exchangeNum.text = $"1:{Mathf.RoundToInt(coinRate)}";
        }
        else
        {
            exchangeNum.text = $"{Mathf.RoundToInt(1 / coinRate)}:1";
        }
        coverRate = coinRate;

        addBtn.onClick.AddListener(() =>
        {
            currenCoverNum += 10;
            gemTxt.text = "x" + currenCoverNum;
            convertNum.text = (currenCoverNum * coverRate).ToString();
            roseCoinTxt.text = "x" + currenCoverNum * coverRate;
            buyNum.text = currenCoverNum.ToString();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bugLayout);
        });
        decreaseBtn.onClick.AddListener(() =>
        {
            if (currenCoverNum <= 0)
            {
                return;
            }

            currenCoverNum -= 10;
            currenCoverNum = Math.Max(0, currenCoverNum);
            gemTxt.text = "x" + currenCoverNum;
            convertNum.text = (currenCoverNum * coverRate).ToString();
            roseCoinTxt.text = "x" + currenCoverNum * coverRate;
            buyNum.text = currenCoverNum.ToString();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bugLayout);
        });

        maxBtn.onClick.AddListener(() =>
        {
            int playerGemCoin = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
            currenCoverNum = playerGemCoin;
            gemTxt.text = "x" + currenCoverNum;
            convertNum.text = (currenCoverNum * coverRate).ToString();
            roseCoinTxt.text = "x" + currenCoverNum * coverRate;
            buyNum.text = currenCoverNum.ToString();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bugLayout);
        });

        convertBtn.onClick.AddListener(() =>
        {
            int playerGemCoin = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
            if (currenCoverNum < 1)
            {
                return;
            }

            if (currenCoverNum > playerGemCoin)
            {
                int needNum = currenCoverNum - playerGemCoin;
                UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                CloseSelf();
                return;
            }

            PurchaseEventCoin(activityId, coinName, onSuccess);
        });
        SetDefaultValue(0);
    }


    public class ExchangeActivityRes
    {
        public int currencyAmount;
    }

    public void PurchaseEventCoin(string activityId, string coinName, Action<int> onSuccess = null, Action<string> onFail = null)
    {
        if (isConverting)
        {
            return;
        }

        isConverting = true;
        JObject obj = new JObject()
        {
            ["activityId"] = activityId,
            ["exchangeAmount"] = currenCoverNum
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ExchangeActivityCurrency,
            HttpMethod.POST,
            JsonConvert.SerializeObject(obj),
            (message =>
            {
                isConverting = false;
                AccountDataManager.Inst.BalanceInfo.Refresh();

                var data = JsonConvert.DeserializeObject<ExchangeActivityRes>(message);
                onSuccess?.Invoke(data.currencyAmount);

                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(new List<CommonRewardItemData>()
                {
                    new CommonRewardItemData()
                    {
                        IconSp = eventCoinSprite,
                        RewardAmount = Mathf.RoundToInt(currenCoverNum * coverRate),
                        rewardName = coinName
                    }
                });
                CloseSelf();
            }),
            (arg0 =>
            {
                isConverting = false;
                TipPanel.ShowToast(arg0);
            }));
    }

    #endregion
}
