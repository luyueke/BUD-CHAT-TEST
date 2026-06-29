using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using EventTracking;
using Game.Avatar;
using Game.Event;
using Game.Pet;
using Game.Store;
using GameData;
using GameData.PgcData;
using Message;
using Product;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ShapeThemePanel : BasePanel<ShapeThemePanel>
{
    [SerializeField] private Transform BackGround;

    [SerializeField] private CButton BackBtn;

    [SerializeField]
    private CButton BuyBtn;

    [SerializeField] private Text OriginPriceText;

    [SerializeField] private Text PriceText;

    [SerializeField] private Text ThemeText;

    [SerializeField] private Text AssetNameText;

    [SerializeField] private ShapeThemeItem shapeThemeItem;

    [SerializeField] private Transform Content;

    [Header("人物形象")][SerializeField] internal Transform characterRoot;

    [SerializeField] internal AvatarCameraController avatarCameraController;

    private int ThemeId;

    private ShapeThemeProp shapeData;

    private List<ShapeThemeItem> ItemList = new List<ShapeThemeItem>();

    private RectTransform rectTransform;

    private GoodsData selectData;

    internal CharacterWrap characterWrap;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;

    internal CharacterData saveCharacterData;

    private GameObject itemObj;

    public override void OnCreate()
    {
        BackBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.FindPanel(WindowId.FittingRoomWindow, PanelId.FittingRoomPanel).gameObject.SetActive(true);
            CloseSelf();
        });
        BuyBtn.onClick.AddListener(BuyBtnClick);
        rectTransform = GetComponent<RectTransform>();
        itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BackGround);
    }

    public override void OnShow(params object[] args)
    {
        if (args.Length <= 0)
        {
            return;
        }
        InitShapeThemeView();
        BuyBtn.gameObject.SetActive(false);
        AssetNameText.text = "";
        ThemeId = int.Parse((string)args[0]);
        SetItemList();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        InitBG();
    }

    private void InitBG()
    {
        if (BackGround == null)
        {
            return;
        }
        //string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        //var itemObj = Loader
        //    .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
        //    .Instantiate(BackGround);
        itemObj.gameObject.SetActive(true);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        var list = shapeData.GetbackgroundIconList();
        item.InitCustomBgItemForUrl(shapeData.backgroundColor, list);
        item.gameObject.SetActive(true);
    }

    private void SetItemList()
    {
        if(AssetsDataManager.ShapeThemePropList == null || AssetsDataManager.ShapeThemePropList.Count == 0)
        {
            return;
        }
        for (int i = 0; i < AssetsDataManager.ShapeThemePropList.Count; i++)
        {
            if (AssetsDataManager.ShapeThemePropList[i].themeId == ThemeId)
            {
                shapeData = AssetsDataManager.ShapeThemePropList[i];
                break;
            }
        }

        for (int i = 0; i < shapeData.products.Count; i++)
        {
            ShapeThemeItem promotionItem = GameObject.Instantiate(shapeThemeItem, Content);
            promotionItem.gameObject.SetActive(true);
            ItemList.Add(promotionItem);
            promotionItem.SeItemtData(shapeData, shapeData.products[i]);
        }
    }

    private void InitShapeThemeView()
    {
        saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData == null)
            saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        if (saveCharacterData != null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrap.SetParent(characterRoot, true);
            animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;

            var otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
        }

        //SpecialAnimContainer.SetCallBack(OnSpecialPGCClick);
    }



    private void BuyBtnClick()
    {
        if(selectData == null)
        {
            return;
        }
        Buy(selectData);
    }

    public void Buy(GoodsData data)
    {
        var tmpGoodsData = new GoodsData()
        {
            ButtonType = data.ButtonType,
            Id = data.Id,
            GoodsType = data.GoodsType,
            subType = data.subType,
            Price = new CurrencyData()
            {
                CurrencyType = data.Price.CurrencyType,
                Value = data.OriginalPrice.Value - data.OriginalPrice.Value * (data.Discount * 0.01f) //data.Price.Value,
            }
        };

        AssetsDataManager.BuyGoods(tmpGoodsData, (success, reason, needNum) =>
        {
            if (!success)
            {
                if (reason.Equals("余额不足"))
                {
                    //新用户行为埋点
                    if (SignInPanel.isNewPlayer)
                    {
                        LoadEvent.ReportPopupStatus("fail", "pay_fail");
                    }
                    switch (data.Price.CurrencyType)
                    {
                        case CurrencyType.Coin:
                            ExchangeCoinPanel exchangeCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                            exchangeCoinPanel.SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                            break;
                        case CurrencyType.Badge:
                            ExchangeCoinPanel badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                            badgePanel.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                            break;
                        case CurrencyType.PinkCoin:
                            if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                            {
                                ExchangeCoinPanel pinkCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                pinkCoinPanel.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                            }
                            //ExchangeCoinPanel pinkCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                            //pinkCoinPanel.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                            break;
                        case CurrencyType.Gem:
                            UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                            break;
                    }
                    return;
                }
                //UIManager.Inst.OpenPanel(PanelId.TipPanel, reason);
            }
            else
            {
                //UGC商城埋点
                if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
                {
                    LoadEvent.ReportPopupStatus(tmpGoodsData.Price.Value.ToString(), "PaySuccessfulValue");
                    LoadEvent.ReportPopupStatus(tmpGoodsData.subType.ToString(), "PaySuccessfulType");
                }
                //新用户行为埋点
                if (SignInPanel.isNewPlayer)
                {
                    LoadEvent.ReportPopupStatus("PinkCoin_" + tmpGoodsData.Price.Value.ToString(), "pay_success");
                }
                var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                panel?.InitData(data, "购买成功！");
                EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
                BuyBtn.gameObject.SetActive(false);
                SetItemList();
            }
            data.IsPayingRequest = false;
        }, shapeData.themeId);
    }

    public void ShapeEmoteItemClick(GoodsData data)
    {
        PreviewEmote(data);
    }

    private void PreviewEmote(GoodsData goodsData)
    {
        CancelEmote();

        for (int i = 0, C = goodsData.Assets.Count; i < C; i++)
        {
            var assets = goodsData.Assets[i];
            if (assets.ResourceType == ResourceType.Emote)
            {
                var emoteAssets = assets as EmoteAssetsData;
                avatarCameraController.SetEmoteView(emoteAssets.Id);
                switch (emoteAssets.EmoteSubType)
                {
                    case EmoteSubType.Single:
                    case EmoteSubType.SingleLoop:
                        goodsData.Loading(true);
                        animationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id, OnDownloadOver: () => goodsData.Loading(false));
                        break;
                    case EmoteSubType.Double:
                    case EmoteSubType.DoubleLoop:
                        goodsData.Loading(true);
                        animationCtrl.PlayDoubleEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                        break;
                    //case EmoteSubType.PetSingle:
                    //case EmoteSubType.PetSingleLoop:
                    //    goodsData.Loading(true);
                    //    petAnimationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id, OnDownloadOver: () => goodsData.Loading(false));
                    //    break;
                    //case EmoteSubType.PetWithPlayer:
                    //case EmoteSubType.PetWithPlayerLoop:
                    //    goodsData.Loading(true);
                    //    petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                    //    break;
                    case EmoteSubType.LinkEmote:
                        goodsData.Loading(true);
                        animationCtrl.PlayLinkEmoteForUICharacter(emoteAssets.Id, SpecialAnim.Idle, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                        break;
                }
            }
        }
    }

    private void CancelEmote()
    {
        avatarCameraController.ResetEmoteView();
        avatarCameraController.SetCameraZoom(0);

        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.ResetEmoteForUICharacter();
        //animationCtrlIK.StopAnimAndResetJointNode();
        //animationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
        //otherAnimationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
        //ResetIKPosition();
        otherAnimationCtrl.UpdateAnim(0);
        otherAnimationCtrl.gameObject.SetActive(false);
    }

    public void ShapeItemClickEvent(GoodsData data, ProductType productType)
    {
        if (productType != ProductType.Emote)
        {
            CancelEmote();
        }
        if (data == null)
        {
            return;
        }
        selectData = data;
        BuyBtn.gameObject.SetActive(!data.IsOwned);
        OriginPriceText.text = data.OriginalPrice.Value.ToString();
        ThemeText.text = "-" + data.Discount + "%";
        PriceText.text = (data.OriginalPrice.Value - (data.OriginalPrice.Value * (data.Discount * 0.01f))).ToString();
        AssetNameText.text = data.Name;
        for (int i = 0; i < ItemList.Count; i++)
        {
            ItemList[i].ClickEvent(int.Parse(data.Id));
        }

        for (int i = 0, C = data.Assets.Count; i < C; i++)
        {
            var assets = data.Assets[i];
            var pgcAssets = assets as PGCAssetsData;
            if(pgcAssets == null)
            {
                continue;
            }
            var subType = UniqueType.GetAvatar(pgcAssets.AvatarSubType);
            var specialConfig = DataTables.GetSpecialSkinConfig(pgcAssets.Id);
            var config = DataTables.GetAvatarCommonData(data.GetPgcId());
            data.Loading(true);
            characterWrap.RefreshAvatar(saveCharacterData);

            var classType = UniqueType.GetAvatar(pgcAssets.Id);
            characterWrap.ChangePart(classType, pgcAssets.Id);
            characterWrap.ChangeColor(classType, config.defaultColor);
            characterWrap.Move(classType, config.pDef);
            characterWrap.Rotate(classType, config.rDef);
            characterWrap.Scale(classType, config.sDef);
            characterWrap.HVScale(classType, config.vhSDef);
            characterWrap.SetLeftOrRight(classType, config.leftRightType);

            //if (specialConfig != null) ShowSpecialContainer();
        }

    }
}
