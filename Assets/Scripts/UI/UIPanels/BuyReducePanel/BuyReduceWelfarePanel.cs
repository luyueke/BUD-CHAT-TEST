using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;
using Fsbm.Runtime;
using UI.Manager;
using GameData.Manager;

public class BuyReduceWelfarePanel : MonoBehaviour
{
    [SerializeField] private RawImage BackGround;
    [SerializeField] private CButton ShowBtn;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private Text Price;
    [SerializeField] private Transform OverTips;
    [SerializeField] private Text OverSavePrice;
    [SerializeField] private Transform DisCountTips;
    [SerializeField] private Text NeedBuyNum;
    [SerializeField] private Text SaveGemNum;
    [SerializeField] private Transform RedPoint;
    [SerializeField] private Text RedPointCount;
    [SerializeField] private Transform OneLock;
    [SerializeField] private Transform SecLock;
    [SerializeField] private Transform ThirdLock;
    [SerializeField] private Toggle ReduceToggle0;
    [SerializeField] private Toggle ReduceToggle1;
    [SerializeField] private Toggle ReduceToggle2;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform itemParent;

    //[SerializeField] private List<BuyReduceItem> buyReduceItemList;
    private List<BuyReduceItem> activeItems = new List<BuyReduceItem>();

    private List<BuyDiscountData> buyReduceDatas = new List<BuyDiscountData>();

    private List<DiscountItemInfo> discountItemInfos = new List<DiscountItemInfo>();

    private int selectType = 0;

    private int carItemPrice = 0;


    private string rewardId = "40100531";
    //public static RawImage ShowItemBg;

    public void Awake()
    {
      //  ShowItemBg = BackGround;
        ShowBtn.onClick.AddListener(() =>
        {
            string rewardName = Es.DataTables.GetPgcNameData(rewardId)?.Name;
            var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
            panel.SetEventPreview(new List<string>() { rewardId }, rewardName, "", "", "", "", BackGround.texture);
        });
        BuyBtn.onClick.AddListener(() =>
        {
            if(carItemPrice <= 0)
            {
                return;
            }

            var hintPanel = UIManager.Inst.OpenPanel<BuyReduceHintPanel>(PanelId.BuyReduceHintPanel, Price.text);
            hintPanel.EnterCallBack = BuyReduceEventLister;
        });
        ReduceToggle0.onValueChanged.AddListener(OnceToggleClick);
        ReduceToggle1.onValueChanged.AddListener(SecToggleClick);
        ReduceToggle2.onValueChanged.AddListener(ThirdToggleClick);
        RefreshDataFromServer();
    }

    private void ShowRewardPanel(DiscountItemInfo packageData)
    {
        switch ((BUDRewardType)packageData.rewardType)
        {
            case BUDRewardType.RewardAvatarFrame:
                HeadCycleData headData = UserUIWidgetManager.Inst.GetHeadCycleDataById(packageData.avatarFrameType);
                if (headData != null)
                {
                    PreviewManager.Inst.ShowAvatarFramePreview(headData.Id);             
                }
                break;
            case BUDRewardType.RewardHomepageSkin:
                UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, packageData.homepageSkinType);
                break;
            case BUDRewardType.RewardChatBubbles:
                GameChatBubbleData chatData = UserUIWidgetManager.Inst.GetChatDataByID(packageData.chatBubblesType);
                if (chatData != null)
                {
                    var tem = new RewardPreviewInfo(BUDRewardType.RewardChatBubbles, CurrencyType.None, chatData.PgcId, chatData.Name, "");
                    tem.SetTitleAndDes(chatData.Name, chatData.Desc);
                    PreviewManager.Inst.ShowPreview(tem);
                }
                break;
            case BUDRewardType.RewardLuckyCoin:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardLuckyCoin, CurrencyType.LuckyCoin, "", "", ""));
                break;
            case BUDRewardType.RewardYouYouCoin:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""));
                break;
            case BUDRewardType.RewardPurpleDreamCoin:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardPurpleDreamCoin, CurrencyType.PurpleDreamCoin, "", "", ""));
                break;
            case BUDRewardType.RewardCommunityAnimationTicket:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardCommunityAnimationTicket, CurrencyType.CommunityAnimationTicket, "", "", ""));
                break;
            case BUDRewardType.RewardCommunityInstrumentTicket:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardCommunityAnimationTicket, CurrencyType.CommunityInstrumentTicket, "", "", ""));
                break;
            case BUDRewardType.RewardCommunitySkinTicket:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardCommunitySkinTicket, CurrencyType.CommunitySkinTicket, "", "", ""));
                break;
            case BUDRewardType.RewardUGCVehicleTicket:
                PreviewManager.Inst.ShowPreview(new RewardPreviewInfo(BUDRewardType.RewardUGCVehicleTicket, CurrencyType.CommunityVehicleTicket, "", "", ""));
                break;
            case BUDRewardType.RewardPgcResource:
                var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                panel.SetEventPreview(packageData.pgcIds, packageData.name, "", "", "", null, BackGround.texture);
                break;
            case BUDRewardType.RewardPgcBundle:
                var panel1 = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                panel1.SetEventPreview( packageData.pgcIds , packageData.name, "Bundle_"+packageData.bundleId, XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle), "", "", BackGround.texture);
                break;
        }
    }

    private void BuyReduceEventLister()
    {
        if (carItemPrice < 200)
        {
            selectType = 0;
        }
        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
        int price = 0;
        switch (selectType)
        {
            case 0:
                price = carItemPrice;
                break;
            case 1:
                price = carItemPrice - 30;
                break;
            case 2:
                price = carItemPrice - 50;
                break;
            case 3:
                price = carItemPrice - 150;
                break;
        }
        if(num < price)
        {
            UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, price - num);
            //var panel = UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel);
            //panel.OnTabClick(RechargeId.GemPack);
        }
        else
        {
            IAPDataManager.Inst.BuyDiscountListItem(selectType, buyReduceDatas, BuyOverRefresh);
        }
    }

    private void RefreshDataFromServer()
    {
        IAPDataManager.Inst.GetPayDiscountInfo(res =>
        {
            if (res != null && res.list != null)
            {
                RefreshData(res.list);
            }
        });
    }

    private void RefreshData(List<DiscountItemInfo> packageDatas)
    {
        buyReduceDatas.Clear();
        discountItemInfos.Clear();
        RedPoint.gameObject.SetActive(false);
        OverTips.gameObject.SetActive(false);
        //DisCountTips.gameObject.SetActive(false);
        Price.text = " 购买";

        // 回收所有现有的 items
        RecycleAllItems();

        // 使用对象池获取新的 items
        for (int i = 0; i < packageDatas.Count; i++)
        {
            discountItemInfos.Add(packageDatas[i]);
            BuyReduceItem item = PoolManager.Instance.GetGameObject<BuyReduceItem>(itemPrefab, itemParent);
            if (item != null)
            {
                item.gameObject.SetActive(true);
                var icon = GetIconSprite(packageDatas[i]);
                item.SetData(packageDatas[i], ShowRewardPanel,icon);
                item.clickCallBack = BuyCarChangeUpdate;
                activeItems.Add(item);
            }
        }
       var rectTrans =  itemParent.GetComponent<RectTransform>();
        var newSize = rectTrans.sizeDelta;
        newSize.y =(float) (354 * activeItems.Count) / 5;
        rectTrans.sizeDelta = newSize;
    }

    private void RecycleAllItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (activeItems[i] != null && activeItems[i].gameObject != null)
            {
                activeItems[i].gameObject.SetActive(true);
                PoolManager.Instance.PushGameObject(activeItems[i].gameObject, itemPrefab);
            }
        }
        activeItems.Clear();
    }

    private void OnDestroy()
    {
        RecycleAllItems();
    }

    private void BuyCarChangeUpdate(BuyDiscountData data)
    {
        if(buyReduceDatas == null)
        {
            buyReduceDatas = new List<BuyDiscountData>();
        }

        bool isAdd = false;
        for(int i = 0; i < buyReduceDatas.Count; i++)
        {
            if (buyReduceDatas[i].id == data.id)
            {
                buyReduceDatas[i].amount = data.amount;
                isAdd = true;
                if (data.amount == 0)
                {
                    buyReduceDatas.RemoveAt(i);
                }
                break;
            }
        }

        if (!isAdd)
        {
            buyReduceDatas.Add(data);
        }
        ShowPanelStatus();
    }

    private void ShowPanelStatus()
    {
        carItemPrice = 0;
        OverTips.gameObject.SetActive(false);
        OneLock.gameObject.SetActive(true);
        SecLock.gameObject.SetActive(true);
        ThirdLock.gameObject.SetActive(true);
        DisCountTips.gameObject.SetActive(true);
        int itemCount = 0;
        for(int i = 0; i < buyReduceDatas.Count; i++)
        {
            foreach(var item in discountItemInfos)
            {
                if(item.id != buyReduceDatas[i].id)
                {
                    continue;
                }
                carItemPrice += item.discountedPrice * buyReduceDatas[i].amount;
                itemCount += buyReduceDatas[i].amount;
                break;
            }
        }
        if (carItemPrice < 200)
        {
            NeedBuyNum.text = (200 - carItemPrice).ToString();
            SaveGemNum.text = "30";
            Price.text = string.Format("{0} 购买", carItemPrice);
        }
        else if (carItemPrice >= 200 && carItemPrice < 300)
        {
            OneLock.gameObject.SetActive(false);
            NeedBuyNum.text = (300 - carItemPrice).ToString();
            SaveGemNum.text = "50";
            Price.text = string.Format("{0} 购买", carItemPrice - 30);
            ReduceToggle0.isOn = true;
            selectType = 1;
        }
        else if (carItemPrice >= 300 && carItemPrice < 500)
        {
            OneLock.gameObject.SetActive(false);
            SecLock.gameObject.SetActive(false);
            NeedBuyNum.text = (500 - carItemPrice).ToString();
            SaveGemNum.text = "150";
            Price.text = string.Format("{0} 购买", carItemPrice - 50);
            ReduceToggle1.isOn = true;
            selectType = 2;
        }
        else
        {
            OneLock.gameObject.SetActive(false);
            SecLock.gameObject.SetActive(false);
            ThirdLock.gameObject.SetActive(false);
            OverTips.gameObject.SetActive(true);
            DisCountTips.gameObject.SetActive(false);
            OverSavePrice.text = "150";
            Price.text = string.Format("{0} 购买", carItemPrice - 150);
            ReduceToggle2.isOn = true;
            selectType = 3;
        }

        RedPoint.gameObject.SetActive(buyReduceDatas.Count > 0);
        RedPointCount.text = itemCount.ToString();
    }

    private void BuyOverRefresh(BuyRedeceRespon redeceRespon)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();
        for(int i = 0; i < redeceRespon.list.Count; i++)
        {
            string itemName = "";
            DiscountItemInfo info = null;
            foreach (var item in discountItemInfos)
            {
                if (item.id != buyReduceDatas[i].id)
                {
                    continue;
                }
                info = item;
                itemName = item.name;
                break;
            }
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = GetIconSprite(info) ,//XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "item_Icon_" + (redeceRespon.list[i].id - 1), gameObject),
                RewardAmount = redeceRespon.list[i].amount,
                rewardName = itemName
            }) ;
        }
        if (!string.IsNullOrEmpty(redeceRespon.pgcId))
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = PgcUtils.GetIconSpriteByPgcId(redeceRespon.pgcId, gameObject),
                RewardAmount = 1,
                rewardName = Es.DataTables.GetPgcNameData(redeceRespon.pgcId)?.Name 
            });
        }
        panel.ShowRewards(rewardItemDatas);
        RefreshDataFromServer();
        TokenDataManager.Inst.GetTokenData();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }


public Sprite GetIconSprite(DiscountItemInfo packageData)
    {
        Sprite sprite = null;
        switch ((BUDRewardType)packageData.rewardType)
        {
            case BUDRewardType.RewardAvatarFrame:
                sprite = UserUIWidgetManager.Inst.GetHeadCycleSp(packageData.avatarFrameType, gameObject);
                break;
            case BUDRewardType.RewardHomepageSkin:
                sprite = ProfileThemeManager.Inst.LoadThemeIcon(packageData.homepageSkinType, gameObject);
                break;
            case BUDRewardType.RewardChatBubbles:
                sprite = UserUIWidgetManager.Inst.GetChatBubbleIconByType((ChatBubblesType)packageData.chatBubblesType, this.gameObject);
                break;
            case BUDRewardType.RewardLuckyCoin:
            case BUDRewardType.RewardYouYouCoin:
            case BUDRewardType.RewardPurpleDreamCoin:
            case BUDRewardType.RewardCommunityAnimationTicket:
            case BUDRewardType.RewardCommunityInstrumentTicket:
            case BUDRewardType.RewardCommunitySkinTicket:
            case BUDRewardType.RewardUGCVehicleTicket:
                sprite = PgcUtils.LoadRewardIcon((BUDRewardType)packageData.rewardType, gameObject);
                break;
            case BUDRewardType.RewardPgcBundle:
                sprite = PgcUtils.LoadBundleIcon(packageData.bundleId, gameObject);
                break;
            case BUDRewardType.RewardPgcResource:
                if(packageData.pgcIds.Count >0)
                {
                    sprite = PgcUtils.GetIconSpriteByPgcId(packageData.pgcIds[0], gameObject);
                }
                break;
        }

        return sprite;
    }

    public void OnceToggleClick(bool isOn)
    {
        if(carItemPrice < 200)
        {
            return;
        }
        selectType = 1;
        OverSavePrice.text = "30";
        Price.text = string.Format("{0} 购买", carItemPrice - 30);
    }

    public void SecToggleClick(bool isOn)
    {
        if (carItemPrice < 300)
        {
            return;
        }
        selectType = 2;
        OverSavePrice.text = "50";
        Price.text = string.Format("{0} 购买", carItemPrice - 50);
    }

    public void ThirdToggleClick(bool isOn)
    {
        if (carItemPrice < 500)
        {
            return;
        }
        selectType = 3;
        OverSavePrice.text = "150";
        Price.text = string.Format("{0} 购买", carItemPrice - 150);
    }
}
