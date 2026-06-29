using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Basic.Utils;
using Game.Avatar;
using BUD.AnimPose;
using Es;
using Game.Audio;
using Game.Base;
using Game.Config;
using Game.Utils;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
using RTG;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

public class AnimationStudioEditCoverPanel : BaseAnimEditPanel
{
    [Header("Character")] 
    public UIDragUtil DragUtil;
    public Transform Trans_PreviewRoot;

    [Header("UI操作")] 
    public LoadingButton Btn_ConfirmEditCover;
    public Text Txt_CurTime;
    public Text Txt_TotalTime;

    [Header("时间轴操作相关")] 
    public TimeLineController TimeLineCtr;
    #region Datas
    private const string TAG = "AnimationStudioEditPanel";
    #endregion
    
    //当前选中的关键帧
    private KeyFrameItem _curSelectKeyFrameItem;
    private List<KeyFrameItem> _curKeyFrameItems = new List<KeyFrameItem>();

    private bool IsPublishViewEnter = false;
    
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        GameTimeUtils.Inst.StartCollect(TAG);

        _curAnimInfo = (AnimInfo)args[0];
        _animFrameData = (AnimFrameData)args[1];
        if (args.Length >= 3)
        {
            IsPublishViewEnter = (bool)args[2];
        }
        StartPreview();
        InitWidget();
        SetShotCamPos();
        Cam_ShotCam.enabled = true;
    }

    public override void OnHidden()
    {
        base.OnHidden();
        var panel = UIManager.Inst.FindPanel<AnimationStudioEditPanel>(PanelId.AnimationStudioEditPanel);
        if (panel != null)
        {
            panel.ReShowPanel();
        }
        creater.Release();
        GameObject.Destroy(animNode);

        if (IsPublishViewEnter)
        {
            MessageHelper.Broadcast(DraftMessage.RefreshDraft);
        }
    }

    public override void StartPreview()
    {
        base.StartPreview();
        var selfData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var petData = AccountDataManager.Inst.PetInfo.avatarInfo;
        if (animNode == null)
        {
            var poseData = DataTables.GetPoseModeConfig(_curAnimInfo.animType);
            animNode = new GameObject("animNode");
            animNode.transform.localPosition = poseData.UIParentPos;
        }
        creater = creater.Create((UgcPoseSubType)_curAnimInfo.animType, animNode.transform);
        creater.CreateWhiteRole(this.gameObject);
        creater.CreateRole(selfData, petData);
        
        creater.curImageMode = PoseImageType.WhiteBody;
        creater.ChangeImageModeAction(PoseImageType.WhiteBody);
        
        _animIKController = creater.GetCurrentIkController();
        _animIKController.ChangeAnimResType(AnimResType.UGC);
        _animIKController.dataManager.SetAnimFrameData(_animFrameData);
        
        _animIKController.ChangeShotMaterial(true);
        
        Cam_ShotCam.transform.SetParent(animNode.transform);
    }

    public override void InitWidget()
    {
        base.InitWidget();
        AddListener();
        InitController();
        SelectEnterFrameIndex();
    }

    private void SelectEnterFrameIndex()
    {
        var enterItem = _curKeyFrameItems[0];
        _animIKController.CreatePropIKs((UgcPoseSubType)_curAnimInfo.animType,_curAnimInfo.propList);
        TimeLineCtr.OnKeyFrameItemSelect(enterItem);
        TimeLineCtr.RefreshKeyFrameItemStateByData(_animFrameData);
    }

    private void InitController()
    {
        TimeLineCtr.SetAction(OnSnapItem, OnSelectKeyFrame, OnSetAnimtion, OnStopAction);
        _curKeyFrameItems = TimeLineCtr.CreateTimeLine(_animFrameData);
    }

    private void AddListener()
    {
        Btn_ConfirmEditCover.onClick.AddListener(OnConfirmEditCoverClick);
    }
    
    private void OnConfirmEditCoverClick()
    {
        Btn_ConfirmEditCover.SetLoadingVisible(true);
        SaveUgcAnimationData((isSuccess) =>
        {
            TipPanel.ShowToast(isSuccess ? "保存成功:D" : "保存失败");
            Btn_ConfirmEditCover.SetLoadingVisible(false);
            CloseSelf();
        });
    }

    private void RefreshOPMode()
    {
        //获取动画总时长
        var curMaxFrameIndex = _animFrameData.animFrames.Last().frame;
        var curTime = curMaxFrameIndex / _curAnimInfo.frameFrequency;
        GameUtils.CovertTimeLineTextFormat(Txt_TotalTime, curTime);
    }

    
    //点击关键帧
    private void OnSelectKeyFrame(KeyFrameItem keyFrameItem)
    {
        this._curSelectKeyFrameItem = keyFrameItem;
        TimeLineCtr.SnapToItem(keyFrameItem);
        RefreshOPMode();
        TimeLineCtr.SetCurAnimStepLogic();
    }

    #region ButtonFunc
    private void SaveUgcAnimationData(Action<bool> saveCallBack)
    {
        AnimInfo animInfo = null;
        if (IsPublishViewEnter)
        {
            animInfo = _curAnimInfo;
        }
        else
        {
            animInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AnimInfo>();
        }
        var draftInfo = UGCAnimAssetManager.Inst.GetOrCreateDraftInfo(animInfo);
        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG);//重启编辑时长
        draftInfo.SetMetaData(JsonConvert.SerializeObject(_animFrameData));
        draftInfo.editTime += curEditTime;
        StartShot();
        var shotData = GetShotData();
        EndShot();
        draftInfo.SetCover(shotData);
        draftInfo.baseInfo.coverAutoSaved = CoverSaveStatus.ManualSaved;
        draftInfo.UploadAndSave((info, isSuccess) => {
            saveCallBack?.Invoke(isSuccess);
            CloseSelf();
        });
        
        LoggerUtils.Log("###原编辑总时长：" + draftInfo.editTime + "  当次编辑时长：" + curEditTime);
    }
    #endregion

    private void OnSnapItem(KeyFrameItem item)
    {
        if(item == null)
            return;
        
        this._curSelectKeyFrameItem = item;
        var curFrameIndex = this._curSelectKeyFrameItem.GetIndex();
        var curTime = curFrameIndex / _curAnimInfo.frameFrequency;
        GameUtils.CovertTimeLineTextFormat(Txt_CurTime, curTime);
    }

    private void OnStopAction()
    {
    }

    public override void OnSetAnimtion(float time)
    {
        base.OnSetAnimtion(time);
        SetShotPos();
    }

    #region 截屏相关

    protected override void StartShot()
    {
        SetShotCamPos();
        SetShotPos();
    }

    #endregion
}