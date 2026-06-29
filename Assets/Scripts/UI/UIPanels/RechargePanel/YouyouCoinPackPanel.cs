
using System.Collections.Generic;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;


public class YouyouCoinPackPanel : BasePanel<YouyouCoinPackPanel>
{
    public Button closeBtn;
    public Button reductBtn;
    public Button increaseBtn;
    public Button buyBtn;
    public Text limitPurchaseTxt;
    public Text priceTxt;
    public Image progressImg;

    private const float ProgressImgMaxWidth = 387f; // progressImg 的总长度
    private int currentNum = 1;
    private int maxNum;
    private BaseLimitPackageData _curData;

    public override void OnCreate()
    {
        base.OnCreate();
        
        closeBtn.onClick.AddListener(() =>
        {
            CloseSelf();
        });
        
        reductBtn.onClick.AddListener(() =>
        {
            if (currentNum <= 1) return;
            currentNum--;
            UpdateUI();
        });

        increaseBtn.onClick.AddListener(() =>
        {
            if (currentNum >= maxNum) return;
            currentNum++;
            UpdateUI();
        });
        
        
        buyBtn.onClick.AddListener(() =>
        {
            if (_curData == null)
            {
                return;
            }
            PurchaseOrder(_curData.limitPackageCurrencyType, currentNum);
        });
        
    }
    
    
    
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length > 0)
        {
            maxNum = (int)args[0];
            _curData = (BaseLimitPackageData)args[1];
        }

        SetupUI();
    }

    
    private void SetupUI()
    {
        currentNum = 1; // Reset to 1 when the panel is shown
        UpdateUI();
    }

    private void UpdateUI()
    {
        limitPurchaseTxt.text = $"限购 {currentNum}/{maxNum} 个";
        priceTxt.text = (currentNum * 10).ToString();
        
        UpdateProgressBar();
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
                int needNum = currentNum * 10 - AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
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
            ["productType"] = 7,
            ["productId"] = _curData.productId,
            ["purchaseAmount"] = currentNum
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyProductPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
        {
            BuyProductResult rsp = JsonConvert.DeserializeObject<BuyProductResult>(response);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            if (rsp != null && rsp.rewardList != null && rsp.rewardList.Count > 0)
            {
                ShowPackReward(0,rsp.rewardList[0].amount);
            }
            else
            {
                ShowPackReward();
            }
        }, (_) =>
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
        });
    }
    
    private void ShowPackReward(int randomLuckyAmount = 0, int randomYouYouAmount = 0)
    {
        var rewardItemDatas = new List<CommonRewardItemData>();
        foreach (var rewardData in _curData.rewardList)
        {
           
            var itemData = new CommonRewardItemData()
            {
                IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewardData.rewardType, gameObject),
                RewardAmount = rewardData.amount,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardData.rewardType)
            };
            if ((BUDRewardType)rewardData.rewardType == BUDRewardType.RewardYouYouCoin)
            {
                if (randomYouYouAmount != 0)
                {
                    itemData.RewardAmount = randomYouYouAmount;
                }
            } else if ((BUDRewardType)rewardData.rewardType == BUDRewardType.RewardLuckyCoin)
            {
                if (randomLuckyAmount != 0)
                {
                    itemData.RewardAmount = randomLuckyAmount;
                }
            }
            else if ((BUDRewardType)rewardData.rewardType == BUDRewardType.RewardGem)
            {
                var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.RewardAtlas);
                var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "ic_rewards_big_3", gameObject);
                itemData.IconSp = sprite;
            }
            
            rewardItemDatas.Add(itemData);
        }
        
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardItemDatas);
        
        ReddotManagerUtils.Inst.RefreshRedDot();
        BoardcastOnBuySuccess();
        CloseSelf();
    }
    
    private void BoardcastOnBuySuccess()
    {
        MessageHelper.Broadcast(MessageName.OnPurchaseLimitedPackageSuccess);   
    }

}
