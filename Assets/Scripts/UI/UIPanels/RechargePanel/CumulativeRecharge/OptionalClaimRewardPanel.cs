using System;
using System.Collections.Generic;
using Basic.Utils;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Config;
using Game.Database;
using Game.Pet;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Product;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.CreaterRewardPanel;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using xasset;

namespace Game.Store
{

    public class OptionalClaimRewardPanel : BasePanel<OptionalClaimRewardPanel>
    {
        public SpriteAtlas Atlas;
        [Header("UI相关")] 
        [SerializeField] private CButton backBtn;
        [SerializeField] private Transform BG;
        [Header("抽奖按钮相关")] [SerializeField] private CButton changeOtherOcBtn;

        [Header("列表相关")] 
        [SerializeField] private Transform cacheNode;
        [SerializeField] private Transform scrollContent;
        [SerializeField] private GashaponCumulativeRechargeItem itemPrefab;

        [Header("道具信息")] 
        [SerializeField] private Text titleText;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Image currencyRewardImage;
        [SerializeField] private Text currencyRewardNum;

        [Header("人物展示")] 
        [SerializeField] private GameObject playerImageView;
        [SerializeField] internal Transform characterRoot;
        [SerializeField] internal AvatarCameraController avatarCameraController;
        [SerializeField] internal Transform TitleParent;//称号展示区
         [SerializeField] internal GameObject txt_anim;//可选动作详情
          [SerializeField] internal GameObject txt_title;//可选称号详情
        

        [Header("各个根节点")] 
        [SerializeField] private GameObject previewRoot; //人物预览页
        [SerializeField] internal CButton Btn_Exchange;
        [SerializeField] internal GameObject Go_ExchageUnable;

        private List<GashaponCumulativeRechargeItem> items = new List<GashaponCumulativeRechargeItem>();
        private LinkedList<GashaponCumulativeRechargeItem> cacheItems = new LinkedList<GashaponCumulativeRechargeItem>();

        //人物3d预览
        internal CharacterWrap characterWrap;

        // 宠物3D 预览
        internal PetWrap petWrap;

        internal BaseAvatarWrapper avatarWrapper;
        internal CharacterWrap otherCharacterWrap;
        internal PlayerAnimationCtrl animationCtrl;
        internal PlayerAnimationCtrl otherAnimationCtrl;
        internal PetAnimationCtrl petAnimationCtrl;
        private RechargeLevelData _curRechargeLevelData;

        public override void OnCreate()
        {
            base.OnCreate();
            InitBG();
            InitUI();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            InitPreviewPlayer();
            InitPreviewPet();
        }

        public void SetPreviewData(RechargeLevelData data)
        {
             if(data.id == 12)//可选称号时不显示角色
            {
                SetRewardRawImageShow(true);
                playerImageView.SetActive(false);
                currencyRewardImage.gameObject.SetActive(false);
                currencyRewardNum.SetText("");
                playerImageView.gameObject.SetActive(false);
                characterRoot.gameObject.SetActive(false);
                avatarCameraController.gameObject.SetActive(false);
            }
            txt_anim.SetActive(data.id != 12);
            txt_title.SetActive(data.id == 12);
            this._curRechargeLevelData = data; 
            UpdateListview(this._curRechargeLevelData.optionalPgcList);
        }

        private void InitUI()
        {
            backBtn.onClick.AddListener(OnBackBtnClick);
            changeOtherOcBtn?.onClick.AddListener(ChangeOtherOc);
        }

        private void InitPreviewPlayer()
        {
            var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
            if (saveCharacterData != null && characterWrap == null)
            {
                characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
                characterWrap.SetParent(characterRoot, true);
                animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                avatarCameraController.RotateTarget = characterRoot;
                animationCtrl.gameObject.SetActive(true);

                otherCharacterWrap =
                    AvatarController.Inst.CreateUIAvatar(AccountDataManager.Inst.UserInfo.otherAvatarInfo);
                otherCharacterWrap.SetParent(characterRoot, true);
                otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                otherCharacterWrap.Avatar.gameObject.SetActive(false);
            }
        }

        private void InitPreviewPet()
        {
            var savePetData = AccountDataManager.Inst.PetInfo.avatarInfo;
            if (savePetData != null && petWrap == null)
            {
                petWrap = PetAvatarController.Inst.CreateUIAvatar(savePetData);
                petWrap.SetParent(characterRoot, true);
                petAnimationCtrl = petWrap.Avatar.GetComponentInChildren<PetAnimationCtrl>();
                petAnimationCtrl.gameObject.SetActive(false);
                avatarCameraController.RotateTarget = characterRoot;
            }
        }

        public override void OnHidden()
        {
            base.OnHidden();
            StopAvatarAnim();
        }

        private void StopAllEmoteSound()
        {
            //关闭的时候清除所有音效
            if (animationCtrl != null && animationCtrl.gameObject != null)
            {
                AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
            }

            if (otherAnimationCtrl != null && otherAnimationCtrl.gameObject != null)
            {
                AkSoundManager.Inst.StopAll(otherAnimationCtrl.gameObject);
            }

            if (petAnimationCtrl != null && petAnimationCtrl.gameObject != null)
            {
                AkSoundManager.Inst.StopAll(petAnimationCtrl.gameObject);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopAllEmoteSound();
        }

        protected override void Start()
        {
            base.Start();
            Invoke("DefClickFirst", 0.2f);
        }

        private void InitBG()
        {
            if (BG == null)
            {
                return;
            }

            string atlasPath = RechargePanel.RechargePanelAtlas;
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(BG);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#9859FF", atlasPath, new List<string>()
            {
                "s4_limit_bg_1", "s4_limit_bg_2", "s4_limit_bg_3"
            });
            item.gameObject.SetActive(true);
        }

        private void UpdateListview(List<int> pgcIds)
        {
            ClearItems();
            if (pgcIds == null || pgcIds.Count == 0)
                return;

            for (int i = 0; i < pgcIds.Count; i++)
            {
                GashaponRewardData priceData = new GashaponRewardData();
                var pgcIdStr = pgcIds[i].ToString();
                priceData.Id = pgcIdStr;
                priceData.Level = Level.S;
                priceData.RewardType = RewardType.RewardPgcResource;
                priceData.Num = 1;
                priceData.PgcDatas = new List<AssetsData>();
                var assetsData = GetAssetDataByPgcId(pgcIdStr);
                priceData.PgcDatas.Add(assetsData);
                GashaponCumulativeRechargeItem itemScript = GetItem();
                itemScript.Init(priceData, OnItemClick);
                items.Add(itemScript);
            }
        }

        private AssetsData GetAssetDataByPgcId(string pgcId)
        {
            var productList = AssetsDataManager.StoreData.ProductList;
            for (int i = 0, C = productList.Count; i < C; i++)
            {
                var productData = productList[i];
                var assetData = productData.AssetDataList[0];
                if (assetData.PgcId.ToString() == pgcId)
                {
                    var config = Es.DataTables.GetGameResData(pgcId);
                    switch (productData.ProductType)
                    {
                        case Product.ProductType.Avatar:
                            var assetsData = new PGCAssetsData();
                            assetsData.Id = assetData.PgcId;
                            assetsData.Name = assetData.Name;
                            assetsData.ResourceType = ResourceType.Avatar;
                            assetsData.AvatarSubType = (AvatarSubType)config.SubType;
                            return assetsData;
                            break;
                        case Product.ProductType.Emote:
                            EmoteAssetsData emoteAssetsData = new EmoteAssetsData();
                            emoteAssetsData.Id = assetData.PgcId;
                            emoteAssetsData.Name = assetData.Name;
                            emoteAssetsData.ResourceType = ResourceType.Emote;
                            emoteAssetsData.EmoteSubType = (EmoteSubType)config.SubType;
                            return emoteAssetsData;
                            break;
                    }
                }
            }

            // 在 StoreData 里找不到时，尝试从 TitleConfig 取（称号类）
            var titleData = UserUIWidgetManager.Inst.GetTitleDataByPgcId(pgcId);
            if (titleData != null)
            {
                var assetsData = new PGCAssetsData();
                assetsData.Id = pgcId;
                assetsData.Name = titleData.Name;
                assetsData.ResourceType = ResourceType.AvatarFrame;
                return assetsData;
            }

            return null;
        }

        private void DefClickFirst()
        {
            StopAllEmoteSound();
            if (items.Count > 0)
            {
                items[0].OnItemClick();
            }
        }

        private void SetRewardRawImageShow(bool isShow)
        {
            playerImageView.SetActive(!isShow);
            currencyRewardImage.gameObject.SetActive(isShow);
        }

        private void OnItemClick(GashaponCumulativeRechargeItem item, GashaponRewardData info)
        {

            bool isSelect = false;
            string pgcId = "";
            if (GashaponUtils.HasPGCData(info))
            {
                pgcId = info.PgcDatas[0].Id;
            }


            // var rewardItemDatas = new List<CommonRewardItemData>();
            //         Sprite iconSp = null;
                    
            //             var titleData1 = UserUIWidgetManager.Inst.GetTitleDataByPgcId(pgcId);
            //             if (titleData1 != null && !string.IsNullOrEmpty(titleData1.Icon))
            //             {
            //                 iconSp = Loader.Load<Sprite>(titleData1.Icon)?.RetainAsset(gameObject);
            //             }
                    
            //         var itemData = new CommonRewardItemData()
            //         {
            //             rewardType = (int)BUDRewardType.RewardTypeTitle,
            //             IconSp = iconSp,
            //             pgcId = pgcId,
            //             RewardAmount = 1,
            //             rewardName = "dd"
            //         };
                
            //         rewardItemDatas.Add(itemData);
                
            //         var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            //         panel.ShowRewards(rewardItemDatas);
            //         return;

            changeOtherOcBtn?.gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(pgcId))
            {
                GameResData resData = Es.DataTables.GetGameResData(pgcId);
                
                if (resData == null)
                {
                    // 称号类道具在 GameResData 里没有数据，隐藏3D预览
                    SetRewardRawImageShow(true);
                    playerImageView.SetActive(false);
                    currencyRewardImage.gameObject.SetActive(false);
                    currencyRewardNum.SetText("");
                    playerImageView.gameObject.SetActive(false);
                    characterRoot.gameObject.SetActive(false);
                    avatarCameraController.gameObject.SetActive(false);
                    var titleData = UserUIWidgetManager.Inst.GetTitleDataByPgcId(pgcId);
                    var prefab = Loader.Load<GameObject>(titleData.PreviewPath)?.RetainAsset();
                    foreach (Transform _item in TitleParent)
                    {
                        if(_item)
                            Destroy(_item.gameObject);
                    }
                    var go = Instantiate(prefab,TitleParent);
                    go.transform.localScale = Vector2.one * 2;
                }
                else if (resData.ResourceType == (int)ResourceType.Avatar ||
                    resData.ResourceType == (int)ResourceType.UgcAvatar)
                {
                    bool isMusic = resData.SubType == (int)AvatarSubType.MusicalInstrument;
                    if (avatarWrapper != characterWrap && avatarWrapper != null)
                    {
                        avatarWrapper.Avatar.SetActive(false);
                    }

                    avatarWrapper = characterWrap;
                    avatarWrapper.Avatar.gameObject.SetActive(true);
                    SetCharacterAvatarCamera();
                }
                else if (resData.ResourceType == (int)ResourceType.PGCPetAvatar ||
                         resData.ResourceType == (int)ResourceType.UGCPetAvatar)
                {
                    characterWrap.Avatar.SetActive(false);
                    otherCharacterWrap.Avatar.SetActive(false);

                    if (avatarWrapper != petWrap && avatarWrapper != null)
                    {
                        avatarWrapper.Avatar.SetActive(false);
                    }

                    avatarWrapper = petWrap;
                    avatarWrapper.Avatar.gameObject.SetActive(true);
                    SetPetAvatarCamera();
                }
                else if (resData.ResourceType == (int)ResourceType.Emote)
                {
                    var emoteSubType = ((EmoteSubType)resData.SubType);
                    if (emoteSubType.IsPet())
                    {
                        if (avatarWrapper != petWrap && avatarWrapper != null)
                        {
                            avatarWrapper.Avatar.SetActive(false);
                        }

                        avatarWrapper = petWrap;
                        SetPetAvatarCamera();
                    }
                    else
                    {
                        if (avatarWrapper != characterWrap && avatarWrapper != null)
                        {
                            avatarWrapper.Avatar.SetActive(false);
                        }

                        avatarWrapper = characterWrap;
                        SetCharacterAvatarCamera();
                    }

                    avatarWrapper.Avatar.gameObject.SetActive(true);
                    changeOtherOcBtn.gameObject.SetActive(emoteSubType.IsDouble() && !emoteSubType.IsPet());
                }
            }

            var currencyType = GameUtils.ConvertRewardType((int)info.RewardType);
            if (GameUtils.IsCurrencyType((int)currencyType))
            {
                SetRewardRawImageShow(true);
                var iconSprite = PgcUtils.LoadCurrencyIcon(currencyType, currencyRewardImage.gameObject);
                if (iconSprite != null)
                {
                    currencyRewardImage.sprite = iconSprite;
                }

                currencyRewardNum.SetText(info.Num > 1 ? "x" + info.Num : "");
            }
            else
            {
                SetRewardRawImageShow(false);
                CancelTryOn();
                TryOn(info.PgcDatas[0], item);
                PreviewEmote(info.PgcDatas[0]);
            }

            string itemName = "";
            if (GashaponUtils.HasPGCData(info))
            {
                itemName = info.PgcDatas[0].Name;
                itemNameText.gameObject.SetActive(true);
                itemNameText.SetLocalText(itemName);
            }
            else
            {
                itemNameText.gameObject.SetActive(false);
            }


            for (int i = 0; i < items.Count; i++)
            {
                items[i].SetSelectStatus(false);
            }


            Go_ExchageUnable.SetActive(true);
            Btn_Exchange.onClick.RemoveAllListeners();

            if (_curRechargeLevelData.rewardStatus == (int)ClaimStatus.Unlocked)
            {
                var isOwned = AssetsDataManager.IsOwned(pgcId);
                if (!isOwned)
                {
                    Btn_Exchange.gameObject.SetActive(true);
                    Go_ExchageUnable.SetActive(false);
                    Btn_Exchange.onClick.AddListener(() =>
                    {
                        ClaimSingleItem(_curRechargeLevelData.id, pgcId, itemName);
                    });
                }
            }
        }
        
        private void ClaimSingleItem(int rewardId, string pgcId, string itemName)
        {
            JObject jb = new JObject
            {
                ["rewardId"] = rewardId,
                ["pgcId"] = Convert.ToInt32(pgcId)
            };
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimRechargeBenifits,
                HttpMethod.POST,
                JsonConvert.SerializeObject(jb),
                onReceive: arg0 =>
                {
                    var rewardItemDatas = new List<CommonRewardItemData>();
                    Sprite iconSp = null;
                    if (rewardId == 12)
                    {
                        var titleData = UserUIWidgetManager.Inst.GetTitleDataByPgcId(pgcId);
                        if (titleData != null && !string.IsNullOrEmpty(titleData.Icon))
                        {
                            iconSp = Loader.Load<Sprite>(titleData.Icon)?.RetainAsset(gameObject);
                        }
                    }
                    else
                    {
                        iconSp = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject);
                    }
                    var itemData = new CommonRewardItemData()
                    {
                        rewardType = (int)BUDRewardType.RewardTypeTitle,
                        IconSp = iconSp,
                        RewardAmount = 1,
                        rewardName = itemName
                    };
                
                    rewardItemDatas.Add(itemData);
                
                    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                    panel.ShowRewards(rewardItemDatas);
                    panel.SetCloseAct(CloseSelf);
                
                    MessageHelper.Broadcast(MessageName.RefreshCumData);
                    Go_ExchageUnable.SetActive(true);
                }, onFail: arg0 =>
                {
                });
        }

        private void StopAvatarAnim()
        {
            avatarCameraController.ResetEmoteView();
            animationCtrl.ResetEmoteForUICharacter();
            petAnimationCtrl.ResetEmoteForUICharacter();
            otherAnimationCtrl.gameObject.SetActive(false);
            otherAnimationCtrl.ResetEmoteForUICharacter();
        }

        private void HideItemsLoading()
        {
            foreach (var item in items)
            {
                item.SetLoadingVisible(false);
            }
        }

        private void CancelTryOn()
        {
            StopAvatarAnim();
            ResetCharacterRotate();
            if (avatarWrapper is PetWrap)
            {
                avatarWrapper?.RefreshAvatar(AccountDataManager.Inst.PetInfo.avatarInfo);
            }
            else if (avatarWrapper is CharacterWrap)
            {
                avatarWrapper?.RefreshAvatar(AccountDataManager.Inst.UserInfo.avatarInfo);
            }

        }

        internal void TryOn(AssetsData asset, GashaponCumulativeRechargeItem itemNode)
        {
            HideItemsLoading();
            if (asset == null ||
                (asset.ResourceType != ResourceType.Avatar && asset.ResourceType != ResourceType.PGCPetAvatar)) return;

            itemNode?.SetLoadingVisible(true);
            var pgcAssets = asset as PGCAssetsData;
            var classType = asset.ResourceType == ResourceType.Avatar
                ? UniqueType.GetAvatar(pgcAssets.AvatarSubType)
                : UniqueType.GetPGCPetAvatar(pgcAssets.AvatarSubType);
            var config = asset.ResourceType == ResourceType.Avatar
                ? DataTables.GetAvatarCommonData(asset.Id)
                : DataTables.GetPetAvatarCommonData(asset.Id);
            avatarWrapper.ChangePart(classType, pgcAssets.Id, () => { itemNode?.SetLoadingVisible(false); });
            avatarWrapper.ChangeColor(classType, config.defaultColor);
            avatarWrapper.Move(classType, config.pDef);
            avatarWrapper.Rotate(classType, config.rDef);
            avatarWrapper.Scale(classType, config.sDef);
            avatarWrapper.HVScale(classType, config.vhSDef);
            avatarWrapper.SetLeftOrRight(classType, config.leftRightType);
            avatarCameraController.SetCameraZoom(classType);
        }

        private void ResetCharacterRotate()
        {
            characterRoot.eulerAngles = new Vector3(0, -180, 0);
        }

        internal void PreviewEmote(AssetsData asset)
        {
            if (asset == null || asset.ResourceType != ResourceType.Emote) return;

            var emoteAssets = asset as EmoteAssetsData;
            switch (emoteAssets.EmoteSubType)
            {
                case EmoteSubType.Single:
                case EmoteSubType.SingleLoop:
                    animationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id);
                    break;
                case EmoteSubType.Double:
                case EmoteSubType.DoubleLoop:
                    animationCtrl.PlayDoubleEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl);
                    break;
                case EmoteSubType.PetSingle:
                case EmoteSubType.PetSingleLoop:
                    petAnimationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id);
                    break;
                case EmoteSubType.PetWithPlayer:
                case EmoteSubType.PetWithPlayerLoop:
                    petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(emoteAssets.Id, animationCtrl);
                    break;

            }

            var classType = UniqueType.Get(emoteAssets.ResourceType, (int)emoteAssets.EmoteSubType);
            avatarCameraController.SetCameraZoom(classType);
            avatarCameraController.SetEmoteView(emoteAssets.Id);
        }

        #region Item相关

        private GashaponCumulativeRechargeItem GetItem()
        {
            if (cacheItems != null && cacheItems.Count != 0)
            {
                GashaponCumulativeRechargeItem cache = cacheItems.Last.Value;
                cacheItems.RemoveLast();
                cache.gameObject.SetActive(true);
                cache.transform.SetParent(scrollContent);
                return cache;
            }

            GashaponCumulativeRechargeItem newIns = Instantiate(itemPrefab, scrollContent);
            return newIns;
        }

        private void RecycleItem(GashaponCumulativeRechargeItem item)
        {
            if (cacheItems == null)
            {
                cacheItems = new LinkedList<GashaponCumulativeRechargeItem>();
            }

            cacheItems.AddLast(item);
            item.gameObject.SetActive(false);
            item.SetSelectStatus(false);
            item.transform.SetParent(cacheNode);
        }

        private void ClearItems()
        {
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    RecycleItem(items[i]);
                }

                items.Clear();
            }
        }

        #endregion

        private void OnBackBtnClick()
        {
            UIManager.Inst.ClosePanel(this);
        }

        private void SetPetAvatarCamera()
        {
            avatarCameraController.ZoomUpperPosY = -0.1f;
            avatarCameraController.ZoomUppereCameraSize = 0.4f;
            avatarCameraController.ZoomWholePosY = -0.2f;
            avatarCameraController.ZoomWholeCameraSize = 0.7f;
            avatarCameraController.ZoomFootPosY = -0.46f;
            avatarCameraController.ZoomFootCameraSize = 0.3f;

            avatarCameraController.customEmoteCameraScale = 0.7f;
        }

        private void SetCharacterAvatarCamera()
        {
            avatarCameraController.ZoomUpperPosY = 0.3f;
            avatarCameraController.ZoomUppereCameraSize = 0.6f;
            avatarCameraController.ZoomWholePosY = 0;
            avatarCameraController.ZoomWholeCameraSize = 1f;
            avatarCameraController.ZoomFootPosY = -0.375f;
            avatarCameraController.ZoomFootCameraSize = 0.75f;
            avatarCameraController.customEmoteCameraScale = 1.0f;
        }

        public void ChangeOtherOc()
        {
            UIManager.Inst.OpenPanelTakeAni<OcChangePanel>(PanelId.OcChangePanel, OcChangeScene.DoubleEmote).OnCloseAction =
                ChangeOtherOc;
        }

        private void ChangeOtherOc(BaseAvatarData baseAvatarData)
        {
            if (baseAvatarData != null)
            {
                try
                {
                    var data = (CharacterData)baseAvatarData;
                    otherCharacterWrap.SetCharacterData(data);
                    PlayerPrefs.SetString(GameConsts.EmoteOtherPlayerOcKey + AccountDataManager.Inst.Uid,
                        CharacterData.SerializeObject(data));
                }
                catch
                {
                }
            }
        }
    }
}
