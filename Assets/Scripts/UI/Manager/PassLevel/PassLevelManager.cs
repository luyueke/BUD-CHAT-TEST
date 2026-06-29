using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using Game.MapSetting;
using Game.Props.PropsManagers;
using GameData.BaseInfo;
using Message;
using Newtonsoft.Json;
using UI.Manager;
using UnityEngine;

/// <summary>
/// 通关&&通关结算管理
/// </summary>
public class PassLevelManager : GameInstance<PassLevelManager>, IAutoInit, IModeManager
{
    //通关结果处理器
    private PassLevelHandlerBase _handler;

    //通关总结果
    private PassLevelResult _curPassResult = PassLevelResult.NotPassed;


    //通关开始时间：注意这里的开始时间和倒计时开始时间还不一样，如果设置了倒计时,由于倒计时动画的存在，通关时间由倒计时接管
    private long PassLevelStartTime { get; set; }
    private Dictionary<string, IPassLevelMgr> _mgrs = new Dictionary<string, IPassLevelMgr>();
    private PassLevelData _passLevelData;
    public PassLevelData PassLevelData => _passLevelData;

    public void Init()
    {
        MessageHelper.AddListener(MessageName.PassLevelJudging, DoJudging);
        RegisterManager();
        InitDataListener();
    }

    public override void Release()
    {
        RecoverBgmVolume();
        MessageHelper.RemoveListener(MessageName.PassLevelJudging, DoJudging);
        base.Release();
        ResetAllData();
        ClearAllRegisterManager();
    }

    private void RegisterManager()
    {
        _mgrs ??= new Dictionary<string, IPassLevelMgr>();
        // _mgrs.TryAdd(nameof(GameDurationManager), GameDurationManager.Inst);
        _mgrs.TryAdd(nameof(WinConditionManager), WinConditionManager.Inst);
        // _mgrs.TryAdd(nameof(PlayerHpManager), PlayerHpManager.Inst);
    }

    private void ClearAllRegisterManager()
    {
        _mgrs?.Clear();
    }

    private void ResetAllData(bool isRelease = false)
    {
        PassLevelStartTime = 0;
        //防止在Manager 里 Release 时再创建了Manager
        if (isRelease)
        {
            if (PassLevelDataManager.HasInstance)
            {
                PassLevelDataManager.Inst.CurStatus = PassLevelStatus.NotStart;
            }
        }
        else
        {
            PassLevelDataManager.Inst.CurStatus = PassLevelStatus.NotStart;
        }

        _curPassResult = PassLevelResult.NotPassed;
        _handler = null;
        _passLevelData = null;
        RecoverBgmVolume();
    }

    public int GetCurPassLevelTime()
    {
        return _passLevelData?.CostTime ?? 0;
    }

    public PassLevelStatus GetCurPassLevelStatus()
    {
        return PassLevelDataManager.Inst.CurStatus;
    }

    // 这里通关时间计算需要区分是否有倒计时功能，因为倒计时自己有动画效果，倒计时模块控制真正开始计时的时机
    private int GetPassLevelBasicTime()
    {
        if (PassLevelStartTime > 0)
        {
            var basicCost = (int)(GameUtils.GetTimeStamp() - PassLevelStartTime);
            LoggerUtils.Log($"PassLevelManager: GetPassLevelBasicTime is {basicCost}");
            return basicCost;
        }

        return 0;
    }

    #region message监听

    private void DoJudging()
    {
        if (PassLevelDataManager.Inst.CurStatus != PassLevelStatus.Running) return;
        // if (GameManager.Inst.loadingPageIsClosed == false) return;

        foreach (var m in _mgrs.Values)
        {
            if (m == null)
            {
                continue;
            }

            _curPassResult = m.CountPassLevelResult();
            if (_curPassResult == PassLevelResult.NotPassed)
            {
                continue;
            }

            // 触发了成功/失败，进行结算
            SavePassLevelData();
            StopPassLevel();
            LoggerUtils.Log($"PassLevelManager DoJudging , passLevelData:{JsonConvert.SerializeObject(_passLevelData)}");
            GlobalCoroutineUtils.Inst.StartCoroutine(HandleJudgingAfterFrameEnd());
            break;
        }
    }

    private IEnumerator HandleJudgingAfterFrameEnd()
    {
        yield return null;
        _handler = PassLevelHandlerFactory.CreateHandler();
        _handler?.HandleResult(_curPassResult);
    }

    private void SavePassLevelData()
    {
        _passLevelData = new PassLevelData
        {
            Result = _curPassResult,
            CostTime = GetPassLevelBasicTime()
        };
        foreach (var mgr in _mgrs.Values)
        {
            mgr?.SavePassLevelData(ref _passLevelData);
        }
    }

    #endregion

    #region PlayAgain/Exit

    /// <summary>
    /// 需求：
    /// 单机房：
    /// try again - 道具重置，单机房，不涉及重新分配联机房或者不能重置的逻辑
    /// 联机房（正常的ugc游玩）
    /// try again：只是重置出生点，重置时间，道具不重置，也不需要重新进房
    /// </summary>
    public void PlayAgainOnline()
    {
        // if (Global.Room == null) return;
        // UploadExperienceCount();
        // //联机房
        // ForceStateFix();
        // ResetAllData();
        // if (PlayModePanel.Instance) PlayModePanel.Instance.OnRetryBtnClick();
        // TimerManager.Inst.RunOnce(nameof(PlayAgainOnline), 1.5f, () =>
        // {
        //     if (BlackPanel.Instance != null)
        //     {
        //         BlackPanel.Hide();
        //     }
        //     StartPassLevel();
        // });

        //todo:fsc 临时回出生点
        var spManager = GlobalNodeManager.Inst.Get<SpawnPointManager>();
        var pos = spManager.GetDefaultSpawnPoint();
        var rotation = spManager.GetDefaultSpawnRotation();
        var kinematicCharacter = AvatarController.Inst.SelfController;
        kinematicCharacter.Motor.SetPositionAndRotation(pos, rotation);
        MobileJoystick.Inst.OnResetJoystick();
        kinematicCharacter.PlayerAnimCtrl.ResetEmoteAnimation();

        //重启通关
        ResetAllData();
        StartPassLevel();
    }

    public void PlayAgainOffline(bool shouldUploadExCount = false)
    {
        LoggerUtils.Log("PassLevelManager:PlayAgainOffline");
        ResetAllData();
        if (shouldUploadExCount)
        {
            UploadExperienceCount();
        }

        // SceneSkipManager.Instance.ReEnterMapOfflineModeIn3DScene();
        GameController.ReEnterGame();
    }

    // public void PlayNextLevel(MapPublishTestModeInfo mInfo)
    // {
// #if UNITY_EDITOR
//         LoggerUtils.Log($"PassLevelManager:PlayNextLevel--{JsonConvert.SerializeObject(mInfo)}");
// #endif
//         ResetAllData();
//         UploadExperienceCount(new MapInfo()
//         {
//             mapId = mInfo.mapId,
//             dataType = 0,
//             dataSubType = 0
//         });
//         SceneSkipManager.Instance.EnterNextMapOfflineModeIn3DScene(mInfo);
    // }

    //调接口，增加游玩次数
    private void UploadExperienceCount(MapInfo mInfo = null)
    {
        // var mapInfo = mInfo;
        // if (mInfo == null)
        // {
        //     mapInfo = GlobalFieldController.CurMapInfo;
        // }
        //
        // if (mapInfo != null)
        // {
        //     MapLoadManager.Inst.SetExperenceView(mapInfo, 0);
        // }
    }

    //play again或者完成通关时之前打断各种状态
    public void ForceStateFix(Action callback = null, Action leaveTriggerAct = null)
    {
        // var delayTime = 0f;
        // if (StateManager.IsFishing)
        // {
        //     FishingManager.Inst.ForceStopFishing();
        // }
        //
        // if (StateManager.IsFlying)
        // {
        //     FlyManager.Inst.StopFly();
        // }
        //
        // if (CameraModeManager.Inst.GetCurrentCameraMode() == CameraModeEnum.FreePhotoCamera)
        // {
        //     leaveTriggerAct?.Invoke();
        //     TimerManager.Inst.RunOnce(nameof(leaveTriggerAct), 0.1f,
        //         () => { CameraModeManager.Inst.EnterMode(CameraModeEnum.NormalGuestCamera); });
        //     delayTime = 1f;
        // }
        //
        // //kingdom才有 不处理
        // // if (StateManager.IsInWardrobe)
        // // {
        // // }
        //
        // if (StateManager.IsInAddFriendEmo)
        // {
        //     PlayerEmojiControl.Inst.OnReset();
        // }
        //
        // if (StateManager.IsInScooter)
        // {
        //     ScooterManager.Inst.ForceInterrupt();
        // }
        //
        // var selfPlayer = PlayerManager.Inst.selfPlayer;
        //
        // if (PlayerBaseControl.Inst != null && PlayerBaseControl.Inst.GetNoAbilityFlag(EObjAbilityType.Move))
        // {
        //     PlayerBaseControl.Inst.ClearNoAbilityFlag(EObjAbilityType.Move);
        // }
        //
        // //关闭氧气UI，退出氧气计时器
        // if (OxygenManager.Inst.isOpenOxygen)
        // {
        //     OxygenPanel.Hide();
        //     OxygenManager.Inst.ForceCloseOxygen();
        // }
        //
        // if (delayTime > 0)
        // {
        //     TimerManager.Inst.RunOnce(nameof(delayTime), delayTime, callback);
        // }
        // else
        // {
        //     callback?.Invoke();
        // }
    }

    //相机模式退出时，处于trigger碰撞中无法正常退出相机模式（销毁ui时destroyimmedily报错），这里先离开碰撞
    public void PlayLeaveFlag()
    {
        // var selfPlayer = PlayerManager.Inst.selfPlayer;
        // var curPos = selfPlayer.GetPlayerPosition();
        // var backDis = 0.5f;
        // var newPos = new Vector3(curPos.x - backDis, curPos.y, curPos.z - backDis);
        // selfPlayer.SetPlayerPositionAndRotation(newPos, selfPlayer.GetPlayerRotation());
    }

    public void ExitCurrentMap(Action callback = null)
    {
        StopPassLevel();
        GameController.ExitGame(() =>
        {
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
            UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.BackToLastWindow();
            callback?.Invoke();
        });
    }

    #endregion

    #region 上报通关结果，并拿到奖励数据

    public void SendPassLevelDataReq(Action<PassLevelRewardInfo> onSuccess, Action<string> onFailed)
    {
        // if (_passLevelData == null)
        // {
        //     LoggerUtils.LogError("Do not pass this level !! _passLevelData is null!");
        //     return;
        // }
        //
        //
        // var data = new ConclusionReqData()
        // {
        //     mapId = GlobalFieldController.CurMapInfo.mapId,
        //     playDuration = GetCurPassLevelTime(),
        //     isWin = _curPassResult == PassLevelResult.Passed ? 1 : 0,
        //     winType = _passLevelData.PassedType,
        //     failType = _passLevelData.FailedType,
        //     entranceType = GetEntranceTypeData(),
        // };
        // if (_handler is PassLevelGameMixerHandler )
        // {
        //     data.mixerVersion = GameManager.Inst.gameMixerInfo.baseInfo.mixerVersion;
        //     data.mixerId = GameManager.Inst.gameMixerInfo.baseInfo.mixerId;
        // }
        // //重试三次,超时五秒
        // HttpUtils.MakeHttpRequestWithRetry("/ugcmap/conclusion", (int)HTTP_METHOD.POST,
        //     JsonConvert.SerializeObject(data),
        //     (content) =>
        //     {
        //         LoggerUtils.Log($"/ugcmap/conclusion success=>{content}");
        //         HttpResponDataStruct hResponse = JsonConvert.DeserializeObject<HttpResponDataStruct>(content);
        //         PassLevelRewardInfo rewardInfo = JsonConvert.DeserializeObject<PassLevelRewardInfo>(hResponse.data);
        //
        //         onSuccess?.Invoke(rewardInfo);
        //     },
        //     (error) =>
        //     {
        //         LoggerUtils.Log($"/ugcmap/conclusion failed=>{error}");
        //         onFailed?.Invoke(error);
        //     }, timeOut: 5.0f
        // );
    }

    public class PassLevelRewardInfo
    {
        public List<PassLevelRewardInfoItem> rewards = new List<PassLevelRewardInfoItem>();
        public int isFirstWin;
    }

    public class PassLevelRewardInfoItem
    {
        public int type; //0exp，1badge，2coin
        public int amount;
    }

    public enum PassLevelRewardType
    {
        Exp = 0,
        Badge = 1,
        Coin = 2,
    }

    private int GetEntranceTypeData()
    {
        // if (_handler is PassLevelDailyChallengeHandler && !PlayerManager.Inst.IsTransported())
        // {
        //     return 1;
        // }
        // if (_handler is PassLevelGameMixerHandler)
        // {
        //     return 2;
        // }
        //
        // if (_handler is PassLevelWeekChallengeHandler && !PlayerManager.Inst.IsTransported())
        // {
        //     return 3;
        // }

        return 0;
    }

    #endregion

    #region 通关入口/出口

    public void OnEdit()
    {
        StopPassLevel(true);
    }

    public void OnPlay()
    {
        //todo:fsc 判断第一帧关闭后才开始
        StartPassLevel();
    }

    public void OnGuest()
    {
        //todo:fsc 判断第一帧关闭后才开始
        StartPassLevel();
    }

    public void StartPassLevel()
    {
        // LoggerUtils.Log("PassLevel: StartPassLevel");
        if (PassLevelDataManager.Inst.CurStatus != PassLevelStatus.NotStart) return;
        GlobalCoroutineUtils.Inst.StartCoroutine(DoStartPassLevel());
    }

    private IEnumerator DoStartPassLevel()
    {
        yield return null;

        PassLevelStartTime = GameUtils.GetTimeStamp();
        foreach (var m in _mgrs.Values)
        {
            m?.OnPassLevelStart();
        }

        PassLevelDataManager.Inst.CurStatus = PassLevelStatus.Running;
    }

    public void StopPassLevel(bool isClearData = false)
    {
        // LoggerUtils.Log("PassLevel: StopPassLevel");
        if (PassLevelDataManager.Inst.CurStatus == PassLevelStatus.NotStart) return;

        foreach (var m in _mgrs.Values)
        {
            m?.OnPassLevelStop();
        }

        PassLevelDataManager.Inst.CurStatus = PassLevelStatus.NotStart;
        if (isClearData) ResetAllData();
    }

    public void PausePassLevel()
    {
        // LoggerUtils.Log("PassLevel: PausePassLevel");
        if (PassLevelDataManager.Inst.CurStatus != PassLevelStatus.Running) return;
        foreach (var m in _mgrs.Values)
        {
            m?.OnPassLevelPause();
        }

        PassLevelDataManager.Inst.CurStatus = PassLevelStatus.Wait;
    }

    public void ContinuePassLevel()
    {
        // LoggerUtils.Log("PassLevel: ContinuePassLevel");
        if (PassLevelDataManager.Inst.CurStatus != PassLevelStatus.Wait) return;

        foreach (var m in _mgrs.Values)
        {
            m?.OnPassLevelContinue();
        }

        PassLevelDataManager.Inst.CurStatus = PassLevelStatus.Running;
    }

    #endregion

    #region 音效控制

    private float originAudioVolume;
    private readonly float WEAK_VOLUME_FACTOR = 0.3f;

    public void WeakBgmVolume()
    {
        // if (originAudioVolume <= 0f)
        // {
        //     originAudioVolume = SceneBuilder.Inst.BgBehaviour.GetAudioVolume();
        //     SceneBuilder.Inst.BgBehaviour.SetAudioVolume(originAudioVolume * WEAK_VOLUME_FACTOR);
        // }
    }

    public void RecoverBgmVolume()
    {
        // if (originAudioVolume > 0)
        // {
        //     SceneBuilder.Inst.BgBehaviour.SetAudioVolume(originAudioVolume);
        //     originAudioVolume = 0f;
        // }
    }

    #endregion

    #region 数据接口

    private void InitDataListener()
    {
        PassLevelDataManager.Inst.OnMapDataInit = () =>
        {
            var cmpData = PassLevelDataManager.Inst.GetComponentData();
            foreach (var m in _mgrs.Values)
            {
                m?.OnParseComponentData(cmpData);
            }
        };
    }

    #endregion
}

public enum PassLevelResult
{
    NotPassed, //注意这里notPass不一定意味着failed
    Passed,
    Failed
}

//todo:fsc 移植其他数据到DataMgr


public class ConclusionReqData
{
    public string mapId;
    public int playDuration; //通关时长
    public int isWin; //是否胜利
    public int winType; //胜利类型 1:end flag
    public int failType; //失败类型 1:time out
    public int entranceType; //入口类型 0 普通入口 1 flash five 2 gamemixer
    public int mixerVersion; //合集版本
    public string mixerId; //合集id
}
