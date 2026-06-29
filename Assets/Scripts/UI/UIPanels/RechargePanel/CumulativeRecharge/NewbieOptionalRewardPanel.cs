using System;
using System.Collections.Generic;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Config;
using Game.Event;
using Game.Pet;
using GameData.PgcData;
using Sirenix.Utilities;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Store
{
    public class NewbieOptionalRewardPanel : BasePanel<NewbieOptionalRewardPanel>
    {
        public SpriteAtlas Atlas;
        [Header("UI相关")] [SerializeField] private CButton backBtn;
        [SerializeField] private Transform BG;
        [Header("抽奖按钮相关")] [SerializeField] private CButton changeOtherOcBtn;

        [Header("列表相关")] [SerializeField] private Transform cacheNode;
        [SerializeField] private Transform scrollContent;
        [SerializeField] private NewbieCumulativeRechargeItem itemPrefab;

        [Header("道具信息")] [SerializeField] private Text titleText;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Image currencyRewardImage;
        [SerializeField] private Text currencyRewardNum;

        [Header("人物展示")] [SerializeField] private GameObject playerImageView;
        [SerializeField] internal Transform characterRoot;
        [SerializeField] internal AvatarCameraController avatarCameraController;

        [Header("各个根节点")] [SerializeField] private GameObject previewRoot; //人物预览页
        [SerializeField] internal CButton Btn_Exchange;
        [SerializeField] internal GameObject Go_ExchageUnable;

        private List<NewbieCumulativeRechargeItem> items = new List<NewbieCumulativeRechargeItem>();
        private LinkedList<NewbieCumulativeRechargeItem> cacheItems = new LinkedList<NewbieCumulativeRechargeItem>();

        //人物3d预览
        internal CharacterWrap characterWrap;

        // 宠物3D 预览
        internal PetWrap petWrap;

        internal BaseAvatarWrapper avatarWrapper;
        internal CharacterWrap otherCharacterWrap;
        internal PlayerAnimationCtrl animationCtrl;
        internal PlayerAnimationCtrl otherAnimationCtrl;
        internal PetAnimationCtrl petAnimationCtrl;

        private string taskId;
        private int eventId;
        private Action<string> claimAction;

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

        public void SetPreviewData(string taskId, int eventId, List<string> pgcIds, Action<string> claimAction)
        {
            this.taskId = taskId;
            this.eventId = eventId;
            this.claimAction = claimAction;
            UpdateListview(pgcIds);
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
            item.InitCustomBgItem("#a645f6", atlasPath, new List<string>()
            {
                "pinktask_bg1", "pinktask_bg2"
            });
            item.gameObject.SetActive(true);
        }


        private void UpdateListview(List<string> pgcIds)
        {
            ClearItems();
            if (pgcIds == null || pgcIds.Count == 0)
                return;

            for (int i = 0; i < pgcIds.Count; i++)
            {
                NewbieRewardData newbieRewardData = new NewbieRewardData();
                newbieRewardData.pgcId = pgcIds[i];
                NewbieCumulativeRechargeItem itemScript = GetItem();
                itemScript.Init(newbieRewardData, i, GetSpriteName(i),OnItemClick);
                items.Add(itemScript);
            }
        }

        private string GetSpriteName(int itemIndex)
        {
            switch (itemIndex)
            {
                case 0:
                    return "newbie_sign_reward1";
                case 1:
                    return "newbie_sign_reward2";
            }

            return "newbie_sign_reward1";
        }


        private void WearPgcClothes(List<string> pgcIds)
        {
            if (pgcIds == null)
            {
                return;
            }

            for (int i = 0; i < pgcIds.Count; i++)
            {
                var pgcId = pgcIds[i];
                var config = DataTables.GetGameResData(pgcId);
                if (config == null) continue;
                switch ((ResourceType)config.ResourceType)
                {
                    case ResourceType.Avatar:
                        TryOn(pgcId);
                        break;
                }
            }
        }

        internal void TryOn(string pgcId)
        {
            if (characterWrap == null)
            {
                return;
            }


            var config = DataTables.GetAvatarCommonData(pgcId);
            var classType = UniqueType.GetAvatar(pgcId);
            characterWrap.ChangePart(classType, pgcId);
            characterWrap.ChangeColor(classType, config.defaultColor);
            characterWrap.Move(classType, config.pDef);
            characterWrap.Rotate(classType, config.rDef);
            characterWrap.Scale(classType, config.sDef);
            characterWrap.HVScale(classType, config.vhSDef);
            characterWrap.SetLeftOrRight(classType, config.leftRightType);
        }


        private void DefClickFirst()
        {
            StopAllEmoteSound();
            if (items.Count > 0)
            {
                items[0].OnItemClick();
            }
        }

        private void OnItemClick(NewbieCumulativeRechargeItem item, NewbieRewardData info, int itemIndex)
        {
            bool isSelect = false;
            string pgcId = info.pgcId;

            changeOtherOcBtn?.gameObject.SetActive(false);
            string itemName = "";
         

            List<string> packPgcIds = new List<string>();
            
            if (itemIndex == 0)
            {
                itemName = "粉色熊熊套装";
                packPgcIds.Add("10900048");
                packPgcIds.Add("10400419");
            }
            else if (itemIndex == 1)
            {
                itemName = "紫色熊熊套装";
                packPgcIds.Add("10900047");
                packPgcIds.Add("10400420");
            }
            
            if (!packPgcIds.IsNullOrEmpty())
            {
                CancelTryOn();
                WearPgcClothes(packPgcIds);
                SetCharacterAvatarCamera();
            }
            
            itemNameText.gameObject.SetActive(false);

            for (int i = 0; i < items.Count; i++)
            {
                items[i].SetSelectStatus(false);
            }


            Go_ExchageUnable.SetActive(true);
            Btn_Exchange.onClick.RemoveAllListeners();

            var isOwned = AssetsDataManager.IsOwned(pgcId);
            if (!isOwned)
            {
                Btn_Exchange.gameObject.SetActive(true);
                Go_ExchageUnable.SetActive(false);
                Btn_Exchange.onClick.AddListener(() => { ClaimSingleItem(itemIndex, packPgcIds, itemName); });
            }
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

        private void ClaimSingleItem(int rewardIndex, List<string> packPgcIds, string itemName)
        {
            if (packPgcIds.IsNullOrEmpty())
            {
                return;
            }
            EventCenterDataManager.Inst.CliamReward(this.taskId, this.eventId, 1, rewardIndex + 1, (claimRspData) =>
            {

                ShowReward(rewardIndex);
                AccountDataManager.Inst.BalanceInfo.Refresh();
                this.claimAction?.Invoke(packPgcIds[0]);
                CloseSelf();
            });
        }

        private void ShowReward(int rewardIndex)
        {
            var rewardItemDatas = new List<CommonRewardItemData>();
           

            switch (rewardIndex)
            {
                case 0:
                    rewardItemDatas.Add(new CommonRewardItemData() {
                        rewardType = (int)BUDRewardType.RewardPgcResource,
                        pgcId = "10900048",
                        RewardAmount = 1,
                        rewardName = "粉色熊熊头套"
                    }); 
                    rewardItemDatas.Add(new CommonRewardItemData() {
                        rewardType = (int)BUDRewardType.RewardPgcResource,
                        pgcId = "10400419",
                        RewardAmount = 1,
                        rewardName = "粉色熊熊服装"
                    });
                    break;
                case 1:
                    rewardItemDatas.Add(new CommonRewardItemData() {
                        rewardType = (int)BUDRewardType.RewardPgcResource,
                        pgcId = "10900047",
                        RewardAmount = 1,
                        rewardName = "紫色熊熊头套"
                    }); 
                    rewardItemDatas.Add(new CommonRewardItemData() {
                        rewardType = (int)BUDRewardType.RewardPgcResource,
                        pgcId = "10400420",
                        RewardAmount = 1, 
                        rewardName = "紫色熊熊服装"
                    });
                    break;
            }
             

            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowRewards(rewardItemDatas);
        }

        private void StopAvatarAnim()
        {
            avatarCameraController.ResetEmoteView();
            animationCtrl.ResetEmoteForUICharacter();
            petAnimationCtrl.ResetEmoteForUICharacter();
            otherAnimationCtrl.gameObject.SetActive(false);
            otherAnimationCtrl.ResetEmoteForUICharacter();
        }

        private void CancelTryOn()
        {
            StopAvatarAnim();
            ResetCharacterRotate();

            characterWrap.RefreshAvatar(AccountDataManager.Inst.UserInfo.avatarInfo);
        }


        private void ResetCharacterRotate()
        {
            characterRoot.eulerAngles = new Vector3(0, -180, 0);
        }


        #region Item相关

        private NewbieCumulativeRechargeItem GetItem()
        {
            if (cacheItems != null && cacheItems.Count != 0)
            {
                NewbieCumulativeRechargeItem cache = cacheItems.Last.Value;
                cacheItems.RemoveLast();
                cache.gameObject.SetActive(true);
                cache.transform.SetParent(scrollContent);
                return cache;
            }

            NewbieCumulativeRechargeItem newIns = Instantiate(itemPrefab, scrollContent);
            return newIns;
        }

        private void RecycleItem(NewbieCumulativeRechargeItem item)
        {
            if (cacheItems == null)
            {
                cacheItems = new LinkedList<NewbieCumulativeRechargeItem>();
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