using Game.MusicalInstrument;
using Game.Store;
using GameData.Manager;
using GameData.PgcData;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class BuyButton : MonoBehaviour
    {
        [SerializeField] private Text ContentText;
        [SerializeField] private Image IconImage;
        [SerializeField] private Text OriginPrice;
        [SerializeField] private Text TipText;
        [SerializeField] private GameObject disCountMark;
        [SerializeField] private GameObject monthCardTipGo;
        [SerializeField] private GameObject goldCard;//金卡
        [SerializeField] private GameObject silverCard;//银卡
        [SerializeField] private Text txt_monthCardDiscount;
        [SerializeField] private Text txt_monthCardDiscount1;
        [SerializeField] private Text txt_monthCardDiscount2;
        [SerializeField] private GameObject disCountInfo;//促销
        [SerializeField] private Text txt_disCountInfo;
        [SerializeField] Button UgcVehcileBtn;

        private void Awake()
        {
            // if (monthCardTipGo != null)
            // {
            //     Image image = monthCardTipGo.GetComponent<Image>();
            //     if (image != null)
            //     {
            //         string spriteatlasPath = "Assets/Loadable/UI/UIPanel/FittingRoomPanel/FittingRoomPanel.spriteatlas";
            //         Sprite sp1 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "monthcardTip", gameObject);
            //         if (sp1 != null)
            //         {
            //             image.sprite = sp1;
            //         }
            //     }
            // }

            if (UgcVehcileBtn != null)
            {
                UgcVehcileBtn.onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel, ActivityId.UGCVehicleConsume.ToString());
                });
          
            }
        }

        

        public void SetTarget(GoodsData mData)
        {
            if (mData.Price == null) return;

           
            monthCardTipGo?.SetActive(false);
            UgcVehcileBtn?.gameObject.SetActive(false);
            if (disCountMark != null)
            {
                disCountMark.SetActive(false);
            }
            disCountInfo.gameObject.SetActive(false);
            if (mData.Price.Value == 0)
            {
                IconImage.gameObject.SetActive(false);
                ContentText.text = "免费获取";
                return;
            }

            IconImage.gameObject.SetActive(true);
            IconImage.sprite = PgcUtils.LoadCurrencyIcon((CurrencyType)mData.Price.CurrencyType, gameObject);
            // BuyGoods 里实际扣费用 Mathf.Ceil 取整，展示与扣费保持一致
            ContentText.text = Mathf.CeilToInt(mData.Price.Value).ToString();

            OriginPrice?.gameObject.SetActive(false);
            TipText?.gameObject.SetActive(false);

            int themeId = 0;
            ShapeThemeProp shapeData = null;
            if (int.TryParse(mData.Id,out themeId))
            {
                shapeData = GetShapeThemeData(themeId);
            }

            if (mData.GoodsType == GoodsType.BundleUgc)
            {
                bool isSkinInfo = false;
                if (mData.UgcBundleInfo != null && mData.UgcBundleInfo.skinInfo != null && mData.UgcBundleInfo.skinInfo.paymentInfo != null && mData.UgcBundleInfo.skinInfo.paymentInfo.price != mData.Price.Value)
                {
                    OriginPrice.gameObject.SetActive(true);
                    TipText.gameObject.SetActive(true);
                    OriginPrice.text = mData.UgcBundleInfo.skinInfo.paymentInfo.price.ToString();
                    isSkinInfo = true;
                }
                if (mData.Price.CurrencyType == CurrencyType.PinkCoin && AnniversaryMonthCardMgr.Inst.IsAnyMonthCardActive())
                {
                    if (!CheckUseTicket(mData))
                    {
                        OriginPrice.gameObject.SetActive(true);
                        if (!isSkinInfo)
                        {
                            OriginPrice.text = mData.OriginalPrice.Value.ToString();
                        }
                        ContentText.text = Mathf.CeilToInt(mData.Price.Value).ToString();
                        if (monthCardTipGo != null) monthCardTipGo.SetActive(true);
                        bool hasGoldCard1 = AnniversaryMonthCardMgr.Inst.IsAnyMonthGoldCardActive();
                        if (goldCard != null) goldCard.SetActive(hasGoldCard1);
                        if (silverCard != null) silverCard.SetActive(!hasGoldCard1 && AnniversaryMonthCardMgr.Inst.IsAnyMonthSilverCardActive());
                        if(txt_monthCardDiscount != null)
                            txt_monthCardDiscount.text = $"{AnniversaryMonthCardMgr.Inst.GetDiscountRate() * 10}折";
                        if(txt_monthCardDiscount1 != null)
                            txt_monthCardDiscount1.text = $"{AnniversaryMonthCardMgr.Inst.GetDiscountRate() * 10}折";
                        if(txt_monthCardDiscount2 != null)
                            txt_monthCardDiscount2.text = $"月卡{AnniversaryMonthCardMgr.Inst.GetDiscountRate() * 10}折权益";

                    }
                }
            }
            else if (disCountMark != null && DiscountCardUtils.IsSupportDiscountCard(mData) && DiscountCardUtils.IsOwnedDiscountCard())
            {
                OriginPrice.gameObject.SetActive(true);
                OriginPrice.text = mData.Price.Value.ToString();
                ContentText.text = Mathf.FloorToInt(mData.Price.Value * DiscountCardUtils.GetDiscount()).ToString();
                disCountMark.SetActive(true);
                disCountMark.GetComponentInChildren<Text>(true).SetLocalText("{0}折生效中", Mathf.RoundToInt(DiscountCardUtils.GetDiscount() * 10));
            }
            else if (shapeData != null)
            {
                OriginPrice.gameObject.SetActive(true);
                OriginPrice.text = mData.OriginalPrice.Value.ToString();
                ContentText.text = Mathf.FloorToInt(mData.OriginalPrice.Value - mData.OriginalPrice.Value * (shapeData.discount * 0.01f)).ToString();
                disCountInfo.gameObject.SetActive(true);
                txt_disCountInfo.text = "-" + shapeData.discount + "%";
            }
            else
            {
                if (mData.Price.CurrencyType == CurrencyType.PinkCoin && AnniversaryMonthCardMgr.Inst.IsAnyMonthCardActive())
                {
                    if (!CheckUseTicket(mData))
                    {
                        OriginPrice.gameObject.SetActive(true);
                        OriginPrice.text = mData.OriginalPrice.Value.ToString();
                        ContentText.text = Mathf.CeilToInt(mData.Price.Value).ToString();
                        if (monthCardTipGo != null) monthCardTipGo.SetActive(true);
                        bool hasGoldCard2 = AnniversaryMonthCardMgr.Inst.IsAnyMonthGoldCardActive();
                        if (goldCard != null) goldCard.SetActive(hasGoldCard2);
                        if (silverCard != null) silverCard.SetActive(!hasGoldCard2 && AnniversaryMonthCardMgr.Inst.IsAnyMonthSilverCardActive());
                        if(txt_monthCardDiscount != null)
                            txt_monthCardDiscount.text = $"{AnniversaryMonthCardMgr.Inst.GetDiscountRate() * 10}折";
                        if(txt_monthCardDiscount1 != null)
                            txt_monthCardDiscount1.text = $"{AnniversaryMonthCardMgr.Inst.GetDiscountRate() * 10}折";
                        if(txt_monthCardDiscount2 != null)
                            txt_monthCardDiscount2.text = $"月卡{AnniversaryMonthCardMgr.Inst.GetDiscountRate() * 10}折权益";
                    }

                }
            }
            CheckedTickUser(mData);
        }

        private ShapeThemeProp GetShapeThemeData(int pgcId)
        {
            if(AssetsDataManager.ShapeThemePropList == null || AssetsDataManager.ShapeThemePropList.Count == 0)
            {
                return null;
            }
            foreach (var themePropData in AssetsDataManager.ShapeThemePropList)
            {
                if (themePropData.products.Contains(pgcId))
                {
                    return themePropData;
                }
            }
            return null;
        }

        bool CheckedTickUser(GoodsData data)
        {

            if (FittingRoomPanel.curTab != MainTabs.Tab.Ugc)
            {
                return false;
            }
            switch (data.subType)
            {
                case 0:
                case 1008://姿势和乐谱不能用卷
                case 1014://演员不能用卷
                    return false;
                case 1007: //动作卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityAnimationTicket) <= 0
                        || data.Price.Value > 200) return false;
                    IconImage.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunityAnimationTicket, IconImage.gameObject);
                    OriginPrice.gameObject.SetActive(false);
                    TipText.gameObject.SetActive(false);
                    ContentText.text = "1";
                    return true;
                case 24: //乐器卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityInstrumentTicket) <= 0
                        || data.Price.Value > 200) return false;
                    IconImage.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunityInstrumentTicket, IconImage.gameObject);
                    OriginPrice.gameObject.SetActive(false);
                    TipText.gameObject.SetActive(false);
                    ContentText.text = "1";
                    return true;
                case 1011: //载具卷
                    UgcVehcileBtn?.gameObject.SetActive(BusinessLiveManager.Inst.IsActivityLive(((int)ActivityId.UGCVehicleConsume).ToString()));
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityVehicleTicket) <= 0
                        || data.Price.Value > 200)
                    {
                        return false;
                    }

                    UgcVehcileBtn?.gameObject.SetActive(false);
                    IconImage.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunityVehicleTicket, IconImage.gameObject);
                    OriginPrice.gameObject.SetActive(false);
                    TipText.gameObject.SetActive(false);
                    ContentText.text = "1";
                    return true;
                case 1015: //剧场卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityTheaterTicket) <= 0
                        || data.OriginalPrice.Value > 350) return false;
                    IconImage.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunityTheaterTicket, IconImage.gameObject);
                    OriginPrice.gameObject.SetActive(false);
                    TipText.gameObject.SetActive(false);
                    ContentText.text = "1";
                    return true;
                default: //其余都算皮肤卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunitySkinTicket) <= 0
                        || data.Price.Value > 60) return false;
                    IconImage.sprite = PgcUtils.LoadCurrencyIcon(CurrencyType.CommunitySkinTicket, IconImage.gameObject);
                    OriginPrice.gameObject.SetActive(false);
                    TipText.gameObject.SetActive(false);
                    ContentText.text = "1";
                    return true;
            }


        }

        bool CheckUseTicket(GoodsData data)
        {
            if (FittingRoomPanel.curTab != MainTabs.Tab.Ugc)
            {
                return false;
            }
            return AssetsDataManager.CheckUseTicket(data);
        }

    }
}