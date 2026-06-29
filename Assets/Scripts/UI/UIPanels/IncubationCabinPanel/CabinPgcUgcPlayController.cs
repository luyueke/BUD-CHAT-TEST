using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.KinematicCharacter;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Game.Store;
using UnityEngine;
using xasset;

public class CabinPgcUgcPlayController
{

    [HideInInspector] public PlayerAnimationCtrl PlayerAnimCtrl;
    [HideInInspector] public KinematicCharacterController PlayerKCCtrl;
    [HideInInspector] public AvatarCameraController AvatarCameraCtrl;

    AnimIKController animationCtrlIK;
    public CharacterWrap Wrap;


    private Transform mBoardTransform;

    #region PGC

    private List<EmoAniConfig> emoAniDataList;
    private List<GameObject> expressionGameObject;

    pEmoteData _data;

    #endregion

    EmoUIConfig emoteUIConfig;

    Action _playEndCallback; //动画播放完成回调 只针对非循环动画
    private string mEmoteId;

    private bool isPgcEmote = false;
    // "leisure"/"default" 等无配置表记录的内置待机 ID，需要走特殊播放分支
    private string _specialIdleId;
    // 每次调用 PlayAnim() 时自增，用于丢弃旧的异步下载回调，防止覆盖新播放请求
    private int _playGeneration = 0;
    public void Init(PlayerAnimationCtrl playerAnimationCtrl, CharacterWrap wrap, KinematicCharacterController controller, AvatarCameraController avatarCameraController)
    {
        PlayerAnimCtrl = playerAnimationCtrl;
        Wrap = wrap;
        PlayerKCCtrl = controller;
        AvatarCameraCtrl = avatarCameraController;
        animationCtrlIK = PlayerAnimCtrl.Wrap.Avatar.GetComponent<AnimIKController>();
        // 孵化舱预览始终使用 1P 音效，避免走未配置的 3P Wwise 事件
        PlayerAnimCtrl.SetPlayerID(AccountDataManager.Inst.Uid);
    }


    public void InitData(pEmoteData _pEmoteData)
    {
        _specialIdleId = null;
        if (_pEmoteData.emoteId == "leisure" || _pEmoteData.emoteId == "default")
        {
            _specialIdleId = _pEmoteData.emoteId;
            return;
        }
        isPgcEmote = UniqueType.IsPgc(_pEmoteData.emoteId);
        if (isPgcEmote)
        {
            emoteUIConfig = Es.DataTables.GetEmoUIConfig(_pEmoteData.emoteId);
            emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == _pEmoteData.emoteId);
        }
        else
        {
            mEmoteId = _pEmoteData.ugcData?.id;
        }
    }

    public void InitData(characterInteraction interaction)
    {
        // 防止上一次待机动画设置的 _specialIdleId 残留，导致 PlayAnim() 误走 PlaySpecialIdleAnim() 分支
        _specialIdleId = null;
        isPgcEmote = interaction.isPgc == 1;
        if (isPgcEmote)
        {
            emoteUIConfig = Es.DataTables.GetEmoUIConfig(interaction.emoteId);
            emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == interaction.emoteId);
        }
        else
        {
            mEmoteId = interaction.ugcData?.id;
        }
    }

    public void InitData(voiceCommands vCommands)
    {
        // 防止上一次待机动画设置的 _specialIdleId 残留，导致 PlayAnim() 误走 PlaySpecialIdleAnim() 分支
        _specialIdleId = null;
        isPgcEmote = vCommands.isPgc == 1;
        if (isPgcEmote)
        {
            emoteUIConfig = Es.DataTables.GetEmoUIConfig(vCommands.emoteId);
            emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == vCommands.emoteId);
        }
        else
        {
            mEmoteId = vCommands.ugcData?.id;
        }
    }

    public void SetPlayEndCallback(Action callback)
    {
        _playEndCallback = callback;
    }

    public void CancelAnim()
    {
        // animationCtrlIK 在 Init() 前为 null，未初始化时无需清理，直接返回
        if (animationCtrlIK == null)
            return;

        PlayerAnimCtrl?.ClearExpression(expressionGameObject);

        AvatarCameraCtrl?.ResetEmoteView();
        AvatarCameraCtrl?.SetCameraZoom(0);

        PlayerAnimCtrl?.ResetEmoteForUICharacter();
        animationCtrlIK.StopAnimAndResetJointNode();
        animationCtrlIK.ChangeAnimResType(GameData.BaseInfo.AnimResType.PGC);
        ResetIKPosition();

        _playEndCallback = null;
    }

    /// <summary>
    /// 切换角色到 leisure 待机状态，用于动画播完但语音仍在播放时的过渡。
    /// 与 CancelAnim() 的区别：不重置相机视角，保持预览层可见。
    /// </summary>
    public void PlayLeisureIdle()
    {
        // animationCtrlIK 在 Init() 前为 null，未初始化时无需操作
        if (animationCtrlIK == null)
            return;

        PlayerAnimCtrl?.ClearExpression(expressionGameObject);
        animationCtrlIK.StopAnimAndResetJointNode();
        animationCtrlIK.ChangeAnimResType(AnimResType.PGC);
        ResetAvatarNodeTransform();
        PlayerAnimCtrl?.SetPlayerState(PlayerState.Default);
        PlayerAnimCtrl?.ResetEmoteForUICharacter();
        _playEndCallback = null;
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


    }

    Vector3 defPos = new Vector3(0, 0.5f, 0);
    private void ResetAvatarNodeTransform()
    {
        if(PlayerAnimCtrl == null)
        {
            return;
        }
        var avatarNode = PlayerAnimCtrl?.transform.parent;

        if (avatarNode == null)
            return;

        avatarNode.localPosition = defPos;
        avatarNode.localRotation = Quaternion.identity;
        avatarNode.localScale = Vector3.one;
    }

    private void PlayPGCAnim(bool isPlaySound = true)
    {
        ResetAvatarNodeTransform();
        animationCtrlIK.StopAnimAndResetJointNode();
        animationCtrlIK.ChangeAnimResType(AnimResType.PGC);

        PlayerAnimCtrl?.ClearExpression(expressionGameObject);
        if (emoAniDataList.Count == 1)
        {
            var emoAniConfig = emoAniDataList[0].ConvertToAniConfig();
            PlayerAnimCtrl?.PlayConfigAni(emoAniConfig,()=>
            {
                _playEndCallback?.Invoke();
                PlayerAnimCtrl?.ClearExpression(expressionGameObject);
            }, isPlaySound);
            expressionGameObject = PlayerAnimCtrl.CreateExpression(emoAniConfig);
        }
        else
        {
            PlayerAniConfig emoAniConfig;
            if (emoteUIConfig.emoType == 1 || emoteUIConfig.emoType == 2)
            {
                emoAniConfig = PlayerAnimCtrl?.PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(),
                    PlayAniType.SingleLoopStart, PlayAniType.SingleLooping, CreateEffect, isPlaySound);
            }
            else
            {
                emoAniConfig = PlayerAnimCtrl?.PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(),
                    PlayAniType.DoubleStart, PlayAniType.DoubleLoop, CreateEffect, isPlaySound);
            }

            expressionGameObject = PlayerAnimCtrl?.CreateExpression(emoAniConfig);
        }
    }

    public void PlayAnim(bool isPlaySound = true)
    {
        _playGeneration++;
        int gen = _playGeneration;

        if (_specialIdleId != null)
        {
            PlaySpecialIdleAnim();
            return;
        }
        if (isPgcEmote)
        {
            if (emoAniDataList == null || emoAniDataList.Count == 0)
            {
                _playEndCallback?.Invoke();
                return;
            }
            var emoteId = emoAniDataList[0].emoId;
            PlayerAnimCtrl?.DownloadAnimationAB(emoteId, (success) =>
            {
                if (gen != _playGeneration)
                    return;

                if (!success)
                {
                    LoggerUtils.LogError($"动画预览下载失败: {emoteId}");
                    _playEndCallback?.Invoke();
                    return;
                }
                PlayPGCAnim(isPlaySound);
            });
        }
        else
        {
            PlayUGCAnim(gen, isPlaySound);
        }
    }


    // leisure/default 无配置表记录，直接设置 PlayerState 并重置表情动画
    private void PlaySpecialIdleAnim()
    {
        ResetAvatarNodeTransform();
        PlayerAnimCtrl?.ClearExpression(expressionGameObject);
        animationCtrlIK.StopAnimAndResetJointNode();
        animationCtrlIK.ChangeAnimResType(AnimResType.PGC);
        if (_specialIdleId == "leisure")
        {
            PlayerAnimCtrl?.SetPlayerState(PlayerState.Leisure);
        }
        else
        {
            PlayerAnimCtrl?.SetPlayerState(PlayerState.Default);
        }
        PlayerAnimCtrl?.ResetEmoteForUICharacter();
    }

    private void PlayUGCAnim(int gen, bool isPlaySound = true)
    {
        if (string.IsNullOrEmpty(mEmoteId) || PlayerAnimCtrl == null) return;
        PlayerAnimCtrl.ClearExpression(expressionGameObject);
        AssetsDataManager.GetUgcAnimInfo(mEmoteId, (success, data) =>
        {
            if (gen != _playGeneration)
                return;

            var animInfo = data?.UgcInfo as AnimInfo;
            if (animInfo == null)
            {
                _playEndCallback?.Invoke();
                return;
            }
            var playerController = PlayerAnimCtrl;
            var playerAnimIkController = playerController.Wrap.Avatar.GetComponent<AnimIKController>();
            var playerAnimIk = playerController.Wrap.Avatar.GetComponent<AvatarAnimIK>();
            playerAnimIk.ResetDefaultPosition();
            List<AnimPropData> propList = new List<AnimPropData>();
            if (animInfo.propList != null && animInfo.propList.Count != 0)
            {
                foreach (var propData in animInfo.propList)
                {
                    AnimPropData animData = new AnimPropData()
                    {
                        index = propData.index,
                        bindIndex = propData.bindIndex,
                        metaDataUrl = propData.metaDataUrl,
                        id = propData.id
                    };
                    propList.Add(animData);
                }
            }

            playerAnimIkController.StopAnimAndResetJointNode();
            playerAnimIkController.RemoveOtherAnimIk();
            playerAnimIkController.ChangeAnimResType(AnimResType.UGC);
            playerAnimIkController.CreatePropIKs(UgcPoseSubType.Single, propList, false);
            DownloadAnim(animInfo.metaDataUrl, (frameData) =>
            {
                if (gen != _playGeneration)
                    return;

                if (frameData != null)
                {
                    playerAnimIkController.SetAnimFrameData(frameData);
                    if (animInfo.loop == 0)
                    {
                        playerAnimIkController.PlayOnceAnim(()=>
                        {
                            PlayerAnimCtrl?.ClearExpression(expressionGameObject);
                            _playEndCallback?.Invoke();
                        }, isPlaySound);
                    }
                    else
                    {
                        playerAnimIkController.PlayLoopAnim(null, isPlaySound);
                    }
                }
                else
                {
                    _playEndCallback?.Invoke();
                }
            });
        });
    }


    private void DownloadAnim(string url, Action<AnimFrameData> callback)
    {
        if (string.IsNullOrEmpty(url))
        {
            LoggerUtils.LogError("url is null " + url);
            return;
        }

        var assetRequest = Asset.LoadRemoteAssetAsync(url);
        if (assetRequest != null)
        {
            assetRequest.completed += (_) =>
            {
                if (assetRequest.result == Request.Result.Success)
                {
                    var content = System.Text.Encoding.UTF8.GetString(assetRequest.asset);
                    var frameData = JsonConvert.DeserializeObject<AnimFrameData>(content);
                    callback?.Invoke(frameData);
                }
                else
                {
                    callback?.Invoke(null);
                }
            };
        }
    }





    #region 动画时长查询

    public void FetchAnimMaxDelaySecond(characterInteraction data, Action<int> callback)
    {
        if (data.isPgc == 1)
        {
            FetchPGCAnimMaxDelay(data.emoteId, callback);
        }
        else
        {
            FetchUGCAnimMaxDelay(data.ugcData, callback);
        }
    }

    public void FetchAnimMaxDelaySecond(voiceCommands data, Action<int> callback)
    {
        if (data.isPgc == 1)
        {
            FetchPGCAnimMaxDelay(data.emoteId, callback);
        }
        else
        {
            FetchUGCAnimMaxDelay(data.ugcData, callback);
        }
    }

    private void FetchPGCAnimMaxDelay(string emoteId, Action<int> callback)
    {
        if (PlayerAnimCtrl == null)
        {
            callback(-1);
            return;
        }

        var configs = DataTables.GetEmoAniConfigList()
            .FindAll(c => c.emoId == emoteId);

        if (configs == null || configs.Count != 1)
        {
            callback(-1);
            return;
        }

        PlayerAnimCtrl.GetBodyAnimDurationAsync(emoteId, duration =>
        {
            callback(duration > 0f ? Mathf.FloorToInt(duration) : -1);
        });
    }

    private void FetchUGCAnimMaxDelay(UgcIdleData ugcData, Action<int> callback)
    {
        if (ugcData == null || string.IsNullOrEmpty(ugcData.id))
        {
            callback(-1);
            return;
        }

        AssetsDataManager.GetUgcAnimInfo(ugcData.id, (success, data) =>
        {
            var animInfo = data?.UgcInfo as AnimInfo;
            if (animInfo == null || animInfo.loop != 0)
            {
                callback(-1);
                return;
            }

            DownloadAnim(animInfo.metaDataUrl, frameData =>
            {
                if (frameData == null || frameData.framefrequency <= 0)
                {
                    callback(-1);
                    return;
                }

                float duration = frameData.animFrames.Count / (float)frameData.framefrequency;
                callback(Mathf.FloorToInt(duration));
            });
        });
    }

    #endregion

    #region 表情动作


    private List<GameObject> CreateEffect(PlayerAniConfig emoAniConfig)
    {
        PlayerAnimCtrl?.ClearExpression(expressionGameObject);
        expressionGameObject = PlayerAnimCtrl?.CreateExpression(emoAniConfig);

        return expressionGameObject;
    }



    // protected override bool IsLoop()
    // {
    //     return emoteInfo!=null && emoteInfo.noLoop == 1;
    // }

    #endregion
}
