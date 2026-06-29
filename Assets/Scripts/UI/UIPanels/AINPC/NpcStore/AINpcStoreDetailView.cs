using System;
using System.Collections;
using Basic.Utils;
using Game.Avatar;
using Game.Base;
using Game.CommunityGame;
using Game.ECS;
using Game.Props.PropsManagers;
using Game.PropStore;
using Game.Utils;
using GameData.Base;
using GameData.BaseInfo;
using GameData.MapData;
using Message;
using Newtonsoft.Json;
using UI;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.AINPCStudio
{
    public class AINpcStoreDetailView : MonoBehaviour
    {
        [Header("人物相关")]
        public Transform characterRoot;
        public CharacterWrap characterWrap;
        public UIDragUtil dragUtil;
        public UserInfoView UserInfoView;
        public CButton Btn_UserHead;
        public PurchaseButton PurchaseButton;
        public CText Txt_ItemName;
        public OwnedNpcOpButton OwnedNpcOpButton;
        [Header("AINpc")]
        public CButton Btn_ChatToNpc;
        public CButton Btn_InfoCard;
        public CButton Btn_SelectNpc;
        public GameObject Go_SelectedBtn;
        public Text Txt_SelectNpc;
        [Header("Adapter")]
        public AINpcPropStoreAdapter NpcStoreAdapter;
        [Header("口头禅对话框")]
        public NpcDialogBox npcDialogBox;
        //Npc口头禅对话框

        private AINpcInfo _curPreviewNpcInfo;
        private Action<AINpcInfo> _onSelectNpcAct;
        private Func<string, bool> _canSelect;
        private NpcStoreEnterType _curEnterType = NpcStoreEnterType.Store;
        private BudTimer _hideTimer;

        public void InitEnterMode(NpcStoreEnterType enterType, Action<AINpcInfo> action, Func<string, bool> canSelect)
        {
            this._curEnterType = enterType;
            this._onSelectNpcAct = action;
            this._canSelect = canSelect;
            Init();
        }

        private void Init()
        {
            MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);

            npcDialogBox.SetFadeInFadeOutTime(0.5f);

            Txt_SelectNpc.text = _curEnterType == NpcStoreEnterType.SelectNPC ? "选择NPC" : "开始游戏";
        }

        private void OnRelease()
        {
            MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        }

        private void OnDestroy()
        {
            OnRelease();
        }

        private void OnBuyUgcItemSuccess(string ugcId){
            if (NpcStoreAdapter != null)
            {
                NpcStoreAdapter.OnBuySuccess(ugcId);
            }

            if (_curPreviewNpcInfo != null)
            {
                if (_curPreviewNpcInfo.id == ugcId)
                {
                    var isOwned = true;
                    var isMyBuddy = AccountDataManager.Inst.IsMyAIBuddy(ugcId);
                    SetOpButtonsState(isOwned, isMyBuddy);
                }
            }
        }

        private void SetInfoCardAction(AINpcInfo npcInfo)
        {
            Btn_InfoCard.onClick.RemoveAllListeners();
            Btn_InfoCard.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanel(PanelId.AINpcInfoCardPopupPanel, npcInfo);
            });
        }

        private void SetChatToNpcAction(AINpcInfo npcInfo) {
            Btn_ChatToNpc.onClick.RemoveAllListeners();
            Btn_ChatToNpc.onClick.AddListener(() => {
                UIManager.Inst.OpenPanel(PanelId.AINpcChatPanel, npcInfo);
            });
        }

        private void SetSelectedNpcAction(AINpcInfo npcInfo)
        {
            Btn_SelectNpc.onClick.RemoveAllListeners();
            Btn_SelectNpc.onClick.AddListener(() =>
            {
                this._onSelectNpcAct?.Invoke(npcInfo);
            });
        }

        #region 刷新页面，当Item点击的时候
        public void OnStoreItemClick(RecommendItemData data)
        {
            if (string.IsNullOrEmpty(data.ugcData))
            {
                LoggerUtils.LogError("NpcStore - V2 data.ugcData IsNull");
                return;
            }

            var npcInfo = JsonConvert.DeserializeObject<AINpcInfo>(data.ugcData);
            _curPreviewNpcInfo = npcInfo;

            var ugcId = data.ugcId;
            var consumed = data?.interactInfo == null ? 0 : data?.interactInfo.consumed;
            var isOwned = consumed == 1;
            var itemName = npcInfo?.name;
            var paymentInfo = npcInfo?.paymentInfo;
            var isMyBuddy = AccountDataManager.Inst.IsMyAIBuddy(npcInfo);
            AccountUserInfo accountUserInfo = data?.creatorInfo;

            Btn_ChatToNpc.gameObject.SetActive(false);
            Btn_InfoCard.gameObject.SetActive(false);
            UserInfoView.IsOpenProfilePanel = false;
            UserInfoView.SetData(accountUserInfo);
            Btn_UserHead.onClick.RemoveAllListeners();
            Btn_UserHead.onClick.AddListener(() =>
            {
                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.AINpc, ugcId);
            });
            PurchaseButton.SetData(npcInfo, consumed, paymentInfo);
            PurchaseButton.SetBuyAction(() =>
            {
                if (LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice == 1)
                {
                    LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice = 0;
                }
            });
            Txt_ItemName.text = itemName;
            OwnedNpcOpButton.InitData(npcInfo);

            SetOpButtonsState(isOwned, isMyBuddy);

            UserInfoView.gameObject.SetActive(true);
            Txt_ItemName.gameObject.SetActive(true);

            StartPreviewNpc(npcInfo);
        }

        public void OnPurchasedItemClick(AINpcPurchasedItemData data)
        {
            var npcInfo = data.ugcInfo;
            _curPreviewNpcInfo = npcInfo;

            var ugcId = npcInfo.id;
            var consumed = data?.interactInfo == null ? 0 : data?.interactInfo.consumed;
            var isOwned = consumed == 1;
            var itemName = npcInfo.name;
            var paymentInfo = npcInfo.paymentInfo;
            var isMyBuddy = AccountDataManager.Inst.IsMyAIBuddy(npcInfo);
            AccountUserInfo accountUserInfo = data?.creatorInfo;

            Btn_ChatToNpc.gameObject.SetActive(false);
            Btn_InfoCard.gameObject.SetActive(false);
            UserInfoView.IsOpenProfilePanel = false;
            UserInfoView.SetData(accountUserInfo);
            Btn_UserHead.onClick.RemoveAllListeners();
            Btn_UserHead.onClick.AddListener(() =>
            {
                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.AINpc, ugcId);
            });
            PurchaseButton.SetData(npcInfo, consumed, paymentInfo);
            PurchaseButton.SetBuyAction(() =>
            {
                if (LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice == 1)
                {
                    LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice = 0;
                }
            });
            Txt_ItemName.text = itemName;
            OwnedNpcOpButton.InitData(npcInfo);

            SetOpButtonsState(isOwned, isMyBuddy);

            UserInfoView.gameObject.SetActive(true);
            Txt_ItemName.gameObject.SetActive(true);

            StartPreviewNpc(npcInfo);
        }

        public void OnPublishedItemClick(DraftListItem data)
        {
            var npcInfo = data.npc;
            _curPreviewNpcInfo = npcInfo;

            var ugcId = npcInfo.id;
            var consumed = 1;
            var isOwned = consumed == 1;
            var itemName = npcInfo.name;
            var paymentInfo = npcInfo.paymentInfo;
            var isMyBuddy = AccountDataManager.Inst.IsMyAIBuddy(npcInfo);
            AccountUserInfo accountUserInfo = AccountDataManager.Inst.UserInfo;

            Btn_ChatToNpc.gameObject.SetActive(false);
            Btn_InfoCard.gameObject.SetActive(false);
            UserInfoView.IsOpenProfilePanel = false;
            UserInfoView.SetData(accountUserInfo);
            Btn_UserHead.onClick.RemoveAllListeners();
            Btn_UserHead.onClick.AddListener(() =>
            {
                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.AINpc, ugcId);
            });
            PurchaseButton.SetData(npcInfo, consumed, paymentInfo);
            PurchaseButton.SetBuyAction(() =>
            {
                if (LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice == 1)
                {
                    LobbyInfoManager.Inst.LobbyInfo.enableNPCHalfPrice = 0;
                }
            });
            Txt_ItemName.text = itemName;
            OwnedNpcOpButton.InitData(npcInfo);

            SetOpButtonsState(isOwned, isMyBuddy);
            UserInfoView.gameObject.SetActive(true);
            Txt_ItemName.gameObject.SetActive(true);

            StartPreviewNpc(npcInfo);
        }

        public void OnPgcItemClick(AINpcInfo npcInfo)
        {
            _curPreviewNpcInfo = npcInfo;
            SetOpButtonsState(true, false);
        }
        #endregion

        #region 预览相关方法
        private void StartPreviewNpc(AINpcInfo npcInfo)
        {
            SetInfoCardAction(npcInfo);
            SetSelectedNpcAction(npcInfo);
            SetChatToNpcAction(npcInfo);
            InitRoleData(npcInfo);
            StartPreviewPetPhrase(npcInfo);
        }

        private void InitRoleData(AINpcInfo npcInfo)
        {
            var npcAvatarJson = npcInfo.npcAvatarJson;
            CharacterData vCharacterData = CharacterData.DeserializeObject(npcAvatarJson).Clone();

            if (characterWrap == null)
            {
                characterWrap = AvatarController.Inst.CreateUIAvatar(vCharacterData);
                characterWrap.SetParent(characterRoot, true);
            }
            else
            {
                characterWrap.RefreshAvatar(vCharacterData);
            }

            dragUtil.RotateTarget = characterWrap.Avatar.transform;
        }
        #endregion

        private void SetOpButtonsState(bool isOwned, bool isMyBuddy)
        {
            Go_SelectedBtn.SetActive(false);
            if (_curEnterType == NpcStoreEnterType.Store)
            {
                OwnedNpcOpButton.gameObject.SetActive(isOwned);
                PurchaseButton.gameObject.SetActive(!isOwned);
            }
            else
            {
                OwnedNpcOpButton.gameObject.SetActive(false);
                PurchaseButton.gameObject.SetActive(!isOwned);
                Btn_SelectNpc.gameObject.SetActive(isOwned);
                if (_curEnterType == NpcStoreEnterType.SelectNPC)
                {
                    if (_curPreviewNpcInfo != null)
                    {
                        if (_curPreviewNpcInfo.paymentInfo == null || _curPreviewNpcInfo.paymentInfo.price == 0)
                        {
                            PurchaseButton.gameObject.SetActive(false);
                            Btn_SelectNpc.gameObject.SetActive(true);
                        }
                    }
                }
            }
            Btn_ChatToNpc.gameObject.SetActive(true);
            Btn_InfoCard.gameObject.SetActive(true);

            if (this._canSelect != null)
            {
                bool canSelect = this._canSelect(_curPreviewNpcInfo.id);
                if (!canSelect)
                {
                    Btn_SelectNpc.gameObject.SetActive(false);
                    Go_SelectedBtn.SetActive(true);
                }
            }
        }

        #region NPC口头禅对话框

        private void StartPreviewPetPhrase(AINpcInfo npcInfo)
        {
            HideDialog();
            
            var petPhrase = npcInfo.npcPetPhrases;
            if (petPhrase == null)
            {
                return;
            }
            
            SetNpcTalk(petPhrase[0]);
        }

        private float speedDelta = 0.05f;
        public void SetNpcTalk(string text, bool needAni = true)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            var playTime = text.Length * speedDelta;
            
            npcDialogBox.gameObject.SetActive(true);
            npcDialogBox.ResetContent();
            npcDialogBox.SetTextAndSpeak(playTime, text, needAni, true, true, DelayHideDialog);
        }

        private void DelayHideDialog()
        {
            _hideTimer = TimerManager.Inst.RunOnce("DelayHideDialog", 7f, HideDialog);
        }
        
        private void HideDialog()
        {
            if (npcDialogBox != null)
            {
                npcDialogBox.ResetContent();
                npcDialogBox.gameObject.SetActive(false);
                npcDialogBox.cGroup.alpha = 0;
            }
            
            TimerManager.Inst.Stop(_hideTimer);
            _hideTimer = null;
        }
        #endregion

    }
}
