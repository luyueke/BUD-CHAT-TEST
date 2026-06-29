using System;
using System.Collections.Generic;
using AIGame.Base;
using Basic;
using Game.Avatar;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.KinematicCharacter;
using Game.Props;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Game.Vehicle.PGCVehicle.KVC;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using Newtonsoft.Json;
using Pb.Base;
using UI.Base;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.U2D;
using UnityEngine.UI;


public enum UIOperationType
{
    World = 1,
    Fix = 2
}
public class UIOperationOnWorldPanel : BasePanel<TestPanel1>
{
    public GameObject TouchBtn;
    public Button TextBtn;
    public Transform BtnTransform;
    public Transform VehicleBtnTransform;
    private RectTransform svParent;
    private RectTransform vehicleSvParent;

    //是否禁用
    private bool isInteractable = true;
    //是否可见
    private int maxBtnLength = 3;
    private bool isActive = false;
    private SpriteAtlas uiAtlas;

    private List<Button> touchBtns;
    private List<Image> touchImage;
    private List<Button> vehicleBtns;
    private List<Image> vehicleImage;
    private NodeBaseBehaviour touchBehaviour;
    private UIWorldFilterConditionAnalysis analysis = new UIWorldFilterConditionAnalysis();
    private UIWorldPlayerFilterConditionAnalysis driverAnalysis = new UIWorldPlayerFilterConditionAnalysis();
    private List<NodeBaseBehaviour> filteredBehaviour = new List<NodeBaseBehaviour>();
    private List<PlayerStateController> vehicleDrivers = new List<PlayerStateController>();
    private Camera mainCamera;
    private Camera uiCamera;

    private AIHospitalGame aiGame;
    public static bool bBeginRaycast = false;


    public override void OnCreate()
    {
        base.OnCreate();

        touchBtns = new List<Button>();
        touchImage = new List<Image>();
        vehicleBtns = new List<Button>();
        vehicleImage = new List<Image>();
        for (int i = 0; i < maxBtnLength; i++)
        {
            var btnGo = GameObject.Instantiate(TouchBtn, BtnTransform);
            var btn = btnGo.GetComponent<Button>();
            var img = btnGo.GetComponent<Image>();
            touchBtns.Add(btn);
            touchImage.Add(img);
        }
        for (int i = 0; i < maxBtnLength; i++)
        {
            var btnGo = GameObject.Instantiate(TouchBtn, VehicleBtnTransform);
            var btn = btnGo.GetComponent<Button>();
            var img = btnGo.GetComponent<Image>();
            vehicleBtns.Add(btn);
            vehicleImage.Add(img);
        }
        uiAtlas = Loader.Load<SpriteAtlas>("Assets/Loadable/UI/UIPanel/UIOperationOnWorldPanel/TouchSpriteAtlas.spriteatlas").RetainAsset();
        AvatarRaycast avtRaycast = AvatarController.Inst.SelfAvatarRaycast;
        mainCamera = GlobalCameraManager.Inst.GlobalMainCamera;
        uiCamera = GlobalCameraManager.Inst.UICamera;
        svParent = BtnTransform.parent.GetComponent<RectTransform>();
        vehicleSvParent = VehicleBtnTransform.parent.GetComponent<RectTransform>();
        avtRaycast.OnRaycast.AddListener(OnAvatarRaycast);
        GlobalNodeManager.Inst.RegisterAvatarTrigger();
        bBeginRaycast = true;
        //Debug.LogError("UIOperation OnCreate");

        // 监听其他玩家的 buddy 同步（召唤待机/唤醒动作、口令互动），驱动其他端 buddy 表现
        MessageHelper.AddListener<string, string, InteractSyncData>(MessageName.OnRecvBuddySummonSync, OnRecvBuddySummonSync);
        MessageHelper.AddListener<string, InteractSyncData>(MessageName.OnRecvBuddyInteractSync, OnRecvBuddyInteractSync);

        // 触碰地图伙伴编排器 .Inst，确保其监听已注册（并对已加载的地图伙伴兜底装配）
        _ = UI.UIPanels.AINPC.AIBuddyInMap.AIBuddyInMapUIManager.Inst;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener<string, string, InteractSyncData>(MessageName.OnRecvBuddySummonSync, OnRecvBuddySummonSync);
        MessageHelper.RemoveListener<string, InteractSyncData>(MessageName.OnRecvBuddyInteractSync, OnRecvBuddyInteractSync);
    }

    // 其他玩家召唤 buddy：在其 buddy 上播唤醒动作 + 启动待机循环
    private void OnRecvBuddySummonSync(string playerId, string usingEmoteJson, InteractSyncData awake)
    {
        var stateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(playerId);
        if (stateCtrl == null) return;
        var buddyGo = stateCtrl.PlayerKCCtrl != null ? stateCtrl.PlayerKCCtrl.gameObject : null;

        if (awake != null)
            AIBoxBuddyCallPanel.PlayActivation(stateCtrl, buddyGo, awake);

        if (!string.IsNullOrEmpty(usingEmoteJson))
        {
            PendingEmoteData usingEmote = null;
            try { usingEmote = JsonConvert.DeserializeObject<PendingEmoteData>(usingEmoteJson); }
            catch (Exception e) { LoggerUtils.LogError("解析 buddy usingEmote 失败:" + e.Message); }
            if (usingEmote != null)
                AIBoxBuddyCallPanel.StartBuddyStandbyOn(stateCtrl, usingEmote, awake != null ? 4f : 0f);
        }
    }

    // 其他玩家触发口令：在其 buddy 上播动作 + 语音
    private void OnRecvBuddyInteractSync(string playerId, InteractSyncData interact)
    {
        var stateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(playerId);
        if (stateCtrl == null) return;
        var buddyGo = stateCtrl.PlayerKCCtrl != null ? stateCtrl.PlayerKCCtrl.gameObject : null;
        AIBoxBuddyCallPanel.PlayActivation(stateCtrl, buddyGo, interact);
    }

    public void SetActive(bool interactable)
    {
        isInteractable = interactable;
        BtnTransform.gameObject.SetActive(isInteractable && isActive);
    }

    private void OnAvatarRaycast(Collider[] colliders)
    {
        if (!bBeginRaycast)
        {
            BtnTransform.gameObject.SetActive(false);
            return;
        }
        touchBtns.ForEach(x => x.gameObject.SetActive(false));
        vehicleBtns.ForEach(x => x.gameObject.SetActive(false));
        TextBtn.gameObject.SetActive(false);
        Prop(colliders);
        Interact(colliders);
        ChatToAIBuddy(colliders);
        VehicleInteract(colliders);
    }

    private bool Prop(Collider[] colliders)
    {
        filteredBehaviour.Clear();
        analysis.FilerCollider(colliders, ref filteredBehaviour);
        if (filteredBehaviour.Count == 0)
        {
            touchBehaviour = null;
            isActive = false;
            BtnTransform.gameObject.SetActive(isInteractable && isActive);
            return false;
        }
        touchBehaviour = filteredBehaviour[0];
        // 地图 buddy 与玩家牵手时会跟随玩家离开放置位，而交互图标锚定放置位会“卡”在原地不消失；牵手期间隐藏其图标
        if (touchBehaviour is AIBuddyInMapBehaviour &&
            BuddyLinkEmoteManager.Inst != null &&
            BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(AccountDataManager.Inst.Uid))
        {
            touchBehaviour = null;
            isActive = false;
            BtnTransform.gameObject.SetActive(isInteractable && isActive);
            return false;
        }
        if (touchBehaviour is AIHospital_CharacterBehaviour aIHospital_CharacterBehaviour)
        {
            aiGame = aiGame ? aiGame : AIGameController.Inst.GetCurAIGame<AIHospitalGame>();
            if (aiGame != null && aiGame.isPgcEnter && aiGame.CurrentStep <= AIGameHospitalConfig.taskTarget.Count - 1)
            {
                bool condition1 = AIGameHospitalConfig.taskTarget[aiGame.CurrentStep].targetRole == Game.Props.PropsManagers.AIGames.AIHospital.FSM.HospitalNpcRoleType.Dustman;
                bool condition2 = aIHospital_CharacterBehaviour.GetNpcRoleType() == Game.Props.PropsManagers.AIGames.AIHospital.FSM.HospitalNpcRoleType.Dustman;
                if (condition1 && condition2 && touchBehaviour.IsCanClick)
                {
                    AddS9DustManInteract(aIHospital_CharacterBehaviour);
                }
            }

            return true;
        }
        if (!touchBehaviour.IsCanClick)
        {
            return false;
        }

        var configData = touchBehaviour.entity.GetPropConfig();
        if (configData == null)
        {
            return false;
        }
        if ((UIOperationType)configData.touchType == UIOperationType.World)
        {
            isActive = true;
            string touchName = touchBehaviour.GetTouchName();
            if (string.IsNullOrEmpty(touchName))
            {
                var spr = uiAtlas.GetSprite(configData.TouchIcon);
                touchImage[0].sprite = spr;
                touchBtns[0].gameObject.SetActive(true);
                touchBtns[0].onClick.RemoveAllListeners();
                touchBtns[0].onClick.AddListener(OnTouchClick);
            }
            else
            {
                TextBtn.gameObject.SetActive(true);
                var btnText = TextBtn.GetComponentInChildren<Text>();
                btnText.text = touchName;
                btnText.SetPreferredSize();
                TextBtn.onClick.RemoveAllListeners();
                TextBtn.onClick.AddListener(OnTouchClick);
            }
            //TODO:偏移位置可以配置到道具表中
            UpdatePos(touchBehaviour.transform.position, Vector3.zero);
            if (touchBehaviour is TheatreTriggerBehaviour)
            {
                UpdatePos(touchBehaviour.transform.position, Vector3.up * 1.8f);
            }
            if (touchBehaviour is AIPark_StageBehaviour stage)
            {
                UpdatePos(touchBehaviour.transform.position, stage.v3);
            }
            if (touchBehaviour is AIBuddyInMapBehaviour aiBuddyBehaviour)
            {
                // 指令按钮：展示该地图伙伴本体口令（数据由 AIBuddyInMapUIManager 缓存，动作播在该伙伴身上）
                System.Action openCommand = () =>
                {
                    // 试玩态（GamePlayPanel 打开）不支持与地图 buddy 互动，仅提示；发布后进地图才可互动
                    if (UIManager.Inst.FindPanel<GamePlayPanel>(PanelId.GamePlayPanel) != null)
                    {
                        TipPanel.ShowToast("试玩中不支持此功能，请在发布后进入地图体验和AI伙伴互动");
                        return;
                    }
                    if (aiBuddyBehaviour.HasBuddy)
                    {
                        UIManager.Inst.OpenPanel<AIBoxBuddyCommandPanel>(PanelId.AIBoxBuddyCommandPanel, aiBuddyBehaviour);
                    }
                };
                // 指令按钮统一用口令图标（与 self-buddy 一致），不沿用道具表通用 TouchIcon；隐藏文字按钮
                TextBtn.gameObject.SetActive(false);
                var cmdSpr = uiAtlas.GetSprite("btn_buddyCommand");
                touchImage[0].sprite = cmdSpr;
                touchImage[0].SetNativeSize();
                touchBtns[0].gameObject.SetActive(true);
                touchBtns[0].onClick.RemoveAllListeners();
                touchBtns[0].onClick.AddListener(() => openCommand());

                // 互动选项按钮：双人 emote / 牵手 / 载具（对该地图共享 buddy 发起，复用 AIBuddyOptionPanel）
                // 与 self-buddy 互动选项按钮同款图标；打开前注入目标 buddy 的确定性 key（驱动 map buddy 模式）
                var optSpr = uiAtlas.GetSprite("btn_buddyOption");
                touchImage[1].sprite = optSpr;
                touchImage[1].SetNativeSize();
                touchBtns[1].gameObject.SetActive(true);
                touchBtns[1].onClick.RemoveAllListeners();
                touchBtns[1].onClick.AddListener(() =>
                {
                    // 试玩态不支持与地图 buddy 互动，仅提示；发布后进地图才可互动
                    if (UIManager.Inst.FindPanel<GamePlayPanel>(PanelId.GamePlayPanel) != null)
                    {
                        TipPanel.ShowToast("试玩中不支持此功能，请在发布后进入地图体验和AI伙伴互动");
                        return;
                    }
                    if (aiBuddyBehaviour.HasBuddy)
                    {
                        AIBuddyOptionPanel.MapBuddyTargetKey = aiBuddyBehaviour.LocalBuddyId;
                        UIManager.Inst.OpenPanel<AIBuddyOptionPanel>(PanelId.AIBuddyOptionPanel);
                    }
                });
                // icon 锚点跟随伙伴化身的实际位置（被载具挪走/放下到别处后仍贴在伙伴头顶，而非卡在放置点 behaviour 处）；
                // 未装配出真实伙伴（编辑态白模/未就绪）时回退到放置点位置
                var mapBuddyCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(aiBuddyBehaviour.LocalBuddyId);
                Vector3 buddyAnchorPos = (mapBuddyCtrl != null && mapBuddyCtrl.PlayerKCCtrl != null)
                    ? mapBuddyCtrl.PlayerKCCtrl.transform.position
                    : aiBuddyBehaviour.transform.position;
                UpdatePos(buddyAnchorPos, Vector3.up * 1.7f);
            }
        }
        var optConfig = filteredBehaviour[0].entity.GetEditOperationConfig();
        //TODO： 需要3D显示操作功能代码逻辑
        BtnTransform.gameObject.SetActive(isInteractable && isActive);

        return true;
    }

    private bool Interact(Collider[] colliders)
    {
        if (AvatarController.Inst.SelfStateController.IsInLinkEmote() || AvatarController.Inst.SelfStateController.IsInDoubleEmote() || AvatarController.Inst.SelfStateController.IsInLinkAIBuddy())
        {
            return false;
        }

        foreach (var collider in colliders)
        {
            if (!collider.name.StartsWith("KinematicCharacter")) continue;

            var playerStateController = collider.GetComponentInChildren<OtherStateController>();

            if (playerStateController != null)
            {
                // 双人循环
                if (playerStateController.doubleInteract)
                {
                    touchBehaviour = null;
                    var spr = uiAtlas.GetSprite("test");
                    if (playerStateController.linkEmoteData != null)
                    {
                        spr = uiAtlas.GetSprite("btn_link");
                    }

                    touchImage[1].sprite = spr;
                    touchImage[1].SetNativeSize();
                    touchBtns[1].gameObject.SetActive(true);
                    touchBtns[1].onClick.RemoveAllListeners();
                    touchBtns[1].onClick.AddListener(() =>
                    {
                        OnTouchOtherPlayer(playerStateController);

                    });
                    BtnTransform.gameObject.SetActive(true);
                    UpdatePos(playerStateController.transform.position, Vector3.up * 1.7f);
                    TextBtn.gameObject.SetActive(false);
                    touchBtns[0].gameObject.SetActive(false);
                    return true;
                }
            }
        }
        return false;
    }

    private bool ChatToAIBuddy(Collider[] colliders)
    {
        if (AvatarController.Inst.SelfStateController.IsInLinkEmote() || AvatarController.Inst.SelfStateController.IsInDoubleEmote()
            || AvatarController.Inst.SelfStateController.IsInLinkAIBuddy())
        {
            return false;
        }

        if (AIBuddyAvatarController.Inst.SelfStateController == null)
        {
            return false;
        }

        foreach (var collider in colliders)
        {
            if (!collider.name.StartsWith("KinematicCharacter"))
                continue;

            var playerStateController = collider.GetComponentInChildren<SelfBuddyStateController>();

            if (playerStateController != null)
            {
                // buddy 正坐在载具上时，不显示交互按钮（其他玩家本就无 SelfBuddyStateController，自然也不显示）
                if (GameAIBuddyManager.Inst != null && GameAIBuddyManager.Inst.IsBuddyOnVehicle(playerStateController.PlayerID))
                {
                    return false;
                }

                touchBehaviour = null;

                // 口令列表按钮
                var cmdSpr = uiAtlas.GetSprite("btn_buddyCommand"); // TODO: 替换为口令列表按钮图标
                touchImage[0].sprite = cmdSpr;
                touchImage[0].SetNativeSize();
                touchBtns[0].gameObject.SetActive(true);
                touchBtns[0].onClick.RemoveAllListeners();
                touchBtns[0].onClick.AddListener(() =>
                {
                    UIManager.Inst.OpenPanel(PanelId.AIBoxBuddyCommandPanel);
                });

                // 互动选项按钮（原 AIBuddyOptionPanel）
                var optSpr = uiAtlas.GetSprite("btn_buddyOption"); // TODO: 替换为互动选项按钮图标
                touchImage[1].sprite = optSpr;
                touchImage[1].SetNativeSize();
                touchBtns[1].gameObject.SetActive(true);
                touchBtns[1].onClick.RemoveAllListeners();
                touchBtns[1].onClick.AddListener(() =>
                {
                    // CameraMode 开启时，互动选项改为打开相机模式内的 NPC 子菜单（换装/载具/动作），而非独立 OptionPanel
                    if (UIManager.Inst.FindPanel<CameraModePanel>(PanelId.CameraModePanel) != null)
                    {
                        MessageHelper.Broadcast(MessageName.UICameraModeOpenNpc);
                    }
                    else
                    {
                        UIManager.Inst.OpenPanel<AIBuddyOptionPanel>(PanelId.AIBuddyOptionPanel);
                    }
                });

                BtnTransform.gameObject.SetActive(true);
                UpdatePos(playerStateController.transform.position, Vector3.up * 1.7f);
                TextBtn.gameObject.SetActive(false);
                return true;
            }
        }
        return false;
    }

    private void VehicleInteract(Collider[] colliders)
    {
        vehicleDrivers.Clear();
        driverAnalysis.FilerCollider(colliders, ref vehicleDrivers);
        if(vehicleDrivers.Count == 0){
            return;
        }
        var driver = vehicleDrivers[0];
        var spr = uiAtlas.GetSprite("btn_vehicle");
        vehicleImage[0].sprite = spr;
        vehicleImage[0].SetNativeSize();
        vehicleBtns[0].gameObject.SetActive(true);
        vehicleBtns[0].onClick.RemoveAllListeners();
        vehicleBtns[0].onClick.AddListener(() =>
        {
            GameVehicleManager.Inst.SendGetInVehicle(AccountDataManager.Inst.Uid, driver.PlayerID);
        });
        VehicleBtnTransform.gameObject.SetActive(true);
        //link相关的按钮不要显示
        touchBtns[1].gameObject.SetActive(false);

        UpdatePosByTarget(VehicleBtnTransform, vehicleSvParent, driver.transform.position, Vector3.up * 1f);
    }
    private void ChatToHospitalNPC(AIHospital_CharacterBehaviour npcBev)
    {
        var playerStateController = npcBev.GetComponentInChildren<KinematicCharacterController>();

        if (playerStateController != null && npcBev.IsCanClick)
        {
            var spr = uiAtlas.GetSprite("btn_buddyCommand");
            touchImage[1].sprite = spr;
            touchBtns[1].gameObject.SetActive(true);
            touchBtns[1].onClick.RemoveAllListeners();
            touchBtns[1].onClick.AddListener(() =>
                {
                    AIBuddyAvatarController.Inst.CurrentInteractNpcID = playerStateController.PlayerID;
                    GameAINpcChatManager.Inst.StartChatToAIBuddy(true, playerStateController.PlayerID);
                });
            BtnTransform.gameObject.SetActive(true);
            UpdatePos(playerStateController.transform.position, Vector3.up * 2.8f);
        }
    }

    private void UpdatePos(Vector3 modelPos, Vector3 offset)
    {
        if (mainCamera != null)
        {
            Vector3 camPos = mainCamera.WorldToScreenPoint(modelPos + offset);
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(svParent, camPos, uiCamera, out pos);
            BtnTransform.localPosition = pos;
        }
    }

    private void UpdatePosByTarget(Transform target,RectTransform svParent, Vector3 modelPos, Vector3 offset)
    {
        if (mainCamera != null)
        {
            Vector3 camPos = mainCamera.WorldToScreenPoint(modelPos + offset);
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(svParent, camPos, uiCamera, out pos);
            target.localPosition = pos;
        }
    }

    private void OnTouchClick()
    {
        if (touchBehaviour != null)
        {
            if (AIGameController.Inst.GetCurAIGame<AIParkGame>() != null)
            {
                if (AIParkGuideMgr.Inst.IsGuideJustShowSwingProp())
                {
                    if (touchBehaviour is AIPark_SwingBehaviour)
                    {
                        MessageHelper.Broadcast(MessageName.OnS11GuideStepClick, (int)AIGameParkConfig.EPgcGuideID.Touch_Hand_Swing, false);
                    }
                    return;
                }
                else
                {
                    AIParkPropsManager.Inst.SelfEnterProp(touchBehaviour);
                }
            }
            else
            {
                touchBehaviour.OnTouchClick();
            }
        }
    }

    private void OnTouchOtherPlayer(OtherStateController playerStateController)
    {
        if (playerStateController.isUgcDoubleAnim)
        {
            playerStateController.ugcEmoteData.ReceiverId = AccountDataManager.Inst.Uid;
            playerStateController.ugcEmoteData.Interact = InteractType.Interact;
            EmoteNetManager.Inst.SendUgcEmoteReq(playerStateController.ugcEmoteData);
        }
        else
        {
            if (playerStateController.emoteData != null &&
                playerStateController.emoteData.emoAniType == (int)EmoteType.LinkEmote)
            {
                if (AvatarController.Inst.SelfStateController.IsInSpecialAnim())
                {
                    TipPanel.ShowToast("请先解除特殊动作道具再进行牵手哦");
                    return;
                }
            }

            EmoteNetManager.Inst.SendEmoteInteractReq(playerStateController.emoteData.pgcId, playerStateController.PlayerID, (EmoteType)playerStateController.emoteData.emoAniType, InteractType.Interact);
        }
    }

    /// <summary>
    /// 订制s9清洁工的交互
    /// </summary>
    private void AddS9DustManInteract(AIHospital_CharacterBehaviour touchBehaviour)
    {
        //if (AvatarController.Inst.SelfStateController.IsInLinkEmote() || AvatarController.Inst.SelfStateController.IsInDoubleEmote() || AvatarController.Inst.SelfStateController.IsInLinkAIBuddy())
        //{
        //    return;
        //}
        var playerStateController = touchBehaviour._npcStateController;

        if (playerStateController != null)
        {
            var spr = uiAtlas.GetSprite("btn_interact");

            touchImage[1].sprite = spr;
            touchImage[1].SetNativeSize();
            touchBtns[1].gameObject.SetActive(true);
            touchBtns[1].onClick.RemoveAllListeners();
            touchBtns[1].onClick.AddListener(() =>
            {
                touchBehaviour.OnTouchDustMan();

            });
            BtnTransform.gameObject.SetActive(true);
            UpdatePos(playerStateController.transform.position, Vector3.up * 1.7f);
        }
    }

    public abstract class BaseRaycastFilterAnalysis<T> where T : MonoBehaviour
    {
        protected List<iFilerCondition> conditions = new List<iFilerCondition>();
        protected List<iMultFilterCondition> multConditions = new List<iMultFilterCondition>();
        protected List<T> nodes = new List<T>();
        protected virtual void ChangeColldersToType(Collider[] colliders)
        {
            nodes = new List<T>();
            for (int i = 0; i < colliders.Length; i++)
            {
                var bahaviours = colliders[i].GetComponentsInParent<T>();
                nodes.AddRange(bahaviours);
            }
        }

        public virtual void FilerCollider(Collider[] colliders, ref List<T> behaviours)
        {
            ChangeColldersToType(colliders);
            FilerCollider(ref behaviours);
        }

        public virtual void FilerCollider(ref List<T> behaviours)
        {
            List<T> filteredBehaviours = new List<T>();
            foreach (var nodeBehaviour in nodes)
            {
                if (nodeBehaviour is AIPark_StageBehaviour) 
                {
                    //过滤舞台
                    continue;
                }
                if (!IsFileredNode(nodeBehaviour))
                {
                    filteredBehaviours.Add(nodeBehaviour);
                }
            }
            behaviours = filteredBehaviours;
            MultFilteredNode(ref behaviours);
        }

        protected void MultFilteredNode(ref List<T> node)
        {
            for (var i = 0; i < multConditions.Count; i++)
            {
                multConditions[i].FileredNode(ref node);
            }
        }

        protected bool IsFileredNode(T node)
        {
            for (var i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].IsFileredNode(node))
                {
                    return true;
                }
            }
            return false;
        }
    }


    public class UIWorldFilterConditionAnalysis : BaseRaycastFilterAnalysis<NodeBaseBehaviour>
    {
        public UIWorldFilterConditionAnalysis()
        {
            // var tags = new List<string>() {"prop"};
            conditions.Add(new InViewFilerCondition());
            // conditions.Add(new TagFilterCondition(tags));
            conditions.Add(new PropTouchFilterCondition());
            conditions.Add(new TransactionFilterCondition());
            conditions.Add(new TypeFilterCondition());
            if (AIGameController.Inst.GetCurAIGame<AIParkGame>() != null)
            {
                conditions.Add(new SwingFilterCondition());
                conditions.Add(new CurPropFilterCondition());
                conditions.Add(new AIParkChatFilterCondition());
            }
            multConditions.Add(new DistanceFilterCondition());
        }
    }

    public class UIWorldPlayerFilterConditionAnalysis : BaseRaycastFilterAnalysis<PlayerStateController>
    {
        public UIWorldPlayerFilterConditionAnalysis()
        {
            //如果当前是游戏场景,且有载具在地图上,则过滤载具
            if(GameController.IsGame() 
            && GameController.GetEnterGameModel() == GameData.EnterGameModel.GuestScene){
                conditions.Add(new VehicleFilterCondition());
            }
        }
    }



    /// <summary>
    /// 多节点过滤结构
    /// </summary>
    public interface iMultFilterCondition
    {
        /// <summary>
        /// 过滤不符合条件节点
        /// </summary>
        public void FileredNode<T>(ref List<T> behaviour) where T : MonoBehaviour;
    }


    public class DistanceFilterCondition : iMultFilterCondition
    {
        public void FileredNode<T>(ref List<T> behaviours) where T : MonoBehaviour
        {
            if (behaviours.Count == 0)
                return;
            var pos = AvatarController.Inst.GetSelfAvatarPosition();
            float minValue = 100;
            int index = 0;
            for (var i = 0; i < behaviours.Count; i++)
            {
                if (behaviours[i] is NodeBaseBehaviour nodeBaseBehaviour)
                {
                    float dis = GetDistance(pos, nodeBaseBehaviour);
                    if (dis < minValue && nodeBaseBehaviour.IsCanClick)
                    {
                        minValue = dis;
                        index = i;
                    }
                }

            }
            var behaviour = behaviours[index];
            behaviours.Clear();
            behaviours.Add(behaviour);
        }
        //特殊道具考虑接口实现，可扩展
        private float GetDistance(Vector3 pos, NodeBaseBehaviour nBehav)
        {
            return Vector3.Distance(pos, nBehav.transform.position);
        }
    }

    /// <summary>
    /// 单节点过滤结构
    /// </summary>
    public interface iFilerCondition
    {
        /// <summary>
        /// 是否被过滤掉
        /// </summary>
        /// <param name="behaviour"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour;
    }

    public class TagFilterCondition : iFilerCondition
    {
        private List<string> tags;
        public TagFilterCondition(List<string> _tags)
        {
            tags = _tags;
        }

        public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
        {
            if (!tags.Contains(behaviour.tag))
            {
                return true;
            }

            return false;
        }
    }


    public class InViewFilerCondition : iFilerCondition
    {
        public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
        {
            var cam = GlobalCameraManager.Inst.GlobalMainCamera;
            Vector2 viewPos = cam.WorldToViewportPoint(behaviour.transform.position);
            Vector3 dir = (behaviour.transform.position - cam.transform.position).normalized;
            float dot = Vector3.Dot(cam.transform.forward, dir); //判断物体是否在相机前面
            if (dot > 0 && viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1)
            {
                return false;
            }
            return true;
        }
    }


    public class PropTouchFilterCondition : iFilerCondition
    {
        public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
        {
            var nodeBehaviour = behaviour as NodeBaseBehaviour;
            var propData = nodeBehaviour.entity.GetPropConfig();
            if (propData == null)
            {
                return false;
            }
            if (propData.touchType != 1)
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 内购过滤条件
    /// </summary>
    public class TransactionFilterCondition : iFilerCondition
    {
        public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
        {
            var nodeBehaviour = behaviour as NodeBaseBehaviour;
            var transactionComp = nodeBehaviour.entity?.GetComp<TransactionComponent>();
            if (transactionComp == null)
            {
                return false;
            }
            return !transactionComp.isTransaction;
        }
    }

    /// <summary>
    /// 类型过滤
    /// </summary>
    public class TypeFilterCondition : iFilerCondition
    {
        public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
        {
            var nodeBehaviour = behaviour as NodeBaseBehaviour;
            if (nodeBehaviour.GetType() == typeof(AIPark_SeesawChairBehaviour))
            {
                return true;
            }
            if (nodeBehaviour.GetType() == typeof(AIPark_SwingChairBehaviour))
            {
                return true;
            }
            if (nodeBehaviour.GetType() == typeof(AIPark_TrojanhorseChairBehaviour))
            {
                return true;
            }
            return false;
        }
    }

    public class VehicleFilterCondition : iFilerCondition
    {
        public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
        {
            var player = behaviour as PlayerStateController;
            if(player == null){
                return true;
            }
            //如果自己是司机，则不显示
            if(AccountDataManager.Inst.IsMySelf(player.PlayerID)){
                return true;
            }
            //如果自己已经是乘客，则不显示
            if(GameVehicleManager.Inst.IsPassenger(AccountDataManager.Inst.Uid)){
                return true;
            }

            //如果自己正在驾驶载具，则不显示
            if(GameVehicleManager.Inst.IsDriver(AccountDataManager.Inst.Uid)){
                return true;
            }

            //如果检查到的玩家是司机且有空位
            if(GameVehicleManager.Inst.IsDriverAndHasSeat(player.PlayerID)){
                return false;
            }

            

            return true;
        }
    }

    public class SwingFilterCondition : iFilerCondition
    {
        public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
        {
            var nodeBehaviour = behaviour as NodeBaseBehaviour;
            if (AIParkGuideMgr.Inst.IsGuideJustShowSwingProp())
            {
                return nodeBehaviour.GetType() != typeof(AIPark_SwingBehaviour);
            }
            return false;
        }
    }

    public class CurPropFilterCondition : iFilerCondition
    {
        public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
        {

            var curPropId = AIParkPropsManager.Inst.selfUsePropId;
            var nodeId = (behaviour as NodeBaseBehaviour)?.entity?.Id ?? -1;
            if(curPropId != -1){
                if(curPropId == nodeId){
                    return true;
                }else{
                    var actionType = AIParkPropsManager.Inst.GetActionType(behaviour as NodeBaseBehaviour);
                    var npc = AIPark_CharacterManager.Inst.GetNpc(((int)ParkNpcRoleType.self).ToString());
                    var npcActionType = AIParkPropsManager.Inst.GetActionType(npc.GetCurrentState());

                    if(npcActionType == actionType){
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class AIParkChatFilterCondition : iFilerCondition
    {
        public bool IsFileredNode<T>(T behaviour) where T : MonoBehaviour
        {
            if(AIParkChatView.isShowChatView){
                return true;
            }
            return false;
        }
    }
}