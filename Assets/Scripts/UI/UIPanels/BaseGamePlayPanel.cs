using System;
using System.Collections;
using Basic;
using BUD.AnimPose;
using Cinemachine;
using DG.Tweening;
using Game.Avatar;
using Game.Avatar.FSM.DataStructure;
using Game.Base;
using Game.Event;
using Game.KinematicCharacter;
using Game.MusicalInstrument;
using Game.Pet;
using Game.Props.PropsManagers;
using Game.SurfaceDetection.Base;
using Game.Utils;
using Game.Vehicle.PGCVehicle;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using GameSync.Manager;
using Message;
using Newtonsoft.Json;
using Pb.Base;
using UI.Avatar;
using UI.Base;
using UI.Utils;
using UnityEngine;
using UnityEngine.UI;
using static CustomBodyTypeController;

public  class BaseGamePlayPanel<T> : BasePanel<T> where T: BasePanel<T>
{
    [SerializeField] private Transform m_collectStarProgress;
    private Text m_collectStarProgressTxt;

    // Start is called before the first frame update

    public MobileJoystick m_joyStick;
    protected KinematicCharacterController kinematicCharacter;
    protected KinematicCharacterController petCharacter;
    private CinemachineBrain cineBrain;
    private DebugInputUtil debugInputUtil;
    protected CameraJoyStick cameraStick;
    private CharacterData avatarInfo;
    #region 基础操作组

    // 水下上浮按钮
    private Button waterJumpBtn;

    #endregion

    public override void OnCreate()
    {
        base.OnCreate();
        CreateDebugInput();
        avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
        PetData petInfo = AccountDataManager.Inst.PetInfo.avatarInfo;
        var spManager = GlobalNodeManager.Inst.Get<SpawnPointManager>();
        var pos = spManager.GetDefaultSpawnPoint();
        var rotation = spManager.GetDefaultSpawnRotation();
        GuestInstrumentOPManager.Inst.AddListener();
        kinematicCharacter = AvatarController.Inst.CreateSelfGameAvatar(avatarInfo, petInfo);
        petCharacter = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid)?.PetKCCtrl;
        UserInfoHeadView.Load(kinematicCharacter.gameObject, new PlayerInfo() {
            Uid = AccountDataManager.Inst.UserInfo.uid,
            Name = AccountDataManager.Inst.UserInfo.nickname,
            AvatarJson = AccountDataManager.Inst.UserInfo.avatarJson,
        });
         // 出生点直接 Motor.SetPositionAndRotation(pos, rot) 把胶囊“硬塞到”某个 y 上，
         // 而 DefaultKCC 第一次进入又把 Motor.IsOnSimulate 关掉了，所以这一帧不会做地面探测/解穿插，
         // 于是就会出现“初始就卡入地面、Motor pos 负数”。
        pos.y += 0.13f; //临时解决方案
        kinematicCharacter.Motor.SetPositionAndRotation(pos, rotation);

        var cam = GlobalCameraManager.Inst.GlobalMainCamera;
        cineBrain = cam.GetComponent<CinemachineBrain>();
        m_joyStick = this.GetComponentInChildren<MobileJoystick>();
        m_joyStick.SetSelfAvatar(kinematicCharacter, petCharacter, cam);
        cameraStick = this.GetComponentInChildren<CameraJoyStick>();
        cineBrain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
        cameraStick.CamereTarget = kinematicCharacter.CameraTarget.transform;
        AcatarFollowUIManager.Inst.Init(kinematicCharacter);
        FrameDataManager.Inst.Init();
        EmoteNetManager.Inst.Init();
        StatusNetManager.Inst.Init();
        RoomEditAvatarManager.Inst.Init();
        GameVehicleManager.Inst.Init();
        TheatreGameManager.Inst.Init();
        kinematicCharacter.Motor.SetCapsuleHeightData((BodyType)avatarInfo.bodyType);
        #region 操作按钮初始化

        var operationBtns = GameObjectEx.FindChildByName(transform, "OperateBtns");
        if (operationBtns != null)
        {
            waterJumpBtn = GameObjectEx.FindComponentByName<Button>(operationBtns, "WaterJumpBtn");
            waterJumpBtn.gameObject.SetActive(false);
            waterJumpBtn.onClick.AddListener(OnWaterJumpBtnClick);
        }
        #endregion

        #region 用户状态事件注册

        MessageHelper.AddListener(StateMessage.StateEnterWater, OnStateEnterWater);
        MessageHelper.AddListener(StateMessage.StateExitWater, OnStateExitWater);
        #endregion

    }



    #region 用户状态切换事件回调

    private void OnStateExitWater()
    {
        MobileJoystick.Inst.SetJumpVisible(true);
        waterJumpBtn.gameObject.SetActive(false);
    }
    private void OnStateEnterWater()
    {
        MobileJoystick.Inst.SetJumpVisible(false);
        waterJumpBtn.gameObject.SetActive(true);
    }

    private void OnWaterJumpBtnClick()
    {
        MobileJoystick.Inst.SimulatorJump();
    }

    #endregion


    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        cineBrain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
        kinematicCharacter.gameObject.SetActive(true);
        SurfaceDetectManager.Inst.Enable = true;
        GuestInstrumentOPManager.Inst.RemoveListener();
        RefreshCollectStarProgress();
        ControlDebugEnable(true);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        cineBrain.m_UpdateMethod = CinemachineBrain.UpdateMethod.SmartUpdate;
        if (kinematicCharacter != null)
        {
            kinematicCharacter.gameObject.SetActive(false);
        }
        SurfaceDetectManager.Inst.Enable = false;
        ControlDebugEnable(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        cineBrain.m_UpdateMethod = CinemachineBrain.UpdateMethod.SmartUpdate;
        AvatarController.Inst.DestorySelfGameAvatar(kinematicCharacter);
        AvatarController.Inst.DestoryOtherPlayer();
        PetAvatarController.Inst.DestorySelfGameAvatar(petCharacter);
        PetAvatarController.Inst.DestoryOtherPet();
        GameAIBuddyChatManager.Inst.LogAIBuddyChatInfo();
        AIBuddyAvatarController.Inst.DestoryAllPlayer();
        GameVehicleManager.Inst.Release();
        TheatreGameManager.Inst.Release();
        PGCVehicleManager.Inst.Release();
        #region 用户状态事件注销

        MessageHelper.RemoveListener(StateMessage.StateEnterWater, OnStateEnterWater);
        MessageHelper.RemoveListener(StateMessage.StateExitWater, OnStateExitWater);
        #endregion
    }

    #region 本地测试使用

    private void CreateDebugInput()
    {
#if UNITY_EDITOR
        var newObj = new GameObject();
        debugInputUtil = newObj.AddComponent<DebugInputUtil>();
#endif
    }

    private void ControlDebugEnable(bool isEnable)
    {
#if UNITY_EDITOR
        if (!debugInputUtil) return;
        if (isEnable)
        {
            debugInputUtil.BeginDebugInput();
        }
        else
        {
            debugInputUtil.StopDebugInput();
        }
#endif
    }

    #endregion

    #region 公共部分

    public void RefreshCollectStarProgress()
    {
        var winCondition = WinConditionManager.Inst.GetCurrentWinCondition();
        var isShow = winCondition is CollectStarCondition;
        m_collectStarProgress.gameObject.SetActive(isShow);
        if (isShow)
        {
            var starCondition = winCondition as CollectStarCondition;
            var mgr = starCondition.Mgr;
            mgr.SetCollectProgress = SetCollectStarProgress;
            mgr.SetProgressUIShow();
        }
    }

    private void SetCollectStarProgress(string s)
    {
        if (m_collectStarProgressTxt == null)
        {
            m_collectStarProgressTxt = m_collectStarProgress.GetComponentInChildren<Text>();
        }
        m_collectStarProgressTxt.text = s;
        var sizeDelta = m_collectStarProgressTxt.GetComponent<RectTransform>().sizeDelta;
        m_collectStarProgressTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(m_collectStarProgressTxt.preferredWidth, sizeDelta.y);
    }


    protected void RetryBtnClick()
    {
        ClientManager.Inst.BackToSpawn();
    }

    protected void ScreenShotBtnClick()
    {
        UIManager.Inst.OpenPanel<ShotBlackPanel>(PanelId.ShotBlackPanel);
        
        //上报事件
        EventCenterDataManager.Inst.ReportTask(PostEventId.TakePhotoCheckIn);

        // StartCoroutine(ShotAnimation());
        // var selfAvatar = AvatarController.Inst.SelfController;
        // if (selfAvatar != null)
        // {
        //     Game.Audio.AkSoundManager.Inst.PostEvent("play_screenshot", selfAvatar.gameObject);
        // }
        // var mainCamera = GlobalCameraManager.Inst.GlobalMainCamera;
        // var bytes = ScreenShotUtils.ScreenShot(mainCamera, new Rect(0, 0, Screen.width, Screen.height), true);
        // if (bytes.Length == 0)
        // {
        //     TipPanel.ShowToast("保存失败，请再试一次!");
        //     return;
        // }
        //
        // string userId = AccountDataManager.Inst.Uid;
        // userId = string.IsNullOrEmpty(userId) ? "shotTemplate" : userId;
        // LocalDataUtils.Inst.SaveTempImgRes(userId,bytes);
    }

    // private IEnumerator ShotAnimation()
    // {
    //     var blackPanel = UIManager.Inst.OpenPanel<ShotBlackPanel>(PanelId.ShotBlackPanel);
    //     RawImage tempRawImage = blackPanel.GetComponent<RawImage>();
    //     tempRawImage.enabled = true;
    //     tempRawImage.CrossFadeAlpha(0, 0, false);
    //     tempRawImage.color = new Color(1, 1, 1, 1);
    //     Image blackImage = blackPanel.BlackImage;
    //     blackImage.color = new Color(1, 1, 1, 0);
    //     blackImage.DOFade(1, 0.4f).SetEase(Ease.InExpo).onComplete = () =>
    //     {
    //         blackImage.DOFade(0, 0.4f).SetEase(Ease.OutExpo);
    //     };
    //     //临时存储截面图片信息
    //     yield return new WaitForEndOfFrame();
    //     Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
    //     screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
    //     screenShot.Apply();
    //     tempRawImage.texture = screenShot;
    //     tempRawImage.CrossFadeAlpha(1, 0, false);
    //
    //     yield return new WaitForSeconds(1.3f);
    //     var lastTexture = tempRawImage.texture;
    //     if (lastTexture)
    //     {
    //         Object.Destroy(lastTexture);
    //     }
    //     tempRawImage.color = new Color(1, 1, 1, 0);
    //     UIManager.Inst.ClosePanel(WindowId.CommonWindow, PanelId.ShotBlackPanel);
    // }

    protected void ChangeAvaterSkin(SkinInfo skinInfo)
    {
        avatarInfo.ChangeSkinData(skinInfo);
    }

    protected void ChangeVehicleSkin(VehicleInfo vehicleInfo, Action action = null)
    {
        avatarInfo.AddVehicleData(vehicleInfo);
        AvatarController.Inst.RefreshAvatarByData(AccountDataManager.Inst.Uid, avatarInfo, action);
    }


    #endregion



}
