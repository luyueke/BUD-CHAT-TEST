using System.Collections.Generic;
using Game.Store;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;


public class SpringLimitedDetailPanel : BasePanel<SpringLimitedDetailPanel>
{
    public Button closeBtn;
    public Button reductBtn;
    public Button increaseBtn;
    public Button buyBtn;
    public Button buyMaskBtn;
    public Text limitPurchaseTxt;
    public Text priceTxt;
    public Image progressImg;
    public CButton maxBtn;
    public Text mustHaveTxt;
    public Image ownedTag;

    public List<CButton> items;

    private const float ProgressImgMaxWidth = 387f; // progressImg 的总长度
    private int currentNum = 1;
    private int maxNum;

    private int gemPrice = 30;
    private NewYearLimitedPackageListItem _newYearLimitedPackageListItem;
    private string pgcId = "40900003";


    public override void OnCreate()
    {
        base.OnCreate();

        closeBtn.onClick.AddListener(() =>
        {
            CloseSelf();
        });

        reductBtn.onClick.AddListener(() =>
        {
            if (maxNum <= 0)
            {
                return;
            }
            if (currentNum <= 1) return;
            currentNum--;
            UpdateUI();
        });

        increaseBtn.onClick.AddListener(() =>
        {
            if (maxNum <= 0)
            {
                return;
            }
            if (currentNum >= maxNum) return;
            currentNum++;
            UpdateUI();
        });


        buyBtn.onClick.AddListener(() =>
        {
            if (maxNum <= 0)
            {
                return;
            }
            if (_newYearLimitedPackageListItem == null)
            {
                return;
            }

            PurchaseOrder((int)CurrencyType.Gem, currentNum);
        });

        maxBtn.onClick.AddListener(() =>
        {
            if (maxNum <= 0)
            {
                return;
            }
            currentNum = maxNum;
            UpdateUI();
        });

        for (int i = 0; i < items.Count; i++)
        {
            if (i == 0)
            {
                items[i].onClick.RemoveAllListeners();
                items[i].onClick.AddListener(() =>
                {
                    var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                    panel.SetNewyearEventPreview(new List<string>(){pgcId},"御剑飞行" , "","","新春限定", null);
                });
                continue;
            }

            CurrencyType currencyType = i switch
            {
                1 or 2 => CurrencyType.PurpleDreamCoin,
                3 or 4 => CurrencyType.YouYouCoin,
                _ => default
            };

            if (currencyType != default)
            {
                items[i].onClick.RemoveAllListeners();
                items[i].onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, currencyType);
                });
            }
        }
    }
    
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length > 0)
        {
            maxNum = (int)args[0];
            _newYearLimitedPackageListItem = (NewYearLimitedPackageListItem)args[1];
        }

        SetupUI();
    }


    private void SetupUI()
    {
        currentNum = 1; // Reset to 1 when the panel is shown
        UpdateUI();
        UpdateOwnedTag();
    }

    private void UpdateUI()
    {
        buyBtn.gameObject.SetActive(maxNum > 0);
        buyMaskBtn.gameObject.SetActive(maxNum <= 0);
        if (maxNum > 0)
        {
            limitPurchaseTxt.text = $"限购 {currentNum}/{maxNum} 个";
            mustHaveTxt.text = $"再开启{maxNum}次必得御剑飞行双人牵手动作";
            priceTxt.text = (currentNum * gemPrice).ToString();

            UpdateProgressBar();

            RefreshBuyBtnLayout();
        }
        else
        {
            limitPurchaseTxt.text = "限购 0/0 个";
            mustHaveTxt.text = "再开启0次必得御剑飞行双人牵手动作";
        }
    }

    private void RefreshBuyBtnLayout()
    {
        // 强制刷新 buyBtn 的布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(buyBtn.GetComponent<RectTransform>());
    }

    private void UpdateProgressBar()
    {
        if (progressImg != null)
        {
            float progressRatio = (float)currentNum / maxNum; // 计算比例
            float newWidth = progressRatio * ProgressImgMaxWidth; // 按比例计算新的宽度
            progressImg.rectTransform.sizeDelta = new Vector2(newWidth, progressImg.rectTransform.sizeDelta.y); // 更新宽度
        }
    }

    private void PurchaseOrder(int limitPackageCurrencyType, int currentNum)
    {
        switch (limitPackageCurrencyType)
        {
            case 3:
            {
                int needNum = currentNum * gemPrice -
                              AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
                if (needNum > 0)
                {
                    UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                    return;
                }

                break;
            }
        }

        JObject req = new JObject()
        {
            ["productType"] = (int)BUDProductType.FlySword,
            ["productId"] = _newYearLimitedPackageListItem.productId,
            ["purchaseAmount"] = currentNum
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyProductPay, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (response) =>
            {
                BuyProductResult rsp = JsonConvert.DeserializeObject<BuyProductResult>(response);
                AccountDataManager.Inst.BalanceInfo.Refresh();
                if (rsp != null && rsp.rewardList != null && rsp.rewardList.Count > 0)
                {
                    ShowPackReward(rsp.rewardList);
                }
            }, (_) => { AccountDataManager.Inst.BalanceInfo.Refresh(); });
    }

    private void ShowPackReward(List<LimitPackageRewardData> rewardList = null)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        var rewardItemDatas = new List<CommonRewardItemData>();
        foreach (var rewardData in rewardList)
        {
            Sprite rewardIconSp;
            var rewardType = rewardData.rewardType;
            rewardIconSp = rewardType != (int)BUDRewardType.RewardPgcResource ? PgcUtils.LoadRewardIcon((BUDRewardType)rewardData.rewardType, panel.gameObject) : PgcUtils.GetIconSpriteByPgcId(pgcId, panel.gameObject);
            var itemData = new CommonRewardItemData()
            {
                IconSp = rewardIconSp,
                RewardAmount = rewardData.amount,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardData.rewardType)
            };
            rewardItemDatas.Add(itemData);
        }

        panel.ShowRewards(rewardItemDatas);

        ReddotManagerUtils.Inst.RefreshRedDot();
        BoardcastOnBuySuccess();
        CloseSelf();
        UpdateOwnedTag();
    }

    private void UpdateOwnedTag()
    {
        bool isOwned = AssetsDataManager.IsOwned(pgcId);
        ownedTag.gameObject.SetActive(isOwned);
    }

    private void BoardcastOnBuySuccess()
    {
        MessageHelper.Broadcast(MessageName.OnPurchaseNewYearPackageSuccess);
    }
}