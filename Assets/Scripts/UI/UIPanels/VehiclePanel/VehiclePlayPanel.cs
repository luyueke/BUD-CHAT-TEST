using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Extensions;
using BUD.AnimPose;
using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.MapSetting;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using UI.BaseWidgets;
using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Game.KinematicCharacter.KinematicCharacterMotor;
using UGCAsset;

public class VehiclePlayPanel : BaseGamePlayPanel<VehiclePlayPanel>
{
    [SerializeField] private CButton m_returnBtn;
    [SerializeField] private CButton retryBtn;
    [SerializeField] private CButton screenShotBtn;
    [SerializeField] private CButton soundBtn; 
    [SerializeField] private CButton dirveHeightBtn;
    [SerializeField] private Transform heightSliderPos;
    [SerializeField] private Slider heightSlider;

    private bool isSoundBtnDown = false;
    private float soundTimer = 0f;
    private float soundBroadcastInterval = 0.5f; // 鸣笛广播间隔时间

    public Action<float> heightSetCallBack;

    private VehicleInfo vehicleInfo;

    private string playingDriveUrl;

    private CharacterWrap doubleWarp;

    private CharacterWrap driverWarp;

    private float SetHeight;

    private GameObject audioPos;

    private GameObject driveAudioPos;

    private bool isPlay = false;
    public override void OnCreate()
    {
        base.OnCreate();
        m_returnBtn.onClick.AddListener(ChangeEditMode);
        screenShotBtn.onClick.AddListener(ScreenShotBtnClick);
        screenShotBtn.gameObject.SetActive(false); //试驾不需要截图按钮
        // 喇叭按钮要处理长按鸣笛
        soundBtn.onClick.AddListener(OnSoundBtnDown);
        dirveHeightBtn.onClick.AddListener(OnDriveHeightClick);
        retryBtn.onClick.AddListener(() =>
        {
            heightSlider.value = 0f;
            RetryBtnClick();
        });

        heightSlider.onValueChanged.AddListener((value) =>
        {
            ChangeVehicleHeight(value);
        });
        kinematicCharacter.Motor.IsDriveVehicle = true;
        kinematicCharacter.Motor.CurUGCVehicleStatus = UGCVehicleStatus.Drive;
        isSoundBtnDown = false;
        ChangeNodeActive(false);
        petCharacter?.gameObject.SetActive(false); //试驾不需要宠物
        MessageHelper.AddListener(MessageName.OnVehicleEditDriverAudioPlay, OnVehicleDriverAudioPlay);
        MessageHelper.AddListener(MessageName.OnVehicleEditDriverAudioStop, OnVehicleDriverAudioStop);
    }

    private void SceneCharge(bool enable)
    {
        float num = enable ? 5f : 1 / 5f;
        GameObject gdgt_terrain_PREFAB = GameObject.Find("Scene/Stage/gdgt_terrain_PREFAB");
        if (gdgt_terrain_PREFAB != null && gdgt_terrain_PREFAB.transform.childCount > 0)
        {
            for (int i = 0; i < gdgt_terrain_PREFAB.transform.childCount; i++)
            {
                gdgt_terrain_PREFAB.transform.GetChild(i).localScale *= num;
            }
            if (enable)
            {
                TempSetMaterial(115);
            }
            else
            {
                TempSetMaterial(0);
            }
        }

    }

    private void TempSetMaterial(int matId)
    {
        var root = GameObject.Find("Scene/Stage/gdgt_terrain_PREFAB");
        if (root == null)
        {
            return;
        }

        var mpb = new MaterialPropertyBlock();
        var unionId = new MaterialUnionID(matId);

        for (int i = 0; i < root.transform.childCount; i++)
        {
            var child = root.transform.GetChild(i);
            if (child == null) continue;

            var renderers = child.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) continue;

            // 复用游戏内材质加载逻辑（支持普通材质/UGC材质）
            var loader = new GameMaterialLoader(child.gameObject, renderers, mpb);
            loader.Load(unionId);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener(MessageName.OnVehicleEditDriverAudioPlay, OnVehicleDriverAudioPlay);
        MessageHelper.RemoveListener(MessageName.OnVehicleEditDriverAudioStop, OnVehicleDriverAudioStop);
    }

    protected override void OnEnable()
    {
       // Debug.LogError("play onenable");
        base.OnEnable();        
        SceneCharge(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
     //   Debug.LogError("play OnDisable");
        SceneCharge(false);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        BindPlayCameraToSelf();
        if (args != null && args.Length > 0)
        {
            var vehicleTryPlayInfo = args[0] as VehicleTryPlayInfo;
            vehicleInfo = vehicleTryPlayInfo.vehicleInfo;
            isPlay = vehicleTryPlayInfo.isPlay;
            if(vehicleInfo == null)
            {
                CloseSelf();
                LoggerUtils.LogError("载具信息为空!");
                return;
            }

            if(vehicleInfo.vehicleType == 2)
            {
                var characterData = AccountDataManager.Inst.UserInfo.avatarInfo;
                var tsf = kinematicCharacter.PlayerAnimCtrl.gameObject.transform;
                doubleWarp = AvatarController.Inst.CreateGameAvatarWithIKController(characterData, tsf.Find("vehiclepos"));
                ResetDriverAnimator(doubleWarp.Avatar);
            }
            ChangeVehicleSkin(vehicleInfo, ResetDriverInfo);

            kinematicCharacter.Motor.SetPosition(new Vector3(pos.x, vehicleInfo.vehicleHeight, pos.z));
            GameDataManager.Inst.mapGlobalData.vehicleInfo = vehicleInfo;
            Invoke("PlayHonkAudio", 0.5f);//延迟0.5秒播放完转场动画
        }
        StartCoroutine(CoEnsureNodesHidden());
    }

    /// <summary>
    /// 打开 VehiclePlayPanel 时，确保相机跟随当前玩家模型（不同入口进入时 Follow 可能为空或指向旧对象）
    /// </summary>
    private void BindPlayCameraToSelf()
    {
        // 确保主相机 CinemachineBrain 开启，否则虚拟相机不会生效
        GameCameraUtils.Inst.SetMainCameraCinemachineBrain(true);

        var vcam = GameCameraUtils.Inst.GetPlayVirtualCamera();
        if (vcam == null)
        {
            return;
        }

        // BaseGamePlayPanel.OnShow 会把 kinematicCharacter 激活，这里直接绑定到其 CameraTarget
        var followTarget = kinematicCharacter != null ? kinematicCharacter.CameraTarget?.transform : null;
        if (followTarget == null)
        {
            return;
        }

        vcam.Follow = followTarget;
        vcam.LookAt = followTarget;
    }

    private IEnumerator CoEnsureNodesHidden()
    {
        // 兜底：部分节点可能在进入试玩后 1~2 帧才完成创建/注册进 GamePropNodeManager
        yield return null;
        ChangeNodeActive(false);
        yield return null;
        ChangeNodeActive(false);
    }

    private void SetCharacterPose()
    {
        if (doubleWarp?.Avatar == null || vehicleInfo == null)
        {
            return;
        }
        if (doubleWarp.Avatar.transform.parent != null && vehicleInfo.doubleUserDetail != null)
        {
            doubleWarp.Avatar.transform.parent.localPosition = driverWarp.Avatar.transform.parent.localPosition - vehicleInfo.doubleUserDetail.pDef;
        }

        ApplyUgcPoseWithSeatPreserved(
            doubleWarp,
            vehicleInfo.doublePoseData,
            new Vector3(0, 0.5f, 0),
            vehicleInfo?.doubleUserDetail?.rDef
        );
    }

    private static void DisableAnimator(GameObject go)
    {
        if (go == null)
        {
            return;
        }
        var ani = go.GetComponent<Animator>();
        if (ani != null)
        {
            ani.enabled = false;
        }
    }

    /// <summary>
    /// 应用UGC姿势数据
    /// </summary>
    private void ApplyUgcPoseWithSeatPreserved(CharacterWrap wrap, string poseJson, Vector3 customAvatarBaseOffset, Vector3? seatEulerRotation)
    {
        if (wrap?.Avatar == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(poseJson))
        {
            return;
        }

        DisableAnimator(wrap.Avatar);

        var optNode = wrap.Avatar.transform.parent;
        var optLocalPos = optNode != null ? optNode.localPosition : Vector3.zero;
        var optLocalScale = optNode != null ? optNode.localScale : Vector3.one;

        var ikController = wrap.Avatar.GetComponent<AnimIKController>();
        if (ikController != null)
        {
            ikController.ChangeAnimResType(AnimResType.UGC);
            var frameData = JsonConvert.DeserializeObject<KeyFrameData>(poseJson);
            ikController.SetKeyFrameData(UgcPoseSubType.Single, frameData);
        }

        // 恢复姿势设置前的座位坐标（避免被 SetKeyFrameData 覆盖）
        if (optNode != null)
        {
            optNode.localPosition = optLocalPos;
            optNode.localScale = optLocalScale;
        }

        if (seatEulerRotation.HasValue)
        {
            var rot = Quaternion.Euler(seatEulerRotation.Value);
            if (wrap.Avatar.transform.parent != null)
            {
                wrap.Avatar.transform.parent.localRotation = rot;
            }
        }
    }

    private void ChangeEditMode()
    {
        if(isPlay)
        {
            GameController.ChangeMode(GameMode.Edit, () =>
            {
                ChangeNodeActive(true);
                heightSetCallBack?.Invoke(SetHeight);
                CloseSelf();
                UIManager.Inst.OpenPanel(PanelId.UGCVehicleEditPanel);
                UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
                kinematicCharacter.Motor.IsDriveVehicle = false;
                kinematicCharacter.Motor.CurUGCVehicleStatus = UGCVehicleStatus.None;
            });
        }
        else{
            GameController.ExitGame(() => {
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, true);
                UIManager.Inst.BackToLastWindow();
                //MessageHelper.Broadcast(DraftMessage.RefreshDraft);
            });
            //CloseSelf();
        }
    }
    
    private void OnSoundBtnDown()
    {
        isSoundBtnDown = true;
        soundTimer = 0f;
        if (vehicleInfo.vehicleAudio == null)
        {
            return;
        }
        var playingHornUrl = vehicleInfo.vehicleAudio.hornUrl;

        if (string.IsNullOrEmpty(playingHornUrl))
        {
            var audio = vehicleInfo.vehicleAudio.hornWwise;
            if(audio != null)
            {
                AkSoundManager.Inst.PlaySound(audio.group, audio.switchs, audio.wwise3P, kinematicCharacter.Motor.gameObject);
            }
        }
        else
        {
            if(audioPos == null)
            {
                audioPos = kinematicCharacter.Motor.transform.GetChild(0).gameObject;
            }
            AkSoundManager.Inst.StopUGCAudio(audioPos);
            AkSoundManager.Inst.PlayUGCAudioByUrl(playingHornUrl,false,audioPos);
        }
    }

    private void PlayHonkAudio()
    {
        if(vehicleInfo == null || vehicleInfo.vehicleAudio == null)
        {
            return;
        }
        var starUrl = vehicleInfo.vehicleAudio.starUrl;
        if (string.IsNullOrEmpty(starUrl))
        {
            var audio = vehicleInfo.vehicleAudio.starWwise;
            if(audio != null)
            {
                AkSoundManager.Inst.PlaySound(audio.group, audio.switchs, audio.wwise3P, kinematicCharacter.Motor.gameObject);
            }
        }
        else
        {
            if(audioPos == null)
            {
                audioPos = kinematicCharacter.Motor.transform.GetChild(0).gameObject;
            }
            AkSoundManager.Inst.StopUGCAudio(audioPos);
            AkSoundManager.Inst.PlayUGCAudioByUrl(starUrl,false,audioPos);
        }
        kinematicCharacter.Motor.SetVehicleHeight(vehicleInfo.vehicleHeight, true);
        dirveHeightBtn.gameObject.SetActive(isPlay);
    }

    private void OnVehicleDriverAudioPlay()
    {
        if(vehicleInfo == null || vehicleInfo.vehicleAudio == null)
        {
            return;
        }

        if (vehicleInfo.vehicleAudio != null)
        {
            if (!string.IsNullOrEmpty(vehicleInfo.vehicleAudio.starUrl) || !string.IsNullOrEmpty(vehicleInfo.vehicleAudio.hornUrl))
            {
                var audioPos = kinematicCharacter.Motor.transform.GetChild(0).gameObject;
                AkSoundManager.Inst.StopUGCAudio(audioPos.gameObject);
            }
        }

        playingDriveUrl = vehicleInfo.vehicleAudio.driveUrl;

        if (string.IsNullOrEmpty(playingDriveUrl))
        {
            var audio = vehicleInfo.vehicleAudio.driveWwise;
            if (audio != null)
            {
                AkSoundManager.Inst.PlaySound(audio.group, audio.switchs, audio.wwise3P, kinematicCharacter.Motor.gameObject);
            }
        }
        else
        {
            if(driveAudioPos == null && kinematicCharacter.PlayerAnimCtrl != null)
            {
                driveAudioPos = kinematicCharacter.PlayerAnimCtrl.gameObject;
            }
            AkSoundManager.Inst.StopUGCAudio(driveAudioPos);
            AkSoundManager.Inst.PlayUGCAudioByUrl(playingDriveUrl,true,driveAudioPos);
        }
    }

    private void OnVehicleDriverAudioStop()
    {
        if (vehicleInfo.vehicleAudio != null)
        {
            if (!string.IsNullOrEmpty(vehicleInfo.vehicleAudio.starUrl) || !string.IsNullOrEmpty(vehicleInfo.vehicleAudio.hornUrl))
            {
                var audioPos = kinematicCharacter.Motor.transform.GetChild(0).gameObject;
                AkSoundManager.Inst.StopUGCAudio(audioPos.gameObject);
            }
        }
        if (string.IsNullOrEmpty(vehicleInfo?.vehicleAudio?.driveUrl))
        {
            var audio = vehicleInfo.vehicleAudio?.driveWwise;
            if (audio != null)
            {
                AkSoundManager.Inst.StopSound(audio.stopWwise3P, kinematicCharacter.Motor.gameObject);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(playingDriveUrl) && playingDriveUrl == vehicleInfo.vehicleAudio.driveUrl)
            {
                AkSoundManager.Inst.StopUGCAudio(driveAudioPos);
            }
        }
    }

    private void OnDriveHeightClick()
    {
        heightSliderPos.gameObject.SetActive(!heightSliderPos.gameObject.activeSelf);
        heightSlider.value = vehicleInfo.vehicleHeight / 4f;// - CharacterPosY
    }

    Vector3 pos;
    private void ChangeVehicleHeight(float heightValue)
    {
        pos = kinematicCharacter.gameObject.transform.position;
        SetHeight =  heightValue * 4f;//CharacterPosY +
        kinematicCharacter.Motor.SetVehicleHeight(SetHeight, true);
        kinematicCharacter.Motor.SetPosition(new Vector3(pos.x, SetHeight, pos.z));
    }

    protected override void Update()
    {
        base.Update();
        if(isSoundBtnDown){
            // 玩家一直按着喇叭的情况，每隔一段时间广播一次鸣笛事件
            soundTimer += Time.deltaTime;
            if(soundTimer >= soundBroadcastInterval){
                soundTimer = 0f;
                MessageHelper.Broadcast(MessageName.OnVehicleTryHonking, AccountDataManager.Inst.UserInfo.uid);
            }
        }
    }

    private void ChangeNodeActive(bool isShow)
    {
        // 仅隐藏地图节点（StageParent 下的道具/形状等），避免误伤玩家节点（例如 dialogpos 下挂的载具/音效节点）
        bool ShouldToggleNode(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            var stageParent = SceneBuilder.Inst != null ? SceneBuilder.Inst.StageParent : null;
            if (stageParent == null)
            {
                // 无法确定地图根节点时，宁可不隐藏，避免误隐藏玩家节点
                return false;
            }

            return go.transform != null && go.transform.IsChildOf(stageParent);
        }

        var baseShapes = GamePropNodeManager.Inst.GetBehaviours<SimpleShapeBehaviour>();
        if(!baseShapes.IsNullOrEmpty())
        {
            for (var i = 0; i < baseShapes.Count; i++)
            {
                if (ShouldToggleNode(baseShapes[i].gameObject))
                {
                    baseShapes[i].gameObject.SetActive(isShow);
                }
            }
        }
        var previewNode = GamePropNodeManager.Inst.GetBehaviours<PreviewModelBehaviour>();
        if (!previewNode.IsNullOrEmpty())
        {
            for (var i = 0; i < previewNode.Count; i++)
            {
                if (ShouldToggleNode(previewNode[i].gameObject))
                {
                    previewNode[i].gameObject.SetActive(isShow);
                }
            }
        }
        var propNode = GamePropNodeManager.Inst.GetBehaviours<PropBehaviour>();
        if (!propNode.IsNullOrEmpty())
        {
            for (var i = 0; i < propNode.Count; i++)
            {
                if (ShouldToggleNode(propNode[i].gameObject))
                {
                    propNode[i].gameObject.SetActive(isShow);
                }
            }
        }
        var textNode = GamePropNodeManager.Inst.GetBehaviours<DTextBehaviour>();
        if (!textNode.IsNullOrEmpty())
        {
            for (var i = 0; i < textNode.Count; i++)
            {
                if (ShouldToggleNode(textNode[i].gameObject))
                {
                    textNode[i].gameObject.SetActive(isShow);
                }
            }
        }
        var combineNode = GamePropNodeManager.Inst.GetBehaviours<CombineBehaviour>();
        if (!combineNode.IsNullOrEmpty())
        {
            for (var i = 0; i < combineNode.Count; i++)
            {
                if (ShouldToggleNode(combineNode[i].gameObject))
                {
                    combineNode[i].gameObject.SetActive(isShow);
                }
            }
        }
    }

    private void ResetDriverInfo()
    {
        var tsf = kinematicCharacter.PlayerAnimCtrl.gameObject.transform;
        for(int i = 0; i < tsf.childCount; i++)
        {
            if(tsf.GetChild(i).name == "dialogpos" || tsf.GetChild(i).name == "rayUseVehicleObj" || tsf.GetChild(i).name == "vehiclepos")
            {
                continue;
            }
            tsf.GetChild(i).gameObject.SetActive(false);
        }
        var characterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var vehiclePos = tsf.Find("vehiclepos");
        if (vehiclePos == null)
        {
            LoggerUtils.LogError("ResetDriverInfo - vehiclepos 节点不存在");
            return;
        }
        driverWarp = AvatarController.Inst.CreateGameAvatarWithIKController(characterData, vehiclePos);
        var resType = UniqueType.GetAvatar(AvatarSubType.SpecialSkin);
        var adapter = driverWarp.GetPartAdapter(resType) as SpecialSkinPartAdapter;
        if(adapter != null)
        {
            adapter.GetCurrentLoadedObj()?.gameObject?.SetActive(false);
        }
        ResetDriverAnimator(driverWarp.Avatar);
        ApplyUgcPoseWithSeatPreserved(
            driverWarp,
            vehicleInfo?.curPoseData,
            new Vector3(0, 0.5f, 0),
            vehicleInfo?.detailInfo?.rDef
        );

        SetCharacterPose();
    }

    private void ResetDriverAnimator(GameObject avatar)
    {
        var ani = avatar.transform.parent?.parent?.GetComponent<Animator>();
        if(ani != null)
        {
            ani.enabled = false;
        }
    }

}
