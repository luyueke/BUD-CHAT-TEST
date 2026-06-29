using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BUD.AnimPose;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using EventTracking;
using EventTracking;
using Game.AINPCStudio;
using Game.AnimationStudio;
using Game.AnimationStudio;
using Game.Audio;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.Config;
using Game.Config;
using Game.COSXML;
using Game.Event;
using Game.KinematicCharacter;
using Game.MusicalInstrument;
using Game.Pet;
using Game.Store;
using Game.Utils;
using Game.Vehicle.PGCVehicle;
using Game.Vehicle.PGCVehicle.KVC;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using GameData.UGCData;
using GameSync.Manager;
using GameUI;
using Message;
using Message;
using Network;
using Network.Http;
using Newbie;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Game;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.CommonConfirm;
using UI.UIPanels.RechargePanel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public enum FittingRoomSource //定义进入试衣间的来源
    {
        Default = 0,
        IncubationCabin = 1,//养成仓
    }
    public class FittingRoomSelectData
    {
        public Action<GoodsData> OnSelect = null;
        public Func<GoodsData, bool> CanSelect = null;
    }
    public partial class FittingRoomPanel : BasePanel<FittingRoomPanel>
    {
        [SerializeField] internal Transform transBg;
        [Header("人物形象")]
        [SerializeField] internal Transform characterRoot;
        [SerializeField] internal AvatarCameraController avatarCameraController;
        [SerializeField] internal Button backButton;
        [SerializeField] internal Button searchButton;
        [SerializeField] internal Button editNameButton;
        [SerializeField] internal Text editNameText;
        [SerializeField] internal CButton arrowLeftBtn;
        [SerializeField] internal CButton arrowRightBtn;
        [SerializeField] internal CButton actorCardBtn;
        [SerializeField] internal CButton theatreBuyBtn;
        [SerializeField] internal CButton tryPlayBtn;
        [SerializeField] internal CButton introductionBtn;
        [Header("主标签UI")]
        [SerializeField] internal MainTabs mainTabsUI;
        [Header("内容页签")]
        [SerializeField] internal Transform contentRoot;
        [Header("促销页签")]
        [SerializeField] internal Transform shapeRoot;
        [Header("类别选择UI")]
        [SerializeField] internal ClassList classList;
        [Header("操作按钮UI")]
        [SerializeField] internal OperationView operationUI;
        [Header("操作按钮UI")]
        [SerializeField] internal ItemInfo itemInfoUI;
        [Header("第二颜色选择UI")]
        [SerializeField] internal ColorPicker secondColorUI;
        [Header("促销Banner")]
        [SerializeField] internal BannerRootView bannerRoot;
        [Header("促销道具UI")]
        [SerializeField] internal PromotionList promotionRoot;
        [Header("Ugc推荐选择UI")]
        [SerializeField] internal SectionList sectionList;
        [SerializeField] internal SectionList shapeSectionList;
        [Header("背包类别选择UI")]
        [SerializeField] internal BagTabs bagTabsUI;
        [Header("背包UGC来源UI")]
        [SerializeField] internal UgcSource ugcSourceUI;
        [Header("列表")]
        [SerializeField] internal FittingRoomAdapter assetsList;
        [Header("主颜色选择UI")]
        [SerializeField] internal ColorPicker mainColorUI;
        [Header("调整按钮")]
        [SerializeField] internal AdjustNode adjustUI;
        [Header("调整界面")]
        [SerializeField] internal AdjustView adjustView;
        [Header("体型界面")]
        [SerializeField] internal ShapeAdapter shapeList;
        [Header("宠物大小调整界面")]
        [SerializeField] internal PetSizeAdjustView petSizeAdjustView;
        [Header("调整界面")]
        [SerializeField] internal OcList ocList;
        [Header("自定义背景")]
        [SerializeField] internal RemoteImageBehaviour customBg;
        [Header("余额")]
        [SerializeField] internal AccountWidgets accountWidgets;
        [Header("搜索框")]
        [SerializeField] internal SearchView searchView;
        [Header("过滤框")]
        [SerializeField] internal FilterView filterView;
        [Header("提示语")]
        [SerializeField] internal Text tipText;
        [Header("系列背景")]
        [SerializeField] internal ActivityCenterBgItem seriesBg;
        [Header("背包乐谱来源UI")]
        [SerializeField] internal UgcSource musicScoreSourceUI;
        [Header("乐谱试听乐器UI")]
        [SerializeField] internal SwitchMI switchMI;
        [Header("保存OC")]
        [SerializeField] internal LoadingButton saveOcButton;
        [Header("载具选择UI")]
        [SerializeField] internal LoadingButton saveVehicleButton;
        [Header("乐器试听乐谱UI")]
        [SerializeField] internal SwitchMS switchMS;
        [Header("Bundle详情Item列表")]
        [SerializeField] internal BundleItemsList bundleItemsList;
        [Header("社区商品币任务")]
        [SerializeField] internal GameHallPinkCoin gameHallPinkCoin;
        [Header("新-新手任务")]
        [SerializeField] internal FittingRoomNewBieTaskBtn newBieTask;
        [Header("Ugc姿势动画单双人选择")]
        [SerializeField] internal AnimSubTypeSelect animSubTypeUI;
        [Header("载具单双人选择")]
        [SerializeField] internal VehicleSubTypeSelect vehicleSubTypeUI;
        [Header("打折卡")]
        [SerializeField] internal GameObject discountCardContainer;

        [Header("特殊动作PGC")]
        [SerializeField] internal SpecialAnimContainer specialAnimContainer;
        [Header("双人牵手动作等")]
        [SerializeField] internal SwitchAnimView switchAnimView;
        [Header("粉币礼包按钮")]
        [SerializeField] internal CButton getMorePinkCoin;
        [Header("设置偏好按钮")]
        [SerializeField] internal CButton setLabelBt;
         [Header("清除穿搭按钮")]
        [SerializeField] internal CButton clearClothBtn;
        [Header("购物车按钮")]
        [SerializeField] internal CButton shoppingCartBtn;
        [Header("购物车按钮红点")]
        [SerializeField] internal GameObject shoppingCartBtnRedPoint;
        [Header("购物车数量")]
       [SerializeField] internal Text shopCartNum;
        [Header("每周推荐发型")]
        [SerializeField] internal CButton weekHairBt;
        [Header("遮罩")]
        [SerializeField] public GameObject maskObject;
        [Header("购物车界面")]
        [SerializeField] public GameObject ShoppingCartRoot;
        
        [SerializeField] public static MainTabs.Tab curTab;
        internal CharacterWrap otherCharacterWrap;
        private CharacterWrap _actorCharacterWrap;
        // 跟扭蛋预览同款：记录上次点的 SpecialAnim，相同的再点不响应，
        // 避免 PlaySpecialAnimForUICharacter 重置云端 Animator 到 time 0 而本体没动 → 相位错开
        private SpecialAnim? _lastSwitchSpecialAnim;
        internal BaseAvatarWrapper avatarWrapper;
        internal BaseAvatarData saveAvatarData;
        internal bool isCharacterFittingRoom = true;

        public bool canShowActivity = true;

        [SerializeField] public GameObject ocCompetitionBtn;

        #region 人物相关


        internal PlayerAnimationCtrl animationCtrl;
        internal AnimIKController animationCtrlIK;
        internal PlayerHoldBehaviour playerHold;
        internal PlayerAnimationCtrl otherAnimationCtrl;
        internal AnimIKController otherAnimationCtrlIK;
        internal PlayMusicScoreBev playMusicScoreBev;

        #endregion

        #region 宠物相关
        internal PetAnimationCtrl petAnimationCtrl;
        internal AnimIKController petAnimationCtrlIK;
        #endregion


        private Dictionary<MainTabs.Tab, BaseScene> mainSceneDict;

        private BaseScene scene;

        string key = "FirstOpenFittingRoomPanel" + AccountDataManager.Inst.UserInfo.uid;

        public Action<CharacterData> OnCloseAction;

        private AmbientLightSetting _srcLightSetting;
        private bool _srcHallLightVisible;
        private bool _srcGameSceneLightVisible;
        private bool _srcPreviewSceneLightVisible;
        private ClassData selectedClassData;

        // 剧本演员切换服装相关
        private OCTheatreAvatarInfo _currentActorInfo;
        private OCTheatreInfo _currentTheatreInfo;
        private int _actorClothesIndex;
        private List<CharacterData> _actorCharacterDataList;

        // 剧本演员预览相关
        private Coroutine _theatreActorCoroutine;
        private Coroutine _theatreCycleCoroutine;
        private int _theatreActorIndex;
        private static readonly Dictionary<string, List<CharacterData>> _theatreActorCache = new Dictionary<string, List<CharacterData>>();

        public float avatarPosY;
        FittingRoomSource fittingRoomSource;
        CharacterData _cabinCharacterData;
        CharacterData avatarInfo{
            get{
                if(fittingRoomSource == FittingRoomSource.IncubationCabin){
                    return _cabinCharacterData;
                }
                return AccountDataManager.Inst.UserInfo.avatarInfo;
        }}
        public bool GetisCharacterFittingRoom()
        {
            return isCharacterFittingRoom;

        }
        public void PlayNewBieAnimation()
        {
            newBieTask.PlayNewBieTaskAnimation();
        }


        private FittingRoomSelectData selectData;//用于选中返回外部界面
        public void CloseAllSubUI()
        {
            operationUI.gameObject.SetActive(false);
            secondColorUI.gameObject.SetActive(false);
            bagTabsUI.gameObject.SetActive(false);
            assetsList.gameObject.SetActive(false);
            shapeList.gameObject.SetActive(false);
            mainColorUI.gameObject.SetActive(false);
            adjustUI.gameObject.SetActive(false);
            adjustView.gameObject.SetActive(false);
            sectionList.gameObject.SetActive(false);
            ugcSourceUI.gameObject.SetActive(false);
            itemInfoUI.gameObject.SetActive(false);
            ocList.gameObject.SetActive(false);
            customBg.gameObject.SetActive(false);
            searchView.gameObject.SetActive(false);
            tipText.gameObject.SetActive(false);
            searchButton.gameObject.SetActive(false);
            setLabelBt.gameObject.SetActive(false);
            weekHairBt.gameObject.SetActive(false);
            editNameButton.gameObject.SetActive(false);
            seriesBg.gameObject.SetActive(false);
            musicScoreSourceUI.gameObject.SetActive(false);
            switchMI?.gameObject.SetActive(false);
            saveOcButton?.gameObject.SetActive(false);
            saveVehicleButton?.gameObject.SetActive(false);
            saveOcButton?.SetClickAble(true);
            if (GoScreenshotBtn != null) GoScreenshotBtn.gameObject.SetActive(false);
            if (SaveWardrobeBtn != null) SaveWardrobeBtn.gameObject.SetActive(false);
            switchMS?.gameObject.SetActive(false);
            bundleItemsList?.gameObject.SetActive(false);
            animSubTypeUI?.gameObject.SetActive(false);
            vehicleSubTypeUI?.gameObject.SetActive(false);
            petSizeAdjustView.gameObject.SetActive(false);
            switchAnimView?.gameObject.SetActive(false);
            getMorePinkCoin?.gameObject.SetActive(false);
            bannerRoot.gameObject.SetActive(false);
            if (ShoppingCartRoot != null) ShoppingCartRoot.SetActive(false);
            avatarCameraController.roleCamera.backgroundColor = new Color(1, 1, 1, 0);
            if(selectedVehicleInfo != null){
                TakeOffVehicle();
            }
            ResetLastTryOn(ResourceType.ErrResourceType);
        }
        public void OpenActivityBtn(bool newStatus)
        {
            newBieTask.gameObject.SetActive(newStatus);
            gameHallPinkCoin.gameObject.SetActive(!newStatus);

        }
        public void ShowOcCompetitionBtn(bool bo)
        {
            // if (bo && isCharacterFittingRoom)
            // {
            //     ocCompetitionBtn.gameObject.SetActive(true);
            // }
            // else
            // {
            //     ocCompetitionBtn.gameObject.SetActive(false);
            // }
        }
        public void ShowAvatar(bool bo)
        {
            if (avatarWrapper != null && avatarWrapper.Avatar != null)
            {
                avatarWrapper.Avatar.gameObject.SetActive(bo);
            }
        }
        public override void OnCreate()
        {
            // 试衣间打开检查一次背包数据
            Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck);
            Message.MessageHelper.AddListener<CharacterViewExpressionItem>(Message.MessageName.ActorCharacterOpenFittingRoomPanel,ActorCharacterOpenFittingRoomPanel);
            Message.MessageHelper.AddListener<WardrobeViewWardrobelistItem>(Message.MessageName.ActorWardrobeViewOpenFittingRoomPanel,ActorWardrobeViewOpenFittingRoomPanel);
            MessageHelper.AddListener<string>(MessageName.ShowToast, ShowToast);
            BusinessLiveManager.Inst.AddConfigUpdateListener(OnBusinessConfigUpdate);
            // 刷新余额
            AccountDataManager.Inst.BalanceInfo.Refresh();
            LimitTimePropManager.Inst.RefashLimitTimeProps();
            backButton.onClick.AddListener(OnBackClick);

            playMusicScoreBev = GetComponent<PlayMusicScoreBev>();

            adjustUI.OnClick = () =>
            {
                ResetLastTryOn(ResourceType.ErrResourceType);
                adjustView.gameObject.SetActive(true);
            };
            saveOcButton.onClick.AddListener(SaveOc);
            saveVehicleButton.onClick.AddListener(SaveVehicle);
            setLabelBt.onClick.AddListener(SetLabel);
            clearClothBtn.onClick.AddListener(ClearCloth);
            shoppingCartBtn.onClick.AddListener(OnShoppingCartClick);
            weekHairBt.onClick.AddListener(WeekHair);
            mainTabsUI.SetCallback(OnMainTabs);
            mainSceneDict = new();

            classList.onValueChangedBase = OnClassSelected;


            maskObject.SetActive(!PlayerPrefs.HasKey(key) && AccountDataManager.Inst.UserInfo.isNewUser == 1);
            itemInfoUI.OnHeadClick = OnHeadClick;
            editNameButton.onClick.AddListener(EditPetName);
            discountCardContainer.GetComponent<CButton>().onClick.AddListener(OnDiscountCardClick);
            editNameText.text = AccountDataManager.Inst.PetInfo.nickname;
            specialAnimContainer.SetCallBack(OnSpecialPGCClick);
            switchAnimView?.SetCallBack(OnSwitchAnimItemClick);

            MessageHelper.AddListener<TaskListRsp>(MessageName.NewComerCommunityCoin, NewComerCommunityCoin);
            EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
            avatarPosY = characterRoot.localPosition.y;
            VipDataManager.Inst.UpdateVipStatus();
            MessageHelper.AddListener<GoodsData>(MessageName.ShapeItemEvent, OnShapeItemEvent);
            MessageHelper.AddListener<GoodsData>(Message.MessageName.ShapeEmoteItemEvent, OnShapeEmoteItemEvent);

            SearchLogicMgr.Inst.PreGetData();

            arrowLeftBtn.onClick.AddListener(() => OnArrowClick(-1));
            arrowRightBtn.onClick.AddListener(() => OnArrowClick(1));
            actorCardBtn.onClick.AddListener(ActorCardBtnOnclick);
            theatreBuyBtn.onClick.AddListener(TheatreBuyBtnOnclick);
            tryPlayBtn.onClick.AddListener(TryPlayBtnOnclick);
            introductionBtn.onClick.AddListener(IntroductionBtnOnClick);
            arrowLeftBtn.gameObject.SetActive(false);
            arrowRightBtn.gameObject.SetActive(false);
            actorCardBtn.gameObject.SetActive(false);
            theatreBuyBtn.gameObject.SetActive(false);
            tryPlayBtn.gameObject.SetActive(false);
            introductionBtn.gameObject.SetActive(false);
        }
        private void SetCurrentActorInfo(OCTheatreAvatarInfo actorInfo)
        {
            _currentActorInfo = actorInfo;
            _actorClothesIndex = 0;
            _actorCharacterDataList = actorInfo.avatarClothes
                .Select(c => CharacterData.DeserializeObject(c.clothesJson))
                .Where(d => d != null)
                .ToList();
            bool hasMultiple = _actorCharacterDataList.Count >= 1;
            arrowLeftBtn.gameObject.SetActive(hasMultiple);
            arrowRightBtn.gameObject.SetActive(hasMultiple);
            actorCardBtn.gameObject.SetActive(hasMultiple);
            theatreBuyBtn.gameObject.SetActive(false);
        }
        private void IntroductionBtnOnClick()
        {
            if (_currentTheatreInfo != null)
                UIManager.Inst.OpenPanel(PanelId.TheatreInfoPanel, _currentTheatreInfo);
        }
        private void TryPlayBtnOnclick()
        {
            if (_currentTheatreInfo != null)
                UIManager.Inst.OpenPanel(PanelId.TheatreGamePanel, _currentTheatreInfo, (int)TheatreEnterType.None, true);
        }
        private void SwitchActorCloth(int delta)
        {
            if (_actorCharacterDataList == null || _actorCharacterDataList.Count == 0) return;
            _actorClothesIndex = (_actorClothesIndex + delta + _actorCharacterDataList.Count) % _actorCharacterDataList.Count;
            if (avatarWrapper is CharacterWrap characterWrap)
                characterWrap.SetCharacterData(_actorCharacterDataList[_actorClothesIndex]);
        }

        private void OnArrowClick(int delta)
        {
            if (_currentTheatreInfo != null)
                SwitchTheatreActor(delta);
            else
                SwitchActorCloth(delta);
        }

        private void SwitchTheatreActor(int delta)
        {
            var avatarList = _currentTheatreInfo?.avatarList;
            if (avatarList == null || avatarList.Count == 0) return;
            _theatreActorIndex = (_theatreActorIndex + delta + avatarList.Count) % avatarList.Count;
            StartLoadTheatreActor(_currentTheatreInfo, _theatreActorIndex);
        }

        private void StartLoadTheatreActor(OCTheatreInfo theatreInfo, int actorIndex = 0)
        {
            if (_theatreActorCoroutine != null)
            {
                StopCoroutine(_theatreActorCoroutine);
                _theatreActorCoroutine = null;
            }
            if (_theatreCycleCoroutine != null)
            {
                StopCoroutine(_theatreCycleCoroutine);
                _theatreCycleCoroutine = null;
            }
            _theatreActorCoroutine = StartCoroutine(LoadTheatreActorCoroutine(theatreInfo, actorIndex));
        }

        private IEnumerator LoadTheatreActorCoroutine(OCTheatreInfo theatreInfo, int actorIndex)
        {
            if (theatreInfo?.avatarList == null || theatreInfo.avatarList.Count == 0) yield break;

            var uncachedIds = theatreInfo.avatarList
                .Where(a => a != null && !string.IsNullOrEmpty(a.playerId) && !_theatreActorCache.ContainsKey(a.playerId))
                .Select(a => a.playerId)
                .Distinct()
                .ToList();

            if (uncachedIds.Count > 0)
            {
                bool done = false;
                string idList = string.Join(",", uncachedIds);
                NetworkManager.Inst.SendHttpRequest(
                    HttpUrlDefine.ActorBatchInfo, HttpMethod.GET,
                    JsonConvert.SerializeObject(new { idList }),
                    content =>
                    {
                        var rsps = JsonConvert.DeserializeObject<BatchActorDetailRsp>(content);
                        if (rsps?.actorList != null)
                        {
                            foreach (var rsp in rsps.actorList)
                            {
                                var actorInfo = rsp?.actorInfo;
                                if (actorInfo?.avatarClothes == null || actorInfo.avatarClothes.Count == 0) continue;
                                var clothes = new List<CharacterData>();
                                foreach (var c in actorInfo.avatarClothes)
                                {
                                    var cd = CharacterData.DeserializeObject(c.clothesJson);
                                    if (cd != null) clothes.Add(cd);
                                }
                                if (clothes.Count > 0) _theatreActorCache[actorInfo.id] = clothes;
                            }
                        }
                        done = true;
                    },
                    _ => done = true);

                float elapsed = 0f;
                while (!done && elapsed < 8f)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (_currentTheatreInfo != theatreInfo) yield break;

            var targetAvatar = theatreInfo.avatarList[Mathf.Clamp(actorIndex, 0, theatreInfo.avatarList.Count - 1)];
            if (targetAvatar == null || string.IsNullOrEmpty(targetAvatar.playerId)) yield break;
            if (!_theatreActorCache.TryGetValue(targetAvatar.playerId, out var clothesList) || clothesList.Count == 0) yield break;

            _actorCharacterWrap?.SetCharacterData(clothesList[0]);
            avatarCameraController.SetCameraZoom(ViewType.ZoomWholeBody);
            _theatreActorCoroutine = null;

            if (clothesList.Count > 1)
                _theatreCycleCoroutine = StartCoroutine(TheatreClotheCycleCoroutine(theatreInfo, clothesList));
        }

        private IEnumerator TheatreClotheCycleCoroutine(OCTheatreInfo theatreInfo, List<CharacterData> clothesList)
        {
            var wait = new WaitForSeconds(3f);
            int index = 0;
            while (_currentTheatreInfo == theatreInfo)
            {
                yield return wait;
                if (_currentTheatreInfo != theatreInfo) yield break;
                index = (index + 1) % clothesList.Count;
                _actorCharacterWrap?.SetCharacterData(clothesList[index]);
            }
            _theatreCycleCoroutine = null;
        }

        private void ActorCardBtnOnclick()
        {
            if (_currentActorInfo == null) return;
            UIManager.Inst.OpenPanel(PanelId.ActorCardInfoPanel, _currentActorInfo);
        }

        private void TheatreBuyBtnOnclick()
        {
            if (_currentTheatreInfo != null)
            {
                UIManager.Inst.OpenPanel(PanelId.TheatreGamePanel, _currentTheatreInfo, (int)TheatreEnterType.Store, false);
                return;
            }
            if (_currentActorInfo == null) return;
            UIManager.Inst.OpenPanel(PanelId.ActorShopCarPanel, _currentActorInfo);
        }

        private void SetLabel()
        {
            var k = UIManager.Inst.OpenPanelTakeAni<AccurateRecommendationPopPanel>(PanelId.AccurateRecommendationPopPanel);
            k.isStore = true;
            k.SetData(new View.UI.PopupPanelSystem.Data.WebtoolNewsData());
        }

        private void OnShoppingCartClick()
        {
            // 购物车为空时不打开界面，弹提示（只统计当前人物/宠物对应的商品，用 isPet 标记区分）
            bool forPet = !isCharacterFittingRoom;
            bool hasItems = ShoppingCartManager.Inst.GetList(forPet).Count > 0;
            if (!hasItems)
            {
                TipPanel.ShowToast("购物车里还没有商品哦");
                return;
            }
            if (mainSceneDict != null && mainSceneDict.TryGetValue(MainTabs.Tab.Ugc, out var scene) && scene is UGCScene ugcScene)
            {
                ugcScene.AddShoppingCartClass();
            }
            if (ShoppingCartRoot != null)
            {
                if (ShoppingCartRoot.TryGetComponent<ShoppingCartRootView>(out var view))
                {
                    view.onClose = OnShoppingCartClose;
                    // 根据当前是人物/宠物试衣间，只显示对应商品
                    // Open() → SetActive(true) → OnEnable → Refresh，不需要再显式 Refresh
                    view.Open(!isCharacterFittingRoom);
                }
            }
            RefreshShoppingCartRedPoint();
        }

        private void OnShoppingCartClose()
        {
            if (mainSceneDict != null && mainSceneDict.TryGetValue(MainTabs.Tab.Ugc, out var scene) && scene is UGCScene ugcScene)
            {
                ugcScene.RemoveShoppingCartClass();
            }
            RefreshShoppingCartRedPoint();
        }

        // 购物车红点:数量大于1时显示红点和数量,否则隐藏
        public void RefreshShoppingCartRedPoint()
        {
            // 红点只统计当前人物/宠物对应的商品数量，用 isPet 标记区分
            bool forPet = !isCharacterFittingRoom;
            int count = ShoppingCartManager.Inst.GetList(forPet).Count;
            bool show = count > 0;
            if (shoppingCartBtnRedPoint != null) shoppingCartBtnRedPoint.SetActive(show);
            if (shopCartNum != null)
            {
                shopCartNum.gameObject.SetActive(show);
                if (show) shopCartNum.text = count.ToString();
            }
        }
        /// <summary>
        /// 一键脱掉当前角色身上的所有 avatar 部件，只剩裸态（内衣内裤）。
        /// 只对 avatar/UgcAvatar/PGCPetAvatar/UGCPetAvatar 资源 TakeOff，
        /// 避免误伤 emote、vehicle 等其它资源类型。
        /// </summary>
        private void ClearCloth()
        {
            if (saveAvatarData == null || avatarWrapper == null) return;

            // 先收集快照再操作，避免 TakeOff 改 partDatas 时迭代器失效
            var resTypes = new List<int>();
            if (saveAvatarData is CharacterData chaData && chaData.partDatas != null)
            {
                foreach (var part in chaData.partDatas) resTypes.Add(part.Type);
            }
            else if (saveAvatarData is PetData petData && petData.partDatas != null)
            {
                foreach (var part in petData.partDatas) resTypes.Add(part.Type);
            }

            foreach (var resType in resTypes)
            {
                var rt = UniqueType.ResourceType(resType);
                if (rt != ResourceType.Avatar && rt != ResourceType.UgcAvatar
                    && rt != ResourceType.PGCPetAvatar && rt != ResourceType.UGCPetAvatar) continue;

                var subType = UniqueType.AvatarSubType(resType);

                if (isCharacterFittingRoom)
                {
                    // 人物：保留体型、肤色、头型
                    if (subType == AvatarSubType.Shape || subType == AvatarSubType.Skin
                        || subType == AvatarSubType.Head) continue;
                }
                else
                {
                    // 宠物：保留皮肤底座(Skin)、身体(Body)、大小(Size)——这些是骨架/基础形态，
                    // 脱掉会导致宠物骨架消失
                    if (subType == AvatarSubType.Skin || subType == AvatarSubType.Body
                        || subType == AvatarSubType.Size) continue;
                }

                TakeOff(resType);
            }
        }
        private void WeekHair()
        {
            UIManager.Inst.OpenPanelTakeAni(PanelId.WeekRecommendedHairPanel);
        }
        private void OnBusinessConfigUpdate(BusinessLiveConfig config)
        {
            if (scene != null && (scene is ActionScene || scene is BUDScene))
            {
                scene.OnDataRefresh();
            }
        }
        public void FittingroomDataRefresh()
        {
            if (scene != null)
            {
                sectionList.OnSelecedSection(0);
                scene.OnDataRefresh();
            }
        }
        public void HideSpecialContainer()
        {
            if (animationCtrl != null)
            {
                // 仍穿着特殊皮肤时保持 preview idle，不清成 base（只隐藏按钮容器）；脱下后(specialAnimPgcId 为空)才重置
                if (string.IsNullOrEmpty(animationCtrl.specialAnimPgcId))
                {
                    animationCtrl.ClearOverrideSpecialAnim();
                    animationCtrl.StopSpecialIdleAnim();
                    animationCtrl.SetPlayerAniState(PlayerAniState.Idle);
                    AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
                    _lastSwitchSpecialAnim = null;
                }
            }
            specialAnimContainer.gameObject.SetActive(false);
        }

        public void ShowSpecialContainer()
        {
            // 重新打开容器（含切换不同特殊皮肤）时清掉 dedup 缓存，避免新皮肤的 Idle 被错误吞掉
            _lastSwitchSpecialAnim = null;
            specialAnimContainer.gameObject.SetActive(true);
            specialAnimContainer.ResetIdle();
        }

        public void OnSpecialPGCClick(SpecialAnim anim)
        {
            // 跟扭蛋预览一致：已经是当前态再点同一个直接吞掉，避免重新驱动云端导致本体/云相位错开。
            // 仅在皮肤已加载（specialAnimPgcId 非空）+ 预览模式下生效；皮肤未加载时不缓存，防止首次 sync 调用早退却占住缓存。
            bool skinReady = animationCtrl != null && !string.IsNullOrEmpty(animationCtrl.specialAnimPgcId);
            if (skinReady
                && animationCtrl.PreferExhibitIdleForPreview
                && _lastSwitchSpecialAnim == anim)
            {
                return;
            }
            if (skinReady) _lastSwitchSpecialAnim = anim;

            animationCtrl.PlaySpecialAnimForUICharacter(anim, (cameraInfo) =>
            {
                if (cameraInfo != null && this != null && gameObject != null)
                {
                    avatarCameraController.SetSpecialSkinView(cameraInfo);
                }
            });
        }

        private void OnSwitchAnimItemClick(SpecialAnim anim)
        {
            var emoteId = switchAnimView.GetPgcId();
            if (!string.IsNullOrEmpty(emoteId))
            {
                animationCtrl.PlayLinkEmoteForUICharacter(emoteId, anim, otherAnimationCtrl);
            }
        }

        // 装备特殊皮肤后：等 effect prefab 加载完成（云 Animator 出现），再立刻顺序调用
        // SpecialAnimContainer.OnSpecialAnimChange(Run) → OnSpecialAnimChange(PreviewIdle)，让云先脱离默认 idle state、
        // 再正确切到 preview idle 路径，跟用户手动点 SpecialAnimContainer 同款行为。
        private System.Collections.IEnumerator WaitForSpecialEffectAndResetIdle()
        {
            const float timeoutSeconds = 3f;
            float elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                if (this == null || animationCtrl == null) yield break;
                if (animationCtrl.specialAnimRoot != null
                    && animationCtrl.specialAnimRoot.GetComponentInChildren<UnityEngine.Animator>(true) != null)
                {
                    break;
                }
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            if (this == null || animationCtrl == null || specialAnimContainer == null) yield break;
    
            specialAnimContainer.OnSpecialAnimChange(SpecialAnim.Run);
            specialAnimContainer.OnSpecialAnimChange(SpecialAnim.Idle);
        
        }

        private void OnDiscountCardClick()
        {
            UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel, ActivityId.MusicAndDanceCommunity.ToString());
        }

        private CharacterWrap EnsureActorCharacterWrap()
        {
            if (_actorCharacterWrap != null) return _actorCharacterWrap;
            _actorCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(avatarInfo, characterRoot);
            _actorCharacterWrap.Avatar.SetActive(false);
            return _actorCharacterWrap;
        }

        private void InitCharacterWrapper()
        {
            var saveCharacterData = avatarInfo;

            // 特殊皮肤是异步加载的：用加载完成回调（此时 specialAnimPgcId 已就绪）再触发一次 idle，
            // 否则下方同步的 OnSpecialPGCClick(Idle) 会在皮肤就绪前早退，导致 special skin 加载时仍是 base 而非 preview。
            var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot, false,
                () => { if (animationCtrl != null) OnSpecialPGCClick(SpecialAnim.Idle); });
            //characterWrapper.SetParent(characterRoot, true);
            animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            // 试穿房本质也是 UI 预览：打开 exhibit idle 偏好，让 RandomSpecialIdleAnim 协程恒走 exhibit 分支，
            // 不再在 preview_idle / idle_exhibit 之间循环切换；并让 DriveSpecialEffectPreviewIdle 把本体 Animator 拉回 time 0 跟云同步。
            if (animationCtrl != null) animationCtrl.PreferExhibitIdleForPreview = true;
            animationCtrlIK = characterWrapper.Avatar.GetComponent<AnimIKController>();
            playerHold = characterWrapper.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
            avatarCameraController.RotateTarget = characterRoot;

            otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.otherAvatarInfo, characterRoot);
            //otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherAnimationCtrlIK = otherCharacterWrap.Avatar.GetComponent<AnimIKController>();
            avatarWrapper = characterWrapper;
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
            saveAvatarData = saveCharacterData;

            OnSpecialPGCClick(SpecialAnim.Idle);
        }
        private void InitPetWrapper()
        {
            var savePetData = AccountDataManager.Inst.PetInfo.avatarInfo;

            var petWrapper = PetAvatarController.Inst.CreateUIAvatarWithIKController(savePetData, characterRoot);
            //petWrapper.SetParent(characterRoot, true);
            petAnimationCtrl = petWrapper.Avatar.GetComponentInChildren<PetAnimationCtrl>();
            petAnimationCtrlIK = petWrapper.Avatar.GetComponent<AnimIKController>();
            characterRoot.localScale = Vector3.one * 1.32f;
            characterRoot.localPosition = new Vector3(0, -0.5f, 0);

            avatarCameraController.RotateTarget = characterRoot;
            otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(avatarInfo, characterRoot);
            //otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherAnimationCtrlIK = otherCharacterWrap.Avatar.GetComponent<AnimIKController>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
            avatarWrapper = petWrapper;
            saveAvatarData = savePetData;
        }

        private void NewComerCommunityCoin(TaskListRsp taskListRsp)
        {
            if (canShowActivity)
            {
                //初始化原新手登录礼
                var taskInfoData = taskListRsp.list.Find(x => x.taskId == TASK_ID.NewbieCheckIn.ToString());
                bool isTaskEnable = EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieCheckIn);
                bool isAllComplete = EventCenterDataManager.Inst.CheckTaskIsAllComplete(taskInfoData, true);
                gameHallPinkCoin.gameObject.SetActive(taskInfoData != null && isTaskEnable && !isAllComplete);
                if (taskInfoData == null)
                {
                    return;
                }
                gameHallPinkCoin?.OnInitCreate(taskInfoData);

                //初始化S10新手登录礼
                var taskInfoDataV2 = taskListRsp.list.Find(x => x.taskId == TASK_ID.NewbieCheckInV2.ToString());
                bool isTaskEnableV2 = EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieCheckInV2);
                bool isAllCompleteV2 = EventCenterDataManager.Inst.CheckTaskIsAllComplete(taskInfoData, true);
                newBieTask.gameObject.SetActive(EventCenterDataManager.Inst.CheckTaskIsEnable(TASK_ID.NewbieCheckInV2));
                if (taskInfoDataV2 == null)
                {
                    return;
                }
                newBieTask?.OnInitCreate(taskInfoDataV2);
            }
            else
            {
                newBieTask.gameObject.SetActive(false);
                gameHallPinkCoin.gameObject.SetActive(false);
            }
        }
        void SetActicityShow(bool canShow)
        {

        }
        private void EditPetName()
        {
            ChangePetNickNamePanel panel = UIManager.Inst.OpenPanel<ChangePetNickNamePanel>(PanelId.ChangePetNickNamePanel, AccountDataManager.Inst.PetInfo);
            panel.SetAction(SetPetNameShow);
        }
        private void SetPetNameShow()
        {
            editNameText.text = AccountDataManager.Inst.PetInfo.nickname;
        }
        private void OnHeadClick()
        {
            // 点击详情停止乐谱播放
            playMusicScoreBev.StopPLay();
        }

        private void InitBGUI()
        {
            if (transBg == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(transBg);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            if (isCharacterFittingRoom)
            {
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "avatar_icon_1", "avatar_icon_2", "avatar_icon_3", "avatar_icon_4"
                });
            }
            else
            {
                item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
                {
                    "pet_icon_1", "pet_icon_2", "pet_icon_3", "pet_icon_4"
                });
            }

            item.gameObject.SetActive(true);

            RectTransform customBgRectTrans = customBg.GetComponent<RectTransform>();
            var height = customBgRectTrans.rect.height;
            customBgRectTrans.anchorMin = new Vector2(0.5f, 0.5f);
            customBgRectTrans.anchorMax = new Vector2(0.5f, 0.5f);
            customBgRectTrans.sizeDelta = new Vector2(height, height);
        }

        public override void OnWindowBeCovered(bool isCover)
        {
            if (isCover)
            {
                if (!this || !this.transform) return;
                DealBaseLayout2D(false);
                DealBaseLayout3D(false);
            }
        }

        public override void OnWindowShow()
        {
            if (!this || !this.transform) return;
            DealBaseLayout2D(true);
            DealBaseLayout3D(true);
        }

        public void SetActionTypeDefault()
        {
            mainTabsUI.DefualtOn(MainTabs.Tab.Action);
        }

        public void OnMainTabs(MainTabs.Tab tab)
        {
            accountWidgets.SetMainTabs(tab);
            curTab = tab;
            shoppingCartBtn.gameObject.SetActive(tab == MainTabs.Tab.Ugc);
            if (tab == MainTabs.Tab.Ugc) RefreshShoppingCartRedPoint();
            string OpenKey = "FirstGetItemOpen" + AccountDataManager.Inst.UserInfo.uid;
            string FinishKey = "FirstGetItemFinish" + AccountDataManager.Inst.UserInfo.uid;
            string BagTagkey = "FirstOpenFittingRoomPanel_" + MainTabs.Tab.Bag.ToString() + AccountDataManager.Inst.UserInfo.uid;
            string BUDTagkey = "FirstOpenFittingRoomPanel_" + MainTabs.Tab.BUD.ToString() + AccountDataManager.Inst.UserInfo.uid;
            if(tab != MainTabs.Tab.Ugc){
                sectionList.HideFilter();
                sectionList.HideSetting();
            }
            if (tab == MainTabs.Tab.BUD)
            {

                //首次打开官方商城的引导
                if (!PlayerPrefs.HasKey(BUDTagkey) && !BootPanel.isPlaying && AccountDataManager.Inst.UserInfo.isNewUser == 1 && isCharacterFittingRoom)
                {
                    TimerManager.Inst.RunOnce("Boot", 0.2f, () => //延迟防止没创建出TAG
                    {
                        classList.DefualtOn(2);
                    });

                    PlayerPrefs.SetInt(BUDTagkey, 1);
                    PlayerPrefs.Save();
                    EventTracking.LoadEvent.ReportPopupStatus("1", "guide_official_done");
                    UIManager.Inst.OpenPanel(PanelId.BootPanel, WindowId.FittingRoomWindow, 109);
                }
                if (tab == MainTabs.Tab.BUD)
                {
                    LoadEvent.ReportTask(146, 0);
                }
            }
            else
            {
                shapeRoot.gameObject.SetActive(false);
                contentRoot.gameObject.SetActive(true);
            }

            scene?.Exit();
            scene = mainSceneDict[tab];
            scene.Enter();
        }

        private void OnClassSelected(ClassData classData)
        {
            selectedClassData = classData;

            CancelEmote();
            CancelPreviewMusicScore();
            CancelPreviewMusicalInstrument();
            HideSpecialContainer();

            avatarCameraController.SetCameraZoom(classData.Id);
            if (!PGCVehicleManager.Inst.HasActiveGameVehicles)
                PGCVehicleManager.Inst.Release();
            PGCVehicleManager.Inst.RemoveUIPGCVehicle();
            avatarWrapper.Avatar.gameObject.SetActive(true);
        }

        public void JumpToBagGoods(int classType, GoodsData data)
        {
            var asset = data.GetFirstAsset<AssetsData>();
            if (asset == null) return;
            if (asset.ResourceType == ResourceType.Avatar || asset.ResourceType == ResourceType.PGCPetAvatar)
            {
                var avatarAsset = asset as PGCAssetsData;
                classType = UniqueType.Get(asset.ResourceType, (int)avatarAsset.AvatarSubType);
                var bagScene = (BagScene)mainSceneDict[MainTabs.Tab.Bag];
                bagScene.SelectGoods(classType, data.Id, asset.ResourceType == ResourceType.Avatar);
                mainTabsUI.DefualtOn(MainTabs.Tab.Bag);
            }
            else if (asset.ResourceType == ResourceType.UgcAvatar || asset.ResourceType == ResourceType.UGCPetAvatar)
            {
                var avatarAsset = asset as UGCAssetsData;
                classType = UniqueType.Get(asset.ResourceType, (int)avatarAsset.AvatarSubType);
                var bagScene = (BagScene)mainSceneDict[MainTabs.Tab.Bag];
                bagScene.SelectGoods(classType, data.Id, asset.ResourceType == ResourceType.Avatar);
                mainTabsUI.DefualtOn(MainTabs.Tab.Bag);
            }
        }

        public void JumpTo(MainTabs.Tab tab, int classType = -10000)
        {
            mainTabsUI.DefualtOn(tab);
            if (classType != -10000) classList.DefualtOn(classType);
        }
        public void JumpTo(int classType)
        {
            if (classType != -10000) classList.DefualtOn(classType);
        }

        public void JumpToPgcItem(string pgcId)
        {
            var resData = DataTables.GetGameResData(pgcId);
            if (resData == null) return;
            int classType = UniqueType.Get(resData.ResourceType, resData.SubType);
            JumpTo(MainTabs.Tab.BUD, classType);
        }

        // 购物车点击 item:优先用入车缓存的 GoodsData;冷启动恢复、无缓存的项按 id 异步拉 UGC 详情再构建后试穿
        public void TryOnCartItem(string id, Action onTriedOn = null)
        {
            if (string.IsNullOrEmpty(id)) return;
            GoodsData goodsData = ShoppingCartManager.Inst.GetGoodsData(id);
            if (goodsData != null)
            {
                ShowCartItemInfo(goodsData);
                TryOn(goodsData);
                onTriedOn?.Invoke();
                return;
            }
            AssetsDataManager.GetUgcInfo(id, recommend =>
            {
                var goods = BuildUgcGoodsData(recommend);
                if (goods == null) return;
                ShoppingCartManager.Inst.CacheGoods(goods);//回填缓存,下次直接命中
                ShowCartItemInfo(goods);
                TryOn(goods);
                onTriedOn?.Invoke();//异步试穿完成后再通知刷新穿戴标记
            });
        }

        private void ShowCartItemInfo(GoodsData goodsData)
        {
            itemInfoUI.gameObject.SetActive(true);
            itemInfoUI.SetTarget(goodsData);
        }

        // 由服务器拉到的 UGC 详情构建最小可试穿 GoodsData(购物车里都是 UGC;支持单件与套装)
        private static GoodsData BuildUgcGoodsData(RecommendItemData recommend)
        {
            var skin = recommend?.skinInfo;
            if (skin == null || recommend.UgcInfo == null) return null;

            // 套装(bundle):拆成多个子部件,参照 AvatarUgcSceneHandler.CreateUgcBundleAssetsData
            if (skin.subType == (int)AvatarSubType.Bundle)
            {
                var bundleAssets = new List<AssetsData>();
                if (skin.bundleItems != null)
                {
                    foreach (var subStr in skin.bundleItems)
                    {
                        var subInfo = JsonConvert.DeserializeObject<SkinInfo>(subStr);
                        if (subInfo == null) continue;
                        bundleAssets.Add(BuildUgcAsset(new RecommendItemData { ugcType = (UgcType)subInfo.subType, UgcInfo = subInfo }));
                    }
                }
                return new GoodsData
                {
                    Id = recommend.UgcInfo.id,
                    GoodsType = GoodsType.BundleUgc,
                    subType = skin.subType,
                    UgcBundleInfo = recommend,
                    Assets = bundleAssets,
                };
            }

            // 单件
            return new GoodsData
            {
                Id = recommend.UgcInfo.id,
                GoodsType = GoodsType.SingleUgc,
                subType = skin.subType,
                Assets = new List<AssetsData> { BuildUgcAsset(recommend) },
            };
        }

        // 由 RecommendItemData 构建单个 UGCAssetsData(参照 CreateUgcAssetsData 的关键字段)
        private static UGCAssetsData BuildUgcAsset(RecommendItemData data)
        {
            return new UGCAssetsData
            {
                Id = data.UgcInfo.id,
                Name = data.UgcInfo.name,
                ResourceType = ResourceType.UgcAvatar,
                AvatarSubType = (AvatarSubType)data.skinInfo.subType,
                UgcInfo = data,
            };
        }

        // 角色当前是否正穿着该商品(单件:该件在身上;套装:所有子部件都在身上)
        public bool IsWearing(string id)
        {
            if (avatarWrapper == null || string.IsNullOrEmpty(id)) return false;
            // 宠物模式 avatarWrapper 是 PetWrap，GetData<CharacterData>() 返回 null，需额外尝试 PetData
            List<CharacterPartData> partDatas = avatarWrapper.GetData<CharacterData>()?.partDatas
                                             ?? avatarWrapper.GetData<PetData>()?.partDatas;
            if (partDatas == null) return false;
            // 角色当前穿着的所有 id(UGC 部件 UId=ugcId、Id=templateId,都收进来)
            var worn = new HashSet<string>();
            foreach (var p in partDatas)
            {
                if (p == null) continue;
                if (!string.IsNullOrEmpty(p.Id)) worn.Add(p.Id);
                if (!string.IsNullOrEmpty(p.UId)) worn.Add(p.UId);
            }
            var goods = ShoppingCartManager.Inst.GetGoodsData(id);
            if (goods?.Assets != null && goods.Assets.Count > 0)
            {
                // 套装要所有子部件都在身上才算「正穿着」
                foreach (var asset in goods.Assets)
                    if (asset == null || !worn.Contains(asset.Id)) return false;
                return true;
            }
            return worn.Contains(id);//无缓存兜底:按购物车 id 判断
        }

        private GoodsData CreateNewModel(int index)
        {
            return scene.CreateNewModel(index);
        }

        private void OnShapeEmoteItemEvent(GoodsData goodsData)
        {
            PreviewEmote(goodsData);
        }

        private void OnShapeItemEvent(GoodsData goodsData)
        {
            CancelEmote();
            //UI.PreviewEmote(data);
            itemInfoUI.gameObject.SetActive(true);
            itemInfoUI.SetTarget(goodsData);

            operationUI.gameObject.SetActive(true);
            operationUI.SetTarget(goodsData);
            TryOn(goodsData);
            operationUI.OnOperation = (operation, target) =>
            {
                switch (operation)
                {
                    case Operation.ShoppingBuy:
                    case Operation.Buy:
                        scene.Buy(goodsData);
                        break;
                    case Operation.SkinTicketButton:
                        scene.UseSkinTicket(goodsData);
                        break;
                    case Operation.Wear:

                        PutOn(goodsData);
                        if (isCharacterFittingRoom)
                        {
                            animationCtrl.PlayerChangeClothesForUICharacer();
                        }
                        else
                        {
                            petAnimationCtrl.PlayChangeClothAni();
                        }
                        break;
                    case Operation.Jump:
                        CancelEmote();
                        goodsData.SourceData.Source.HandSkip(goodsData.SourceData.Id);
                        break;
                    case Operation.TryPlayMusic:
                        TryPlayMusic();
                        break;
                    case Operation.ChangeOtherOc:
                        ChangeOtherOc();
                        break;
                    case Operation.Select:
                        OnSelect(goodsData);
                        break;
                }
            };
        }

        public void OnBackClick()
        {
            if (!VIPSaveInfo(1))
            {
                return;
            }
            if (isCharacterFittingRoom)
            {
                var saveCharacterData = saveAvatarData as CharacterData;
                if(fittingRoomSource == FittingRoomSource.IncubationCabin){
                    //养成仓不设置数据
                    if(saveCharacterData != _cabinCharacterData){
                        //数据发生改变
                    }
                }else{
                    AccountDataManager.Inst.SyncAvatarData(saveCharacterData, isSuc =>
                    {
                        if (isSuc)
                        {
                            AvatarDataManager.Inst.SelfCharacterData = saveCharacterData;
                        }
                    });
                }
                OnCloseAction?.Invoke(saveCharacterData);
            }
            else
            {
                var savePetData = saveAvatarData as PetData;
                AccountDataManager.Inst.SyncPetAvatarData(savePetData, isSuc =>
                {
                    if (isSuc)
                    {
                        AvatarDataManager.Inst.SelfPetData = savePetData;
                    }
                });
            }
            UIManager.Inst.ClosePanel(this);
        }

        public override void OnShow(params object[] args)
        {
            fittingRoomSource = FittingRoomSource.Default;
            if (args.Length == 0)
            {
                isCharacterFittingRoom = true;
                InitCharacterWrapper();
            }
            else
            {
                if (args[0] is string){
                    //来源
                    string source = args[0] as string;
                    if (source == "IncubationCabin")
                    {
                        fittingRoomSource = FittingRoomSource.IncubationCabin;
                    }
                    isCharacterFittingRoom = true;
                    _cabinCharacterData = args[1] as CharacterData;
                    InitCharacterWrapper();
                }else{
                    if (args[0] is bool)
                    {
                        isCharacterFittingRoom = !(bool)args[0];
                    }
                    else
                    {
                        isCharacterFittingRoom = true;
                    }
                    if (isCharacterFittingRoom)
                    {
                        InitCharacterWrapper();
                    }
                    else
                    {
                        InitPetWrapper();
                    }

                if (args[0] is FittingRoomSelectData)
                {
                    selectData = args[0] as FittingRoomSelectData;
                    }
                    else
                    {
                        fittingRoomSource = FittingRoomSource.Default;
                    }
                }
            }
            ShowOcCompetitionBtn(isCharacterFittingRoom);
            InitBGUI();
            // Oc初始化
            ocList.Init(isCharacterFittingRoom);
            sectionList.Init(isCharacterFittingRoom);
            MessageHelper.AddListener<string>(MessageName.ShowToast, ShowToast);

            _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
            _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
            _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
            _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();

            assetsList.IsFittingRoomPanel = true;
            assetsList.gameObject.SetActive(true);
            assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
            assetsList.Init();

            mainSceneDict.Add(MainTabs.Tab.Test, new TestScene(this));
            mainSceneDict.Add(MainTabs.Tab.BUD, new BUDScene(this));
            mainSceneDict.Add(MainTabs.Tab.Action, new ActionScene(this));
            mainSceneDict.Add(MainTabs.Tab.Ugc, new UGCScene(this));
            mainSceneDict.Add(MainTabs.Tab.Bag, new BagScene(this));
            mainTabsUI.DefualtOn(MainTabs.Tab.Ugc);

            Message.MessageHelper.AddListener(Message.MessageName.OnTryListenMIChange, OnTryListenMIChange);
            Message.MessageHelper.AddListener(Message.MessageName.OnTryListenMSChange, OnTryListenMSChange);

            switch ((CustomBodyTypeController.BodyType)avatarInfo.bodyType)
            {
                case CustomBodyTypeController.BodyType.Type4:
                    characterRoot.localPosition = new Vector3(0, avatarPosY + 0.1f, 0);
                    break;
                default:
                    characterRoot.localPosition = new Vector3(0, avatarPosY, 0);
                    break;
            }
        }
        public bool IsClassType(int classType)
        {
            return selectedClassData.Id == classType;
        }
        public bool HaveSelectData()
        {
            return selectData != null;
        }
        public void OnSelect(GoodsData goodsdata)
        {
            if (selectData != null)
            {
                selectData.OnSelect.Invoke(goodsdata);
                CloseSelf();
            }
        }
        public bool CanSelect(GoodsData goodsdata)
        {
            if (selectData != null)
            {
                return selectData.CanSelect.Invoke(goodsdata);
            }
            return false;
        }

        private void ShowToast(string toast)
        {
            TipPanel.ShowToast(toast);
        }

        public override void OnHidden()
        {
            ResetActorShowBtnFlags();
            MessageHelper.RemoveListener<string>(MessageName.ShowToast, ShowToast);
            MessageHelper.RemoveListener<CharacterViewExpressionItem>(Message.MessageName.ActorCharacterOpenFittingRoomPanel,ActorCharacterOpenFittingRoomPanel);
            MessageHelper.RemoveListener<WardrobeViewWardrobelistItem>(Message.MessageName.ActorWardrobeViewOpenFittingRoomPanel,ActorWardrobeViewOpenFittingRoomPanel);

            AmbientLightManager.Inst.CloseUILight(_srcLightSetting);
            AmbientLightManager.Inst.RevertHallLight(_srcHallLightVisible);
            AmbientLightManager.Inst.RevertGameSceneLight(_srcGameSceneLightVisible);
            AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);

            foreach (var kv in mainSceneDict)
            {
                kv.Value.Destory();
            }

            if (isCharacterFittingRoom)
            {
                animationCtrl.ResetEmoteForUICharacter();
                otherAnimationCtrl.gameObject.SetActive(false);
                otherAnimationCtrl.ResetEmoteForUICharacter();
            }

            if (_actorCharacterWrap != null)
            {
                Destroy(_actorCharacterWrap.Avatar);
                _actorCharacterWrap = null;
            }

            ocList?.RemoveListener();

            Message.MessageHelper.RemoveListener(Message.MessageName.OnTryListenMIChange, OnTryListenMIChange);
            Message.MessageHelper.RemoveListener(Message.MessageName.OnTryListenMSChange, OnTryListenMSChange);
        }

        #region 换装

        internal void ChangeOc(OcServerData ocServerData)
        {
            if (avatarWrapper is CharacterWrap characterWrap)
            {
                characterWrap.SetCharacterData(CharacterData.DeserializeObject(ocServerData.ocInfo.avatarJson));
                animationCtrl.CheckAndOverrideSpecialAnim();
                saveAvatarData = characterWrap.ChaData.Clone();
            }
            else if (avatarWrapper is PetWrap petWrap)
            {
                petWrap.SetData(PetData.DeserializeObject(ocServerData.ocInfo.avatarJson));
                saveAvatarData = petWrap.Data.Clone();
            }
        }

        /// <summary>
        /// 试穿 重置参数
        /// </summary>
        /// <param name="goodsData"></param>
        internal ResourceType lastTryOnType = ResourceType.ErrResourceType;
        internal void ResetLastTryOn(ResourceType nowType)
        {
            switch (lastTryOnType)
            {
                case ResourceType.Avatar:
                case ResourceType.UgcAvatar:
                case ResourceType.PGCPetAvatar:
                case ResourceType.UGCPetAvatar:
                    CancelTryOn();
                    break;
                case ResourceType.MusicScore:
                    if (nowType != lastTryOnType)
                    {
                        CancelPreviewMusicScore();
                        CancelPreviewMusicalInstrument();
                        CancelTryOn();
                        CancelEmote();
                    }
                    break;
                case ResourceType.Emote:
                    CancelEmote();
                    break;
                case ResourceType.UgcEmote:
                    CancelEmote();
                    break;
                case ResourceType.UgcPose:
                    break;
                case ResourceType.Vehicle:
                    CancelTryOn();
                    break;
                case ResourceType.AvatarCard:
                    _currentActorInfo = null;
                    _actorCharacterDataList = null;
                    arrowLeftBtn.gameObject.SetActive(false);
                    arrowRightBtn.gameObject.SetActive(false);
                    actorCardBtn.gameObject.SetActive(false);
                    theatreBuyBtn.gameObject.SetActive(false);
                    _actorCharacterWrap?.Avatar.SetActive(false);
                    avatarWrapper.Avatar.SetActive(true);
                    CancelTryOn(); // 恢复 saveAvatarData 到 avatarWrapper
                    break;
                case ResourceType.Theatre:
                    _currentTheatreInfo = null;
                    _theatreActorIndex = 0;
                    if (_theatreActorCoroutine != null)
                    {
                        StopCoroutine(_theatreActorCoroutine);
                        _theatreActorCoroutine = null;
                    }
                    if (_theatreCycleCoroutine != null)
                    {
                        StopCoroutine(_theatreCycleCoroutine);
                        _theatreCycleCoroutine = null;
                    }
                    arrowLeftBtn.gameObject.SetActive(false);
                    arrowRightBtn.gameObject.SetActive(false);
                    tryPlayBtn.gameObject.SetActive(false);
                    theatreBuyBtn.gameObject.SetActive(false);
                    introductionBtn.gameObject.SetActive(false);
                    _actorCharacterWrap?.Avatar.SetActive(false);
                    avatarWrapper.Avatar.SetActive(true);
                    CancelTryOn();
                    break;
            }

            lastTryOnType = nowType;
            if (nowType == ResourceType.ErrResourceType)
            {
                _hasTryOnUnowned = false;
                _hasEmoteUnowned = false;
                RefreshActorShowBtns();
            }
        }

        internal void ResetLastTryOn(GoodsData goodsData)
        {
            switch (goodsData.GoodsType)
            {
                case GoodsType.BundleUgc:
                    var previewType = ResourceType.ErrResourceType;
                    foreach (AvatarAssetsData bundleItem in goodsData.Assets)
                    {
                        if (previewType == ResourceType.ErrResourceType)
                        {
                            previewType = bundleItem.ResourceType;
                        }

                        if (bundleItem.AvatarSubType == AvatarSubType.MusicalInstrument)
                        {
                            ResetLastTryOn(ResourceType.ErrResourceType);
                            previewType = ResourceType.MusicScore;
                        }
                    }
                    ResetLastTryOn(previewType);
                    break;
                case GoodsType.SinglePgc:
                case GoodsType.SingleUgc:
                    var asset = goodsData.GetFirstAsset<AssetsData>();
                    if (asset == null)
                    {
                        ResetLastTryOn(ResourceType.ErrResourceType);
                    }
                    else if (asset.ResourceType == ResourceType.Avatar || asset.ResourceType == ResourceType.UgcAvatar)
                    {
                        var avatarAsset = asset as AvatarAssetsData;
                        if (avatarAsset.AvatarSubType == AvatarSubType.MusicalInstrument)
                        {
                            ResetLastTryOn(ResourceType.MusicScore);
                        }
                        else
                        {
                            ResetLastTryOn(asset.ResourceType);
                        }
                    }
                    else
                    {
                        ResetLastTryOn(asset.ResourceType);
                    }
                    break;
            }
        }

        internal void EnterPreviewMusicScore()
        {
            ResetLastTryOn(ResourceType.MusicScore);
            PreviewMusicScore();
        }

        internal void TryOn(GoodsData goodsData)
        {
            ResetLastTryOn(goodsData);
            _hasTryOnUnowned = !goodsData.IsOwned;
            RefreshActorShowBtns();
            if (!PGCVehicleManager.Inst.HasActiveGameVehicles)
                PGCVehicleManager.Inst.Release();
            if (selectedVehicleInfo != null)
            {
                TakeOffVehicle();
                if(goodsData.Assets != null && goodsData.Assets.Count>0)
                {
                    if(goodsData.Assets[0] is AvatarAssetsData asset)
                    {
                        var classType = asset.ResourceType == ResourceType.Avatar
                        ? UniqueType.GetAvatar(asset.AvatarSubType)
                        : UniqueType.GetPGCPetAvatar(asset.AvatarSubType);
                        avatarCameraController.ResetVehicleView();
                        avatarCameraController.SetCameraZoom(classType);
                    }
                    else if (goodsData.Assets[0] is EmoteAssetsData emoteAssets)
                    {
                        var classType = UniqueType.Get(emoteAssets.ResourceType, (int)emoteAssets.EmoteSubType);
                        avatarCameraController.ResetVehicleView();
                        avatarCameraController.SetCameraZoom(classType);                     
                    }

                }
        
            }
            //avatarWrapper.Avatar.gameObject.SetActive(true);
            for (int i = 0, C = goodsData.Assets.Count; i < C; i++)
            {
                var assets = goodsData.Assets[i];
                switch (assets.ResourceType)
                {
                    case ResourceType.PGCPetAvatar:
                        var petPgcAssets = assets as PGCAssetsData;
                        var petSubType = UniqueType.GetPGCPetAvatar(petPgcAssets.AvatarSubType);
                        var petConfig = DataTables.GetPetAvatarCommonData(goodsData.GetPgcId());
                        goodsData.Loading(true);
                        avatarWrapper.ChangePart(petSubType, petPgcAssets.Id, () => goodsData.Loading(false));
                        avatarWrapper.ChangeColor(petSubType, petConfig.defaultColor);
                        avatarWrapper.Move(petSubType, petConfig.pDef);
                        avatarWrapper.Rotate(petSubType, petConfig.rDef);
                        avatarWrapper.Scale(petSubType, petConfig.sDef);
                        avatarWrapper.HVScale(petSubType, petConfig.vhSDef);
                        avatarWrapper.SetLeftOrRight(petSubType, petConfig.leftRightType);
                        break;
                    case ResourceType.UGCPetAvatar:
                        var petUgcAssets = assets as UGCAssetsData;
                        goodsData.Loading(true);
                        avatarWrapper.ChangeUGCPart(petUgcAssets.UgcInfo.skinInfo, () => goodsData.Loading(false));
                        break;
                    case ResourceType.Avatar:
                        var pgcAssets = assets as PGCAssetsData;
                        var subType = UniqueType.GetAvatar(pgcAssets.AvatarSubType);
                        var specialConfig = DataTables.GetSpecialSkinConfig(pgcAssets.Id);
                        var config = DataTables.GetAvatarCommonData(goodsData.GetPgcId());
                        goodsData.Loading(true);
                        // 特殊皮肤异步加载期间先隐藏角色，避免露出 base；就绪后在回调里显示并切 preview
                        if (specialConfig != null)
                        {
                            avatarWrapper.Avatar.SetActive(false);
                            // 装备新特殊皮肤前清掉 dedup 缓存：否则之前缓存的 Idle 会让下面的 OnSpecialPGCClick(Idle)
                            // 被 dedup 吞掉，新皮肤永远不会被驱动到 preview_idle_exhibit
                            _lastSwitchSpecialAnim = null;
                        }
                        avatarWrapper.ChangePart(subType, pgcAssets.Id, () =>
                        {
                            goodsData.Loading(false);
                            if (specialConfig != null && animationCtrl != null)
                            {
                                avatarWrapper.Avatar.SetActive(true);
                                // ChangePart 的 callback 实际在 PutOn 回调里 sync 触发，比 effect prefab 加载完更早 ——
                                // 直接 OnSpecialPGCClick(Idle) 时 specialAnimRoot 还是骨骼、云 Animator 还没创建出来。
                                // poll 等云 Animator 出现再触发 SpecialAnimContainer 的待机按钮事件（用户验证过手动点是正常的）。
                                CoroutineManager.Inst.StartCoroutine(WaitForSpecialEffectAndResetIdle());
                            }
                        });
                        avatarWrapper.ChangeColor(subType, config.defaultColor);
                        avatarWrapper.Move(subType, config.pDef);
                        avatarWrapper.Rotate(subType, config.rDef);
                        avatarWrapper.Scale(subType, config.sDef);
                        avatarWrapper.HVScale(subType, config.vhSDef);
                        avatarWrapper.SetLeftOrRight(subType, config.leftRightType);
                        if (pgcAssets.AvatarSubType == AvatarSubType.MusicalInstrument) PreviewMusicalInstrument(true, pgcAssets.Id);
                        else
                        {
                            if (specialConfig != null) ShowSpecialContainer();
                        }
                        break;
                    case ResourceType.UgcAvatar:
                        var ugcAssets = assets as UGCAssetsData;
                        goodsData.Loading(true);
                        avatarWrapper.ChangeUGCPart(ugcAssets.UgcInfo.skinInfo, () => goodsData.Loading(false));
                        if (ugcAssets.UgcInfo.skinInfo.subType == (int)AvatarSubType.MusicalInstrument) PreviewMusicalInstrument(false, ugcAssets.UgcInfo.skinInfo.id);
                        break;
                    case ResourceType.Emote:
                        var emoteAssets = assets as EmoteAssetsData;
                        avatarCameraController.SetEmoteView(emoteAssets.Id);
                        // 玩家本体要播 emote 时隐藏特殊皮肤特效，避免穿模（PetSingle 只动宠物、玩家保持特殊待机则不隐藏）
                        if (emoteAssets.EmoteSubType != EmoteSubType.PetSingle && emoteAssets.EmoteSubType != EmoteSubType.PetSingleLoop)
                            animationCtrl.SetSpecialEffectActive(false);
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
                            case EmoteSubType.PetSingle:
                            case EmoteSubType.PetSingleLoop:
                                goodsData.Loading(true);
                                petAnimationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id, OnDownloadOver: () => goodsData.Loading(false));
                                break;
                            case EmoteSubType.PetWithPlayer:
                            case EmoteSubType.PetWithPlayerLoop:
                                goodsData.Loading(true);
                                petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                                break;
                            case EmoteSubType.LinkEmote:
                                goodsData.Loading(true);
                                animationCtrl.PlayLinkEmoteForUICharacter(emoteAssets.Id, SpecialAnim.Idle, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                                break;
                        }
                        break;
                    case ResourceType.MusicScore:
                        var msInfo = assets as MusicScoreAssetsData;
                        PreviewMusicScore(() =>
                        {
                            MusicalInstrumentManager.Inst.CheckInstrumentCanPlayMusicScore(playerHold.curToneInfo, msInfo.UgcInfo.musicScoreInfo, () =>
                            {
                                TipPanel.ShowToast("这个乐谱是22音，你的乐器是15音，听起来可能会少音哦");
                            });
                            MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(playerHold.curToneInfo);
                            playMusicScoreBev.ChangePlayType(PlayMusicScoreBev.PlayType.Loop);
                            playMusicScoreBev.StartPLay(msInfo.UgcInfo.musicScoreInfo, OnMusicScorePlaying);
                        });
                        break;
                    case ResourceType.UgcPose:
                        var poseInfo = assets as UgcPoseAssetsData;
                        avatarCameraController.SetEmoteView((UgcAnimSubType)poseInfo.UgcInfo.poseInfo.poseType);
                        switch ((UgcPoseSubType)poseInfo.UgcInfo.poseInfo.poseType)
                        {
                            case UgcPoseSubType.Single:
                            case UgcPoseSubType.Double:
                                animationCtrlIK.Pose(poseInfo.UgcInfo.poseInfo, otherAnimationCtrlIK);
                                break;
                            case UgcPoseSubType.PetSingle:
                            case UgcPoseSubType.PetWithPlayer:
                                petAnimationCtrlIK.Pose(poseInfo.UgcInfo.poseInfo, otherAnimationCtrlIK);
                                break;
                        }
                        break;
                    case ResourceType.UgcEmote:
                        var ueInfo = assets as UgcAnimAssetsData;
                        avatarCameraController.SetEmoteView((UgcAnimSubType)ueInfo.UgcInfo.animInfo.animType);
                        switch ((UgcAnimSubType)ueInfo.UgcInfo.animInfo.animType)
                        {
                            case UgcAnimSubType.Single:
                            case UgcAnimSubType.Double:
                                animationCtrlIK.SetUIPreviewMode(true);
                                animationCtrlIK.Play(ueInfo.UgcInfo.animInfo, otherAnimationCtrlIK, CancelEmote);
                                break;
                            case UgcAnimSubType.PetSingle:
                            case UgcAnimSubType.PetWithPlayer:
                                petAnimationCtrlIK.SetUIPreviewMode(true);
                                petAnimationCtrlIK.Play(ueInfo.UgcInfo.animInfo, otherAnimationCtrlIK, CancelEmote);
                                break;
                        }
                        break;
                    case ResourceType.UgcVehicle:
                        var vehicleInfo = assets as UgcVehicleAssetsData;
                        SetSelectedVehicleInfo(vehicleInfo.UgcInfo.vehicleInfo, isShowSave: false);
                        break;
                    case ResourceType.Vehicle:
                       if(int.TryParse(assets.Id, out int pgcId)){
                            var pgcVehicleConfig = DataTables.GetPgcVehicleConfig(pgcId);

                            PGCVehicleManager.Inst.CreateUIPGCVehicle(pgcId, characterRoot, new PlayerAnimationCtrl[]{animationCtrl, otherAnimationCtrl}, (vehicleController) =>
                            {
                                avatarCameraController.SetVehicleViewByConfig(pgcVehicleConfig);
                            });
                        }
                        break;
                    case ResourceType.AvatarCard:
                        var actorAssetsTryOn = assets as UgcActorAssetsData;
                        var actorInfoTryOn = actorAssetsTryOn?.UgcInfo?.theatreAvatarInfo;
                        if (actorInfoTryOn?.avatarClothes == null || actorInfoTryOn.avatarClothes.Count == 0) break;
                        SetCurrentActorInfo(actorInfoTryOn);
                        if (_actorCharacterDataList.Count == 0) break;
                        avatarWrapper.Avatar.SetActive(false);
                        var actorWrapTryOn = EnsureActorCharacterWrap();
                        actorWrapTryOn.Avatar.SetActive(true);
                        actorWrapTryOn.SetCharacterData(_actorCharacterDataList[0]);
                        avatarCameraController.SetCameraZoom(ViewType.ZoomWholeBody);
                        break;
                    case ResourceType.Theatre:
                        var theatreAssetsTryOn = assets as UgcTheatreAssetsData;
                        var theatreInfoTryOn = theatreAssetsTryOn?.UgcInfo?.theatreInfo;
                        if (theatreInfoTryOn == null) break;
                        _currentTheatreInfo = theatreInfoTryOn;
                        _theatreActorIndex = 0;
                        tryPlayBtn.gameObject.SetActive(!goodsData.IsOwned);
                        theatreBuyBtn.gameObject.SetActive(goodsData.IsOwned);
                        introductionBtn.gameObject.SetActive(true);
                        bool hasMultipleActorsTryOn = theatreInfoTryOn.avatarList != null && theatreInfoTryOn.avatarList.Count > 1;
                        arrowLeftBtn.gameObject.SetActive(hasMultipleActorsTryOn);
                        arrowRightBtn.gameObject.SetActive(hasMultipleActorsTryOn);
                        avatarWrapper.Avatar.SetActive(false);
                        EnsureActorCharacterWrap().Avatar.SetActive(true);
                        StartLoadTheatreActor(theatreInfoTryOn, 0);
                        break;
                }
            }
        }

        /// <summary>
        /// 穿上 颜色不重置
        /// </summary>
        /// <param name="goodsData"></param>
        internal void PutOn(GoodsData goodsData)
        {
            try
            {
                _hasTryOnUnowned = false;
                ResetLastTryOn(goodsData);
                PGCVehicleManager.Inst.RemoveUIPGCVehicle();
                for (int i = 0, C = goodsData.Assets.Count; i < C; i++)
                {
                    var assets = goodsData.Assets[i];
                    switch (assets.ResourceType)
                    {
                        case ResourceType.Avatar:
                            var pgcAssets = assets as PGCAssetsData;
                            var subType = UniqueType.GetAvatar(pgcAssets.AvatarSubType);
                            var specialConfig = DataTables.GetSpecialSkinConfig(pgcAssets.Id);
                            var config = DataTables.GetAvatarCommonData(goodsData.GetPgcId());
                            goodsData.Loading(true);
                            // 特殊皮肤异步加载期间先隐藏角色，避免露出 base；就绪后在回调里显示并切 preview
                            if (specialConfig != null)
                            {
                                avatarWrapper.Avatar.SetActive(false);
                                // 装备新特殊皮肤前清掉 dedup 缓存，避免后续 OnSpecialPGCClick(Idle) 被吞掉
                                _lastSwitchSpecialAnim = null;
                            }
                            avatarWrapper.ChangePart(subType, pgcAssets.Id, () =>
                            {
                                goodsData.Loading(false);
                                if (specialConfig != null && animationCtrl != null)
                                {
                                    avatarWrapper.Avatar.SetActive(true);
                                    // ChangePart callback 在 PutOn 里 sync 触发，比 effect prefab 加载早。
                                    // poll 等云 Animator 就绪后顺序调 OnSpecialAnimChange(Run) → OnSpecialAnimChange(PreviewIdle)，
                                    // 让云脱离默认 idle state 再切回，绕开 Mecanim Play short-circuit。
                                    CoroutineManager.Inst.StartCoroutine(WaitForSpecialEffectAndResetIdle());
                                }
                            });
                            avatarWrapper.Move(subType, config.pDef);
                            avatarWrapper.Rotate(subType, config.rDef);
                            avatarWrapper.Scale(subType, config.sDef);
                            avatarWrapper.HVScale(subType, config.vhSDef);
                            avatarWrapper.SetLeftOrRight(subType, config.leftRightType);
                            if (pgcAssets.AvatarSubType == AvatarSubType.MusicalInstrument) PreviewMusicalInstrument(true, pgcAssets.Id);
                            else
                            {
                                if (specialConfig != null)
                                {
                                    ShowSpecialContainer();
                                }
                                else
                                {
                                    HideSpecialContainer();
                                }
                            }
                            break;
                        case ResourceType.UgcAvatar:
                            var ugcAssets = assets as UGCAssetsData;
                            goodsData.Loading(true);
                            avatarWrapper.ChangeUGCPart(ugcAssets.UgcInfo.skinInfo, () => goodsData.Loading(false));
                            if (ugcAssets.UgcInfo.skinInfo.subType == (int)AvatarSubType.MusicalInstrument) PreviewMusicalInstrument(false, ugcAssets.UgcInfo.skinInfo.id);
                            break;
                        case ResourceType.MusicScore:
                            var msInfo = assets as MusicScoreAssetsData;
                            PreviewMusicScore(() =>
                            {
                                MusicalInstrumentManager.Inst.CheckInstrumentCanPlayMusicScore(playerHold.curToneInfo, msInfo.UgcInfo.musicScoreInfo, () =>
                                {
                                    TipPanel.ShowToast("这个乐谱是22音，你的乐器是15音，听起来可能会少音哦");
                                });
                                MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(playerHold.curToneInfo);
                                playMusicScoreBev.ChangePlayType(PlayMusicScoreBev.PlayType.Loop);
                                playMusicScoreBev.StartPLay(msInfo.UgcInfo.musicScoreInfo, OnMusicScorePlaying);
                            });
                            return;
                        case ResourceType.PGCPetAvatar:
                            var petPgcAssets = assets as PGCAssetsData;
                            var petSubType = UniqueType.GetPGCPetAvatar(petPgcAssets.AvatarSubType);
                            var petConfig = DataTables.GetPetAvatarCommonData(goodsData.GetPgcId());
                            goodsData.Loading(true);
                            avatarWrapper.ChangePart(petSubType, petPgcAssets.Id, () => goodsData.Loading(false));
                            avatarWrapper.Move(petSubType, petConfig.pDef);
                            avatarWrapper.Rotate(petSubType, petConfig.rDef);
                            avatarWrapper.Scale(petSubType, petConfig.sDef);
                            avatarWrapper.HVScale(petSubType, petConfig.vhSDef);
                            avatarWrapper.SetLeftOrRight(petSubType, petConfig.leftRightType);

                            break;
                        case ResourceType.UGCPetAvatar:
                            var petUgcAssets = assets as UGCAssetsData;
                            goodsData.Loading(true);
                            avatarWrapper.ChangeUGCPart(petUgcAssets.UgcInfo.skinInfo, () => goodsData.Loading(false));
                            break;
                        case ResourceType.UgcPose:
                            var poseInfo = assets as UgcPoseAssetsData;
                            avatarCameraController.SetEmoteView((UgcAnimSubType)poseInfo.UgcInfo.poseInfo.poseType);
                            switch ((UgcPoseSubType)poseInfo.UgcInfo.poseInfo.poseType)
                            {
                                case UgcPoseSubType.Single:
                                case UgcPoseSubType.Double:
                                    animationCtrlIK.Pose(poseInfo.UgcInfo.poseInfo, otherAnimationCtrlIK);
                                    break;
                                case UgcPoseSubType.PetSingle:
                                case UgcPoseSubType.PetWithPlayer:
                                    petAnimationCtrlIK.Pose(poseInfo.UgcInfo.poseInfo, otherAnimationCtrlIK);
                                    break;
                            }
                            break;
                        case ResourceType.UgcEmote:
                            var ueInfo = assets as UgcAnimAssetsData;
                            avatarCameraController.SetEmoteView((UgcAnimSubType)ueInfo.UgcInfo.animInfo.animType);
                            switch ((UgcAnimSubType)ueInfo.UgcInfo.animInfo.animType)
                            {
                                case UgcAnimSubType.Single:
                                case UgcAnimSubType.Double:
                                    animationCtrlIK.SetUIPreviewMode(true);
                                    animationCtrlIK.Play(ueInfo.UgcInfo.animInfo, otherAnimationCtrlIK, CancelEmote);
                                    break;
                                case UgcAnimSubType.PetSingle:
                                case UgcAnimSubType.PetWithPlayer:
                                    petAnimationCtrlIK.SetUIPreviewMode(true);
                                    petAnimationCtrlIK.Play(ueInfo.UgcInfo.animInfo, otherAnimationCtrlIK, CancelEmote);
                                    break;
                            }
                            break;
                        case ResourceType.UgcVehicle:
                            var vehicleInfo = assets as UgcVehicleAssetsData;
                            SetSelectedVehicleInfo(vehicleInfo.UgcInfo.vehicleInfo);
                            return; //载具不用被动保存，玩家直接点击按钮
                        case ResourceType.Vehicle:
                            if(int.TryParse(assets.Id, out int pgcId)){
                                var pgcVehicleConfig = DataTables.GetPgcVehicleConfig(pgcId);
                                PGCVehicleManager.Inst.CreateUIPGCVehicle(pgcId, characterRoot, new PlayerAnimationCtrl[]{animationCtrl, otherAnimationCtrl}, (vehicleController) =>
                                {
                                    avatarCameraController.SetVehicleViewByConfig(pgcVehicleConfig);
                                    selectedVehicleInfo = new VehicleInfo();
                                    selectedVehicleInfo.id = assets.Id;
                                    ShowVehicleButton();
                                });
                            }
                            return;
                        case ResourceType.AvatarCard:
                            var actorAssetsPutOn = assets as UgcActorAssetsData;
                            var actorInfoPutOn = actorAssetsPutOn?.UgcInfo?.theatreAvatarInfo;
                            if (actorInfoPutOn?.avatarClothes == null || actorInfoPutOn.avatarClothes.Count == 0) break;
                            SetCurrentActorInfo(actorInfoPutOn);
                            if (_actorCharacterDataList.Count == 0) break;
                            avatarWrapper.Avatar.SetActive(false);
                            var actorWrapPutOn = EnsureActorCharacterWrap();
                            actorWrapPutOn.Avatar.SetActive(true);
                            actorWrapPutOn.SetCharacterData(_actorCharacterDataList[0]);
                            avatarCameraController.SetCameraZoom(ViewType.ZoomWholeBody);
                            return; // 不保存到 saveAvatarData
                        case ResourceType.Theatre:
                            var theatreAssetsPutOn = assets as UgcTheatreAssetsData;
                            var theatreInfoPutOn = theatreAssetsPutOn?.UgcInfo?.theatreInfo;
                            if (theatreInfoPutOn == null) break;
                            _currentTheatreInfo = theatreInfoPutOn;
                            _theatreActorIndex = 0;
                            tryPlayBtn.gameObject.SetActive(!goodsData.IsOwned);
                            theatreBuyBtn.gameObject.SetActive(goodsData.IsOwned);
                            introductionBtn.gameObject.SetActive(true);
                            bool hasMultipleActorsPutOn = theatreInfoPutOn.avatarList != null && theatreInfoPutOn.avatarList.Count > 1;
                            arrowLeftBtn.gameObject.SetActive(hasMultipleActorsPutOn);
                            arrowRightBtn.gameObject.SetActive(hasMultipleActorsPutOn);
                            avatarWrapper.Avatar.SetActive(false);
                            EnsureActorCharacterWrap().Avatar.SetActive(true);
                            StartLoadTheatreActor(theatreInfoPutOn, 0);
                            return; // 不保存到 saveAvatarData
                    }
                }

                if (avatarWrapper is CharacterWrap characterWrap)
                {
                    saveAvatarData = characterWrap.ChaData.Clone();
                }
                else if (avatarWrapper is PetWrap petWrap)
                {
                    saveAvatarData = petWrap.Data.Clone();
                }


            }
            catch (Exception e) { Debug.Log(e.Message + e.StackTrace); }
        }

        internal void PreviewEmote(GoodsData goodsData)
        {
            CancelEmote();
            _hasEmoteUnowned = !goodsData.IsOwned;
            RefreshActorShowBtns();

            for (int i = 0, C = goodsData.Assets.Count; i < C; i++)
            {
                var assets = goodsData.Assets[i];
                if (assets.ResourceType == ResourceType.Emote)
                {
                    var emoteAssets = assets as EmoteAssetsData;
                    avatarCameraController.SetEmoteView(emoteAssets.Id);
                    // 玩家本体要播 emote 时隐藏特殊皮肤特效，避免穿模（PetSingle 只动宠物、玩家保持特殊待机则不隐藏）
                    if (emoteAssets.EmoteSubType != EmoteSubType.PetSingle && emoteAssets.EmoteSubType != EmoteSubType.PetSingleLoop)
                        animationCtrl.SetSpecialEffectActive(false);
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
                        case EmoteSubType.PetSingle:
                        case EmoteSubType.PetSingleLoop:
                            goodsData.Loading(true);
                            petAnimationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id, OnDownloadOver: () => goodsData.Loading(false));
                            break;
                        case EmoteSubType.PetWithPlayer:
                        case EmoteSubType.PetWithPlayerLoop:
                            goodsData.Loading(true);
                            petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                            break;
                        case EmoteSubType.LinkEmote:
                            goodsData.Loading(true);
                            animationCtrl.PlayLinkEmoteForUICharacter(emoteAssets.Id, SpecialAnim.Idle, otherAnimationCtrl, OnDownloadOver: () => goodsData.Loading(false));
                            break;
                    }
                }
            }
        }

        internal void CancelTryOn()
        {
            if (avatarWrapper is CharacterWrap characterWrap)
            {
                characterWrap.RefreshAvatar(saveAvatarData as CharacterData);
                HideSpecialContainer();
                PGCVehicleManager.Inst.RemoveUIPGCVehicle( PGCVehicleUIUsageType.FittingRoom, ()=>{
                    avatarCameraController.ResetEmoteView();
                });
            }
            else if (avatarWrapper is PetWrap petWrap)
            {
                petWrap.RefreshAvatar(saveAvatarData as PetData);
            }
        }

        internal void CancelEmote()
        {
            _hasEmoteUnowned = false;
            avatarCameraController.ResetEmoteView();
            avatarCameraController.SetCameraZoom(0);
            if (isCharacterFittingRoom)
            {
                animationCtrl.ResetEmoteForUICharacter();
                otherAnimationCtrl.ResetEmoteForUICharacter();
                animationCtrlIK.StopAnimAndResetJointNode();
                animationCtrlIK.SetUIPreviewMode(false);
                animationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
                otherAnimationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
                ResetIKPosition();
                otherAnimationCtrl.UpdateAnim(0);
                otherAnimationCtrl.gameObject.SetActive(false);
            }
            else
            {
                petAnimationCtrl.ResetEmoteForUICharacter();
                otherAnimationCtrl.ResetEmoteForUICharacter();
                petAnimationCtrlIK.StopAnimAndResetJointNode();
                petAnimationCtrlIK.SetUIPreviewMode(false);
                petAnimationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
                otherAnimationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
                ResetPetIKPosition();
                otherAnimationCtrl.UpdateAnim(0);
                otherAnimationCtrl.gameObject.SetActive(false);
            }

            //由于ResetIK会重置Scale，所以需要重新应用BodyType到体型
            if (animationCtrlIK != null && animationCtrlIK.transform.TryGetComponent<CustomBodyTypeController>(out var shapCtrl))
            {
                int bodyType = saveAvatarData is CharacterData ? (saveAvatarData as CharacterData).bodyType : avatarInfo.bodyType;
                CustomBodyTypeController.BodyType type = bodyType == 0 ? CustomBodyTypeController.BodyType.None : (CustomBodyTypeController.BodyType)bodyType;
                shapCtrl.ApplyBodyType(type);
            }

        }

        internal void ResetIKPosition()
        {
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.Single);
            animationCtrlIK.transform.localPosition = poseModeData.RoleDefPos[0];
            animationCtrlIK.transform.localEulerAngles = Vector3.zero;
            animationCtrlIK.transform.localScale = Vector3.one;
            animationCtrlIK.transform.parent.localPosition = poseModeData.EditPos[0];
            animationCtrlIK.transform.parent.localEulerAngles = Vector3.zero;
            animationCtrlIK.transform.parent.localScale = Vector3.one;

            otherAnimationCtrlIK.transform.localPosition = poseModeData.RoleDefPos[0];
            otherAnimationCtrlIK.transform.localEulerAngles = Vector3.zero;
            otherAnimationCtrlIK.transform.localScale = Vector3.one;
            otherAnimationCtrlIK.transform.parent.localPosition = poseModeData.EditPos[0];
            otherAnimationCtrlIK.transform.parent.localEulerAngles = Vector3.zero;
            otherAnimationCtrlIK.transform.parent.localScale = Vector3.one;
        }

        internal void ResetPetIKPosition()
        {
            var poseModeData = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetSingle);
            petAnimationCtrlIK.transform.localPosition = poseModeData.RoleDefPos[0];
            petAnimationCtrlIK.transform.localEulerAngles = Vector3.zero;
            // petAnimationCtrlIK.transform.localScale = Vector3.one;
            petAnimationCtrlIK.transform.parent.localPosition = poseModeData.EditPos[0];
            petAnimationCtrlIK.transform.parent.localEulerAngles = Vector3.zero;
            petAnimationCtrlIK.transform.parent.localScale = Vector3.one;

            var poseModeData1 = DataTables.GetPoseModeConfig((int)UgcPoseSubType.PetWithPlayer);
            otherAnimationCtrlIK.transform.localPosition = poseModeData1.RoleDefPos[0];
            otherAnimationCtrlIK.transform.localEulerAngles = Vector3.zero;
            otherAnimationCtrlIK.transform.localScale = Vector3.one;
            otherAnimationCtrlIK.transform.parent.localPosition = poseModeData1.EditPos[0];
            otherAnimationCtrlIK.transform.parent.localEulerAngles = Vector3.zero;
            otherAnimationCtrlIK.transform.parent.localScale = Vector3.one;
        }

        internal void TakeOff(int resType)
        {

            if(resType == UniqueType.Get(ResourceType.UgcVehicle, (int)VehicleSubType.AllVehicle)){
                TakeOffVehicle();
                return;
            }
            
            avatarWrapper.TakeOff(avatarWrapper.GetMutexType(resType));
            avatarWrapper.TakeOff(resType);

            if (resType == UniqueType.GetAvatar(AvatarSubType.SpecialSkin))
            {
                HideSpecialContainer();
                avatarCameraController.SetCameraZoom(resType);
            }

            if (isCharacterFittingRoom)
            {
                saveAvatarData = avatarWrapper.GetData<CharacterData>().Clone();
            }
            else
            {
                saveAvatarData = avatarWrapper.GetData<PetData>().Clone();
            }

        }

        internal void ChangeShape(int resType, int shapeType)
        {
            avatarWrapper.ChangeShape(resType, shapeType);
            if (avatarWrapper is CharacterWrap characterWrap)
            {
                saveAvatarData = characterWrap.ChaData.Clone();
            }
            else if (avatarWrapper is PetWrap petWrap)
            {
                saveAvatarData = petWrap.Data.Clone();
            }
        }

        internal void ChangeColor(int resType, Color color)
        {
            avatarWrapper.ChangeColor(resType, "#" + ColorUtility.ToHtmlStringRGB(color));
            RefreshSaveData();
        }

        internal void ChangeSize(int resType, Vec3 size)
        {
            avatarWrapper.Scale(resType, size);
            RefreshSaveData();
        }

        internal void ChangeMove(int resType, Vec3 pos)
        {
            avatarWrapper.Move(resType, pos);
            RefreshSaveData();
        }

        internal void ChangeRotate(int resType, Vec3 rot)
        {
            avatarWrapper.Rotate(resType, rot);
            RefreshSaveData();
        }

        internal void ChangeHVSize(int resType, Vec3 size)
        {
            avatarWrapper.HVScale(resType, size);
            RefreshSaveData();
        }

        internal void ChangeLeftOrRight(int resType, int leftOrRight)
        {
            avatarWrapper.SetLeftOrRight(resType, leftOrRight);
            RefreshSaveData();
        }

        private void RefreshSaveData()
        {
            if (avatarWrapper is CharacterWrap characterWrap)
            {
                saveAvatarData = characterWrap.ChaData.Clone();
            }
            else if (avatarWrapper is PetWrap petWrap)
            {
                saveAvatarData = petWrap.Data.Clone();
            }
        }

        public void OpenCustomColor(Color initColor, Color defaultColor, Action<Color> OnColorChange, GameObject target)
        {
            adjustView.gameObject.SetActive(true);
            adjustView.SetAdjustColors(new RoleColorAdjust()
            {
                Getter = () => initColor,
                Default = () => defaultColor,
                Setter = (color) => initColor = color,
                Apply = () => { if (target) OnColorChange?.Invoke(initColor); }
            });
        }

        public void ShowTip(string str)
        {
            tipText.gameObject.SetActive(true);
            tipText.SetLocalText(str);
        }

        internal void OnMusicScorePlaying(List<SyllablePlayData> data)
        {
            playerHold.PlayMusicSyllable(data);
        }

        internal string previewMusicInstrumentId = "";
        internal bool isOk;
        internal Action previewMusicScoreAction;
        internal Dictionary<string, DetailRsp> detailCache = new Dictionary<string, DetailRsp>();
        internal void PreviewMusicScore(bool isPgc, string id, Action action = null)
        {
            previewMusicScoreAction = action;
            if (previewMusicInstrumentId == id)
            {
                if (isOk)
                {
                    action?.Invoke();
                }
                return;
            }
            previewMusicInstrumentId = id;
            if (isPgc)
            {
                ShowSwitch(switchMI);
                avatarWrapper.ChangePart(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), id, () =>
                {
                    if (previewMusicInstrumentId == id) playerHold.PreviewPGCInstrument(id);
                    switchMI.SetTarget(id, "");
                    isOk = true;
                    previewMusicScoreAction?.Invoke();
                });
                return;
            }

            GetMIDetailInfo(id, (rspData) =>
            {
                ShowSwitch(switchMI);
                avatarWrapper.ChangeUGCPart(rspData.skinInfo, () =>
                {
                    if (previewMusicInstrumentId == id) playerHold.PreviewUGCInstrument(rspData.skinActionInfo.instrumentInfo);
                    switchMI.SetTarget(id, rspData.skinInfo.cover);
                    isOk = true;
                    previewMusicScoreAction?.Invoke();
                });

            },
            () =>
            {
                // ugc乐器拿不到，回到默认的话筒
                AccountDataManager.Inst.TryListenMIData = new();
                if (!AccountDataManager.Inst.TryListenMIData.isPgc)
                {
                    ShowSwitch(switchMI.gameObject);
                    switchMI.gameObject.SetActive(true);
                    return;
                }
                previewMusicInstrumentId = "";
                PreviewMusicScore(action);
            });
        }

        internal void PreviewMusicScore(Action action = null)
        {
            var isPgc = AccountDataManager.Inst.TryListenMIData.isPgc;
            var id = AccountDataManager.Inst.TryListenMIData.id;
            PreviewMusicScore(isPgc, id, action);
        }

        internal void ShowSwitch(UnityEngine.Object go)
        {
            switchMI.gameObject.SetActive(go == switchMI);
            switchMS.gameObject.SetActive(go == switchMS);
        }

        internal void GetMIDetailInfo(string id, Action<DetailRsp> suc, Action fail)
        {
            if (detailCache.ContainsKey(id))
            {
                suc?.Invoke(detailCache[id]);
                return;
            }
            JObject req = new JObject()
            {
                ["idList"] = id,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                if (this == null || previewMusicInstrumentId != id || playerHold == null) return;
                BatchDetailRsp rspData = JsonConvert.DeserializeObject<BatchDetailRsp>(content);
                if (rspData.skinList == null || rspData.skinList.Count == 0 || rspData.skinList[0].skinActionInfo == null) return;
                detailCache[id] = rspData.skinList[0];
                suc?.Invoke(detailCache[id]);
            },
            (msg) =>
            {
                fail?.Invoke();
            });
        }

        internal void CancelPreviewMusicScore()
        {
            isOk = false;
            previewMusicInstrumentId = "";
            if (isCharacterFittingRoom)
            {
                playerHold.StopPreviewInstrument();
                playMusicScoreBev.StopPLay();
            }

            switchMI.gameObject.SetActive(false);
        }

        internal void OnTryListenMIChange()
        {
            if (!switchMI.gameObject.activeSelf) return;
            PreviewMusicScore();
        }

        internal string previewMusicScoreId;
        internal void PreviewMusicalInstrument(bool isPgc, string miId)
        {
            PreviewMusicScore(isPgc, miId, () =>
            {
                PreviewMusicalInstrument();
            });
        }

        internal void PreviewMusicalInstrument()
        {
            var id = AccountDataManager.Inst.TryListenMSData.id;
            previewMusicScoreId = id;
            if (string.IsNullOrEmpty(id))
            {
                ShowSwitch(switchMS);
                switchMS.SetTarget(id, "");
                playMusicScoreBev.StopPLay();
                playMusicScoreBev.ChangePlayType(PlayMusicScoreBev.PlayType.Once);
                playMusicScoreBev.StartPLay(playMusicScoreBev.DefaultMS, OnMusicScorePlaying);
                return;
            }

            ShowSwitch(null);
            AssetsDataManager.GetMusicScoreInfo(id, (isSuccess, RecommendItemData) =>
            {
                if (previewMusicScoreId != id) return;
                if (isSuccess)
                {
                    playMusicScoreBev.StopPLay();
                    playMusicScoreBev.ChangePlayType(PlayMusicScoreBev.PlayType.Once);
                    playMusicScoreBev.StartPLay(RecommendItemData.musicScoreInfo, OnMusicScorePlaying);
                    switchMS.gameObject.SetActive(true);
                    ShowSwitch(switchMS);
                    switchMS.SetTarget(id, RecommendItemData.musicScoreInfo.cover);
                }
                else
                {
                    // ugc乐谱拿不到，回到默认的
                    AccountDataManager.Inst.TryListenMSData = new();
                    previewMusicScoreId = "";
                    PreviewMusicalInstrument(true, "");
                }
            });
        }

        internal void CancelPreviewMusicalInstrument()
        {
            previewMusicScoreId = "";
            playMusicScoreBev.StopPLay();
            switchMS.gameObject.SetActive(false);
        }

        internal void OnTryListenMSChange()
        {
            if (!switchMS.gameObject.activeSelf) return;
            PreviewMusicalInstrument();
        }

        #endregion

        #region 保存OC
        private Camera photoCamera;
        public void SaveOc()
        {
            saveOcButton.ShowLoading();
            saveOcButton.SetClickAble(false);
            if (!VIPSaveInfo(0))
            {
                ShowOcButton();
                return;
            }
            OnClickSave();
        }

        private void OnClickSave()
        {
            if (!ocList.CanSave())
            {
                ShowOcButton();
                UIManager.Inst.OpenPanel<BuyOcPanel>(PanelId.BuyOcPanel, isCharacterFittingRoom);
                return;
            }

            PlayerAnimationCtrl playAnim = null;
            CharacterPartData partData = null;
            if (avatarWrapper is CharacterWrap characterWrap)
            {
                playAnim = characterWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
                playAnim.Init(characterWrap);
                playAnim.Play("idle");
                partData = avatarWrapper.GetPartData(UniqueType.GetAvatar(AvatarSubType.Eyes));
            }
            else
            {
                partData = avatarWrapper.GetPartData(UniqueType.GetPGCPetAvatar(AvatarSubType.Eyes));
            }

            void Save()
            {
                photoCamera = Loader.Load<GameObject>("Assets/Arts/Prefabs/CharacterUICamera.prefab")
                    .Instantiate(avatarWrapper.Avatar.transform).GetComponent<Camera>();
                //如果是体型等于Type6 则需要调整位置
                var camera_pos = new Vector3(0, isCharacterFittingRoom ? 0.5f : 0.35f, 1);
                float scaleX = avatarWrapper.Avatar.transform.localScale.x;

                if (isCharacterFittingRoom && saveAvatarData is CharacterData characterData)
                {
                    switch ((CustomBodyTypeController.BodyType)characterData.bodyType)
                    {
                        case CustomBodyTypeController.BodyType.Type2:
                            camera_pos.y = 0.6f;
                            scaleX = 1f;
                            break;
                        case CustomBodyTypeController.BodyType.Type3:
                            camera_pos.y = 0.45f;
                            scaleX = 1f;
                            break;
                        case CustomBodyTypeController.BodyType.Type4:
                            camera_pos.y = 0.35f;
                            scaleX = 1f;
                            break;
                        case CustomBodyTypeController.BodyType.Type6:
                            camera_pos.y = 0.75f;
                            scaleX = 1f;
                            break;
                    }
                }
                photoCamera.transform.localPosition = camera_pos;
                photoCamera.transform.localEulerAngles = new Vector3(0, 180, 0);
                photoCamera.orthographicSize = photoCamera.orthographicSize * ResolutionAutoFit.CameraScale *
                                               scaleX;

                StartCoroutine(TakeMatchPhoto());
            }

            if (partData == null || partData.IsNull())
            {
                Save();
                return;
            }

            AvatarCommonData bData = null;
            if (isCharacterFittingRoom)
            {
                bData = DataTables.GetAvatarCommonData(partData.Id);
            }
            else
            {
                bData = DataTables.GetPetAvatarCommonData(partData.Id);
            }
            Loader.LoadAsyncOrSync<AnimationClip>(bData.aniPath + ".anim", (isSuc, wrapper) =>
            {
                if (isSuc && wrapper != null)
                {
                    if (playAnim != null)
                    {
                        var clip = wrapper.RetainAsset(playAnim.gameObject);
                        AnimationClip clipA = AnimationClip.Instantiate(clip);
                        playAnim.LoadPlay(clipA, 1);
                    }
                    Save();
                }
                else
                {
                    petAnimationCtrl.Play("idle");
                    Save();
                }
            });
        }

        private bool VIPSaveInfo(int index)
        {
            if (!VipDataManager.Inst.isVip)
            {
                var bodyCtrl = avatarWrapper.Avatar.GetComponent<CustomBodyTypeController>();
                if (bodyCtrl != null)
                {
                    var bodyType = bodyCtrl.GetCurrentBodyType();
                    foreach (var shapeData in ShapeDataMgr.Inst.shapeDataList)
                    {
                        if (shapeData.Id == (int)bodyType)
                        {
                            if (shapeData.SaleType == ShapeSaleType.Free)
                            {
                                break;
                            }
                            UIManager.Inst.OpenPanel(PanelId.ShapeVIPHintPanel, WindowId.FittingRoomWindow, index);//打开VIP弹窗
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private IEnumerator TakeMatchPhoto()
        {
            yield return new WaitForEndOfFrame();

            try
            {
                Rect rect = GetScreenShotRect();

                byte[] imgBytes = ScreenShotUtils.TakeShotGamma(photoCamera, rect);

                Destroy(photoCamera.gameObject);

                string fileName = LocalDataUtils.Inst.SaveImgRes(imgBytes);

                var uri = $"FittingRoom/characterInfo/{AccountDataManager.Inst.Uid}/{Path.GetFileName(fileName)}";
                CosXmlUploadManager.UploadFile(uri, fileName, (url, err) =>
                {
                    UploadImgCallback(url, err, fileName);
                });
            }
            catch (Exception e)
            {
                LoggerUtils.LogError(e.Message);
            }
        }

        private void UploadImgCallback(string url, string err, string fileName)
        {
            File.Delete(fileName);

            if (!string.IsNullOrEmpty(err))
            {
                LoggerUtils.LogError($"Upload Character Image Fail!!! Err : {err}");
                return;
            }
            else
            {
                LoggerUtils.Log("Upload Img Success url: " + url);

                SaveDressData(url);
            }
        }

        private void SaveDressData(string fileUrl)
        {
            string jsonContent = null;

            if (avatarWrapper is CharacterWrap characterWrap)
            {
                jsonContent = CharacterData.SerializeObject(characterWrap.ChaData);
            }
            else if (avatarWrapper is PetWrap petWrap)
            {
                jsonContent = PetData.SerializeObject(petWrap.Data);
            }

            ocList.SetOcInfo(fileUrl, jsonContent, resultHandler: isSuccess =>
            {
                LoggerUtils.Log($"[Oc] save result: {isSuccess}");
                if (isSuccess)
                {
                    TipPanel.ShowToast("成功保存到设子卡位");
                }
                ShowOcButton();
                EventCenterDataManager.Inst.GetTaskInfo(TASK_ID.NewbieCheckIn);
            }, refreshList: true); // 设置为true，恢复保存设子时刷新列表的功能
        }

        private Rect GetScreenShotRect()
        {
            Rect rect = new Rect(0, 0, photoCamera.pixelWidth, photoCamera.pixelHeight);
            return rect;
        }
        
        private VehicleInfo selectedVehicleInfo = null;

        public void SetSelectedVehicleInfo(VehicleInfo vehicleInfo, bool isShowSave = true)
        {

            if(selectedVehicleInfo != null){
                TakeOffVehicle();  
            }

            selectedVehicleInfo = vehicleInfo;
            if(vehicleInfo != null)
            {
                GameVehicleManager.Inst.AddPropData(vehicleInfo);
            }

            //商城的试穿不能直接显示展示在大厅按钮，得检查
            if (isShowSave){
                ShowVehicleButton();
            }

            if(vehicleInfo != null){    
                avatarWrapper.ChangeUGCVehicle(AccountDataManager.Inst.UserInfo.uid, vehicleInfo);
                avatarCameraController.SetVehicleView(UniqueType.VehicleSubType(vehicleInfo.vehicleType));
            }

        }

        public void TakeOffVehicle()
        {
            //因为载具装作是通过注入到角色数据中的，所以刷新角色数据就可以把载具脱下
            if(avatarWrapper is CharacterWrap characterWrap){
                characterWrap.GetOutSelfVehicle(AccountDataManager.Inst.UserInfo.uid);
            }
        }

        public void SaveVehicle()
        {
            saveVehicleButton.ShowLoading();
            saveVehicleButton.SetClickAble(false);
            OnClickSaveVehicle();
        }

        private void OnClickSaveVehicle()
        {
            if (selectedVehicleInfo == null)
            {
                ShowVehicleButton();
                return;
            }

            selectedVehicleInfo.isHidden = 0;

            AccountDataManager.Inst.SyncVehicleData(selectedVehicleInfo, isSuccess => {
                if (isSuccess)
                {
                    TipPanel.ShowToast("已在大厅展示该载具，请前往大厅查看");
                }
                ShowVehicleButton();
                
                AccountDataManager.Inst.SyncVehicleData(false);
            });
        }
        
        #endregion

        public void TryPlayMusic()
        {
            if (avatarWrapper is CharacterWrap characterWrap)
            {
                UIManager.Inst.SwapPanel(PanelId.TryMusicalInstrumentPanel, characterWrap.ChaData.Clone());
            }

        }

        public void ChangeOtherOc()
        {
            UIManager.Inst.OpenPanelTakeAni<OcChangePanel>(PanelId.OcChangePanel, OcChangeScene.DoubleEmote).OnCloseAction = ChangeOtherOc;
        }

        private void ChangeOtherOc(BaseAvatarData baseAvatarData)
        {
            if (baseAvatarData != null)
            {
                try
                {
                    var data = (CharacterData)baseAvatarData;
                    otherCharacterWrap.SetCharacterData(data);
                    PlayerPrefs.SetString(GameConsts.EmoteOtherPlayerOcKey + AccountDataManager.Inst.Uid, CharacterData.SerializeObject(data));
                }
                catch { }
            }
        }

        public void ShowOcButton()
        {
            if (!HaveSelectData())
            {
                saveOcButton.gameObject.SetActive(true);
                saveOcButton.SetClickAble(true);
                saveOcButton.HideLoading();
            }
        }

        public void ShowVehicleButton()
        {
            if (!HaveSelectData())
            {
                saveVehicleButton.HideLoading();
                if(selectedVehicleInfo != null){
                    bool isDisplayed = AccountDataManager.Inst.VehicleInfo != null
                        && selectedVehicleInfo.id == AccountDataManager.Inst.VehicleInfo.id
                        && AccountDataManager.Inst.VehicleInfo.isHidden == 0;
                    // 演员编辑器模式下已展示的载具按钮隐藏，避免遮挡截图/衣柜按钮
                    if ((_goScreenshotBtnEnabled || _saveWardrobeBtnEnabled) && isDisplayed)
                    {
                        saveVehicleButton.gameObject.SetActive(false);
                        return;
                    }
                    if(isDisplayed){
                        saveVehicleButton.SetLocalText("已展示");
                        saveVehicleButton.gameObject.SetActive(true);
                        saveVehicleButton.SetClickAble(false);
                        saveOcButton.gameObject.SetActive(false);
                    }else{
                        saveVehicleButton.SetLocalText("展示在大厅");
                        saveVehicleButton.gameObject.SetActive(true);
                        saveVehicleButton.SetClickAble(true);
                        saveOcButton.gameObject.SetActive(false);
                    }
                }else{
                    saveVehicleButton.gameObject.SetActive(false);
                }
            }
        }

        public void HideVihicleButton()
        {
            saveVehicleButton?.gameObject.SetActive(false);
        }


        public void ShowPetSizeAdjustView(CharacterPartData partData)
        {
            Vector3 minSize = 0.5f * Vector3.one;
            Vector3 maxSize = 2f * Vector3.one;
            var data = new RoleDataAdjust()
            {
                AdjustType = AdjustViewItemType.Size,
                Getter = () => partData.Sca ?? Vector3.one,
                Setter = (v) => partData.Sca = v,
                Limit = () => new() { minSize, maxSize },
                Default = () => Vector3.one,
                Apply = () => ChangePetSize(partData.Type, partData.Sca)
            };
            petSizeAdjustView.SetItemData(data);
        }

        private void ChangePetSize(int resType, Vec3 sca)
        {
            avatarWrapper.Scale(resType, sca);
            // avatarWrapper.Avatar.transform.localScale = sca;
        }

        protected override void OnDestroy()
        {
            // 防止 Panel 销毁后仍接收消息回调，导致访问已销毁的 UI 组件
            Message.MessageHelper.RemoveListener<CharacterViewExpressionItem>(Message.MessageName.ActorCharacterOpenFittingRoomPanel, ActorCharacterOpenFittingRoomPanel);
            Message.MessageHelper.RemoveListener<WardrobeViewWardrobelistItem>(Message.MessageName.ActorWardrobeViewOpenFittingRoomPanel, ActorWardrobeViewOpenFittingRoomPanel);
            MessageHelper.Broadcast(MessageName.RefreshGroupConsumeApplyData);
            var panel2 = UIManager.Inst.FindPanel<OcCompetitionPanel>(PanelId.OcCompetitionPanel);
            if (panel2 != null)
            {
                panel2.MyGroup.AvatarGroup.ShowAvatar(true);
            }
            // 清除背包Ugc数据
            AssetsDataManager.ClearBagUgcData();
            //记录首次打开商城
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            // TODO暂时调用，因为3D部件的mesh问题，需要释放一下
            Resources.UnloadUnusedAssets();
            MessageHelper.RemoveListener<TaskListRsp>(MessageName.NewComerCommunityCoin, NewComerCommunityCoin);
            MessageHelper.RemoveListener<GoodsData>(MessageName.ShapeItemEvent, OnShapeItemEvent);
            MessageHelper.RemoveListener<GoodsData>(Message.MessageName.ShapeEmoteItemEvent, OnShapeEmoteItemEvent);
            BusinessLiveManager.Inst.RemoveConfigUpdateListener(OnBusinessConfigUpdate);
            curTab = MainTabs.Tab.Test;
#if UNITY_EDITOR
            // pc上主动GC
            GC.Collect();
#endif
        }

        public override void OnWindowPop()
        {

        }

        #region 细节调整
        public RoleDataAdjust GetRoleDataAdjust(AdjustViewItemType type, Es.AvatarCommonData configData, int classType)
        {
            CharacterPartData partData = saveAvatarData.GetPartData(classType);
            classType = partData.Type;
            var avatarSubType = (AvatarSubType)configData.SubType;
            AdjustAxis axis;
            switch (type)
            {
                case AdjustViewItemType.Size:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.None
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.Size,
                        Getter = () => partData.Sca != null ? partData.Sca : configData.sDef,
                        Setter = (v) => partData.Sca = v,
                        Axis = () => axis,
                        Limit = () => configData.scaLimit,
                        Default = () => configData.sDef,
                        Apply = () => ChangeSize(classType, partData.Sca)
                    };
                case AdjustViewItemType.UpDown:
                    axis = avatarSubType switch
                    {
                        AvatarSubType.Hair => AdjustAxis.Y,
                        AvatarSubType.Earring => AdjustAxis.Y,
                        AvatarSubType.Hand => AdjustAxis.Z,
                        AvatarSubType.FacePaint => AdjustAxis.None,
                        _ => AdjustAxis.X
                    };

                    if (!isCharacterFittingRoom && avatarSubType == AvatarSubType.Hair)
                        axis = AdjustAxis.X;

                    return new()
                    {
                        AdjustType = AdjustViewItemType.UpDown,
                        Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
                        Setter = (v) => partData.Pos = v,
                        Axis = () => axis,
                        Limit = () => configData.vLimit,
                        Default = () => configData.pDef,
                        Apply = () => ChangeMove(classType, partData.Pos)
                    };
                case AdjustViewItemType.LeftRight:
                    axis = avatarSubType switch
                    {
                        AvatarSubType.Hair => AdjustAxis.X,
                        AvatarSubType.Hand => AdjustAxis.X,
                        _ => AdjustAxis.Z
                    };

                    if (!isCharacterFittingRoom && avatarSubType == AvatarSubType.Hair)
                        axis = AdjustAxis.Z;

                    return new()
                    {
                        AdjustType = AdjustViewItemType.LeftRight,
                        Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
                        Setter = (v) => partData.Pos = v,
                        Axis = () => axis,
                        Limit = () => configData.hLimit,
                        Default = () => configData.pDef,
                        Apply = () => ChangeMove(classType, partData.Pos)
                    };
                case AdjustViewItemType.FrontBack:
                    axis = avatarSubType switch
                    {
                        AvatarSubType.Hair => AdjustAxis.Z,
                        AvatarSubType.Earring => AdjustAxis.Z,
                        _ => AdjustAxis.Y
                    };

                    var fbAdjustData = new RoleDataAdjust()
                    {
                        AdjustType = AdjustViewItemType.FrontBack,
                        Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
                        Setter = (v) => partData.Pos = v,
                        Axis = () => axis,
                        Limit = () => avatarSubType == AvatarSubType.Hair ? configData.hLimit : configData.fLimit,
                        Default = () => configData.pDef,
                        Apply = () => ChangeMove(classType, partData.Pos)
                    };

                    if (!isCharacterFittingRoom && avatarSubType == AvatarSubType.Hair)
                    {
                        fbAdjustData.Axis = () => AdjustAxis.Y;
                        fbAdjustData.Limit = () => configData.fLimit;
                    }

                    return fbAdjustData;
                case AdjustViewItemType.Spacing:
                    axis = avatarSubType switch
                    {
                        AvatarSubType.Earring => AdjustAxis.X,
                        _ => AdjustAxis.Z
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.Spacing,
                        Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
                        Setter = (v) => partData.Pos = v,
                        Axis = () => axis,
                        Limit = () => configData.hLimit,
                        Default = () => configData.pDef,
                        Apply = () => ChangeMove(classType, partData.Pos)
                    };
                case AdjustViewItemType.Vertical:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.None
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.Vertical,
                        Getter = () => partData.Pos != null ? partData.Pos : configData.pDef,
                        Setter = (v) => partData.Pos = v,
                        Axis = () => axis,
                        Limit = () => configData.vLimit,
                        Default = () => configData.pDef,
                        Apply = () => ChangeMove(classType, partData.Pos)
                    };
                case AdjustViewItemType.HorizontalStretch:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.X
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.HorizontalStretch,
                        Getter = () => partData.CSca != null ? partData.CSca : configData.vhSDef,
                        Setter = (v) => partData.CSca = v,
                        Axis = () => axis,
                        Limit = () => configData.hScaLimit,
                        Default = () => configData.vhSDef,
                        Apply = () => ChangeHVSize(classType, partData.CSca)
                    };
                case AdjustViewItemType.VerticalStretch:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.Z
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.VerticalStretch,
                        Getter = () => partData.CSca != null ? partData.CSca : configData.vhSDef,
                        Setter = (v) => partData.CSca = v,
                        Axis = () => axis,
                        Limit = () => configData.vScaLimit,
                        Default = () => configData.vhSDef,
                        Apply = () => ChangeHVSize(classType, partData.CSca)
                    };
                case AdjustViewItemType.Rotation:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.None
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.Rotation,
                        Getter = () => partData.Rot != null ? partData.Rot : configData.rDef,
                        Setter = (v) => partData.Rot = v,
                        Axis = () => axis,
                        Limit = () => configData.rotateLimit,
                        Default = () => configData.rDef,
                        Apply = () => ChangeRotate(classType, partData.Rot)
                    };
                case AdjustViewItemType.XRotation:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.X
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.XRotation,
                        Getter = () => partData.Rot != null ? partData.Rot : configData.rDef,
                        Setter = (v) => partData.Rot = v,
                        Axis = () => axis,
                        Limit = () => configData.xrotLimit,
                        Default = () => configData.rDef,
                        Apply = () => ChangeRotate(classType, partData.Rot)
                    };
                case AdjustViewItemType.YRotation:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.Z
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.YRotation,
                        Getter = () => partData.Rot != null ? partData.Rot : configData.rDef,
                        Setter = (v) => partData.Rot = v,
                        Axis = () => axis,
                        Limit = () => avatarSubType == AvatarSubType.Hand || avatarSubType == AvatarSubType.Visor ? configData.yrotLimit : configData.zrotLimit,
                        Default = () => configData.rDef,
                        Apply = () => ChangeRotate(classType, partData.Rot)
                    };
                case AdjustViewItemType.ZRotation:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.Y
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.ZRotation,
                        Getter = () => partData.Rot != null ? partData.Rot : configData.rDef,
                        Setter = (v) => partData.Rot = v,
                        Axis = () => axis,
                        Limit = () => avatarSubType == AvatarSubType.Hand || avatarSubType == AvatarSubType.Visor ? configData.zrotLimit : configData.yrotLimit,
                        Default = () => configData.rDef,
                        Apply = () => ChangeRotate(classType, partData.Rot)
                    };
                case AdjustViewItemType.SwitchHand:
                    axis = avatarSubType switch
                    {
                        _ => AdjustAxis.None
                    };
                    return new()
                    {
                        AdjustType = AdjustViewItemType.SwitchHand,
                        Getter = () => new Vector3(partData.LRType, 0, 0),
                        Setter = (v) => partData.LRType = (int)v.x,
                        Axis = () => axis,
                        Default = () => new Vector3(configData.leftRightType, 0, 0),
                        Apply = () => ChangeLeftOrRight(classType, partData.LRType)
                    };
            }

            return new RoleDataAdjust();
        }



        public List<RoleDataAdjust> AdjustTypeAdjustItems(AvatarCommonData configData, int classType)
        {
            if (configData == null) return null;
            switch (configData.adjustType)
            {
                case (int)AdjustType.Skin:
                    var list = new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.LeftRight, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.XRotation, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.YRotation, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.ZRotation, configData, classType)
                    };
                    if ((AvatarSubType)configData.SubType == AvatarSubType.Hand && (configData.leftRightType == 1 || configData.leftRightType == 2))
                    {
                        list.Insert(0, GetRoleDataAdjust(AdjustViewItemType.SwitchHand, configData, classType));
                    }
                    return list;
                case (int)AdjustType.EyeBrow:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Spacing, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
                    };
                case (int)AdjustType.Nose:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Vertical, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.HorizontalStretch, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.VerticalStretch, configData, classType)
                    };
                case (int)AdjustType.Mouth:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.LeftRight, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
                    };
                case (int)AdjustType.Blush:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Spacing, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType)
                    };
                case (int)AdjustType.FacePaint:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType)
                    };
                case (int)AdjustType.EarRing:
                    return new List<RoleDataAdjust>()
                    {
                        GetRoleDataAdjust(AdjustViewItemType.Size, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Spacing, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.UpDown, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.FrontBack, configData, classType),
                        GetRoleDataAdjust(AdjustViewItemType.Rotation, configData, classType)
                    };
                default:
                    return null;
            }
        }
        #endregion
    }

    public class BaseScene
    {
        // 不属于avatar的分类枚举定义

        internal FittingRoomPanel UI;

        protected List<MainTabs.Tab> mainTabs = new List<MainTabs.Tab>();

        protected Dictionary<MainTabs.Tab, List<ClassData>> classListDict = new Dictionary<MainTabs.Tab, List<ClassData>>();

        protected List<ClassData> classDatas;

        protected ClassData classSelected;

        protected GoodsDataClassifyList assetsDatas = new();

        protected bool Enable;

        internal BaseScene(FittingRoomPanel ui)
        {
            UI = ui;
            InitParams();
        }

        public virtual void InitParams()
        {

        }

        public virtual void Enter()
        {
            UI.CloseAllSubUI();
            UI.assetsList.ResetColor();
            UI.assetsList.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            UI.assetsList.PullToRefreshBehaviour.HideGizmo();
            Enable = true;
        }

        public virtual void Exit()
        {
            Enable = false;
        }

        public virtual void OnDataRefresh()
        {

        }

        public virtual void Destory()
        {

        }

        public virtual GoodsData CreateNewModel(int index)
        {
            return default;
        }

        public virtual void Design()
        {
            if (UI != null && UI.HaveSelectData())
            {
                TipPanel.ShowToast("选择中无法进行创作哦");
                return;
            }

            var resourcesType = UniqueType.ResourceType(classSelected.Id);

            if (resourcesType == ResourceType.Avatar || resourcesType == ResourceType.UgcAvatar)
            {
                var avatarSubType = UniqueType.AvatarSubType(classSelected.Id);
                if (avatarSubType == AvatarSubType.MusicalInstrument)
                {
                    if (!GameController.IsInHallScene())
                    {
                        TipPanel.ShowToast("游玩过程中无法进行创作哦");
                        return;
                    }
                    UI.ResetLastTryOn(ResourceType.ErrResourceType);
                    UIManager.Inst.OpenPanel(PanelId.MusicalInstrumentStudioPanel);
                }
                else
                {
                    UI.ResetLastTryOn(ResourceType.ErrResourceType);
                    UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel, CharacterStyle.Avatar);
                }
            }
            else if (resourcesType == ResourceType.PGCPetAvatar || resourcesType == ResourceType.UGCPetAvatar)
            {
                UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel, CharacterStyle.Pet);
            }
            else if (resourcesType == ResourceType.MusicScore)
            {
                if (!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("游玩过程中无法进行创作哦");
                    return;
                }
                UI.ResetLastTryOn(ResourceType.ErrResourceType);
                UIManager.Inst.OpenPanel(PanelId.MusicScoreStudioPanel);
            }
            else if (resourcesType == ResourceType.UgcPose)
            {
                if (!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("游玩过程中无法进行创作哦");
                    return;
                }
                UI.ResetLastTryOn(ResourceType.ErrResourceType);
                UIManager.Inst.OpenPanel(PanelId.AnimationStudioMainPanel, AnimationStudioType.Pose);
            }
            else if (resourcesType == ResourceType.UgcEmote)
            {
                if (!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("游玩过程中无法进行创作哦");
                    return;
                }
                UI.ResetLastTryOn(ResourceType.ErrResourceType);
                UIManager.Inst.OpenPanel(PanelId.AnimationStudioMainPanel, AnimationStudioType.Animation);
            }else if(resourcesType == ResourceType.UgcVehicle){
                if (!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("游玩过程中无法进行创作哦");
                    return;
                }
                UI.ResetLastTryOn(ResourceType.ErrResourceType);
                UIManager.Inst.OpenPanel(PanelId.VehicleStudioCategoryPanel);
            }
            else if(resourcesType == ResourceType.AvatarCard)
            {
                if (!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("游玩过程中无法进行创作哦");
                    return;
                }
                UI.ResetLastTryOn(ResourceType.ErrResourceType);
                UIManager.Inst.OpenPanel(PanelId.TheatreActorEditorMainPanel);
            }
            else if (resourcesType == ResourceType.Theatre)
            {
                if (!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("游玩过程中无法进行创作哦");
                    return;
                }
                UI.ResetLastTryOn(ResourceType.ErrResourceType);
                UIManager.Inst.OpenPanel(PanelId.TheatreEditorStudioPanel);
            }
            else
            {
                UI.ResetLastTryOn(ResourceType.ErrResourceType);

                UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel, UI.isCharacterFittingRoom ? CharacterStyle.Avatar : CharacterStyle.Pet);
            }
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
                        || data.OriginalPrice.Value > 200) return false;
                    UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
                    {
                        type = CurrencyType.CommunityAnimationTicket,
                        pgcId = data.Id,
                        ClaimCallBack = () =>
                        {
                            var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                            panel?.InitData(data, "购买成功！");
                        }
                    });
                    return true;
                case 24: //乐器卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityInstrumentTicket) <= 0
                        || data.OriginalPrice.Value > 200) return false;
                    UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
                    {
                        type = CurrencyType.CommunityInstrumentTicket,
                        pgcId = data.Id,
                        ClaimCallBack = () =>
                        {
                            var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                            panel?.InitData(data, "购买成功！");
                        }
                    });
                    return true;
                case 1011: //载具卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityVehicleTicket) <= 0
                        || data.OriginalPrice.Value > 200) return false;
                    UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
                    {
                        type = CurrencyType.CommunityVehicleTicket,
                        pgcId = data.Id,
                        ClaimCallBack = () =>
                        {
                            var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                            panel?.InitData(data, "购买成功！");
                        }
                    });
                    return true;
                case 1015: //载具卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityTheaterTicket) <= 0
                        || data.OriginalPrice.Value > 350) return false;
                    UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
                    {
                        type = CurrencyType.CommunityTheaterTicket,
                        pgcId = data.Id,
                        ClaimCallBack = () =>
                        {
                            var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                            panel?.InitData(data, "购买成功！");
                        }
                    });
                    return true;
                default: //其余都算皮肤卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunitySkinTicket) <= 0
                        || data.OriginalPrice.Value > 60) return false;
                    UIManager.Inst.OpenPanel(PanelId.ConsumptionTicketPanel, new ConsumptionTicketConfig
                    {
                        type = CurrencyType.CommunitySkinTicket,
                        pgcId = data.Id,
                        ClaimCallBack = () =>
                        {
                            var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                            panel?.InitData(data, "购买成功！");
                        }
                    });
                    return true;
            }
        }

        /// <summary>
        /// 获取促销数据（这个方法写了多次。没有UI通用数据管理类）
        /// </summary>
        /// <returns></returns>
        private ShapeThemeProp GetShapeThemeData(int pgcId)
        {
            if (AssetsDataManager.ShapeThemePropList == null || AssetsDataManager.ShapeThemePropList.Count == 0)
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

        public virtual void Buy(GoodsData data)
        {
            if (CheckedTickUser(data))
            {
                return;
            }
            var tmpGoodsData = new GoodsData()
            {
                ButtonType = data.ButtonType,
                Id = data.Id,
                GoodsType = data.GoodsType,
                subType = data.subType,
                Price = new CurrencyData()
                {
                    CurrencyType = data.Price.CurrencyType,
                    Value = data.Price.Value,
                }
            };

            int themeId = 0;
            ShapeThemeProp themeData = null;
            if (int.TryParse(data.Id, out themeId))
            {
                themeData = GetShapeThemeData(themeId);
            }

            // 当前拥有打折卡且当前商品支持打折卡
            if (DiscountCardUtils.IsOwnedDiscountCard() && DiscountCardUtils.IsSupportDiscountCard(data))
            {
                tmpGoodsData.Price.Value = Mathf.RoundToInt(tmpGoodsData.Price.Value * DiscountCardUtils.GetDiscount());
            }
            else if (themeData != null)
            {
                tmpGoodsData.Price.Value = data.OriginalPrice.Value - (data.OriginalPrice.Value * (themeData.discount * 0.01f));
            }

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
                    if (themeData != null)
                    {
                        MessageHelper.Broadcast(MessageName.BuyShapeRefresh);
                    }
                }
                data.IsPayingRequest = false;
            }, themeData == null ? 0 : themeData.themeId);
        }

        public virtual void ShoppingCart(GoodsData data)
        {
            
        }
        public virtual void UseSkinTicket(GoodsData data)
        {
            var panel = UIManager.Inst.OpenPanel<UseTicketConfirmPanel>(PanelId.UseTicketConfirmPanel, CurrencyType.Ticket);
            panel.SetConfimAction(() =>
            {
                AssetsDataManager.BuyGoodsUseVoucher(data, (success, reason, needNum) =>
                {
                    if (!success)
                    {
                        //UIManager.Inst.OpenPanel(PanelId.TipPanel, reason);
                    }
                    else
                    {
                        var panel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                        panel?.InitData(data, "购买成功！");
                        UIManager.Inst.ClosePanel(PanelId.UseTicketConfirmPanel);
                    }
                    data.IsPayingRequest = false;
                });
            });
        }

    }

    public enum AdjustType
    {
        NotSupport = 0,
        Skin = 1, //皮肤向
        EyeBrow = 2, //眼镜眉毛
        Nose = 3, //鼻子
        Mouth = 4, //嘴巴
        Blush = 5, //腮红
        FacePaint = 6, //脸部彩绘
        EarRing = 7, //耳环
        Tail = 8, //尾巴
    }
}
