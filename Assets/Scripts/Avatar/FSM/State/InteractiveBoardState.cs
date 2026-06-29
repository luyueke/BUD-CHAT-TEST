using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Es;
using FSM;
using Game.Audio;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using UGCAsset;
using UnityEngine;
using xasset;

public class InteractiveBoardState : PlayerStateTemplate<PlayerStateController> {
    private Transform mBoardTransform;

    #region PGC

    private List<EmoAniConfig> emoAniDataList;
    private List<GameObject> expressionGameObject;

    #endregion


    private uint mBoardId;
    private string mEmoteId;

    private bool isExiting = false; //标识是否已经发起退出请求，因为有退出动画，防止在退出动画播放过程中重复请求退出
    private bool isPgcEmote = false;


    public InteractiveBoardState(PlayerState id, PlayerStateController owner) : base(id, owner) {
    }

    public override void InitData(params object[] args) {
        mBoardId = (uint)args[0];
        mBoardTransform = (Transform)args[1];
        mEmoteId = (string)args[2];
        isPgcEmote = UniqueType.IsPgc(mEmoteId);
        if (isPgcEmote) {
            owner.emoteData = Es.DataTables.GetEmoUIConfig(mEmoteId);
            emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == mEmoteId);
        }
    }

    public override void ReleaseData() {
        base.ReleaseData();
        emoAniDataList?.Clear();
        emoAniDataList = null;
        if (owner != null)
        {
            owner.emoteData = null;
        }
    }


    public override void OnEnter() {
        //不执行base.OnEnter()，防止状态错误
        // base.OnEnter();
        isExiting = false;
        //关闭KCC相关
        owner.PlayerKCCtrl.SetFreezeCharacter(true);
        owner.PlayerKCCtrl.Motor.SetIsOnSimulate(false);
        MessageHelper.Broadcast(MessageName.InteractiveOnBoard, mBoardId, owner.transform);
        //调整人的面向为板子的面向
        PlayerFaceToBoard();
    }

    public override void EnterMainState() {
        base.EnterMainState();
        StateEventManager.Inst.RegisterStateEvent<float, float>(owner.PlayerID, StateEvent.MoveJoystick,
            OnMoveJoystick);
        StateEventManager.Inst.RegisterStateEvent(owner.PlayerID, StateEvent.JumpBtn, OnJumpClick);
    }

    public override void ExitMainState() {
        base.ExitMainState();
        StateEventManager.Inst.UnRegisterStateEvent<float, float>(owner.PlayerID, StateEvent.MoveJoystick,
            OnMoveJoystick);
        StateEventManager.Inst.UnRegisterStateEvent(owner.PlayerID, StateEvent.JumpBtn, OnJumpClick);
    }

    public override void OnExit() {
        base.OnExit();
        owner.PlayerKCCtrl.SetFreezeCharacter(false);
        owner.PlayerKCCtrl.Motor.SetIsOnSimulate(true);


        if (isPgcEmote) {
            owner.PlayerAnimCtrl.ResetEmoteAnimation();
            owner.PlayerAnimCtrl.ClearExpression(expressionGameObject);
        } else {
            var playerAnimIkController = owner.Wrap.Avatar.GetComponent<AnimIKController>();
            playerAnimIkController.StopAnim();
            playerAnimIkController.ChangeUgcToPgcAnim();
        }
        MessageHelper.Broadcast(MessageName.InteracriveDownBoard, owner.transform);
    }

    private void PlayPGCAnim() {
        if (emoAniDataList.Count == 1) {
            var emoAniConfig = emoAniDataList[0].ConvertToAniConfig();
            owner.PlayerAnimCtrl.PlayConfigAni(emoAniConfig, ExitState);
            expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);
        } else {
            PlayerAniConfig emoAniConfig;
            if (owner.emoteData.emoType == 1 || owner.emoteData.emoType == 2) {
                emoAniConfig = owner.PlayerAnimCtrl.PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(),
                    PlayAniType.SingleLoopStart, PlayAniType.SingleLooping, CreateEffect);
            } else {
                emoAniConfig = owner.PlayerAnimCtrl.PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(),
                    PlayAniType.DoubleStart, PlayAniType.DoubleLoop, CreateEffect);
            }

            expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);
        }
    }

    public override void CoexistState() {
        base.CoexistState();
        if (isPgcEmote) {
            PlayPGCAnim();
        } else {
            PlayUGCAnim();
        }
    }


    private void PlayUGCAnim() {
        UGCAnimAssetManager.Inst.GetAssetInfo<UGCAnimGetResponse>(mEmoteId, animInfo => {
            var playerController = owner.PlayerAnimCtrl;
            var playerAnimIkController = playerController.Wrap.Avatar.GetComponent<AnimIKController>();
            var playerAnimIk = playerController.Wrap.Avatar.GetComponent<AvatarAnimIK>();
            playerAnimIk.ResetDefaultPosition();
            List<AnimPropData> propList = new List<AnimPropData>();
            if (animInfo.propList != null && animInfo.propList.Count != 0) {
                foreach (var propData in animInfo.propList) {
                    AnimPropData animData = new AnimPropData() {
                        index = propData.index,
                        bindIndex = propData.bindIndex,
                        metaDataUrl = propData.metaDataUrl,
                        id = propData.id
                    };
                    propList.Add(animData);
                }
            }

            playerAnimIkController.RemoveOtherAnimIk();
            playerAnimIkController.ChangeAnimResType(AnimResType.UGC);
            playerAnimIkController.CreatePropIKs(UgcPoseSubType.Single, propList, false);
            DownloadAnim(animInfo.metaDataUrl, (frameData) => {
                if (frameData != null) {
                    playerAnimIkController.SetAnimFrameData(frameData);
                    if (animInfo.loop == 0) {
                        playerAnimIkController.PlayOnceAnim(ExitState);
                    } else {
                        playerAnimIkController.PlayLoopAnim();
                    }
                }
            });
        });
    }


    private void DownloadAnim(string url, Action<AnimFrameData> callback) {
        if (string.IsNullOrEmpty(url)) {
            LoggerUtils.LogError("url is null " + url);
            return;
        }

        var assetRequest = Asset.LoadRemoteAssetAsync(url);
        if (assetRequest != null) {
            assetRequest.completed += (_) => {
                if (assetRequest.result == Request.Result.Success) {
                    var content = System.Text.Encoding.UTF8.GetString(assetRequest.asset);
                    var frameData = JsonConvert.DeserializeObject<AnimFrameData>(content);
                    callback?.Invoke(frameData);
                } else {
                    callback?.Invoke(null);
                }
            };
        }
    }


    public override void DirectIntoState() {
        base.DirectIntoState();
        if (isPgcEmote) {
            PlayPGCAnim();
        } else {
            PlayUGCAnim();
        }
    }

    public override void InterruptState(PlayerState beState) {
        base.InterruptState(beState);

        if (owner.IsSelf) {
            //只有guest模式需要执行
            MessageHelper.Broadcast(MessageName.InteracriveInterrupt, true);
        }
    }


    public override void MainStateLateUpdate() {
        base.MainStateLateUpdate();
        if (owner == null || owner.transform == null || mBoardTransform == null) return;
        owner.PlayerKCCtrl.Motor.SetPosition(mBoardTransform.position);
        PlayerFaceToBoard();
    }

    //让角色同步板子面向
    private void PlayerFaceToBoard() {
        var orgAngle = owner.PlayerKCCtrl.Motor.transform.eulerAngles;
        orgAngle.y = mBoardTransform.eulerAngles.y;
        owner.PlayerKCCtrl.Motor.SetRotation(Quaternion.AngleAxis(orgAngle.y, Vector3.up));
    }

    private void OnRotateView(float offest) {
        owner.transform.Rotate(Vector3.up, -offest, Space.Self);
        owner.transform.parent.Rotate(Vector3.up, offest, Space.Self);
    }


    private void SelfCancelEmote() {
        if (owner.IsSelf && !isExiting) {
            MessageHelper.Broadcast(MessageName.InteracriveInterrupt, false);
            isExiting = true;
        }
        AkSoundManager.Inst.StopAll(owner.PlayerAnimCtrl.gameObject);
        ExitState();
    }

    private void OnMoveJoystick(float axisForward, float axisRight) {
        if (axisForward != 0 || axisRight != 0) {
            SelfCancelEmote();
        }
    }

    private void OnJumpClick() {
        SelfCancelEmote();
    }

    public override bool RepeatEntry(params object[] args) {
        if (args == null || args.Length == 0)
            return false;
        else
            return mBoardId == (uint)args[0];
    }


    #region 表情动作

    public override void PlayExitAnimation(Action<PlayerStateBase> onComplete) {
        base.PlayExitAnimation(onComplete);
        LoggerUtils.Log("###播放退出动画");
    }

    private List<GameObject> CreateEffect(PlayerAniConfig emoAniConfig) {
        owner.PlayerAnimCtrl.ClearExpression(expressionGameObject);
        expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);

        return expressionGameObject;
    }


    private void ExitState() {
        owner.ExitState(stateID);
    }


    // protected override bool IsLoop()
    // {
    //     return emoteInfo!=null && emoteInfo.noLoop == 1;
    // }

    #endregion
}
