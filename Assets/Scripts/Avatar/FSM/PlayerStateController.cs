using Es;
using FSM;
using Game.Audio;
using Game.Avatar;
using Game.KinematicCharacter;
using Game.Pet;
using Game.Vehicle.PGCVehicle.KVC;
using Pb.Base;
using UnityEngine;

public abstract class PlayerStateController : MonoBehaviour
{
    public FSM.PlayerStateMachine stateMachine;

    [HideInInspector] public PlayerAnimationCtrl PlayerAnimCtrl;
    [HideInInspector] public PetAnimationCtrl PetAnimCtrl;
    [HideInInspector] public PetEmoteState PetEmoState;
    [HideInInspector] public KinematicCharacterController PlayerKCCtrl;
    [HideInInspector] public KinematicCharacterController PetKCCtrl;
    [HideInInspector] public PGCKinematicVehicleController PGCVehicleKinematicCtrl;
    public CharacterWrap Wrap;
    public PetWrap PetWrap;

    public string PlayerID { get;  set; }

    public abstract bool IsSelf { get; }
    
    private bool _isSelfAIBuddy;
    public virtual bool IsSelfAIBuddy 
    { 
        get => _isSelfAIBuddy;
        set => _isSelfAIBuddy = value;
    }
    
    private bool _isAIBuddy;
    public bool IsAIBuddy
    {
        get => _isAIBuddy;
        set => _isAIBuddy = value;
    }

    // AI 伙伴作为乘客坐在载具上：上车置 true、下车置 false。待机循环据此暂停（避免覆盖载具坐姿）。
    // 直接挂在 buddy 自身 stateCtrl 上，由上/下车代码按正确的 buddy 引用设置，无需消息 key 匹配（兼容地图共享 buddy）。
    public bool IsRidingVehicle;

    #region Init
    public void Init(PlayerAnimationCtrl playerAnimationCtrl, CharacterWrap wrap, KinematicCharacterController controller, KinematicCharacterController petController, string uid)
    {
        PetAnimCtrl = petController.PlayerAnimCtrl as PetAnimationCtrl;
        PetEmoState = petController.PlayerAnimCtrl.gameObject.GetOrAddComponent<PetEmoteState>();
        PetEmoState.owner = this;
        PlayerAnimCtrl = playerAnimationCtrl;
        Wrap = wrap;
        PlayerKCCtrl = controller;
        PetKCCtrl = petController;
        PlayerID = uid;
        PetWrap = PetAvatarController.Inst.GetPetKCCtrl(uid);

        InitComponent();
        InitPlayerState();

        PlayerAnimCtrl.CheckAndOverrideSpecialAnim();
        PlayerAnimCtrl.SetPlayerID(PlayerID);
        PlayerKCCtrl.Init(PlayerAnimCtrl, PlayerID, IsSelf);
    }

    public void InitAIBuddy(PlayerAnimationCtrl playerAnimationCtrl, CharacterWrap wrap, KinematicCharacterController controller, string uid)
    {
        PlayerAnimCtrl = playerAnimationCtrl;
        Wrap = wrap;
        PlayerKCCtrl = controller;
        PlayerID = uid;

        InitComponent();
        InitPlayerState();

        PlayerAnimCtrl.CheckAndOverrideSpecialAnim();
        PlayerAnimCtrl.SetPlayerID(PlayerID);
        PlayerKCCtrl.Init(PlayerAnimCtrl, PlayerID, false);
    }

    private void InitComponent()
    {
        PlayerAnimCtrl = GetComponent<PlayerAnimationCtrl>();
    }

    protected void InitPlayerState()
    {
        PlayerStateBase defaultState = new DefaultState(PlayerState.Default, this);
        PlayerStateBase singleEmoteState = new SingleEmoteState(PlayerState.SingleEmote, this);
        PlayerStateBase winFlagState = new WinReachFlagState(PlayerState.WinningFlag, this);
        PlayerStateBase collectStarState = new CollectStarState(PlayerState.CollectedStar, this);
        PlayerStateBase changeClothesState = new ChangeClothesState(PlayerState.ChangeClothes, this);
        PlayerStateBase changeClothesAniState = new ChangeClothesAniState(PlayerState.ChangeClothesAni, this);
        PlayerStateBase skateState = new SkateState(PlayerState.Skate, this);
        PlayerStateBase skiState = new SkiState(PlayerState.Ski, this);
        PlayerStateBase swimState = new SwimState(PlayerState.Swim, this);
        PlayerStateBase doubleEmoteState = new DoubleEmoteState(PlayerState.DoubleEmote, this);
        PlayerStateBase interactiveBoardState = new InteractiveBoardState(PlayerState.InteractiveBoard, this);
        PlayerStateBase ugcEmoteState =  new UgcEmoteState(PlayerState.UgcEmote, this);
        PlayerStateBase ugcDoubleEmoteState =  new UgcDoubleEmoteState(PlayerState.UgcDoubleEmote, this);
        PlayerStateBase selfieState = new SelfieState(PlayerState.CameraMode, this);
        PlayerStateBase musicInstrumentPlayState = new MusicInstrumentPlayState(PlayerState.MusicInstrumentPlay,this);
        PlayerStateBase linkEmoteStartState = new LinkEmoteStartState(PlayerState.LinkEmoteStart, this);
        PlayerStateBase linkEmoteState = new LinkEmoteState(PlayerState.LinkEmote, this);
        PlayerStateBase passengerState = new PassengerState(PlayerState.Passenger, this);
        PlayerStateBase pgcVehicleState = new PGCVehicleState(PlayerState.PGCVehicle, this);
        PlayerStateBase boundState = new BoundState(PlayerState.Bound, this);
        PlayerStateBase capturedState = new CapturedState(PlayerState.Captured, this);
        PlayerStateBase fallingState = new FallingState(PlayerState.Falling, this);


        stateMachine = new FSM.PlayerStateMachine(defaultState, IsSelf);
        stateMachine.AddState(singleEmoteState);
        stateMachine.AddState(winFlagState);
        stateMachine.AddState(changeClothesState);
        stateMachine.AddState(changeClothesAniState);
        stateMachine.AddState(collectStarState);
        stateMachine.AddState(skateState);
        stateMachine.AddState(skiState);
        stateMachine.AddState(swimState);
        stateMachine.AddState(doubleEmoteState);
        stateMachine.AddState(interactiveBoardState);
        stateMachine.AddState(selfieState);
        stateMachine.AddState(musicInstrumentPlayState);
        stateMachine.AddState(ugcEmoteState);
        stateMachine.AddState(ugcDoubleEmoteState);
        stateMachine.AddState(linkEmoteStartState);
        stateMachine.AddState(linkEmoteState);
        stateMachine.AddState(passengerState);
        stateMachine.AddState(pgcVehicleState);
        stateMachine.AddState(boundState);
        stateMachine.AddState(capturedState);
        stateMachine.AddState(fallingState);
        stateMachine.EnterMainState = EnterPlayerState;
        stateMachine.RegisterSyncStateCallback(PlayerID);
        // stateMachine.SetUpdateAnimClipsAction(PlayerAnimCtrl.UpdateAnimClips);
    }
    #endregion

    private void EnterPlayerState(PlayerState stateID)
    {
        PlayerAnimCtrl.SetPlayerState(stateID);
    }

    #region Unity
    protected virtual void Awake()
    {
    }

    protected virtual void OnDestroy()
    {
        //StopEmoteCo();
        //OnCompleteAni = null;
    }

    protected virtual void Update()
    {
        if (stateMachine != null)
        {
            stateMachine.Update();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (stateMachine != null)
        {
            stateMachine.FixedUpdate();
        }
    }

    protected virtual void LateUpdate()
    {
        if (stateMachine != null)
        {
            stateMachine.LateUpdate();
        }
    }
    #endregion

    #region 外部调用
    /// <summary>
    /// 是否能进入当前状态
    /// </summary>
    /// <param name="stateID">状态ID</param>
    /// <returns></returns>
    public bool CanEnterState(PlayerState stateID)
    {
        return stateMachine.CanEnterState(stateID);
    }

    /// <summary>
    /// 主动进入状态
    /// </summary>
    /// <param name="stateID">状态ID</param>
    /// <param name="args">传参</param>
    public void EnterState(PlayerState stateID, params object[] args)
    {
        stateMachine.EnterState(stateID, false, args);
    }

    /// <summary>
    /// 直接进入状态
    /// </summary>
    /// <param name="stateID">状态ID</param>
    /// <param name="args">传参</param>
    public void DirectIntoState(PlayerState stateID, params object[] args)
    {
        stateMachine.EnterState(stateID, true, args);
    }

    /// <summary>
    /// 断线重连调用接口，恢复状态
    /// </summary>
    /// <param name="stateID">状态ID</param>
    /// <param name="args">传参</param>
    public void ReconnectIntoState(PlayerState stateID, params object[] args)
    {
        stateMachine.ReconnectIntoState(stateID, args);
    }

    /// <summary>
    /// 主动退出状态
    /// </summary>
    /// <param name="stateID">状态ID</param>
    /// <param name="isPlayExitAni">是否播放退出动画</param>
    public void ExitState(PlayerState stateID, bool isPlayExitAni = true)
    {
        stateMachine.ExitState(stateID, isPlayExitAni);
    }

    /// <summary>
    /// 重置状态，恢复到Default状态
    /// </summary>
    public void ResetToDefaultState()
    {
        stateMachine.ResetToDefaultState();
    }

    /// <summary>
    /// 更新人物动画片段
    /// </summary>
    public void UpdateAnimClips()
    {
        // stateMachine.UpdateAnimClips();
    }

    /// <summary>
    /// 人物是否包含当前状态
    /// </summary>
    /// <param name="stateID">状态ID</param>
    /// <returns></returns>
    public bool ContainsCurrentState(PlayerState stateID)
    {
        if (stateMachine == null) return false;
        return stateMachine.ContainsCurrentState(stateID);
    }

    /// <summary>
    /// 人物持有的状态和缓存池中是否包含当前状态
    /// </summary>
    /// <param name="stateID">状态ID</param>
    /// <returns></returns>
    public bool ContainsAllState(PlayerState stateID)
    {
        if (stateMachine == null) return false;
        return stateMachine.ContainsAllState(stateID);
    }

    /// <summary>
    /// 是否主状态
    /// </summary>
    /// <param name="state">状态ID</param>
    /// <returns></returns>
    public bool IsMainState(PlayerState stateID)
    {
        return stateMachine.IsMainState(stateID);
    }

    /// <summary>
    /// 召唤载具前的统一校验（包含：牵手等待 LinkEmoteStart、牵手中 LinkEmote、双人动作等）。
    /// </summary>
    public bool CanCallVehicle(out string toastReason)
    {
        // waiting: LinkEmoteStartState 会设置 linkEmoteData 且 emoteOPId 为空 -> doubleInteract=true
        bool isInLinkWaitOrLinking =
            ContainsCurrentState(PlayerState.LinkEmoteStart)
            || ContainsCurrentState(PlayerState.LinkEmote)
            || IsInLinkEmote()
            || IsInLinkAIBuddy()
            || doubleInteract;

        // 双人表情（非牵手）也禁止召唤
        bool isInDoubleEmote = IsInDoubleEmote();

        if (isInLinkWaitOrLinking || isInDoubleEmote)
        {
            toastReason = "双人牵手连接中/双人动作时，不能召唤载具";
            return false;
        }

        toastReason = null;
        return true;
    }

    public bool CanCallVehicle()
    {
        return CanCallVehicle(out _);
    }

    public bool IsInLinkEmote()
    {
        return LinkEmoteManager.Inst.IsPlayerLinking(PlayerID);
    }

    public bool IsLinkPlayerB()
    {
        return LinkEmoteManager.Inst.IsPlayerB(PlayerID);
    }
    
    public bool IsInLinkAIBuddy()
    {
        return BuddyLinkEmoteManager.Inst.IsInBuddyLinkState(PlayerID);
    }

    public bool IsInSpecialAnim()
    {
        if (PlayerAnimCtrl != null && !string.IsNullOrEmpty(PlayerAnimCtrl.specialAnimPgcId))
        {
            return true;
        }

        return false;
    }

    public bool IsInDoubleEmote()
    {
        return (emoteData != null && !string.IsNullOrEmpty(emoteOPId));
    }

    public virtual void BindPlayerID(string playerId)
    {
        this.PlayerID = playerId;
    }

    public virtual void UnBindPlayerID()
    {
        if(string.IsNullOrEmpty(PlayerID))
        {
            return;
        }
        StateEventManager.Inst.UnRegisterPlayerEvent(PlayerID);
        stateMachine.ReleaseStateMachine();
    }
    #endregion

    #region Input Event
    public virtual void MoveJoystick(Vector3 screenOffset) { }

    public virtual void OnClickJumpBtn() {

    }

    #endregion

    #region State Parameter

    public EmoteNetData ugcEmoteData;
    public EmoUIConfig emoteData; // 当前EmoteData
    public string emoteOPId;      // 双人动画对方Id
    public bool isUgcDoubleAnim;  //Ugc双人emote
    public EmoUIConfig petEmoteData;
    public EmoteNetData linkEmoteData;
    // 双人动作邀请
    public bool doubleInteract
    {
        get
        {
            if (isUgcDoubleAnim) return true;
            if (linkEmoteData != null && string.IsNullOrEmpty(emoteOPId)) return true;
            if (emoteData == null) return false;
            if ((emoteData.emoType == 3 || emoteData.emoType == 4) && string.IsNullOrEmpty(emoteOPId))
            {
                return true;
            }
            return false;
        }
    }
    #endregion

    #region 脚下箭头

    private PlayerStateController targetPlayer;
    public PlayerStateController TargetPlayer
    {
        get
        {
            return targetPlayer;
        }
        set
        {
            if (targetPlayer != value)
            {
                targetPlayer = value;
            }
            if (targetPlayer != null)
            {
                if (footFlag == null)
                {
                    footFlag = CharacterFootFlag.LoadEffect(PlayerKCCtrl.transform);
                }
                footFlag.gameObject.SetActive(true);
                footFlag.SetTarget(this, targetPlayer);
            }
            else
            {
                if (footFlag != null)
                {
                    footFlag.gameObject.SetActive(false);
                }
            }
        }
    }

    public CharacterFootFlag footFlag;

    #endregion

    #region PGCVehicle
    
    public void SetPGCVehicleKinematicCtrl(PGCKinematicVehicleController kinematicCtrl)
    {
        PGCVehicleKinematicCtrl = kinematicCtrl;
    }

    #endregion
}
