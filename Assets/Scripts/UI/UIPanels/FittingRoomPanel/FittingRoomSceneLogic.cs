using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Basic.Utils;
using Es;
using EventTracking;
using Game.Avatar;
using Game.Pet;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using static CustomBodyTypeController;

namespace UI.UIPanels.FittingRoom
{
    // 美术测试

    #region TestScene

    public class TestScene : BaseScene
    {
        protected AvatarTestSceneHandler dataHandler;

        protected List<Color> skinColors;
        private string pgcId;


        internal TestScene(FittingRoomPanel ui) : base(ui)
        {
        }

        public override void InitParams()
        {
            if (UI.isCharacterFittingRoom)
            {
                classDatas = new List<ClassData> {
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Hair), "Hair"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Clothes), "Clothes"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.SpecialSkin), "SpecialSkin"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), "MusicalInstrument"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Hats), "Hats"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Hand), "Hand"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Eyes), "Eyes"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Mouth), "Mouth"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.FacePaint), "FacePaint"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Glasses), "Glasses"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Backpack), "Backpack"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Shoe), "Shoe"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Blush), "Blush"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Head), "Head"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Brow), "Brow"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Nose), "Nose"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Earring), "Earring"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Visor), "Visor"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Scarf), "Scarf"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Crossbody), "Crossbody"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Belt), "Belt"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Glove), "Glove"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Cape), "Cape"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Effect), "Effect"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.Single), "Emote1"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.SingleLoop), "Emote2"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.Double), "Emote3"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.DoubleLoop), "Emote4"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.LinkEmote), "Emote9")
                };
                dataHandler = AssetsDataManager.GetData<AvatarTestSceneHandler>();
            }
            else
            {
                classDatas = new List<ClassData> {
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
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetSingle), "Emote1"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetSingleLoop), "Emote2"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetWithPlayer), "Emote3"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetWithPlayerLoop), "Emote4"),
                };
                dataHandler = AssetsDataManager.GetData<PetAvatarTestSceneHandler>();
            }
            skinColors = new();
            var list = AvatarColorManager.GetColorList(AvatarColorManager.SKIN_COM);
            list.ForEach(c =>
            {
                if (ColorUtility.TryParseHtmlString(c, out Color color)) skinColors.Add(color);
            });


            classSelected = classDatas[0];
        }

        public override void Enter()
        {
            base.Enter();
            UI.classList.SetCallback(OnClassListSelected);
            UI.classList.SetClass(classSelected, classDatas);
        }

        public void OnClassListSelected(ClassData data)
        {
            classSelected = data;

            UI.CloseAllSubUI();

            assetsDatas.SetData(dataHandler.GetGoodsData(classSelected.Id));
            UI.assetsList.gameObject.SetActive(true);
            UI.assetsList.OnItemSelected = OnItemSelected;
            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        public override GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            if (assetsData == null)
            {
                return null;
            }
            assetsData.Selected = pgcId == assetsData.Id;
            if (assetsData.Selected) RefreshUI(assetsData);
            return assetsData;
        }

        internal void OnItemSelected(GoodsData data)
        {
            var asset = data.GetFirstAsset<AssetsData>();
            //if (asset.ResourceType == ResourceType.Avatar && UI.animationCtrl != null && !string.IsNullOrEmpty(UI.animationCtrl.specialAnimPgcId)) {
            //    var lastResData = DataTables.GetAvatarCommonData(UI.animationCtrl.specialAnimPgcId);
            //    var lastSubType = UniqueType.GetAvatar((AvatarSubType)lastResData.SubType);
            //    var specialConfig = DataTables.GetSpecialSkinConfig(data.Id);
            //    var pgcAssets = asset as PGCAssetsData;
            //    var subType = UniqueType.GetAvatar(pgcAssets.AvatarSubType);
            //    if (specialConfig != null && lastSubType != subType) {
            //        TipPanel.ShowToast("需先卸下当前动作道具才能穿戴新的哦");
            //        return;
            //    }
            //}
            pgcId = data.Id;

            if (asset.ResourceType == ResourceType.Emote)
            {
                UI.TryOn(data);
            }
            else
            {
                UI.PutOn(data);
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
                    UI.adjustUI.gameObject.SetActive(false);
                    UI.adjustView.gameObject.SetActive(false);
                    UI.secondColorUI.gameObject.SetActive(false);
                    return;
            }

            UI.itemInfoUI.gameObject.SetActive(true);
            UI.itemInfoUI.SetTarget(data);

            UI.operationUI.gameObject.SetActive(true);
            UI.operationUI.SetTarget(data);
            UI.operationUI.OnOperation = (operation, target) =>
            {
                switch (operation)
                {
                    case Operation.TryPlayMusic:
                        UI.TryPlayMusic();
                        break;
                    case Operation.Select:
                        UI.OnSelect(data);
                        break;
                }
            };

            if (UniqueType.ResourceType(classSelected.Id) == ResourceType.Emote && UniqueType.EmoteSubType(classSelected.Id) == EmoteSubType.LinkEmote)
            {
                UI.switchAnimView.gameObject.SetActive(true);
                UI.switchAnimView.SetPgcId(data.Id);
            }
            else
            {
                UI.switchAnimView.gameObject.SetActive(false);
                UI.switchAnimView.SetPgcId("");
            }

            SkinInfo skinInfo = null;
            if (data.GoodsType == GoodsType.SingleUgc)
            {
                skinInfo = data.GetFirstAsset<AssetsData>()?.UgcInfo?.skinInfo;
            }

            AvatarCommonData configData = null;
            if (skinInfo != null)
            {
                configData = AvatarCommonData.From(skinInfo);
            }
            else
            {
                configData = Es.DataTables.GetAvatarData(data.Id);
            }

            data.Name = configData != null ? configData.Icon : data.Id;
            UI.itemInfoUI.SetTarget(data);

            if (configData == null) return;

            var adjustList = UI.AdjustTypeAdjustItems(configData, classSelected.Id);

            if (adjustList == null)
            {
                UI.adjustUI.gameObject.SetActive(false);
                UI.adjustView.gameObject.SetActive(false);
            }
            else
            {
                UI.adjustUI.gameObject.SetActive(true);
                UI.adjustView.SetAdjustItems(adjustList);
            }

            if (configData != null && configData.setColor)
            {
                UI.secondColorUI.gameObject.SetActive(true);
                var skinData = UI.saveAvatarData.GetPartData(classSelected.Id);
                if (!ColorUtility.TryParseHtmlString(skinData.Cr, out Color skinColor))
                {
                    ColorUtility.TryParseHtmlString(configData.defaultColor, out skinColor);
                }

                List<Color> colors = new();
                var list = AvatarColorManager.GetColorList(GetColorKey((AvatarSubType)configData.SubType));
                list.ForEach(c =>
                {
                    if (ColorUtility.TryParseHtmlString(c, out Color color)) colors.Add(color);
                });
                UI.secondColorUI.SetCallback(OnSkinColorSelected);
                UI.secondColorUI.SetColors(true, skinColor, colors);
                UI.secondColorUI.SetCustomButtonCallback(() =>
                {
                    ColorUtility.TryParseHtmlString(configData.defaultColor, out Color defaultColor);
                    UI.OpenCustomColor(UI.secondColorUI.CustomColor, defaultColor, (color) =>
                    {
                        UI.secondColorUI.OnSelecedColor(color);
                    }, UI.secondColorUI.gameObject);
                });
            }
            else
            {
                UI.secondColorUI.gameObject.SetActive(false);
            }
        }

        internal string GetColorKey(AvatarSubType avatarSubType)
        {
            switch (avatarSubType)
            {
                case AvatarSubType.Hair:
                    return AvatarColorManager.HAIR_ALL;
                case AvatarSubType.Brow:
                    return AvatarColorManager.BROW_ALL;
                case AvatarSubType.Blush:
                    return AvatarColorManager.FACESTYLE_ALL;
                case AvatarSubType.Skin:
                    if (!UI.isCharacterFittingRoom)
                    {
                        return AvatarColorManager.SKIN_COM;
                    }
                    else
                    {
                        break;
                    }
            }

            return AvatarColorManager.FACESTYLE_ALL;
        }

        internal void OnSkinColorSelected()
        {
            UI.mainColorUI.gameObject.SetActive(true);
            var skinData = UI.saveAvatarData.GetPartData(UI.isCharacterFittingRoom ? UniqueType.GetAvatar(AvatarSubType.Skin) : UniqueType.GetPGCPetAvatar(AvatarSubType.Skin));
            ColorUtility.TryParseHtmlString(skinData.Cr, out Color skinColor);
            UI.mainColorUI.SetCallback(OnSkinColorSelected);
            UI.mainColorUI.SetColors(true, skinColor, skinColors);
            UI.mainColorUI.SetCustomButtonCallback(() =>
            {
                UI.OpenCustomColor(UI.mainColorUI.CustomColor, Color.white, (color) =>
                {
                    UI.mainColorUI.OnSelecedColor(color);
                }, UI.mainColorUI.gameObject);
            });
        }

        internal void OnSkinColorSelected(Color color)
        {
            UI.ChangeColor(classSelected.Id, color);
        }
    }

    #endregion

    // pgc商城

    #region BUDScene

    public class BUDScene : BaseScene
    {
        public class PgcTuple
        {
            public SectionUIData Item1;
            public List<SectionUIData> Item2;
            public string Item3;
        }

        internal Dictionary<string, Color> BgColors = new Dictionary<string, Color>();

        protected Dictionary<ClassData, PgcTuple> budSectionDic = new();
        private PgcTuple pgcTuple;

        private AvatarBUDSceneHandler dataHandler;


        internal BUDScene(FittingRoomPanel ui) : base(ui)
        {
        }

        public override void InitParams()
        {
            if (UI.isCharacterFittingRoom)
            {
                classDatas = new List<ClassData> {
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Promotion), "Promotion"),
                    new ClassData((int)OtherClass.BUDSeries, "BUDSeries"),
                    new ClassData((int)OtherClass.NewUserFeatured, "NewUserFeatured"), // 新增新手推荐分类
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Hair), "Hair"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Clothes), "Clothes"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.SpecialSkin), "SpecialSkin"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), "MusicalInstrument"),
                    new ClassData(UniqueType.Get(ResourceType.Vehicle, (int)VehicleSubType.AllVehicle), "Vehicle"),
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
                dataHandler = AssetsDataManager.GetData<AvatarBUDSceneHandler>();
                if (AccountDataManager.Inst.UserInfo.isNewUser != 1)
                {
                    classDatas.RemoveAt(2);
                }
                if (AssetsDataManager.ShapeThemePropList == null || AssetsDataManager.ShapeThemePropList.Count == 0)
                {
                    classDatas.RemoveAt(0);
                }
            }
            else
            {
                classDatas = new List<ClassData> {
                    new ClassData((int)OtherClass.BUDSeries, "BUDSeries"),
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
                dataHandler = AssetsDataManager.GetData<PetAvatarBUDSceneHandler>();
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
            classSelected = classDatas.Find(c => c.Id == UniqueType.GetAvatar(AvatarSubType.Promotion) || c.Id == (int)OtherClass.BUDSeries);
        }

        public override void Enter()
        {
            base.Enter();
            UI.shapeSectionList.SetCallback(OnSectionListSelected);
            UI.sectionList.SetCallback(OnSectionListSelected);
            UI.classList.SetCallback(OnClassListSelected);
            UI.classList.SetClass(classSelected, classDatas);
        }

        internal void OnDataChange(AssetsData[] changes)
        {
            if (!Enable) return;
            UI.assetsList.Data.ResetItems(assetsDatas.Count());
        }

        public override void OnDataRefresh()
        {
            base.OnDataRefresh();
            if (classSelected != null)
            {
                RefreshData(classSelected, false);
            }
        }

        private void RefreshData(ClassData data, bool isUseCache = true)
        {
            classSelected = data;
            UI.CloseAllSubUI();
            //UI.operationUI.SetSelectType(data.Id);
            assetsDatas.SetData(dataHandler.GetGoodsData(classSelected.Id), ClassGoods);

            UI.sectionList.gameObject.SetActive(true);
            UI.shapeRoot.gameObject.SetActive(data.Id == UniqueType.GetAvatar(AvatarSubType.Promotion));
            UI.contentRoot.gameObject.SetActive(data.Id != UniqueType.GetAvatar(AvatarSubType.Promotion));
            if (isUseCache && budSectionDic.ContainsKey(data))
            {
                pgcTuple = budSectionDic[data];
            }
            else
            {
                pgcTuple = new PgcTuple();
                if (data.Id == (int)OtherClass.BUDSeries)
                {
                    var series = dataHandler.GetSeriesList(Product.SeriesType.DefaultSeries);
                    OnSeriesUpdate(classSelected.Id, series);
                }
                else if (data.Id == (int)OtherClass.NewUserFeatured)
                {
                    var series = dataHandler.GetSeriesList(Product.SeriesType.NewUserFeatured);
                    OnSeriesUpdate(classSelected.Id, series);
                }
                else if (data.Id == UniqueType.GetAvatar(AvatarSubType.Promotion))
                {
                    //TODO 获取促销主题
                    var sections = dataHandler.GetSections(data.Id);
                    OnShapeSectionUpdate();
                }
                else
                {
                    var sections = dataHandler.GetSections(data.Id);
                    OnSectionUpdate(classSelected.Id, sections);
                }

                budSectionDic[data] = pgcTuple;
            }

            ColorUtility.TryParseHtmlString("#FFD400", out SectionUIData.SelectedColor);
            if (data.Id == UniqueType.GetAvatar(AvatarSubType.Promotion))
            {
                UI.shapeSectionList.SetSection(pgcTuple.Item1, pgcTuple.Item2);
            }
            else
            {
                UI.sectionList.SetSection(pgcTuple.Item1, pgcTuple.Item2);
            }
        }

        public void OnClassListSelected(ClassData data)
        {
            RefreshData(data);
        }

        internal void OnShapeSectionUpdate()
        {
            var list = new List<SectionUIData>();
            if (AssetsDataManager.ShapeThemePropList == null)
            {
                return;
            }
            for (int i = 0, C = AssetsDataManager.ShapeThemePropList.Count; i < C; i++)
            {
                var section = AssetsDataManager.ShapeThemePropList[i];
                Game.Store.SectionConfig config = new Game.Store.SectionConfig();
                config.backgroundColor = "#" + section.resourceBackgroundColor;
                config.selectColor = "#" + section.backgroundColor;
                config.textColor = "#" + section.GetTextColorList()[0];
                config.lineColor = "#" + section.GetTextColorList()[1];
                SectionUIData data = new SectionUIData(section.themeId.ToString(), section.themeName, Color.white);
                data.sectionConfig = config;
                list.Add(data);
            }
            pgcTuple.Item2 = list;
            if (pgcTuple.Item2 == null || pgcTuple.Item2.Count == 0)
            {
                UI.sectionList.SetSection(null, null);
                return;
            }

            if (!pgcTuple.Item2.Contains(pgcTuple.Item1)) pgcTuple.Item1 = pgcTuple.Item2[0];
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

            if (classSelected.Id == (int)OtherClass.BUDSeries || classSelected.Id == (int)OtherClass.NewUserFeatured)
            {
                if (data.seriesConfig == null)
                {
                    return;
                }
                assetsDatas.SetData(data.seriesConfig.GoodsList);
                var gashaponId = data.seriesConfig.GoodsList.FirstOrDefault()?.SourceData.Id;
                var ViewCfg = GashaponPanel.GashaponDataManager.Inst.GetGashaponView(gashaponId);
                if (ViewCfg == null)
                {
                    LoggerUtils.LogError("View == null Gashapon:" + gashaponId);
                    //return;
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

            if (classSelected.Id == UniqueType.GetAvatar(AvatarSubType.Promotion))
            {
                if (data.sectionConfig == null)
                {
                    return;
                }
                UI.shapeRoot.gameObject.SetActive(true);
                UI.contentRoot.gameObject.SetActive(false);
                if (AssetsDataManager.ShapeBannerDataList != null && AssetsDataManager.ShapeBannerDataList.Count > 0)
                {
                    UI.bannerRoot.gameObject.SetActive(true);
                    UI.bannerRoot.SetBanner(AssetsDataManager.ShapeBannerDataList);
                }
                else
                {
                    UI.bannerRoot.gameObject.SetActive(false);
                }
                UI.promotionRoot.CreatShapeItem(int.Parse(data.Id));
                UI.shapeSectionList.SetSelectedItem(data);
            }
            else
            {
                UI.shapeRoot.gameObject.SetActive(false);
                UI.contentRoot.gameObject.SetActive(true);
                UI.assetsList.gameObject.SetActive(true);
                UI.assetsList.OnItemSelected = OnItemSelected;
                UI.assetsList.Data.ResetItems(assetsDatas.Count());
            }
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


            if (pgcTuple.Item1.Id == "All") return true;

            if (pgcTuple.Item1.Id == "Coin")
            {
                return data.Price.CurrencyType == CurrencyType.Coin;
            }

            if (pgcTuple.Item1.Id == "Badge")
            {
                return data.Price.CurrencyType == CurrencyType.Badge;
            }

            if (pgcTuple.Item1.Id == "Gem")
            {
                return data.Price.CurrencyType == CurrencyType.Gem;
            }

            if (pgcTuple.Item1.Id == "Special")
            {
                return data.SourceData.Source != Source.Mall;
            }

            return false;
        }

        public override GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            if (assetsData == null)
            {
                return null;
            }
            assetsData.Selected = pgcTuple.Item3 == assetsData.Id;
            if (assetsData.Selected) RefreshUI(assetsData);
            return assetsData;
        }

        internal void OnItemSelected(GoodsData data)
        {

            //foreach (var asset in data.Assets) {
            //    if (asset.ResourceType == ResourceType.Avatar && UI.animationCtrl != null && !string.IsNullOrEmpty(UI.animationCtrl.specialAnimPgcId)) {
            //        var lastResData = DataTables.GetAvatarCommonData(UI.animationCtrl.specialAnimPgcId);
            //        var lastSubType = UniqueType.GetAvatar((AvatarSubType)lastResData.SubType);
            //        var specialConfig = DataTables.GetSpecialSkinConfig(data.Id);
            //        var pgcAssets = asset as PGCAssetsData;
            //        var subType = UniqueType.GetAvatar(pgcAssets.AvatarSubType);
            //        if (specialConfig != null && lastSubType != subType) {
            //            TipPanel.ShowToast("需先卸下当前动作道具才能穿戴新的哦");
            //            return;
            //        }
            //    }
            //}

            pgcTuple.Item3 = data.Id;

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
            UI.itemInfoUI.gameObject.SetActive(true);
            UI.itemInfoUI.SetTarget(data);

            UI.operationUI.gameObject.SetActive(true);
            UI.operationUI.SetTarget(data);
            UI.operationUI.OnOperation = (operation, target) =>
            {
                switch (operation)
                {
                    case Operation.ShoppingBuy:
                    case Operation.Buy:
                        Buy(data);
                        break;
                    case Operation.ShoppingCart:
                        ShoppingCart(data);
                        break;
                    case Operation.SkinTicketButton:
                        UseSkinTicket(data);
                        break;
                    case Operation.Wear:

                        //foreach (var asset in data.Assets) {
                        //    if (asset.ResourceType == ResourceType.Avatar && UI.animationCtrl != null && !string.IsNullOrEmpty(UI.animationCtrl.specialAnimPgcId)) {
                        //        var lastResData = DataTables.GetAvatarCommonData(UI.animationCtrl.specialAnimPgcId);
                        //        var lastSubType = UniqueType.GetAvatar((AvatarSubType)lastResData.SubType);
                        //        var specialConfig = DataTables.GetSpecialSkinConfig(data.Id);
                        //        var pgcAssets = asset as PGCAssetsData;
                        //        var subType = UniqueType.GetAvatar(pgcAssets.AvatarSubType);
                        //        if (specialConfig != null && lastSubType != subType) {
                        //            TipPanel.ShowToast("需先卸下当前动作道具才能穿戴新的哦");
                        //            return;
                        //        }
                        //    }
                        //}

                        UI.PutOn(data);
                        //UI.JumpToBagGoods(classSelected.Id, data);
                        if (UI.isCharacterFittingRoom)
                        {
                            UI.animationCtrl.PlayerChangeClothesForUICharacer();
                        }
                        else
                        {
                            UI.petAnimationCtrl.PlayChangeClothAni();
                        }
                        break;
                    case Operation.Jump:
                        UI.CancelEmote();
                        data.SourceData.Source.HandSkip(data.SourceData.Id);
                        break;
                    case Operation.TryPlayMusic:
                        UI.TryPlayMusic();
                        break;
                    case Operation.ChangeOtherOc:
                        UI.ChangeOtherOc();
                        break;
                    case Operation.Select:
                        UI.OnSelect(data);
                        break;
                }
            };
        }
    }

    #endregion

    // pgc动作商城

    #region ActionScene

    public class ActionScene : BaseScene
    {
        public class PgcTuple
        {
            public SectionUIData Item1;
            public List<SectionUIData> Item2;
        }

        internal Dictionary<string, Color> BgColors = new Dictionary<string, Color>();
        protected AvatarBUDSceneHandler dataHandler;
        protected Dictionary<ClassData, PgcTuple> classSectionDict = new();
        private PgcTuple pgcTuple;
        private string pgcId;

        internal ActionScene(FittingRoomPanel ui) : base(ui)
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
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.LinkEmote), "Emote9"),
                };
                classSelected = classDatas.Find(c => c.Id == UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.SingleAll));
                dataHandler = AssetsDataManager.GetData<AvatarBUDSceneHandler>();
            }
            else
            {
                classDatas = new List<ClassData>
                {
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetSingleAll), "PetEmote1"),
                    new ClassData(UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetWithPlayerAll), "PetEmote2"),
                };
                classSelected = classDatas.Find(c => c.Id == UniqueType.Get(ResourceType.Emote, (int)EmoteSubType.PetSingleAll));
                dataHandler = AssetsDataManager.GetData<PetAvatarBUDSceneHandler>();
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

        public override void OnDataRefresh()
        {
            base.OnDataRefresh();
            if (classSelected != null)
            {
                RefreshData(classSelected, false);
            }
        }

        public override void Enter()
        {
            base.Enter();
            ColorUtility.TryParseHtmlString("#AEA6CC", out UI.assetsList.BgColor);
            UI.sectionList.SetCallback(OnSectionListSelected);
            UI.classList.SetCallback(OnClassListSelected);
            UI.classList.SetClass(classSelected, classDatas);
        }

        private void RefreshData(ClassData data, bool isUseCache = true)
        {
            classSelected = data;
            pgcId = null;

            UI.CloseAllSubUI();
            assetsDatas.SetData(dataHandler.GetGoodsData(classSelected.Id), ClassGoods);

            UI.sectionList.gameObject.SetActive(true);

            if (isUseCache && classSectionDict.ContainsKey(data))
            {
                pgcTuple = classSectionDict[data];
            }
            else
            {
                pgcTuple = new PgcTuple();
                var sections = dataHandler.GetSections(data.Id);
                OnSectionUpdate(classSelected.Id, sections);
                classSectionDict[data] = pgcTuple;
            }

            ColorUtility.TryParseHtmlString("#FFD400", out SectionUIData.SelectedColor);
            UI.sectionList.SetSection(pgcTuple.Item1, pgcTuple.Item2);
        }

        public void OnClassListSelected(ClassData data)
        {
            RefreshData(data);
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

            if (pgcTuple.Item1.Id == "All") return true;

            if (pgcTuple.Item1.Id == "Coin")
            {
                return data.Price.CurrencyType == CurrencyType.Coin;
            }

            if (pgcTuple.Item1.Id == "Badge")
            {
                return data.Price.CurrencyType == CurrencyType.Badge;
            }

            if (pgcTuple.Item1.Id == "Gem")
            {
                return data.Price.CurrencyType == CurrencyType.Gem;
            }

            if (pgcTuple.Item1.Id == "Special")
            {
                return data.SourceData.Source != Source.Mall;
            }

            return false;
        }

        public override GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            if (assetsData == null)
            {
                return null;
            }
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

            // Ugc

            UI.operationUI.gameObject.SetActive(true);
            UI.operationUI.SetTarget(data);
            UI.operationUI.OnOperation = (operation, target) =>
            {
                switch (operation)
                {
                    case Operation.ShoppingBuy:
                    case Operation.Buy:
                        Buy(data);
                        break;
                    case Operation.SkinTicketButton:
                        UseSkinTicket(data);
                        break;
                    case Operation.Jump:
                        UI.CancelEmote();
                        data.SourceData.Source.HandSkip(data.SourceData.Id);
                        break;
                    case Operation.ChangeOtherOc:
                        UI.ChangeOtherOc();
                        break;
                }
            };

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

    public class UGCScene : BaseScene
    {
        public class UgcTuple
        {
            public SectionUIData Item1;
            public List<SectionUIData> Item2;
            public string Item3;
            public bool Search;
            public string SearchKey;
            public CurrencyType CurrencyPickerType;
        }

        internal List<Color> BgColors = new List<Color>();
        protected Dictionary<ClassData, UgcTuple> classUgcDict = new();
        protected UgcTuple ugcTuple;

        protected AvatarUgcSceneHandler dataHandler;
        SearchPanel.SearchType _searchType;
        private FittingRoomAdapter ActiveList => UI.assetsList;

        internal UGCScene(FittingRoomPanel ui) : base(ui)
        {
        }

        public override void InitParams()
        {

            if (UI.isCharacterFittingRoom)
            {
                classDatas = new List<ClassData> {
                    new ClassData((int)OtherClass.UgcTheme, "UgcTheme"),
                    new ClassData((int)OtherClass.LikeList, "UgcLike"),
                    new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Bundle), "Bundle"),
                    new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Clothes), "Clothes"),
                       new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Hair), "Hair"),
                                     new ClassData(UniqueType.Get(ResourceType.UgcEmote, (int)UgcAnimSubType.PeopleAll), "UgcAnim"),
                    new ClassData(UniqueType.Get(ResourceType.Theatre,(int)UgcTheatreSubType.Theatre),"Theatre"),
                    new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Eyes), "Eyes"),
                    new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Glasses), "Glasses"),
                   new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Backpack), "Backpack"),
                    new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Hats), "Hats"),
                   new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Hand), "Hand"),
                   new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Shoe), "Shoe"),
                   new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Mouth), "Mouth"),
                   new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.FacePaint), "FacePaint"),
                   new ClassData(UniqueType.Get(ResourceType.UgcVehicle, (int)VehicleSubType.FittingRoomVehicle), "Vehicle"),
                   new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.MusicalInstrument), "MusicalInstrument"),
                    // 乐谱已迁移至 MusicStorePanel，先注释掉 Ugc 标签的乐谱入口（勿删原代码）
                    // new ClassData(UniqueType.Get(ResourceType.MusicScore, (int)MusicScoreSubType.GeneralMusicScore),"MusicScore"),
                    // 演员卡已迁移至 ActorCardStorePanel，先注释掉 Ugc 标签的演员卡入口（勿删原代码）
                    // new ClassData(UniqueType.Get(ResourceType.AvatarCard,(int)UgcTheatreSubType.AvatarCard),"ActorCar"),
                    // 姿势已迁移至 PoseStorePanel，先注释掉 Ugc 标签的姿势入口（勿删原代码）
                    // new ClassData(UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.PeopleAll), "UgcPose"),
                };
                dataHandler = AssetsDataManager.GetData<AvatarUgcSceneHandler>();
            }
            else
            {
                classDatas = new List<ClassData> {
                    new ClassData((int)OtherClass.UgcTheme, "UgcTheme"),
                    new ClassData((int)OtherClass.LikeList, "UgcLike"),
                    new ClassData(UniqueType.Get(ResourceType.UgcEmote, (int)UgcAnimSubType.PetAll), "UgcAnim_Pet"),
                    new ClassData(UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.PetAll), "UgcPose_Pet"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Skin), "PetSkin"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Bundle), "PetBundle"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Clothes), "PetClothes"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Ear), "PetEar"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Hair), "PetHair"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Hats), "PetHats"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Scarf), "PetScarf"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Glasses), "PetGlasses"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Eyes), "PetEyes"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Mouth), "PetMouth"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.FacePaint), "PetFacePaint"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Tail), "PetTail"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Backpack), "PetBackpack"),
                    new ClassData(UniqueType.GetUGCPetAvatar(AvatarSubType.Shoe), "PetShoe"),
                };
                dataHandler = AssetsDataManager.GetData<PetAvatarUgcSceneHandler>();
            }


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
            classSelected = classDatas.Find(c => c.Id == (int)OtherClass.UgcTheme);
        }

        public override void Enter()
        {
            base.Enter();
            UI.sectionList.SetCallback(OnSectionListSelected);
            UI.classList.SetCallback(OnClassListSelected);
            UI.classList.SetClass(classSelected, classDatas);
            if (UI.isCharacterFittingRoom) UI.setLabelBt.gameObject.SetActive(true);

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

        // 点击 shoppingCartBtn 时调用：将 ShoppingCart 项插入 ClassList 第一位并选中
        public void AddShoppingCartClass()
        {
            var shoppingCart = classDatas.Find(c => c.Id == (int)OtherClass.ShoppingCart);
            if (shoppingCart == null)
            {
                shoppingCart = new ClassData((int)OtherClass.ShoppingCart, "ShoppingCart");
                classDatas.Insert(0, shoppingCart);
            }
            classSelected = shoppingCart;
            UI.classList.SetClass(classSelected, classDatas);
        }

        // 点击 ShoppingCartRoot 的 btn_close 时调用：从 ClassList 移除 ShoppingCart 项
        public void RemoveShoppingCartClass()
        {
            var idx = classDatas.FindIndex(c => c.Id == (int)OtherClass.ShoppingCart);
            if (idx < 0) return;
            classDatas.RemoveAt(idx);
            // 当前选中如果是 ShoppingCart，则回退到剩余列表的第一个
            if (classSelected != null && classSelected.Id == (int)OtherClass.ShoppingCart)
            {
                classSelected = classDatas.Count > 0 ? classDatas[0] : null;
            }
            UI.classList.SetClass(classSelected, classDatas);
        }

        /// <summary>
        /// Ugc 左侧分类列表点击事件
        /// </summary>
        /// <param name="data"></param>
        public void OnClassListSelected(ClassData data)
        {
            classSelected = data;

            // 购物车打开期间,ClassList 变为筛选购物车(需在 CloseAllSubUI 前判断,它会隐藏购物车面板)
            bool cartOpen = UI.ShoppingCartRoot != null && UI.ShoppingCartRoot.activeSelf;

            UI.CloseAllSubUI();

            // ShoppingCart 项或购物车打开期间:展示/按选中类型筛选购物车,不走常规分类逻辑
            if (data.Id == (int)OtherClass.ShoppingCart || cartOpen)
            {
                if (UI.ShoppingCartRoot != null)
                {
                    if (UI.ShoppingCartRoot.TryGetComponent<ShoppingCartRootView>(out var view))
                    {
                        // 每次进入购物车分支都同步宠物/人物标记，防止试衣间模式切换后显示错误
                        // Open() 若已激活则不触发 OnEnable，先同步 _forPet 再 SetClassFilter(已含Refresh)
                        view.Open(!UI.isCharacterFittingRoom);
                        // ShoppingCart 项=显示全部;其它类=按该类型筛选；SetClassFilter 内部已调 Refresh
                        view.SetClassFilter(data.Id == (int)OtherClass.ShoppingCart ? 0 : data.Id);
                    }
                }
                return;
            }

            UI.searchButton.gameObject.SetActive(true);
            if (UI.isCharacterFittingRoom) UI.setLabelBt.gameObject.SetActive(true);
            UI.getMorePinkCoin?.gameObject.SetActive(true);

            UI.getMorePinkCoin?.onClick.RemoveAllListeners();
            UI.getMorePinkCoin?.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanelTakeAni<GetMorePinkCoinPanel>(PanelId.GetMorePinkCoinPanel);
            });

            int ugcType = AvatarUgcSceneHandler.GetUgcType(classSelected.Id);
            if (UI.isCharacterFittingRoom)
            {
                if (data.Id == (int)OtherClass.UgcTheme)
                {
                    //第一栏特殊
                    SearchLogicMgr.Inst.GetSearchSettingSectionList(99, ugcType, null);
                }
                else
                {
                    int subType = 0;
                    if (ugcType == (int)UgcType.MusicScore || ugcType == (int)UgcType.Anim || ugcType == (int)UgcType.Pose || ugcType == (int)UgcType.ActorCard
                    || ugcType == (int)UgcType.Theatre)
                    {
                        subType = 0;
                    }
                    else
                    {
                        subType = (int)UniqueType.AvatarSubType(classSelected.Id);
                    }
                    SearchLogicMgr.Inst.GetSearchSettingSectionList(subType, ugcType, null);
                }
            }
            // UI.currencyPicker.gameObject.SetActive(true);

            // UI.currencyPicker.SetCallback(currencyPickType =>
            // {
            //    OnCurrencyPickerSelected(currencyPickType);
            // });
            UI.searchButton.onClick.AddListener(() =>
            {
                if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
                {
                    LoadEvent.ReportPopupStatus("SearchDesignClick", "ClickSearchDesign");
                }
            });
            UI.searchButton.gameObject.SetActive(true);
            if (data.Id == (int)OtherClass.LikeList)
            {
                //点赞页不显示搜索按钮
                UI.searchButton.gameObject.SetActive(false);
            }
            UI.filterView.SetClassType(classSelected.Id);
            UI.filterView.gameObject.SetActive(false);
            UI.mainTabsUI.gameObject.SetActive(true);
            switch (data.Id)
            {
                case 60001:
                    // 乐谱
                    UI.EnterPreviewMusicScore();
                    UI.searchButton.onClick.RemoveAllListeners();
                    UI.searchButton.onClick.AddListener(() =>
                    {
                        OnSearchClick();
                        _searchType = SearchPanel.SearchType.MusicScore;
                        // UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel, SearchPanel.SearchType.MusicScore,
                        //     (int)AvatarSubType.All);
                    });
                    break;
                case 50024:
                    // 乐器
                    UI.searchButton.onClick.RemoveAllListeners();
                    UI.searchButton.onClick.AddListener(() =>
                    {
                        OnSearchClick();
                        _searchType = SearchPanel.SearchType.MusicInstrument;
                        // UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel,
                        //     SearchPanel.SearchType.MusicInstrument, (int)AvatarSubType.All);
                    });
                    break;
                case 90100:
                    UI.searchButton.onClick.RemoveAllListeners();
                    UI.searchButton.onClick.AddListener(() =>
                    {
                        OnSearchClick();
                        _searchType = SearchPanel.SearchType.UgcEmote;
                        // UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel,
                        //     SearchPanel.SearchType.UgcEmote, (int)AvatarSubType.All);
                    });
                    break;
                case 110100:
                    UI.searchButton.onClick.RemoveAllListeners();
                    UI.searchButton.onClick.AddListener(() =>
                    {
                        OnSearchClick();
                        _searchType = SearchPanel.SearchType.UgcPose;
                        // UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel,
                        //     SearchPanel.SearchType.UgcPose, (int)AvatarSubType.All);
                    });
                    break;
                case 90200:
                    UI.searchButton.onClick.RemoveAllListeners();
                    UI.searchButton.onClick.AddListener(() =>
                    {
                        OnSearchClick();
                        _searchType = SearchPanel.SearchType.UgcEmote;
                        // UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel,
                        //     SearchPanel.SearchType.UgcEmote, (int)AvatarSubType.All);
                    });
                    break;
                case 110200:
                    UI.searchButton.onClick.RemoveAllListeners();
                    UI.searchButton.onClick.AddListener(() =>
                    {
                        OnSearchClick();
                        _searchType = SearchPanel.SearchType.UgcPose;
                        // UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel,
                        //     SearchPanel.SearchType.UgcPose, (int)AvatarSubType.All);
                    });
                    break;
                case 170100:
                    UI.searchButton.onClick.RemoveAllListeners();
                    UI.searchButton.onClick.AddListener(() =>
                    {
                        OnSearchClick();
                        _searchType = SearchPanel.SearchType.Vehicle;
                        // UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel,
                        //     SearchPanel.SearchType.UgcPose, (int)AvatarSubType.All);
                    });
                    break;
                default:
                    UI.searchButton.onClick.RemoveAllListeners();
                    UI.searchButton.onClick.AddListener(() =>
                    {
                        OnSearchClick();
                        _searchType = UI.isCharacterFittingRoom ? SearchPanel.SearchType.Skin : SearchPanel.SearchType.PetSkin;
                        // UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel, UI.isCharacterFittingRoom ? SearchPanel.SearchType.Skin : SearchPanel.SearchType.PetSkin,
                        //     (int)AvatarSubType.All);
                    });
                    break;
            }

            UI.sectionList.SetSection(null, null);
            if (!UI.filterView.IsFilterMode())
            {
                UI.sectionList.gameObject.SetActive(true);
            }

            if (classUgcDict.ContainsKey(data))
            {
                ugcTuple = classUgcDict[data];
                ColorUtility.TryParseHtmlString("#905CFF", out SectionUIData.SelectedColor);
                UI.sectionList.SetSection(ugcTuple.Item1, ugcTuple.Item2, true);
                if (ugcTuple.Search)
                {
                    UI.sectionList.ShowSearch(OnSearchClick);
                }
                if (UI.isCharacterFittingRoom && data.Id != (int)OtherClass.LikeList)
                {
                    UI.sectionList.ShowFilter(OnFilterClick);
                    UI.sectionList.ShowSetting(OnSettingClick);
                }
            }
            else
            {
                ugcTuple = new UgcTuple();
                int ugcRecommendId;
                switch (data.Id)
                {
                    case (int)OtherClass.UgcTheme:
                        ugcRecommendId = 99;
                        break;
                    case (int)OtherClass.LikeList:
                        ugcRecommendId = (int)OtherClass.LikeList;
                        break;
                    default:
                        ugcTuple.Search = true;
                        ugcRecommendId = (int)UniqueType.AvatarSubType(data.Id);
                        break;
                }
                dataHandler.GetSections(data.Id, ugcRecommendId, OnSectionUpdate);
            }
            UI.sectionList.Init(UI.isCharacterFittingRoom && data.Id != (int)OtherClass.LikeList);
        }

        internal void OnSectionUpdate(int classType, List<Game.Store.UgcSectionData> sectionDatas)
        {
            if (!Enable || classType != classSelected.Id) return;
            if (sectionDatas == null) return;
            if (UI.filterView.IsFilterMode())
            {
                return;
            }
            if (UI.searchView.isSearching)
            {
                return;
            }

            var list = new List<SectionUIData>();
            for (int i = 0, C = sectionDatas.Count; i < C; i++)
            {
                var section = sectionDatas[i];
                var sectionData =
                    new SectionUIData(section.sectionId, section.sectionName, BgColors[i % BgColors.Count]);
                if (classType == (int)OtherClass.UgcTheme)
                {
                    sectionData.sectionConfig = section.sectionCfg;
                }
                else
                {
                    sectionData.sectionConfig = null;
                }

                list.Add(sectionData);
            }

            ugcTuple.Item2 = list;
            if (ugcTuple.Item2 == null || ugcTuple.Item2.Count == 0)
            {
                UI.sectionList.SetSection(null, null);
                return;
            }

            if (!ugcTuple.Item2.Contains(ugcTuple.Item1)) ugcTuple.Item1 = ugcTuple.Item2[0];

            ColorUtility.TryParseHtmlString("#905CFF", out SectionUIData.SelectedColor);
            UI.sectionList.SetSection(ugcTuple.Item1, ugcTuple.Item2, true);

            if (ugcTuple.Search)
            {
                UI.sectionList.ShowSearch(OnSearchClick);
            }
            if (UI.isCharacterFittingRoom && classSelected.Id != (int)OtherClass.LikeList)
            {
                UI.sectionList.ShowFilter(OnFilterClick);
                UI.sectionList.ShowSetting(OnSettingClick);
            }
        }

        internal void OnSearchClick()
        {
            UI.filterView.gameObject.SetActive(false);
            UI.classList.HideClass((int)OtherClass.LikeList); //搜索页 隐藏点赞栏
            UI.mainTabsUI.gameObject.SetActive(false);
            UI.sectionList.gameObject.SetActive(false);

            ActiveList.Data.ResetItems(0);
            ActiveList.gameObject.SetActive(false);
            UI.bundleItemsList.gameObject.SetActive(false);
            UI.searchView.isFittingRoom = true;
            UI.searchView.gameObject.SetActive(true);
            UI.searchView.SetSearchAction(OnSearchAction, OnClearAction, OnCancelAction);
            UI.searchView.SetInitSearchAction(OnSearchClick);
            UI.searchView.Show();
        }

        internal void OnFilterClick()
        {
            UI.ShowTip("");

            UI.mainTabsUI.gameObject.SetActive(false);
            UI.sectionList.gameObject.SetActive(false);
            UI.filterView.gameObject.SetActive(true);
            UI.bundleItemsList.gameObject.SetActive(false);

            ActiveList.Data.ResetItems(0);
            ActiveList.gameObject.SetActive(false);
            UI.filterView.SetFilterAction(OnCancelAction);
            UI.filterView.SetClassType(classSelected.Id);
            UI.filterView.Show(classSelected.Id == (int)OtherClass.UgcTheme);
        }

        internal void OnSettingClick()
        {
            var k = UIManager.Inst.OpenPanelTakeAni<FittingRecommendationPopPanel>(PanelId.FittingRecommendationPopPanel);
            int ugcType = AvatarUgcSceneHandler.GetUgcType(classSelected.Id);
            if (classSelected.Id == (int)OtherClass.UgcTheme)
            {
                k.SetData(99, ugcType);
            }
            else
            {
                if (ugcType == (int)UgcType.MusicScore || ugcType == (int)UgcType.Anim || ugcType == (int)UgcType.Pose)
                {
                    k.SetData(0, ugcType);
                }
                else
                {
                    k.SetData((int)UniqueType.AvatarSubType(classSelected.Id), ugcType);
                }
            }
            k.closeFunc = () =>
            {
                dataHandler.ClearSections(classSelected.Id);
                OnClassListSelected(classSelected);
            };
        }

        internal void OnSearchAction(string str)
        {
            UI.ShowTip("搜索中");
            SearchLogicMgr.Inst.AddSearchHistoryWord(str);
            if (!UI.filterView.IsFilterMode())
            {
                ActiveList.gameObject.SetActive(true);
            }
            ActiveList.Data.ResetItems(0);

            ugcTuple.SearchKey = str;
            Action nextAction = null;
            if (UI.isCharacterFittingRoom && classSelected.Id == (int)OtherClass.UgcTheme)
            {
                nextAction = dataHandler.SearchGoodsData(99, str, OnSearchItemDataChange);
            }
            else
            {
                nextAction = dataHandler.SearchGoodsData(classSelected.Id, str, OnSearchItemDataChange);
            }
            ActiveList.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            ActiveList.PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(() => nextAction?.Invoke());

            if (!string.IsNullOrEmpty(str) && Regex.IsMatch(str, @"^[0-9A-Z]{7}$"))
            {
                SearchPanel.Search(str, (int)AvatarSubType.All, _searchType, false);
            }

        }

        internal void OnClearAction()
        {
            ugcTuple.SearchKey = null;
            UI.ShowTip("");
            ActiveList.Data.ResetItems(0);
            ActiveList.gameObject.SetActive(false);
        }

        internal void OnCancelAction()
        {
            UI.classList.ShowClass((int)OtherClass.LikeList); //搜索页 显示点赞栏
            if (!UI.filterView.IsFilterMode())
            {
                ActiveList.gameObject.SetActive(true);
            }
            UI.bundleItemsList.gameObject.SetActive(false);
            UI.sectionList.gameObject.SetActive(true);
            UI.mainTabsUI.gameObject.SetActive(true);
            UI.searchView.gameObject.SetActive(false);

            OnClassListSelected(classSelected);
        }

        internal void OnSearchItemDataChange(string searchKey, bool isEnd, List<GoodsData> datas)
        {
            if (!UI.gameObject) return;
            UI.searchView.isSearching = false;
            ActiveList.PullToRefreshBehaviour.HideGizmo();
            if (!Enable || searchKey != ugcTuple.SearchKey) return;
            if (isEnd && (datas == null || datas.Count == 0))
            {
                UI.ShowTip("没有找到相关内容");
                ActiveList.Data.ResetItems(0);
                return;
            }

            UI.ShowTip("");
            if (UI.isCharacterFittingRoom) datas?.RemoveAll(IsMovedToStorePanel);
            assetsDatas.SetData(datas);
            if (!UI.filterView.IsFilterMode())
            {
                ActiveList.gameObject.SetActive(true);
            }
            UI.searchView.HideSearchDiscoverAndHistoryView();
            ActiveList.OnItemSelected = OnItemSelected;
            ActiveList.Data.ResetItems(assetsDatas.Count());
        }

        /// <summary>
        /// 当前分类下 section 列表点击
        /// </summary>
        /// <param name="data"></param>
        internal void OnSectionListSelected(SectionUIData data)
        {
            ugcTuple.Item1 = data;

            if (classSelected.Id == (int)OtherClass.LikeList)
            {
                UI.ShowTip("");
            }

            if (classSelected.Id == (int)OtherClass.UgcTheme && data.sectionConfig != null)
            {
                UI.customBg.gameObject.SetActive(true);
                UI.customBg.Load(data.sectionConfig.backgroundUrl);

                ColorUtility.TryParseHtmlString(data.sectionConfig.selectColor, out ActiveList.BgColor);
                ColorUtility.TryParseHtmlString(data.sectionConfig.backgroundColor, out ActiveList.SelectedColor);
                var color = DataUtil.DeSerializeColorCheckHash(data.sectionConfig.backgroundConfigColor);
                color.a = 0;
                UI.avatarCameraController.roleCamera.backgroundColor = color;
            }
            else
            {
                ActiveList.ResetColor();
            }
            var nextAction = dataHandler.GetGoodsDatas(classSelected.Id, data.Id, 0, OnItemDataChange);
            ActiveList.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            ActiveList.PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(() => nextAction?.Invoke());
        }

        internal void OnCurrencyPickerSelected(CurrencyType currencyPickerType)
        {
            ugcTuple.CurrencyPickerType = currencyPickerType;
            var nextAction = dataHandler.GetGoodsDatas(classSelected.Id, ugcTuple.Item1.Id, (int)currencyPickerType, OnItemDataChange);
            ActiveList.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            ActiveList.PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(() => nextAction?.Invoke());
        }

        // 乐谱/演员卡/姿势已迁移到独立商城，不在 UGC 主列表显示
        private static bool IsMovedToStorePanel(GoodsData g)
        {
            return g != null && (g.subType == 1000 + (int)UgcType.MusicScore
                              || g.subType == 1000 + (int)UgcType.Pose
                              || g.subType == 1000 + (int)UgcType.ActorCard);
        }

        internal void OnItemDataChange(string sectionsId, List<GoodsData> datas, bool isEnd)
        {
            if (UI == null || UI.gameObject == null) return;
            if (UI.isCharacterFittingRoom) datas?.RemoveAll(IsMovedToStorePanel);
            ActiveList.PullToRefreshBehaviour.HideGizmo();
            if (!Enable || ugcTuple.Item1 == null || sectionsId != ugcTuple.Item1.Id) return;
            if (UI.searchView.isSearching)
            {
                return;
            }
            bool isFilterMode = UI.filterView.IsFilterMode();
            if (classSelected.Id == (int)OtherClass.UgcTheme)
            {
                bool isEmptyData = datas.Count == 1;  //有一个"去创作"
                var msg = isEmptyData && !isFilterMode ? LocalizationManager.Inst.GetLocalizedText("暂无符合条件的商品，试试调整筛选条件") : "";
                UI.ShowTip(msg);
            }
            else if (classSelected.Id == (int)OtherClass.LikeList)
            {
                bool isEmptyData = datas.Count == 0;
                var msg = isEmptyData && !isFilterMode ? LocalizationManager.Inst.GetLocalizedText("暂无点赞的商品，可以去商城逛逛哦") : "";
                UI.ShowTip(msg);
                bool showNoMoreData = isEnd && !isEmptyData;
                //UI.NoMoreDataTips.gameObject.SetActive(showNoMoreData);
                if (showNoMoreData)
                {
                    for (int i = 0; i < 4; i++) datas.Add(new GoodsData()
                    {
                        Id = $"{classSelected.Id}",
                        ButtonType = ButtonType.NoMoreTips,
                    });
                }
            }
            assetsDatas.SetData(datas);
            if (!UI.filterView.IsFilterMode())
            {
                ActiveList.gameObject.SetActive(true);
            }
            ActiveList.OnItemSelected = OnItemSelected;
            ActiveList.Data.ResetItems(assetsDatas.Count());

            // 切换到剧本演员分类时，自动选中第一个
            if (UniqueType.ResourceType(classSelected.Id) == ResourceType.AvatarCard
                && string.IsNullOrEmpty(ugcTuple.Item3)
                && assetsDatas.Count() > 0)
            {
                var firstItem = assetsDatas.Get(1);
                if (firstItem != null) OnItemSelected(firstItem);
            }
        }

        internal void OnDataChange(AssetsData[] changes)
        {
            if (!Enable) return;
            assetsDatas.Refresh();
            ActiveList.Data.ResetItems(assetsDatas.Count());
        }

        public override GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            if (assetsData == null)
            {
                return null;
            }
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

            if (data.Assets != null && data.Assets.Count > 0 && data.Assets[0].ResourceType == ResourceType.Theatre)
            {
                var theatreInfo = (data.Assets[0] as UgcTheatreAssetsData)?.UgcInfo?.theatreInfo;
                if (theatreInfo != null)
                    UIManager.Inst.OpenPanel(PanelId.TheatreInfoPanel, theatreInfo, (int)TheatreEnterType.Store);
            }

            ActiveList.Data.ResetItems(assetsDatas.Count());
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

            UI.itemInfoUI.gameObject.SetActive(true);
            UI.itemInfoUI.SetTarget(data);

            UI.operationUI.gameObject.SetActive(true);
            UI.operationUI.SetTarget(data);
            UI.operationUI.OnOperation = (operation, target) =>
            {
                switch (operation)
                {
                    case Operation.Wear:
                        UI.PutOn(data);
                        //UI.JumpToBagGoods(classSelected.Id, data);
                        if (UI.isCharacterFittingRoom)
                        {
                            UI.animationCtrl.PlayerChangeClothesForUICharacer();
                        }
                        else
                        {
                            UI.petAnimationCtrl.PlayChangeClothAni();
                        }
                        break;
                    case Operation.ShoppingBuy:
                    case Operation.Buy:
                        Buy(data);
                        break;
                    case Operation.SkinTicketButton:
                        UseSkinTicket(data);
                        break;
                    case Operation.TryPlayMusic:
                        UI.TryPlayMusic();
                        break;
                    case Operation.LikeUgc:
                        UI.operationUI.Like();
                        break;
                    case Operation.ChangeOtherOc:
                        UI.ChangeOtherOc();
                        break;
                    case Operation.Select:
                        UI.OnSelect(data);
                        break;
                }
            };
            UI.discountCardContainer.gameObject.SetActive(false);
            if (data.GoodsType == GoodsType.BundleUgc)
            {
                UI.bundleItemsList.gameObject.SetActive(true);
                UI.bundleItemsList.SetTarget(data);
            }
            else
            {
                if (DiscountCardUtils.IsSupportDiscountCard(data) && !DiscountCardUtils.IsOwnedDiscountCard())
                {
                    UI.discountCardContainer.gameObject.SetActive(true);
                }
                UI.bundleItemsList.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    // 背包

    #region BagScene

    public class BagScene : BaseScene
    {
        // 缓存选项数据
        public class BagTuple
        {
            public BagTabs.Tab Item1;
            public UgcSource.Source Item2;
            public string Item3;
            public bool Item4;
        }

        protected Dictionary<ClassData, BagTuple> classBagDict = new();
        protected BagTuple bagTuple;
        protected AvatarBagSceneHandler dataHandler;
        protected List<Color> skinColors;
        //protected List<int> shapeDataList = new List<int>() { 0, 1 , 2, 3, 4, 5,6 };//TODO先临时定义体型ID，后续看怎么调整
        private bool _isSwitchingCategory = false;
        private Coroutine _resetFlagCoroutine;
        // 上一次穿戴的套装子部件 classType 集合，用于"脱下套装"按钮按这些 classType 挨个 TakeOff
        private readonly List<int> _lastWornBundleClassTypes = new();
        // 当前 OC（设子）原始衣服的 avatarJson 快照——穿套装不会污染它，脱套装时用来恢复
        private string _ocBaselineJson;
        private FittingRoomAdapter ActiveList => UI.assetsList;
        protected const int MusicScoreId = 60001;
        protected const int UgcEmote = 90100;
        protected const int UgcPose = 110100;
        protected const string PetSizeId = "79700001";
        protected int CurSelectShapeType = -1;
        internal BagScene(FittingRoomPanel ui) : base(ui)
        {
        }

        /// <summary>
        /// 初始化参数
        /// </summary>
        public override void InitParams()
        {

            if (UI.isCharacterFittingRoom)
            {
                classDatas = new List<ClassData> {
                    new ClassData((int)OtherClass.Oc, "Oc"),
                    new ClassData(UniqueType.GetUgcAvatar(AvatarSubType.Bundle), "Suit"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Skin), "Skin"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Shape), "Shape"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Hair), "Hair"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Clothes), "Clothes"),
                                    new ClassData(UniqueType.Get(ResourceType.UgcEmote, (int)UgcAnimSubType.PeopleAll), "UgcAnim"),
                                                       new ClassData(UniqueType.Get(ResourceType.UgcVehicle, (int)VehicleSubType.FittingRoomVehicle), "Vehicle"),
                                                             new ClassData(UniqueType.GetAvatar(AvatarSubType.SpecialSkin), "SpecialSkin"),
                                                                                 new ClassData(UniqueType.GetAvatar(AvatarSubType.Effect), "Effect"),
                                                                                        new ClassData(UniqueType.GetAvatar(AvatarSubType.Eyes), "Eyes"),
                                                                                             new ClassData(UniqueType.GetAvatar(AvatarSubType.Glasses), "Glasses"),
                                                                                                 new ClassData(UniqueType.GetAvatar(AvatarSubType.Backpack), "Backpack"),
                                                                                                        new ClassData(UniqueType.GetAvatar(AvatarSubType.Hats), "Hats"),
                                                                                                        new ClassData(UniqueType.GetAvatar(AvatarSubType.Hand), "Hand"),
                                                                                                                       new ClassData(UniqueType.GetAvatar(AvatarSubType.Shoe), "Shoe"),
                                                                                                                       new ClassData(UniqueType.GetAvatar(AvatarSubType.Mouth), "Mouth"),
                                                                                                                            new ClassData(UniqueType.GetAvatar(AvatarSubType.FacePaint), "FacePaint"),
                                                                                                                                                new ClassData(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), "MusicalInstrument"),
                                                                                                                                                    new ClassData(UniqueType.Get(ResourceType.MusicScore, (int)MusicScoreSubType.GeneralMusicScore), "MusicScore"),
                                                                                                                                                                      new ClassData(UniqueType.GetAvatar(AvatarSubType.Blush), "Blush"),
                                                                                                                                                                             new ClassData(UniqueType.GetAvatar(AvatarSubType.Head), "Head"),
                                                                                                                                                                                 new ClassData(UniqueType.GetAvatar(AvatarSubType.Brow), "Brow"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Nose), "Nose"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Earring), "Earring"),
                                        new ClassData(UniqueType.GetAvatar(AvatarSubType.Visor), "Visor"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Scarf), "Scarf"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Crossbody), "Crossbody"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Belt), "Belt"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Glove), "Glove"),
                    new ClassData(UniqueType.GetAvatar(AvatarSubType.Cape), "Cape"),
                    new ClassData(UniqueType.Get(ResourceType.Theatre,(int)UgcTheatreSubType.AvatarCard),"Theatre"),
                    new ClassData(UniqueType.Get(ResourceType.AvatarCard,(int)UgcTheatreSubType.AvatarCard),"ActorCar"),
                    new ClassData(UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.PeopleAll), "UgcPose"),

                };
                dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
            }
            else
            {
                classDatas = new List<ClassData> {
                    //TODO： 第一阶段暂不增加设置
                    new ClassData((int)OtherClass.Oc, "PetOc"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Skin), "PetSkin"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Size), "PetSize"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Clothes), "PetClothes"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Ear), "PetEar"),
                    new ClassData(UniqueType.GetPGCPetAvatar(AvatarSubType.Hair), "PetHair"),
                    new ClassData(UniqueType.Get(ResourceType.UgcEmote, (int)UgcAnimSubType.PetAll), "UgcAnim_Pet"),
                    new ClassData(UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.PetAll), "UgcPose_Pet"),
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
                dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
            }



            skinColors = new();
            var list = AvatarColorManager.GetColorList(AvatarColorManager.SKIN_COM);
            list.ForEach(c =>
            {
                if (ColorUtility.TryParseHtmlString(c, out Color color)) skinColors.Add(color);
            });


            dataHandler.AddDataChange(UI.gameObject, OnDataChange);
            OnRedDotUpdate();
            classSelected = classDatas.Find(c => c.Id == (int)OtherClass.Oc);

        }

        internal void OnDataChange(AssetsData[] changes)
        {
            if (_isSwitchingCategory) return;
            OnRedDotUpdate();
            if (!Enable) return;

            // 套装不走 BuyPredicate/CreatePredicate（BundleUgc 会被过滤掉），直接全量刷新
            if (UniqueType.AvatarSubType(classSelected.Id) == AvatarSubType.Bundle)
            {
                assetsDatas.SetData(dataHandler.GetGoodsData(GetSelectedClassType()), null);
            }
            else
            {
                switch (bagTuple.Item1)
                {
                    case BagTabs.Tab.Bud:
                        assetsDatas.SetData(dataHandler.GetGoodsData(GetSelectedClassType()), null);
                        break;
                    case BagTabs.Tab.Ugc:
                        var datas = dataHandler.GetGoodsData(GetSelectedClassType());
                        switch (bagTuple.Item2)
                        {
                            case UgcSource.Source.Buy:
                                assetsDatas.SetData(datas, BuyPredicate);
                                break;
                            case UgcSource.Source.Create:
                                assetsDatas.SetData(datas, CreatePredicate);
                                break;
                        }
                        break;
                }
            }

            ActiveList.Data.ResetItems(assetsDatas.Count());
        }

        public void OnRedDotUpdate()
        {
            UI.mainTabsUI.SetRedDot(UI.isCharacterFittingRoom ? dataHandler.RedDot > 0 : dataHandler.PetRedDot > 0);

            if (!Enable) return;

            UI.classList.SetRedDot(dataHandler.RedDotClassHash);
            var resourcesType = UniqueType.ResourceType(classSelected.Id);
            if (resourcesType == ResourceType.Avatar || resourcesType == ResourceType.UgcAvatar)
            {
                dataHandler.RedDotPgc.TryGetValue(UniqueType.GetAvatar(UniqueType.AvatarSubType(classSelected.Id)),
                    out var count);
                UI.bagTabsUI.SetRedDot(BagTabs.Tab.Bud, count > 0);
                var ugcType = UniqueType.GetUgcAvatar(UniqueType.AvatarSubType(classSelected.Id));
                dataHandler.RedDotUgcCreator.TryGetValue(ugcType, out var count1);
                dataHandler.RedDotUgcBuy.TryGetValue(ugcType, out var count2);
                UI.bagTabsUI.SetRedDot(BagTabs.Tab.Ugc, count1 + count2 > 0);
                UI.ugcSourceUI.SetRedDot(UgcSource.Source.Create, count1 > 0);
                UI.ugcSourceUI.SetRedDot(UgcSource.Source.Buy, count2 > 0);
            }
            else if (resourcesType == ResourceType.PGCPetAvatar || resourcesType == ResourceType.UGCPetAvatar)
            {
                dataHandler.RedDotPgc.TryGetValue(UniqueType.GetPGCPetAvatar(UniqueType.AvatarSubType(classSelected.Id)),
                    out var count);
                UI.bagTabsUI.SetRedDot(BagTabs.Tab.Bud, count > 0);
                var ugcType = UniqueType.GetUGCPetAvatar(UniqueType.AvatarSubType(classSelected.Id));
                dataHandler.RedDotUgcCreator.TryGetValue(ugcType, out var count1);
                dataHandler.RedDotUgcBuy.TryGetValue(ugcType, out var count2);
                UI.bagTabsUI.SetRedDot(BagTabs.Tab.Ugc, count1 + count2 > 0);
                UI.ugcSourceUI.SetRedDot(UgcSource.Source.Create, count1 > 0);
                UI.ugcSourceUI.SetRedDot(UgcSource.Source.Buy, count2 > 0);
            }
            else if (resourcesType == ResourceType.MusicScore || resourcesType == ResourceType.UgcEmote || resourcesType == ResourceType.UgcPose || resourcesType == ResourceType.Theatre || resourcesType == ResourceType.AvatarCard)
            {
                dataHandler.RedDotUgcCreator.TryGetValue(classSelected.Id, out var count1);
                dataHandler.RedDotUgcBuy.TryGetValue(classSelected.Id, out var count2);
                UI.musicScoreSourceUI.SetRedDot(UgcSource.Source.Create, count1 > 0);
                UI.musicScoreSourceUI.SetRedDot(UgcSource.Source.Buy, count2 > 0);
            }
            else if (resourcesType == ResourceType.UgcVehicle)
            {
                var subType = UniqueType.VehicleSubType(classSelected.Id);
                int ugcType = 0;
                switch (subType)
                {
                    case VehicleSubType.SingleVehicle:
                        ugcType = UniqueType.Get((int)ResourceType.UgcVehicle, (int)VehicleSubType.SingleVehicle);
                        dataHandler.RedDotUgcCreator.TryGetValue(ugcType, out var count1);
                        dataHandler.RedDotUgcBuy.TryGetValue(ugcType, out var count2);
                        UI.bagTabsUI.SetRedDot(BagTabs.Tab.Ugc, count1 + count2 > 0);
                        UI.ugcSourceUI.SetRedDot(UgcSource.Source.Create, count1 > 0);
                        UI.ugcSourceUI.SetRedDot(UgcSource.Source.Buy, count2 > 0);
                        break;
                    case VehicleSubType.DoubleVehicle:
                        ugcType = UniqueType.Get((int)ResourceType.UgcVehicle, (int)VehicleSubType.DoubleVehicle);
                        dataHandler.RedDotUgcCreator.TryGetValue(ugcType, out var dbCount1);
                        dataHandler.RedDotUgcBuy.TryGetValue(ugcType, out var dbCount2);
                        UI.bagTabsUI.SetRedDot(BagTabs.Tab.Ugc, dbCount1 + dbCount2 > 0);
                        UI.ugcSourceUI.SetRedDot(UgcSource.Source.Create, dbCount1 > 0);
                        UI.ugcSourceUI.SetRedDot(UgcSource.Source.Buy, dbCount2 > 0);
                        break;
                    case VehicleSubType.AllVehicle:
                        ugcType = UniqueType.Get((int)ResourceType.UgcVehicle, (int)VehicleSubType.SingleVehicle);
                        dataHandler.RedDotUgcCreator.TryGetValue(ugcType, out var allCount1);
                        dataHandler.RedDotUgcBuy.TryGetValue(ugcType, out var allCount2);
                        var ugcType2 = UniqueType.Get((int)ResourceType.UgcVehicle, (int)VehicleSubType.DoubleVehicle);
                        dataHandler.RedDotUgcCreator.TryGetValue(ugcType2, out var allCount3);
                        dataHandler.RedDotUgcBuy.TryGetValue(ugcType2, out var allCount4);
                        UI.bagTabsUI.SetRedDot(BagTabs.Tab.Ugc, allCount1 + allCount2 + allCount3 + allCount4 > 0);
                        UI.ugcSourceUI.SetRedDot(UgcSource.Source.Create, allCount1 + allCount3 > 0);
                        UI.ugcSourceUI.SetRedDot(UgcSource.Source.Buy, allCount2 + allCount4 > 0);
                        break;
                }

            }
        }

        /// <summary>
        /// 默认选中的Tab - 是官方还是UGC
        /// </summary>
        /// <returns></returns>
        public BagTabs.Tab FirstTab()
        {
            var resourcesType = UniqueType.ResourceType(classSelected.Id);
            if (resourcesType == ResourceType.Avatar || resourcesType == ResourceType.UgcAvatar)
            {
                dataHandler.RedDotPgc.TryGetValue(UniqueType.GetAvatar(UniqueType.AvatarSubType(classSelected.Id)), out var count);
                if (count > 0) return BagTabs.Tab.Bud;
                var ugcType = UniqueType.GetUgcAvatar(UniqueType.AvatarSubType(classSelected.Id));
                dataHandler.RedDotUgcCreator.TryGetValue(ugcType, out var count1);
                dataHandler.RedDotUgcBuy.TryGetValue(ugcType, out var count2);
                if (count1 + count2 > 0) return BagTabs.Tab.Ugc;
            }
            else if (resourcesType == ResourceType.PGCPetAvatar || resourcesType == ResourceType.UGCPetAvatar)
            {
                dataHandler.RedDotPgc.TryGetValue(UniqueType.GetPGCPetAvatar(UniqueType.AvatarSubType(classSelected.Id)), out var count);
                if (count > 0) return BagTabs.Tab.Bud;
                var ugcType = UniqueType.GetUGCPetAvatar(UniqueType.AvatarSubType(classSelected.Id));
                dataHandler.RedDotUgcCreator.TryGetValue(ugcType, out var count1);
                dataHandler.RedDotUgcBuy.TryGetValue(ugcType, out var count2);
                if (count1 + count2 > 0) return BagTabs.Tab.Ugc;
            }
            else if (resourcesType == ResourceType.MusicScore || resourcesType == ResourceType.UgcEmote || resourcesType == ResourceType.UgcPose || resourcesType == ResourceType.Theatre || resourcesType == ResourceType.AvatarCard)
            {
                dataHandler.RedDotUgcCreator.TryGetValue(classSelected.Id, out var count1);
                dataHandler.RedDotUgcBuy.TryGetValue(classSelected.Id, out var count2);
                UI.musicScoreSourceUI.SetRedDot(UgcSource.Source.Create, count1 > 0);
                UI.musicScoreSourceUI.SetRedDot(UgcSource.Source.Buy, count2 > 0);
                return BagTabs.Tab.Ugc;
            }

            return BagTabs.Tab.Bud;
        }

        public override void Enter()
        {
            base.Enter();
            UI.bagTabsUI.SetCallback(OnBagTabs);
            UI.ugcSourceUI.SetCallback(OnUgcSource);
            UI.musicScoreSourceUI.SetCallback(OnUgcSource);
            UI.animSubTypeUI.OnSelectedAction = OnAnimSubType;
            UI.vehicleSubTypeUI.OnSelectedAction = OnVehicleSubType;
            UI.classList.SetCallback(OnClassListSelected);
            UI.classList.SetClass(classSelected, classDatas);
            if (!UI.isCharacterFittingRoom)
            {
                UI.editNameButton.gameObject.SetActive(true);
            }

            // 重置OC计数器，确保每次显示设子列表时都能正确触发引导
            OcItem.ResetOcCounter(false);

            OnRedDotUpdate();
            if (CurSelectShapeType < 0)
            {
                CurSelectShapeType = AccountDataManager.Inst.UserInfo.avatarInfo.bodyType;
            }
            ChangeAvatarPosData(CurSelectShapeType);
            // 记下当前 OC 的初始衣服快照，给 TakeOffBundle 恢复用
            if (UI.saveAvatarData != null) _ocBaselineJson = JsonConvert.SerializeObject(UI.saveAvatarData);
        }

        public void SelectGoods(int classType, string id, bool isCharacter = true)
        {
            var resourceType = UniqueType.ResourceType(classType);
            var avatarSubType = UniqueType.AvatarSubType(classType);
            classType = isCharacter ? UniqueType.GetAvatar(avatarSubType) : UniqueType.GetPGCPetAvatar(avatarSubType);
            classSelected = classDatas.Find(c => c.Id == classType);
            if (classBagDict.ContainsKey(classSelected)) bagTuple = classBagDict[classSelected];
            else bagTuple = new BagTuple();
            bagTuple.Item1 = (resourceType == ResourceType.Avatar || resourceType == ResourceType.PGCPetAvatar) ? BagTabs.Tab.Bud : BagTabs.Tab.Ugc;
            bagTuple.Item2 = UgcSource.Source.Buy;
            bagTuple.Item3 = id;
            classBagDict[classSelected] = bagTuple;
        }

        public override void Exit()
        {
            base.Exit();
            UI.classList.SetRedDot(new HashSet<int>());
        }

        /// <summary>
        /// 点击背包 分类 官方/社区
        /// </summary>
        /// <param name="tab"></param>
        private void OnBagTabs(BagTabs.Tab tab)
        {
            bagTuple.Item1 = tab;

            UI.adjustUI.gameObject.SetActive(false);
            UI.secondColorUI.gameObject.SetActive(false);
            UI.ugcSourceUI.gameObject.SetActive(false);
            UI.ShowTip("");
            if (CurSelectShapeType < 0)
            {
                CurSelectShapeType = AccountDataManager.Inst.UserInfo.avatarInfo.bodyType;
            }
            ChangeAvatarPosData(CurSelectShapeType);
            var avatarSubType = UniqueType.AvatarSubType(classSelected.Id);
            var resourceType = UniqueType.ResourceType(classSelected.Id);
            if (classSelected.Id == UniqueType.Get(ResourceType.MusicScore, (int)MusicScoreSubType.GeneralMusicScore) || resourceType == ResourceType.UgcEmote || resourceType == ResourceType.UgcPose || resourceType == ResourceType.Theatre || resourceType == ResourceType.AvatarCard)
            {
                bagTuple.Item1 = BagTabs.Tab.Ugc;
                UI.bagTabsUI.gameObject.SetActive(false);
                UI.musicScoreSourceUI.gameObject.SetActive(true);
                bagTuple.Item3 = "0";
                UI.musicScoreSourceUI.DefualtOn(bagTuple.Item2);
                return;
            }

            switch (tab)
            {
                case BagTabs.Tab.Bud:
                    ActiveList.gameObject.SetActive(true);
                    ActiveList.OnItemSelected = OnItemSelected;
                    assetsDatas.SetData(dataHandler.GetGoodsData(GetSelectedClassType()), null);
                    ActiveList.Data.ResetItems(assetsDatas.Count());
                    break;
                case BagTabs.Tab.Ugc:
                    UI.ugcSourceUI.gameObject.SetActive(true);
                    UI.ugcSourceUI.DefualtOn(bagTuple.Item2);
                    break;
            }

        }



        public override GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            if (assetsData == null)
            {
                return null;
            }
            assetsData.Selected = bagTuple.Item3 == assetsData.Id;
            if (assetsData.Selected) RefreshUI(assetsData);
            return assetsData;
        }

        /// <summary>
        /// 点击背包社区 来源 创建的和购买的
        /// </summary>
        /// <param name="source"></param>
        internal void OnUgcSource(UgcSource.Source source)
        {
            bagTuple.Item2 = source;

            var resourceType = UniqueType.ResourceType(classSelected.Id);
            if (resourceType == ResourceType.UgcPose || resourceType == ResourceType.UgcEmote)
            {
                UI.animSubTypeUI.gameObject.SetActive(true);
                UI.animSubTypeUI.DefaultOn(bagTuple.Item4);
                return;
            }
            else if (resourceType == ResourceType.UgcVehicle)
            {
                UI.vehicleSubTypeUI.gameObject.SetActive(true);
                UI.vehicleSubTypeUI.DefaultOn(bagTuple.Item4);
                return;
            }
            else
            {
                UI.animSubTypeUI.gameObject.SetActive(false);
                UI.vehicleSubTypeUI.gameObject.SetActive(false);
            }

            ActiveList.gameObject.SetActive(true);
            ActiveList.OnItemSelected = OnItemSelected;
            var datas = dataHandler.GetGoodsData(GetSelectedClassType());
            UI.ShowTip("");
            switch (source)
            {
                case UgcSource.Source.Buy:
                    assetsDatas.SetData(datas, BuyPredicate);
                    if (assetsDatas.Count() == 0) UI.ShowTip("暂无购买的商品");
                    break;
                case UgcSource.Source.Create:
                    assetsDatas.SetData(datas, CreatePredicate);
                    break;
            }

            ActiveList.Data.ResetItems(assetsDatas.Count());
        }

        internal void OnAnimSubType(bool isSingle)
        {
            bagTuple.Item4 = isSingle;

            ActiveList.gameObject.SetActive(true);
            ActiveList.OnItemSelected = OnItemSelected;
            var datas = dataHandler.GetGoodsData(GetSelectedClassType());
            UI.ShowTip("");
            switch (bagTuple.Item2)
            {
                case UgcSource.Source.Buy:
                    assetsDatas.SetData(datas, BuyPredicate);
                    if (assetsDatas.Count() == 0) UI.ShowTip("暂无购买的商品");
                    break;
                case UgcSource.Source.Create:
                    assetsDatas.SetData(datas, CreatePredicate);
                    break;
            }

            ActiveList.Data.ResetItems(assetsDatas.Count());
        }

        internal void OnVehicleSubType(bool isSingle)
        {
            bagTuple.Item4 = isSingle;

            ActiveList.gameObject.SetActive(true);
            ActiveList.OnItemSelected = OnItemSelected;
            var datas = dataHandler.GetGoodsData(GetSelectedClassType());
            UI.ShowTip("");
            switch (bagTuple.Item2)
            {
                case UgcSource.Source.Buy:
                    assetsDatas.SetData(datas, bagTuple.Item1 == BagTabs.Tab.Bud ? null : BuyPredicate);
                    if (assetsDatas.Count() == 0) UI.ShowTip("暂无购买的商品");
                    break;
                case UgcSource.Source.Create:
                    assetsDatas.SetData(datas, CreatePredicate);
                    break;
            }

            ActiveList.Data.ResetItems(assetsDatas.Count());
        }

        internal bool CreatePredicate(GoodsData goodsData)
        {
            if (goodsData == null) return false;
            if (goodsData.ButtonType == ButtonType.Design) return true;
            if (goodsData.ButtonType == ButtonType.TakeOff) return true;
            if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
            if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
            if (!goodsData.IsOwned) return false;
            var asset = goodsData.Assets[0];
            if (!(asset is UGCAssetsData) && !(asset is MusicScoreAssetsData) && !(asset is UgcAnimAssetsData) && !(asset is UgcPoseAssetsData) && !(asset is UgcTheatreAssetsData) && !(asset is UgcActorAssetsData)) return false;
            if (asset.InventoryData == null) return false;
            return asset.InventoryData.Tag == Network.Message.BackpackTag.Creator;
        }

        internal bool BuyPredicate(GoodsData goodsData)
        {
            if (goodsData == null) return false;
            if (goodsData.ButtonType == ButtonType.Design) return true;
            if (goodsData.ButtonType == ButtonType.TakeOff) return true;
            if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
            if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
            if (!goodsData.IsOwned) return false;
            var asset = goodsData.Assets[0];
            if (!(asset is UGCAssetsData) && !(asset is MusicScoreAssetsData) && !(asset is UgcAnimAssetsData) && !(asset is UgcPoseAssetsData) && !(asset is UgcTheatreAssetsData) && !(asset is UgcActorAssetsData)) return false;
            if (asset.InventoryData == null) return false;
            return asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag;
        }

        /// <summary>
        /// 分类选择 衣服 头发 等
        /// </summary>
        /// <param name="data"></param>
        internal void OnClassListSelected(ClassData data)
        {
            UI.CloseAllSubUI();
            classSelected = data;
            var classResourceType = UniqueType.ResourceType(classSelected.Id);
            var isTheatreOrActor = classResourceType == ResourceType.Theatre || classResourceType == ResourceType.AvatarCard;
            if (classSelected.Id != MusicScoreId && !isTheatreOrActor)
            {
                UI.ShowOcButton();
            }
            UI.theatreBuyBtn.gameObject.SetActive(false);
            if (classSelected.Id == UniqueType.Get(ResourceType.UgcVehicle, (int)VehicleSubType.FittingRoomVehicle))
            {
                UI.CloseAllSubUI();
                UI.ShowVehicleButton();
                OnVehicleSelected();
            }

            var subType = UniqueType.AvatarSubType(data.Id);
            UI.petSizeAdjustView.gameObject.SetActive(false);
            if (subType == AvatarSubType.Size)
            {
                UI.petSizeAdjustView.gameObject.SetActive(true);
                if (UI.saveAvatarData is PetData savePetData)
                {
                    if (UI.avatarWrapper is PetWrap petWrapper)
                    {
                        var partData = petWrapper.AddDefaultSizeData(savePetData);
                        UI.ShowPetSizeAdjustView(partData);
                    }
                }
                return;
            }

            switch (data.Id)
            {
                case (int)OtherClass.Oc:
                    OnOcSelected();
                    return;
                case MusicScoreId:
                    // 乐谱
                    UI.EnterPreviewMusicScore();
                    break;
            }

            if (subType == AvatarSubType.Skin && UI.isCharacterFittingRoom)
            {
                bagTuple = new BagTuple() { Item1 = BagTabs.Tab.Bud };
                OnSkinColorSelected();
                return;
            }
            else if (subType == AvatarSubType.Shape && UI.isCharacterFittingRoom)
            {
                bagTuple = new BagTuple() { Item1 = BagTabs.Tab.Bud };
                OnShapeSelected();
                return;
            }

            var ugcEnable = UniqueType.UgcAvatarEnable(data.Id);
            if (data.Id == MusicScoreId || UniqueType.ResourceType(data.Id) == ResourceType.UgcEmote || UniqueType.ResourceType(data.Id) == ResourceType.UgcPose || UniqueType.ResourceType(data.Id) == ResourceType.UgcVehicle || UniqueType.ResourceType(data.Id) == ResourceType.Theatre || UniqueType.ResourceType(data.Id) == ResourceType.AvatarCard) ugcEnable = true;
            // 套装目前只有 UGC 一种来源，隐藏 Bud/Ugc 切换避免空 Bud 列表
            if (subType == AvatarSubType.Bundle) ugcEnable = false;
            UI.bagTabsUI.gameObject.SetActive(ugcEnable);
            if (classBagDict.ContainsKey(data))
            {
                bagTuple = classBagDict[data];

                // 社子改变，选中需要重新赋值
                var partData = UI.saveAvatarData.GetPartData(data.Id);
                var ugcPartData =
                    UI.saveAvatarData.GetPartData(UI.isCharacterFittingRoom ? UniqueType.GetUgcAvatar(UniqueType.AvatarSubType(data.Id)) : UniqueType.GetUGCPetAvatar(UniqueType.AvatarSubType(data.Id)));
                if (partData != null && !partData.IsNull()) bagTuple.Item3 = partData.Id;
                else if (ugcPartData != null && !ugcPartData.IsNull()) bagTuple.Item3 = ugcPartData.UId;
                else bagTuple.Item3 = "0";

            }
            else
            {
                bagTuple = new BagTuple() { Item1 = FirstTab(), Item2 = UgcSource.Source.Buy, Item4 = true };

                var partData = UI.saveAvatarData.GetPartData(data.Id);
                var ugcPartData =
                    UI.saveAvatarData.GetPartData(UI.isCharacterFittingRoom ? UniqueType.GetUgcAvatar(UniqueType.AvatarSubType(data.Id)) : UniqueType.GetUGCPetAvatar(UniqueType.AvatarSubType(data.Id)));
                if (partData != null && !partData.IsNull()) bagTuple.Item3 = partData.Id;
                else if (ugcPartData != null && !ugcPartData.IsNull()) bagTuple.Item3 = ugcPartData.UId;
                else bagTuple.Item3 = "0";
                classBagDict.Add(data, bagTuple);
            }

            if (_resetFlagCoroutine != null)
            {
                UI.StopCoroutine(_resetFlagCoroutine);
            }

            _isSwitchingCategory = true;
            // 套装：直接显示拥有列表，不分 Bud/Ugc，也不分 Create/Buy
            if (subType == AvatarSubType.Bundle)
            {
                UI.ugcSourceUI.gameObject.SetActive(false);
                ActiveList.gameObject.SetActive(true);
                ActiveList.OnItemSelected = OnItemSelected;
                assetsDatas.SetData(dataHandler.GetGoodsData(GetSelectedClassType()), null);
                ActiveList.Data.ResetItems(assetsDatas.Count());
                OnRedDotUpdate();
                _resetFlagCoroutine = UI.StartCoroutine(ResetSwitchingFlagCoroutine());
                return;
            }
            UI.bagTabsUI.DefualtOn(bagTuple.Item1);
            OnRedDotUpdate();
            _resetFlagCoroutine = UI.StartCoroutine(ResetSwitchingFlagCoroutine());
        }

        private IEnumerator ResetSwitchingFlagCoroutine()
        {
            yield return null;
            _isSwitchingCategory = false;
            _resetFlagCoroutine = null;
        }

        internal int GetSelectedClassType()
        {
            var resourceType = UniqueType.ResourceType(classSelected.Id);
            if (resourceType == ResourceType.Avatar || resourceType == ResourceType.UgcAvatar)
            {
                if (bagTuple.Item1 == BagTabs.Tab.Ugc)
                    return UniqueType.GetUgcAvatar(UniqueType.AvatarSubType(classSelected.Id));
            }
            else if (resourceType == ResourceType.PGCPetAvatar || resourceType == ResourceType.UGCPetAvatar)
            {
                if (bagTuple.Item1 == BagTabs.Tab.Ugc)
                    return UniqueType.GetUGCPetAvatar(UniqueType.AvatarSubType(classSelected.Id));
            }
            else if (resourceType == ResourceType.UgcEmote)
            {
                if (classSelected.Id == UgcEmote)
                {
                    return UniqueType.Get(ResourceType.UgcEmote, bagTuple.Item4 ? (int)UgcAnimSubType.Single : (int)UgcAnimSubType.Double);
                }
                else
                {
                    return UniqueType.Get(ResourceType.UgcEmote, bagTuple.Item4 ? (int)UgcAnimSubType.PetSingle : (int)UgcAnimSubType.PetWithPlayer);
                }
            }
            else if (resourceType == ResourceType.UgcPose)
            {
                if (classSelected.Id == UgcPose)
                {
                    return UniqueType.Get(ResourceType.UgcPose, bagTuple.Item4 ? (int)UgcPoseSubType.Single : (int)UgcPoseSubType.Double);
                }
                else
                {
                    return UniqueType.Get(ResourceType.UgcPose, bagTuple.Item4 ? (int)UgcPoseSubType.PetSingle : (int)UgcPoseSubType.PetWithPlayer);
                }
            }
            else if (resourceType == ResourceType.UgcVehicle)
            {
                if (bagTuple.Item1 == BagTabs.Tab.Ugc)
                    return UniqueType.Get(ResourceType.UgcVehicle, bagTuple.Item4 ? (int)VehicleSubType.SingleVehicle : (int)VehicleSubType.DoubleVehicle);
                else if (bagTuple.Item1 == BagTabs.Tab.Bud)
                    return UniqueType.Get(ResourceType.Vehicle, bagTuple.Item4 ? (int)VehicleSubType.SingleVehicle : (int)VehicleSubType.DoubleVehicle);
            }

            return classSelected.Id;
        }

        /// <summary>
        /// 体型选择
        /// </summary>
        internal void OnShapeSelected()
        {
            UI.shapeList.gameObject.SetActive(true);
            UI.shapeList.SetCallback(OnShapeSelect);
            UI.shapeList.SetShapeInfo(UI.avatarWrapper);
            if (!PlayerPrefs.HasKey(ShapeDataMgr.Inst.shapeKey))
            {
                PlayerPrefs.SetInt(ShapeDataMgr.Inst.shapeKey, 1);
                PlayerPrefs.Save();
                OnRedDotUpdate();
                MessageHelper.Broadcast(MessageName.FirstShapeOpenNotice);
            }
        }

        internal void OnShapeSelect(int shapeType)
        {
            CurSelectShapeType = shapeType;
            UI.ChangeShape(GetSelectedClassType(), shapeType);
            ChangeAvatarPosData(shapeType);
        }

        internal void ChangeAvatarPosData(int shapeType)
        {
            var posData = new Vector3(0, -0.5f, 0);
            switch ((BodyType)shapeType)
            {
                case BodyType.Type1:
                case BodyType.Type2:
                    posData.y = -0.6f;
                    break;
                case BodyType.Type3:
                    posData.y = -0.42f;
                    break;
                case BodyType.Type4:
                    posData.y = -0.24f;
                    break;
                case BodyType.Type5:
                    posData.y = -0.5f;
                    break;
                case BodyType.Type6:
                    posData.y = -0.57f;
                    break;
            }
            UI.characterRoot.localPosition = posData;
        }

        /// <summary>
        /// 皮肤选择
        /// </summary>
        internal void OnSkinColorSelected()
        {
            UI.mainColorUI.gameObject.SetActive(true);
            var skinData = UI.saveAvatarData.GetPartData(UI.isCharacterFittingRoom ? UniqueType.GetAvatar(AvatarSubType.Skin) : UniqueType.GetPGCPetAvatar(AvatarSubType.Skin));
            ColorUtility.TryParseHtmlString(skinData.Cr, out Color skinColor);
            UI.mainColorUI.SetCallback(OnSkinColorSelected);
            UI.mainColorUI.SetColors(true, skinColor, skinColors);
            UI.mainColorUI.SetCustomButtonCallback(() =>
            {
                UI.OpenCustomColor(UI.mainColorUI.CustomColor, Color.white, (color) =>
                {
                    UI.mainColorUI.OnSelecedColor(color);
                }, UI.mainColorUI.gameObject);
            });
        }

        internal void OnOcSelected()
        {
            UI.ocList.gameObject.SetActive(true);
            UI.ocList.SetCallback((ocInfo) =>
            {
                UI.ChangeOc(ocInfo);
                // 切换 OC 后刷新 baseline，让 TakeOffBundle 恢复到新 OC 的衣服
                if (ocInfo?.ocInfo?.avatarJson != null)
                {
                    _ocBaselineJson = ocInfo.ocInfo.avatarJson;
                }
            });
        }

        /// <summary>
        /// 穿戴套装：逐个子部件调 ChangeUGCPart。
        /// 故意不包 try/catch，让异常直接暴露在 console 上，方便定位卡 Loading 的根因。
        /// 完成所有子部件的 ChangePart dispatch 后立即清掉 Loading（视觉上不再转圈），
        /// 各子部件的异步下载继续在后台进行，由 avatarWrapper 自己驱动。
        /// </summary>
        private void WearBundle(GoodsData data)
        {
            if (data == null || data.Assets == null || data.Assets.Count == 0)
            {
                LoggerUtils.LogError("[WearBundle] Assets 为空，套装数据没准备好");
                return;
            }
            // 记录这次穿了哪些子部件 classType，给 TakeOff 按钮按这些 classType 挨个脱
            _lastWornBundleClassTypes.Clear();
            data.Loading(true);
            int remaining = data.Assets.Count;
            void OnSubDone()
            {
                remaining--;
                if (remaining <= 0)
                {
                    data.Loading(false);
                    // 所有子部件加载完成后同步更新 saveAvatarData，否则 IsWearing 判断和退出保存均使用旧数据
                    if (UI.avatarWrapper is CharacterWrap characterWrap)
                        UI.saveAvatarData = characterWrap.ChaData.Clone();
                    else if (UI.avatarWrapper is PetWrap petWrap)
                        UI.saveAvatarData = petWrap.Data.Clone();
                }
            }
            foreach (var asset in data.Assets)
            {
                if (asset is UGCAssetsData ugc)
                {
                    var skinInfo = ugc.UgcInfo?.skinInfo;
                    if (skinInfo == null)
                    {
                        LoggerUtils.LogError($"[WearBundle] 子部件 {ugc.Id} skinInfo 为空");
                        OnSubDone();
                        continue;
                    }
                    _lastWornBundleClassTypes.Add(UniqueType.GetUgcAvatar((AvatarSubType)skinInfo.subType));
                    UI.avatarWrapper.ChangeUGCPart(skinInfo, OnSubDone);
                }
                else
                {
                    LoggerUtils.LogError($"[WearBundle] 子部件类型不是 UGCAssetsData: {asset?.GetType().Name}");
                    OnSubDone();
                }
            }
        }

        /// <summary>
        /// 脱下套装：优先恢复到当前 OC（设子）的初始衣服快照；如果没有快照就退回到按子部件 classType 逐个 TakeOff。
        /// </summary>
        private void TakeOffBundle()
        {
            if (!string.IsNullOrEmpty(_ocBaselineJson))
            {
                if (UI.avatarWrapper is CharacterWrap cw)
                {
                    cw.SetCharacterData(CharacterData.DeserializeObject(_ocBaselineJson));
                    UI.saveAvatarData = cw.ChaData.Clone();
                }
                else if (UI.avatarWrapper is PetWrap pw)
                {
                    pw.SetData(PetData.DeserializeObject(_ocBaselineJson));
                    UI.saveAvatarData = pw.Data.Clone();
                }
            }
            else
            {
                // 兜底：没有 baseline 时按穿戴记录挨个脱
                foreach (var resType in _lastWornBundleClassTypes)
                {
                    UI.TakeOff(resType);
                }
            }
            _lastWornBundleClassTypes.Clear();
        }

        internal void OnSkinColorSelected(Color color)
        {
            UI.ChangeColor(GetSelectedClassType(), color);
        }

        internal void OnVehicleSelected()
        {
            UI.SetSelectedVehicleInfo(null, isShowSave: false);
            UI.vehicleSubTypeUI.gameObject.SetActive(true);
            //UI.SetSelectedVehicleInfo(AccountDataManager.Inst.VehicleInfo, isShowSave: false);
        }

        internal string GetColorKey(AvatarSubType avatarSubType)
        {
            switch (avatarSubType)
            {
                case AvatarSubType.Hair:
                    return AvatarColorManager.HAIR_ALL;
                case AvatarSubType.Brow:
                    return AvatarColorManager.BROW_ALL;
                case AvatarSubType.Blush:
                    return AvatarColorManager.FACESTYLE_COM;
                case AvatarSubType.Skin:
                    if (UI.isCharacterFittingRoom)
                    {
                        return AvatarColorManager.SKIN_COM;
                    }
                    else
                    {
                        return AvatarColorManager.SKIN_ALL;
                    }

            }

            return AvatarColorManager.HAIR_ALL;
        }

        internal bool HaveCustomColor(AvatarSubType avatarSubType)
        {
            switch (avatarSubType)
            {
                case AvatarSubType.Blush:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Item选择
        /// </summary>
        /// <param name="data"></param>
        internal void OnItemSelected(GoodsData data)
        {
            switch (data.ButtonType)
            {
                case ButtonType.Design:
                    // 跳转avatar绘制
                    Design();
                    return;
                case ButtonType.TakeOff:
                    UI.ResetLastTryOn(ResourceType.ErrResourceType);
                    // 脱下：套装按上次穿戴记录挨个脱，其他类按 selected classType 脱
                    if (UniqueType.AvatarSubType(GetSelectedClassType()) == AvatarSubType.Bundle)
                    {
                        TakeOffBundle();
                    }
                    else
                    {
                        UI.TakeOff(GetSelectedClassType());
                    }
                    bagTuple.Item3 = "0";
                    ActiveList.Data.ResetItems(assetsDatas.Count());
                    return;
            }


            //foreach (var asset in data.Assets) {
            //    if (asset.ResourceType == ResourceType.Avatar && UI.animationCtrl != null && !string.IsNullOrEmpty(UI.animationCtrl.specialAnimPgcId)) {
            //        var lastResData = DataTables.GetAvatarCommonData(UI.animationCtrl.specialAnimPgcId);
            //        var lastSubType = UniqueType.GetAvatar((AvatarSubType)lastResData.SubType);
            //        var specialConfig = DataTables.GetSpecialSkinConfig(data.Id);
            //        var pgcAssets = asset as PGCAssetsData;
            //        var subType = UniqueType.GetAvatar(pgcAssets.AvatarSubType);
            //        if (specialConfig != null && lastSubType != subType) {
            //            TipPanel.ShowToast("需先卸下当前动作道具才能穿戴新的哦");
            //            return;
            //        }
            //    }
            //}

            bagTuple.Item3 = data.Id;
            // 套装：直接对每个子部件调 ChangeUGCPart，绕开 PutOn 的 try/catch（错误直接暴露），并精确控制 Loading
            if (data.GoodsType == GoodsType.BundleUgc)
            {
                WearBundle(data);
            }
            else
            {
                UI.PutOn(data);
            }
            var itemResourceType = UniqueType.ResourceType(classSelected.Id);
            if (itemResourceType == ResourceType.AvatarCard)
            {
                UI.theatreBuyBtn.gameObject.SetActive(false);
            }
            if (itemResourceType == ResourceType.Theatre)
            {
                var theatreInfo = (data.Assets?[0] as UgcTheatreAssetsData)?.UgcInfo?.theatreInfo;
                if (theatreInfo != null)
                    UIManager.Inst.OpenPanel(PanelId.TheatreInfoPanel, theatreInfo, (int)TheatreEnterType.Store);
            }
            ActiveList.Data.ResetItems(assetsDatas.Count());
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
                    UI.itemInfoUI.gameObject.SetActive(false);
                    UI.operationUI.gameObject.SetActive(false);
                    UI.HideSpecialContainer();
                    return;
            }

            UI.itemInfoUI.gameObject.SetActive(true);
            UI.itemInfoUI.SetTarget(data);

            UI.operationUI.gameObject.SetActive(true);
            UI.operationUI.SetTarget(data);
            UI.operationUI.OnOperation = (operation, target) =>
            {
                switch (operation)
                {
                    case Operation.TryPlayMusic:
                        UI.TryPlayMusic();
                        break;
                    case Operation.ChangeOtherOc:
                        UI.ChangeOtherOc();
                        break;
                    case Operation.LikeUgc:
                        UI.operationUI.Like();
                        break;
                    case Operation.Select:
                        UI.OnSelect(data);
                        break;
                }
            };

            void SetAdjustView(AvatarCommonData configData)
            {
                var adjustList = UI.AdjustTypeAdjustItems(configData, GetSelectedClassType());

                if (adjustList == null)
                {
                    UI.adjustUI.gameObject.SetActive(false);
                    UI.adjustView.gameObject.SetActive(false);
                }
                else
                {
                    UI.adjustUI.gameObject.SetActive(true);
                    UI.adjustView.SetAdjustItems(adjustList);
                }
            }

            AvatarCommonData configData = null;
            if (data.GoodsType == GoodsType.SingleUgc)
            {
                if (classSelected.Id == MusicScoreId) return;
                var assetsData = data.GetFirstAsset<AssetsData>();
                // 修复默认穿戴不显示调整按钮，因为ugcInfo还没有请求到的问题
                AssetsDataManager.GetUgcInfo(assetsData.Id, (recommendItemData) =>
                {
                    if (recommendItemData == null || recommendItemData.skinInfo == null) return;
                    if (recommendItemData.skinInfo.id != bagTuple.Item3) return;
                    configData = AvatarCommonData.From(recommendItemData.skinInfo);
                    SetAdjustView(configData);
                });
                SkinInfo skinInfo = null;
                skinInfo = data.GetFirstAsset<AssetsData>()?.UgcInfo?.skinInfo;
            }
            else
            {
                configData = UI.isCharacterFittingRoom ? Es.DataTables.GetAvatarCommonData(data.Id) : Es.DataTables.GetPetAvatarCommonData(data.Id);
                SetAdjustView(configData);
            }

            if (configData != null && configData.setColor)
            {
                UI.secondColorUI.gameObject.SetActive(true);
                var skinData = UI.saveAvatarData.GetPartData(GetSelectedClassType());
                if (!ColorUtility.TryParseHtmlString(skinData.Cr, out Color skinColor))
                {
                    ColorUtility.TryParseHtmlString(configData.defaultColor, out skinColor);
                }

                List<Color> colors = new();
                var list = AvatarColorManager.GetColorList(GetColorKey((AvatarSubType)configData.SubType));
                list.ForEach(c =>
                {
                    if (ColorUtility.TryParseHtmlString(c, out Color color)) colors.Add(color);
                });
                UI.secondColorUI.SetCallback(OnSkinColorSelected);
                UI.secondColorUI.SetColors(HaveCustomColor((AvatarSubType)configData.SubType), skinColor, colors);
                UI.secondColorUI.SetCustomButtonCallback(() =>
                {
                    ColorUtility.TryParseHtmlString(configData.defaultColor, out Color defaultColor);
                    UI.OpenCustomColor(UI.secondColorUI.CustomColor, defaultColor, (color) =>
                    {
                        UI.secondColorUI.OnSelecedColor(color);
                    }, UI.secondColorUI.gameObject);
                });
            }
            else
            {
                UI.secondColorUI.gameObject.SetActive(false);
            }
        }
    }

    #endregion
}
