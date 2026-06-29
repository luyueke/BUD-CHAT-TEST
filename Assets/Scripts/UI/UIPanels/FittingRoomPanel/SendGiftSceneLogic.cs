using Game.Store;
using GameData.PgcData;
using System;
using System.Collections.Generic;
using System.Linq;
using Basic.Utils;
using Es;
using GameData.BaseInfo;
using UI.Manager;
using UnityEngine;
using Product;

namespace UI.UIPanels.FittingRoom
{
    #region ToolScene

    public class SendGiftToolScene : SendGiftBaseScene
    {
        protected SendGiftToolSceneHandler dataHandler;

        private string pgcId;

        internal SendGiftToolScene(SendGiftPanel ui) : base(ui)
        {
        }

        public override void InitParams()
        {
            classDatas = new List<ClassData>
            {
                new ClassData(UniqueType.GetAvatar(AvatarSubType.Clothes), "Clothes"),
            };
            dataHandler = AssetsDataManager.GetData<SendGiftToolSceneHandler>();


            classSelected = classDatas[0];
        }

        public override void Enter()
        {
            base.Enter();
            UI.characterRoot.gameObject.SetActive(true);
            UI.toolsUI.gameObject.SetActive(false);
            UI.ugcUI.gameObject.SetActive(false);
            UI.classList.SetCallback(OnClassListSelected);
            UI.classList.SetClass(classSelected, classDatas);
        }

        public void OnClassListSelected(ClassData data)
        {
            classSelected = data;

            UI.CloseAllSubUI();

            List<NewYearLimitedPackageListItem> list = IAPDataManager.Inst.GetNewYearLimitedPackageList();
           // Debug.LogError("newYearLimitedPackageList=" + Newtonsoft.Json.JsonConvert.SerializeObject(list));
            List<GoodsData> gList = new List<GoodsData>();
            var isOpen = BusinessLiveManager.Inst.IsRechargeActivityLive(RechargePanel.RechargeId.SpringLimited.ToString());
            if (isOpen && list != null && list.Count >0)
            {      
                for(int i=0;i<list.Count;i++)
                {
                     if(list[i].productId.Contains( "newYearLimitedPack3"))//御剑飞行
                    {
                        continue;
                    }
                    gList.Add(new GoodsData()
                    {
                        Id = list[i].productId,
                        Name = list[i].productName,
                        GoodsType = GoodsType.ToolProduct,
                        Price = new CurrencyData()
                        {
                            CurrencyType = CurrencyType.Gem,
                            Value = list[i].price
                        },
                        GiftType = GiftType.NewYearLimitedPackage
                    });
                }
             
            }
            assetsDatas.SetData(dataHandler.GetToolsGoodsData(gList));
            UI.assetsList.gameObject.SetActive(true);
            UI.assetsList.OnItemSelected = OnItemSelected;
            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        public override GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            assetsData.Selected = pgcId == assetsData.Id;
            if (assetsData.Selected) RefreshUI(assetsData);
            return assetsData;
        }

        internal void OnItemSelected(GoodsData data)
        {
            pgcId = data.Id;
            UI.ShowToolView(data);
            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        /// <summary>
        /// 选择item后的更新UI
        /// </summary>
        /// <param name="data"></param>
        public void RefreshUI(GoodsData data)
        {
            switch (data.ButtonType)
            {
                case ButtonType.Design:
                    return;
                case ButtonType.TakeOff:
                    UI.adjustUI.gameObject.SetActive(false);
                    UI.adjustView.gameObject.SetActive(false);
                    UI.secondColorUI.gameObject.SetActive(false);
                    return;
            }

            UI.itemInfoUI.gameObject.SetActive(true);
            UI.itemInfoUI.SetTarget(data);

            UI.operationUI.gameObject.SetActive(true);
            UI.giftPriceText.text = "¥" + data.Price.Value;
            UI.giftPriceIcon.gameObject.SetActive(false);
            UI.sendGiftBtn.onClick.RemoveAllListeners();
            UI.sendGiftBtn.onClick.AddListener(() => { UI.OpenSendGift(data); });

            UI.requestGiftBtn.onClick.RemoveAllListeners();
            UI.requestGiftBtn.onClick.AddListener(() => { UI.OpenRequetsGift(data); });

            UI.characterRoot.gameObject.SetActive(false);
            UI.toolsUI.gameObject.SetActive(true);
            UI.ugcUI.gameObject.SetActive(false);
            var spriteName = "icon_premium_pass";
            if (data.Id.Contains("giftgaojipass"))
            {
                spriteName = "icon_premium_pass_1";
            }
            else if (data.Id.Contains("gifthaohuapass"))
            {
                spriteName = "icon_deluxe_pass_1";
            }
            else if (data.Id.Contains("gifthaohuauppass"))
            {
                spriteName = "icon_deluxe_pass_upgrade_1";
            }
            else if (data.Id.Contains("giftvip"))
            {
                spriteName = "icon_vip_month_1";
            } else if (data.Id.Contains("newYearLimitedPack1"))
            {
                spriteName = "icon_newYearLimitedPack1_1";
                UI.giftPriceText.text = data.Price.Value.ToString();
                UI.giftPriceIcon.gameObject.SetActive(true);
            } else if (data.Id.Contains("newYearLimitedPack2")){
                spriteName = "icon_newYearLimitedPack2_1";
                UI.giftPriceText.text = data.Price.Value.ToString();
                UI.giftPriceIcon.gameObject.SetActive(true);
            }
            else if (data.Id.Contains("newYearLimitedDancingPack"))
            {
                spriteName = "icon_newYearLimitedDancingPack_1";
                UI.giftPriceText.text = data.Price.Value.ToString();
                UI.giftPriceIcon.gameObject.SetActive(true);
            }
            else if (data.Id.Contains("LaborDay"))
            {
                spriteName = "newLaborPack2";
                UI.giftPriceText.text = data.Price.Value.ToString();
                UI.giftPriceIcon.gameObject.SetActive(true);
            }

            UI.toolsIcon.gameObject.SetActive(true);
            var spriteatlasPath = "Assets/Loadable/UI/UIPanel/FittingRoomPanel/FittingRoomPanel.spriteatlas";
            var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, spriteName, UI.toolsIcon.gameObject);
            if (sprite != null) UI.toolsIcon.sprite = sprite;
        }
    }

    #endregion

    #region BUDScene

    public class SendGiftBUDScene : SendGiftBaseScene
    {
        public class PgcTuple
        {
            public SectionUIData Item1;
            public List<SectionUIData> Item2;
            public string Item3;
        }

        internal Dictionary<string, Color> BgColors = new Dictionary<string, Color>();

        protected Dictionary<ClassData, PgcTuple> classSectionDict = new();
        private PgcTuple pgcTuple;

        private GiftMallHandler dataHandler;

        internal SendGiftBUDScene(SendGiftPanel ui) : base(ui)
        {
        }

        public override void InitParams()
        {
            if (UI.isCharacterFittingRoom)
            {
                classDatas = new List<ClassData>
                {
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Hair), "Hair"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Clothes), "Clothes"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Bundle), "Bundle"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Hats), "Hats"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Hand), "Hand"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Eyes), "Eyes"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Mouth), "Mouth"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.FacePaint), "FacePaint"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Glasses), "Glasses"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Backpack), "Backpack"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Shoe), "Shoe"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Brow), "Brow"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Earring), "Earring"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Visor), "Visor"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Scarf), "Scarf"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Crossbody), "Crossbody"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Belt), "Belt"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Glove), "Glove"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Cape), "Cape"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Effect), "Effect"),
                };
                dataHandler = AssetsDataManager.GetData<GiftMallHandler>();
            }
            else
            {
                classDatas = new List<ClassData>
                {
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Skin), "PetSkin"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Clothes), "PetClothes"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Ear), "PetEar"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Hair), "PetHair"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Hats), "PetHats"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Scarf), "PetScarf"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Glasses), "PetGlasses"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Eyes), "PetEyes"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Mouth), "PetMouth"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.FacePaint), "PetFacePaint"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Tail), "PetTail"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Backpack), "PetBackpack"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Shoe), "PetShoe"),
                };
                dataHandler = AssetsDataManager.GetData<GiftMallHandler>();
            }


            ColorUtility.TryParseHtmlString("#6C57FF", out Color color1);
            ColorUtility.TryParseHtmlString("#A982FF", out Color color2);
            ColorUtility.TryParseHtmlString("#25B1FE", out Color color3);
            ColorUtility.TryParseHtmlString("#FFC132", out Color color4);
            ColorUtility.TryParseHtmlString("#FF9432", out Color color5);
            BgColors.Add("All", color1);
            BgColors.Add("Gem", color2);
            BgColors.Add("Special", color5);
            BgColors.Add("Badge", color3);
            BgColors.Add("Coin", color4);


            pgcTuple = new PgcTuple();

            dataHandler.AddDataChange(UI.gameObject, OnDataChange);
            classSelected = classDatas.Find(c => c.Id == UniqueType.GetAvatar(AvatarSubType.Hair));
        }

        public override void Enter()
        {
            base.Enter();
            UI.characterRoot.gameObject.SetActive(true);
            UI.toolsUI.gameObject.SetActive(false);
            UI.ugcUI.gameObject.SetActive(false);
            UI.sectionList.SetCallback(OnSectionListSelected);
            UI.classList.SetCallback(OnClassListSelected);
            UI.classList.SetClass(classSelected, classDatas);
        }

        internal void OnDataChange(AssetsData[] changes)
        {
            if (!Enable) return;
            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        public void OnClassListSelected(ClassData data)
        {
            classSelected = data;

            UI.CloseAllSubUI();
            List<GoodsData> giftDatas = dataHandler.GetGiftMallGoods(classSelected.Id);
            assetsDatas.SetData(giftDatas, ClassGoods);

            if (classSectionDict.ContainsKey(data)) pgcTuple = classSectionDict[data];
            else
            {
                pgcTuple = new PgcTuple();
                if (data.Id == (int)OtherClass.BUDSeries)
                {
                    var series = dataHandler.GetSeriesList(Product.SeriesType.DefaultSeries);
                    OnSeriesUpdate(classSelected.Id, series);
                }
                else
                {
                    var sections = dataHandler.GetGiftMallSections(data.Id);
                    OnSectionUpdate(classSelected.Id, sections);
                }

                classSectionDict.Add(data, pgcTuple);
            }

            ColorUtility.TryParseHtmlString("#FFD400", out SectionUIData.SelectedColor);
            UI.sectionList.SetSection(pgcTuple.Item1, pgcTuple.Item2);
        }

        internal void OnSectionUpdate(int classType, List<Game.Store.UgcSectionData> sectionDatas)
        {
            if (!Enable || classType != classSelected.Id) return;

            var list = new List<SectionUIData>();
            for (int i = 0, C = sectionDatas.Count; i < C; i++)
            {
                var section = sectionDatas[i];
                list.Add(new SectionUIData(section.sectionId, section.sectionName, BgColors[section.sectionId]));
            }

            pgcTuple.Item2 = list;
            if (pgcTuple.Item2 == null || pgcTuple.Item2.Count == 0)
            {
                UI.sectionList.SetSection(null, null);
                return;
            }

            if (!pgcTuple.Item2.Contains(pgcTuple.Item1)) pgcTuple.Item1 = pgcTuple.Item2[0];
        }

        internal void OnSeriesUpdate(int classType, List<BUDSeriesData> sectionDatas)
        {
            if (!Enable || classType != classSelected.Id) return;

            var list = new List<SectionUIData>();
            for (int i = 0, C = sectionDatas.Count; i < C; i++)
            {
                var section = sectionDatas[i];
                var sectionData = new SectionUIData(section.ServerData.Id.ToString(), section.ServerData.Name,
                    Color.white);
                sectionData.seriesConfig = section;
                list.Add(sectionData);
            }

            pgcTuple.Item2 = list;
            if (pgcTuple.Item2 == null || pgcTuple.Item2.Count == 0)
            {
                UI.sectionList.SetSection(null, null);
                return;
            }

            if (!pgcTuple.Item2.Contains(pgcTuple.Item1)) pgcTuple.Item1 = pgcTuple.Item2[0];
        }

        internal void OnSectionListSelected(SectionUIData data)
        {
            pgcTuple.Item1 = data;

            if (classSelected.Id == (int)OtherClass.BUDSeries)
            {
                assetsDatas.SetData(data.seriesConfig.GoodsList);

                var gashaponId = data.seriesConfig.GoodsList.First()
                    .SourceData.Id;
                var ViewCfg =
                    GashaponPanel.GashaponDataManager.Inst.GetGashaponView(gashaponId);
                if (ViewCfg == null)
                {
                    LoggerUtils.LogError("View == null Gashapon:" + gashaponId);
                }
                else
                {
                    UI.seriesBg.InitCustomBgItem(ViewCfg.BgColor, ViewCfg.AtlasPath, ViewCfg.BgSpriteIds);
                    UI.seriesBg.gameObject.SetActive(true);
                    UI.seriesBg.GetComponent<ColorBgPanel>().RefreshSprite();
                    var color = DataUtil.DeSerializeColorCheckHash(ViewCfg.BgColor);
                    color.a = 0;
                    UI.avatarCameraController.roleCamera.backgroundColor = color;
                }
            }
            else
            {
                assetsDatas.Refresh();
            }

            UI.assetsList.gameObject.SetActive(true);
            UI.assetsList.OnItemSelected = OnItemSelected;
            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        internal bool ClassGoods(GoodsData data)
        {
            if (pgcTuple.Item1 == null) return false;


            if (data.EndTime > 0 && data.SourceData.Source != Source.Mall)
            {
                DateTime endTime = GameUtils.GetDataTimeStamp(data.EndTime);
                if (endTime <= DateTime.Now)
                {
                    return false;
                }
            }

            DataTables.GetGashaponViewConfigList();
            return true;
        }

        public override GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            assetsData.Selected = pgcTuple.Item3 == assetsData.Id;
            if (assetsData.Selected) RefreshUI(assetsData);
            return assetsData;
        }

        internal void OnItemSelected(GoodsData data)
        {
            pgcTuple.Item3 = data.Id;

            try
            {
                UI.CancelTryOn();
                UI.TryOn(data);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + e.StackTrace);
            }

            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        /// <summary>
        /// 选择item后的更新UI
        /// </summary>
        /// <param name="data"></param>
        public void RefreshUI(GoodsData data)
        {
            UI.itemInfoUI.gameObject.SetActive(true);
            UI.itemInfoUI.SetTarget(data);

            UI.characterRoot.gameObject.SetActive(true);
            UI.toolsUI.gameObject.SetActive(false);
            UI.ugcUI.gameObject.SetActive(false);


            if (data.Price.Value > 0 || data.IsOwned)
            {
                UI.disableUI.gameObject.SetActive(false);
                UI.operationUI.gameObject.SetActive(true);
                UI.giftPriceText.text = data.IsOwned ? data.OriginalPrice.Value.ToString() : data.Price.Value.ToString();
                UI.sendGiftBtn.onClick.RemoveAllListeners();
                UI.sendGiftBtn.onClick.AddListener(() => { UI.OpenSendGift(data); });

                UI.requestGiftBtn.onClick.RemoveAllListeners();
                UI.requestGiftBtn.onClick.AddListener(() => { UI.OpenRequetsGift(data); });

                UI.giftPriceIcon.gameObject.SetActive(true);
                UI.giftPriceIcon.sprite =
                    PgcUtils.LoadCurrencyIcon((CurrencyType)data.Price.CurrencyType, UI.giftPriceIcon.gameObject);
            }
            else
            {
                UI.disableText.text = data.IsOwned ? "已拥有" : "免费商品不可赠送/索要";
                UI.operationUI.gameObject.SetActive(false);
                UI.disableUI.gameObject.SetActive(true);
            }
        }
    }

    #endregion

    // pgc动作商城

    #region ActionScene

    public class SendGiftActionScene : SendGiftBaseScene
    {
        public class PgcTuple
        {
            public SectionUIData Item1;
            public List<SectionUIData> Item2;
        }

        internal Dictionary<string, Color> BgColors = new Dictionary<string, Color>();
        protected GiftMallHandler dataHandler;
        protected Dictionary<ClassData, PgcTuple> classSectionDict = new();
        private PgcTuple pgcTuple;
        private string pgcId;

        internal SendGiftActionScene(SendGiftPanel ui) : base(ui)
        {
        }

        public override void InitParams()
        {
            if (UI.isCharacterFittingRoom)
            {
                classDatas = new List<ClassData>
                {
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.SingleAll), "Emote5"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.DoubleAll), "Emote6"),
                    // new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.LinkEmote), "Emote9"),//S7赠礼未出现双人牵手动作，暂时注释
                };
                classSelected = classDatas.Find(c =>
                    c.Id == UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.SingleAll));
                dataHandler = AssetsDataManager.GetData<GiftMallHandler>();
            }
            else
            {
                classDatas = new List<ClassData>
                {
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetSingleAll), "PetEmote1"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetWithPlayerAll), "PetEmote2"),
                };
                classSelected = classDatas.Find(c =>
                    c.Id == UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetSingleAll));
                dataHandler = AssetsDataManager.GetData<GiftMallHandler>();
            }

            ColorUtility.TryParseHtmlString("#6C57FF", out Color color1);
            ColorUtility.TryParseHtmlString("#A982FF", out Color color2);
            ColorUtility.TryParseHtmlString("#25B1FE", out Color color3);
            ColorUtility.TryParseHtmlString("#FFC132", out Color color4);
            ColorUtility.TryParseHtmlString("#FF9432", out Color color5);
            BgColors.Add("All", color1);
            BgColors.Add("Gem", color2);
            BgColors.Add("Special", color5);
            BgColors.Add("Badge", color3);
            BgColors.Add("Coin", color4);

            pgcTuple = new PgcTuple();

            dataHandler.AddDataChange(UI.gameObject, OnDataChange);
        }

        internal void OnDataChange(AssetsData[] changes)
        {
            if (!Enable) return;
            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        public override void Enter()
        {
            base.Enter();
            UI.characterRoot.gameObject.SetActive(true);
            UI.toolsUI.gameObject.SetActive(false);
            UI.ugcUI.gameObject.SetActive(false);

            ColorUtility.TryParseHtmlString("#AEA6CC", out UI.assetsList.BgColor);
            UI.sectionList.SetCallback(OnSectionListSelected);
            UI.classList.SetCallback(OnClassListSelected);
            UI.classList.SetClass(classSelected, classDatas);
        }

        public void OnClassListSelected(ClassData data)
        {
            classSelected = data;
            pgcId = null;

            UI.CloseAllSubUI();
            assetsDatas.SetData(dataHandler.GetGiftMallGoods(classSelected.Id), ClassGoods);

            // UI.sectionList.gameObject.SetActive(true);

            if (classSectionDict.ContainsKey(data)) pgcTuple = classSectionDict[data];
            else
            {
                pgcTuple = new PgcTuple();
                var sections = dataHandler.GetGiftMallSections(data.Id);
                OnSectionUpdate(classSelected.Id, sections);
                classSectionDict.Add(data, pgcTuple);
            }

            ColorUtility.TryParseHtmlString("#FFD400", out SectionUIData.SelectedColor);
            UI.sectionList.SetSection(pgcTuple.Item1, pgcTuple.Item2);
        }

        internal void OnSectionUpdate(int classType, List<Game.Store.UgcSectionData> sectionDatas)
        {
            if (!Enable || classType != classSelected.Id) return;

            var list = new List<SectionUIData>();
            for (int i = 0, C = sectionDatas.Count; i < C; i++)
            {
                var section = sectionDatas[i];
                list.Add(new SectionUIData(section.sectionId, section.sectionName, BgColors[section.sectionId]));
            }

            pgcTuple.Item2 = list;
            if (pgcTuple.Item2 == null || pgcTuple.Item2.Count == 0)
            {
                UI.sectionList.SetSection(null, null);
                return;
            }

            if (!pgcTuple.Item2.Contains(pgcTuple.Item1)) pgcTuple.Item1 = pgcTuple.Item2[0];
        }

        internal void OnSectionListSelected(SectionUIData data)
        {
            pgcTuple.Item1 = data;
            assetsDatas.Refresh();
            UI.assetsList.gameObject.SetActive(true);
            UI.assetsList.OnItemSelected = OnItemSelected;
            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        internal bool ClassGoods(GoodsData data)
        {
            if (pgcTuple.Item1 == null) return false;

            if (data.EndTime > 0 && data.SourceData.Source != Source.Mall)
            {
                DateTime endTime = GameUtils.GetDataTimeStamp(data.EndTime);
                if (endTime <= DateTime.Now)
                {
                    return false;
                }
            }

            return true;
        }

        public override GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            assetsData.Selected = pgcId == assetsData.Id;
            if (assetsData.Selected) RefreshUI(assetsData);
            return assetsData;
        }

        internal void OnItemSelected(GoodsData data)
        {
            pgcId = data.Id;
            try
            {
                UI.PreviewEmote(data);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + e.StackTrace);
            }

            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        /// <summary>
        /// 选择item后的更新UI
        /// </summary>
        /// <param name="data"></param>
        public void RefreshUI(GoodsData data)
        {
            UI.itemInfoUI.gameObject.SetActive(true);
            UI.itemInfoUI.SetTarget(data);

            UI.characterRoot.gameObject.SetActive(true);
            UI.toolsUI.gameObject.SetActive(false);
            UI.ugcUI.gameObject.SetActive(false);
            
            if (data.Price.Value > 0 || data.IsOwned)
            {
                UI.disableUI.gameObject.SetActive(false);
                UI.operationUI.gameObject.SetActive(true);
                UI.giftPriceText.text = data.IsOwned ? data.OriginalPrice.Value.ToString() : data.Price.Value.ToString();
                UI.sendGiftBtn.onClick.RemoveAllListeners();
                UI.sendGiftBtn.onClick.AddListener(() => { UI.OpenSendGift(data); });

                UI.requestGiftBtn.onClick.RemoveAllListeners();
                UI.requestGiftBtn.onClick.AddListener(() => { UI.OpenRequetsGift(data); });

                UI.giftPriceIcon.gameObject.SetActive(true);
                UI.giftPriceIcon.sprite =
                    PgcUtils.LoadCurrencyIcon((CurrencyType)data.Price.CurrencyType, UI.giftPriceIcon.gameObject);
            }
            else
            {
                UI.disableText.text = data.IsOwned ? "已拥有" : "免费商品不可赠送/索要";
                UI.operationUI.gameObject.SetActive(false);
                UI.disableUI.gameObject.SetActive(true);
            }

            if (UniqueType.EmoteSubType(classSelected.Id) == EmoteSubType.LinkEmote)
            {
                UI.switchAnimView.gameObject.SetActive(true);
                UI.switchAnimView.SetPgcId(data.Id);
            }
            else
            {
                UI.switchAnimView.gameObject.SetActive(false);
                UI.switchAnimView.SetPgcId("");
            }
        }
    }

    #endregion

    // ugc社区

    #region UGCScene

    public class SendGiftUGCScene : SendGiftBaseScene
    {
        public class UgcTuple
        {
            public string Item3;
            public string SearchKey;
        }

        internal List<Color> BgColors = new List<Color>();
        protected UgcTuple ugcTuple;

        protected SendGiftUgcSceneHandler dataHandler;

        private List<GoodsData> searchUgcList = new List<GoodsData>();

        internal SendGiftUGCScene(SendGiftPanel ui) : base(ui)
        {
        }

        public override void InitParams()
        {
            dataHandler = AssetsDataManager.GetData<SendGiftUgcSceneHandler>();

            ColorUtility.TryParseHtmlString("#FF6BA3", out Color color1);
            ColorUtility.TryParseHtmlString("#009EFF", out Color color2);
            ColorUtility.TryParseHtmlString("#00B89C", out Color color3);
            ColorUtility.TryParseHtmlString("#FFB200", out Color color4);
            BgColors.Add(color1);
            BgColors.Add(color2);
            BgColors.Add(color3);
            BgColors.Add(color4);


            ugcTuple = new UgcTuple();

            dataHandler.AddDataChange(UI.gameObject, OnDataChange);
            OnSearchClick();
        }

        public override void Enter()
        {
            base.Enter();
            UI.characterRoot.gameObject.SetActive(true);
            UI.toolsUI.gameObject.SetActive(false);
            UI.ugcUI.gameObject.SetActive(false);

            ReloadSearchData();
        }

        public override void Destory()
        {
            base.Destory();
            dataHandler?.ClearAllCache();
        }

        public override void OnDataRefresh()
        {
            base.OnDataRefresh();
            dataHandler?.ClearAllCache();
        }

        internal void ReloadSearchData()
        {
            if (searchUgcList != null && searchUgcList.Count > 0)
            {
                UI.assetsList.ResetColor();
                for (int i = 0; i < searchUgcList.Count; i++)
                {
                    searchUgcList[i].Selected = false;
                }

                UI.assetsList.gameObject.SetActive(true);
                assetsDatas.SetData(searchUgcList);
                UI.assetsList.Data.ResetItems(assetsDatas.Count());
                UI.assetsList.OnItemSelected = OnItemSelected;
            }
        }

        internal void OnSearchClick()
        {
            UI.sectionList.gameObject.SetActive(false);
            searchUgcList.Clear();
            UI.assetsList.Data.ResetItems(0);
            UI.searchUgcView.SetSearchAction(OnSearchAction, OnClearAction, OnCancelAction);
        }

        internal void OnSearchAction(string str)
        {
            UI.ShowTip("搜索中");
            searchUgcList.Clear();
            UI.assetsList.Data.ResetItems(0);

            ugcTuple.SearchKey = str;
            var nextAction = dataHandler.SearchGoodsData(0, str, OnSearchItemDataChange);
            UI.assetsList.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            UI.assetsList.PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(() => nextAction?.Invoke());
        }

        internal void OnClearAction()
        {
            ugcTuple.SearchKey = null;
            UI.ShowTip("");
            searchUgcList.Clear();
            UI.assetsList.Data.ResetItems(0);
            UI.operationUI.gameObject.SetActive(false);
            UI.itemInfoUI.gameObject.SetActive(false);
        }


        internal void OnCancelAction()
        {
        }

        internal void OnSearchItemDataChange(string searchKey, bool isEnd, List<GoodsData> datas)
        {
            if (!UI.gameObject) return;
            UI.assetsList.PullToRefreshBehaviour.HideGizmo();
            if (!Enable || searchKey != ugcTuple.SearchKey) return;
            if (isEnd && (datas == null || datas.Count == 0))
            {
                UI.ShowTip("没有找到相关内容");
                searchUgcList.Clear();
                UI.assetsList.Data.ResetItems(0);
                return;
            }

            UI.ShowTip("");
            searchUgcList.Clear();
            searchUgcList.AddRange(datas);
            assetsDatas.SetData(searchUgcList);
            UI.assetsList.gameObject.SetActive(true);
            UI.assetsList.OnItemSelected = OnItemSelected;
            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        internal void OnDataChange(AssetsData[] changes)
        {
            if (!Enable) return;
            assetsDatas.Refresh();
            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        public override GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            assetsData.Selected = ugcTuple.Item3 == assetsData.Id;
            if (assetsData.Selected) RefreshUI(assetsData);
            return assetsData;
        }

        internal void OnItemSelected(GoodsData data)
        {
            switch (data.ButtonType)
            {
                case ButtonType.Design:
                    // 跳转avatar绘制
                    Design();
                    return;
                case ButtonType.TakeOff:
                    // 脱下
                    return;
            }

            ugcTuple.Item3 = data.Id;
            try
            {
                UI.TryOn(data);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message + e.StackTrace);
            }

            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        /// <summary>
        /// 选择item后的更新UI
        /// </summary>
        /// <param name="data"></param>
        public void RefreshUI(GoodsData data)
        {
            switch (data.ButtonType)
            {
                case ButtonType.Design:
                    return;
                case ButtonType.TakeOff:
                    return;
            }


            if (data.CantWear ||
                (data.Assets != null && data.Assets.Count > 0 &&
                 data.Assets[0]?.UgcInfo?.skinInfo != null &&
                 data.Assets[0].UgcInfo.skinInfo.skinType == (int)SkinType.Pet))
            {
                UI.characterRoot?.gameObject.SetActive(false);
                UI.toolsUI?.gameObject.SetActive(false);
                UI.ugcUI?.gameObject.SetActive(true);

                if (data.Assets != null && data.Assets.Count > 0 &&
                    data.Assets[0]?.UgcInfo?.UgcInfo?.cover != null)
                {
                    UI.ugcRemoteImage?.Load(data.Assets[0].UgcInfo.UgcInfo.cover);
                }
            }
            else
            {
                UI.characterRoot?.gameObject.SetActive(true);
                UI.toolsUI?.gameObject.SetActive(false);
                UI.ugcUI?.gameObject.SetActive(false);
            }


            UI.itemInfoUI.gameObject.SetActive(true);
            UI.itemInfoUI.SetTarget(data);

            if (data.Price.Value <= 0)
            {
                UI.operationUI.gameObject.SetActive(false);
                UI.disableUI.gameObject.SetActive(true);
            }
            else
            {
                UI.operationUI.gameObject.SetActive(true);
                UI.disableUI.gameObject.SetActive(false);
                UI.giftPriceText.text = data.Price.Value.ToString();
                UI.sendGiftBtn.onClick.RemoveAllListeners();
                UI.sendGiftBtn.onClick.AddListener(() => { UI.OpenSendGift(data); });

                UI.requestGiftBtn.onClick.RemoveAllListeners();
                UI.requestGiftBtn.onClick.AddListener(() => { UI.OpenRequetsGift(data); });

                UI.giftPriceIcon.gameObject.SetActive(true);
                UI.giftPriceIcon.sprite =
                    PgcUtils.LoadCurrencyIcon((CurrencyType)data.Price.CurrencyType, UI.giftPriceIcon.gameObject);
            }

            if (data.GoodsType == GoodsType.BundleUgc)
            {
                UI.bundleItemsList.gameObject.SetActive(true);
                UI.bundleItemsList.SetTarget(data);
            }
            else
            {
                UI.bundleItemsList.gameObject.SetActive(false);
            }
        }
    }

    #endregion
}