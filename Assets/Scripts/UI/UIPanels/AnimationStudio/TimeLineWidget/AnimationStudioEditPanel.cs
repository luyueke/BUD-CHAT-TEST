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

public class AnimationStudioEditPanel : BaseAnimEditPanel
{
    public Camera Cam_Preview;
    [Header("Character")] 
    public AvatarCameraController camCtr;
    public Transform Trans_PreviewRoot;
    [Header("UI操作")] 
    public LoopTypeContent LoopTypeCtr;
    public Transform BaseLayout3D;
    public Transform BaseLayout2D;
    public CButton Btn_TestSave;
    public CButton Btn_Exit;
    public LoadingButton Btn_Save;
    public CButton Btn_EditCover;
    public CButton Btn_EditMusic;
    public CButton Btn_Undo;
    public CButton Btn_Redo;
    public CButton Btn_Play;
    public CButton Btn_Stop;
    public CButton Btn_AddEmptyFrameSlot;
    public CButton Btn_DelFrameItem;
    public CButton Btn_ClearFrame;
    public Image Img_ClearFrame;
    public Text Txt_ClearFrame;
    public CButton Btn_ZoomIn;
    public CButton Btn_ZoomOut;
    public CButton Btn_EditAnim;
    public Text Txt_CurTime;
    public Text Txt_TotalTime;

    [Header("时间轴操作相关")] 
    public TimeLineController TimeLineCtr;

    private GameObject rotCenter;
    #region Datas
    private const string TAG = "AnimationStudioEditPanel";
    #endregion
    
    //当前选中的关键帧
    private KeyFrameItem _curSelectKeyFrameItem;
    private List<KeyFrameItem> _curKeyFrameItems = new List<KeyFrameItem>();
    private bool _isPlaying = false;
    private bool _isLoop = false;
    private Dictionary<int,PropAnimIK> propIKs = new Dictionary<int, PropAnimIK>();

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        GameTimeUtils.Inst.StartCollect(TAG);

        _curAnimInfo = (AnimInfo)args[0];
        _animFrameData = (AnimFrameData)args[1];
        DataCheck(_animFrameData);
        StartPreview();
        InitWidget();
    }

    public override void OnHidden()
    {
        base.OnHidden();
        creater.Release();
        GameObject.Destroy(animNode);
        if (rotCenter != null)
        {
            GameObject.Destroy(rotCenter);
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
        _animIKController = creater.CreateRole(selfData, petData);
        creater.ChangeImageModeAction(PoseImageType.Current);
        propIKs = _animIKController.GetPropIKs();
        _animIKController.SetAnimFrameData(_animFrameData);
        _animBgmController = _animIKController.GetAnimBgmCtr();
        
        Cam_ShotCam = GameCameraUtils.Inst.GetShotCamera();
        Cam_ShotCam.transform.SetParent(animNode.transform);
        
        Cam_Preview.transform.SetParent(animNode.transform);
        Cam_Preview.transform.localPosition = _poseModeConfig.ShotCamPos;
        Cam_Preview.orthographicSize = _poseModeConfig.ShotCamSize;
        Cam_Preview.transform.SetParent(Trans_PreviewRoot);

        SetAnimNodeRotateCenter();
    }
    
    protected void SetAnimNodeRotateCenter()
    {
        rotCenter = new GameObject("AnimNodeRotateCenter");
        rotCenter.transform.SetParent(animNode.transform);
        rotCenter.transform.localPosition = new Vector3(_poseModeConfig.ShotCamPos.x, 0, 0);
        rotCenter.transform.SetParent(animNode.transform.parent);
        animNode.transform.SetParent(rotCenter.transform);
        camCtr.MoveRoot = rotCenter.transform;
        camCtr.RotateTarget = rotCenter.transform;
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
        KeyFrameItem enterItem = null;
        if (AnimDataManager.Inst.animPose != null && !string.IsNullOrEmpty(AnimDataManager.Inst.animPose.poseData))
        {        
            var frameData = JsonConvert.DeserializeObject<KeyFrameData>(AnimDataManager.Inst.animPose.poseData);
            var enterFrameIndex = AnimDataManager.Inst.CurOPFrame;
            frameData.frame = enterFrameIndex;
            frameData.isEmptySlot = 0;
            
            //就是说我这个Item里面的Data是由_animFrameData注入的，然后是引用类型，所以导致我改了Item里面的Data，这个脚本里的_animFrameData也被改了
            enterItem = _curKeyFrameItems.Find(x => x.GetIndex() == enterFrameIndex);
            enterItem.AddFrameData(frameData);
            //更新数据
            var targetIndex = _animFrameData.animFrames.FindIndex(x => x.frame == frameData.frame);
            _animFrameData.animFrames[targetIndex] = frameData;
        }
        else
        {
            enterItem = _curKeyFrameItems[0];
        }
        
        if (enterItem != null)
        {
            _animIKController.CreatePropIKs((UgcPoseSubType)_curAnimInfo.animType,_curAnimInfo.propList);
            TimeLineCtr.OnKeyFrameItemSelect(enterItem);
        }
    }

    private void InitController()
    {
        TimeLineCtr.SetAction(OnSnapItem, OnSelectKeyFrame, OnSetAnimtion, OnStopAction, OnLoopEnd);
        _curKeyFrameItems = TimeLineCtr.CreateTimeLine(_animFrameData);
    }

    private void AddListener()
    {
        LoopTypeCtr.Init(OnSetLoopType);
        InitBtnSaveTest();
        Btn_Exit.onClick.AddListener(OnBtnExitClick);
        Btn_Save.onClick.AddListener(OnBtnSaveClick);
        Btn_EditCover.onClick.AddListener(OnBtnEditCoverClick);
        Btn_EditMusic.onClick.AddListener(OnBtnEditMusicClick);
        Btn_Play.onClick.AddListener(OnBtnPlayClick);
        Btn_Stop.onClick.AddListener(OnBtnStopClick);
        Btn_AddEmptyFrameSlot.onClick.AddListener(OnBtnAddEmptyKeyFrameSlotClick);
        Btn_DelFrameItem.onClick.AddListener(OnBtnDelKeyFrameItemClick);
        Btn_ClearFrame.onClick.AddListener(OnBtnClearFrameClick);
        Btn_EditAnim.onClick.AddListener(OnBtnEditAnimClick);
        Btn_ZoomIn.onClick.AddListener(() => { OnBtnZoomClick(true);});
        Btn_ZoomOut.onClick.AddListener(() => { OnBtnZoomClick(false);});
    }

    private void OnSetLoopType(bool isLoop)
    {
        this._isLoop = isLoop;
        AnimDataManager.Inst.isLoop = isLoop;
    }

    private void InitBtnSaveTest()
    {
#if UNITY_EDITOR
        Btn_TestSave.gameObject.SetActive(true);
        Btn_TestSave.onClick.AddListener(() =>
        {
            var folderName = "测试动画保存";

            // 获取桌面路径
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // 创建文件夹路径
            string folderPath = Path.Combine(desktopPath, folderName);

            // 创建文件夹，如果不存在则创建
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 序列化并生成 JSON 字符串
            var SaveJsonStr1 = JsonConvert.SerializeObject(_curAnimInfo, Formatting.Indented);
            var fileName1 = "AnimInfo_" + _curAnimInfo.name + ".json";
            var filePath1 = Path.Combine(folderPath, fileName1);

            var SaveJsonStr2 = JsonConvert.SerializeObject(_animFrameData, Formatting.Indented);
            var fileName2 = "AnimFrameData_" + _curAnimInfo.name + ".json";
            var filePath2 = Path.Combine(folderPath, fileName2);

            // 将 JSON 字符串写入文件
            File.WriteAllText(filePath1, SaveJsonStr1);
            File.WriteAllText(filePath2, SaveJsonStr2);

            // 输出日志，方便调试
            LoggerUtils.LogError($"文件已保存到: {filePath1}");
            LoggerUtils.LogError($"文件已保存到: {filePath2}");
        });
#endif
    }


    private void RefreshOPMode()
    {
        if (_curSelectKeyFrameItem != null)
        {
            var frameData = _curSelectKeyFrameItem.GetData();
            var curFrameIndex = _curSelectKeyFrameItem.GetIndex();
            
            Btn_DelFrameItem.gameObject.SetActive(frameData != null);
            Btn_AddEmptyFrameSlot.gameObject.SetActive(frameData == null);
            Btn_ClearFrame.SetClickAble(frameData != null && curFrameIndex != 0 && frameData.isEmptySlot != 1);
            Img_ClearFrame.color = (frameData != null && curFrameIndex != 0 && frameData.isEmptySlot != 1) ? Color.white : DataUtil.DeSerializeColorCheckHash("#6C6C6C");
            Txt_ClearFrame.color = (frameData != null && curFrameIndex != 0 && frameData.isEmptySlot != 1) ? Color.white : DataUtil.DeSerializeColorCheckHash("#6C6C6C");
            Btn_EditAnim.GetComponentInChildren<Text>().text = frameData?.isEmptySlot == 1 ? "添加关键帧" : "编辑关键帧";
            Btn_EditAnim.gameObject.SetActive(frameData != null && !_isPlaying);
        }
        
        Btn_Play.gameObject.SetActive(!_isPlaying);
        Btn_Stop.gameObject.SetActive(_isPlaying);
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
        
        keyFrameItem.RefreshItemBg(FrameSelectMode.Selected);
    }

    #region ButtonFunc
    private void OnBtnStopClick()
    {
        _isPlaying = false;
        TimeLineCtr.SetCurAnimStepLogic();
        TimeLineCtr.SnapToNearestItem();
    }
    
    private void OnBtnPlayClick()
    {
        if(this._curSelectKeyFrameItem == null)
            return;

        var startIndex = this._curSelectKeyFrameItem.GetIndex();
        var endIndex = _animFrameData.animFrames.Last().frame;
        
        if(endIndex == 0)
            return;

        if (startIndex == endIndex)
        {
            startIndex = 0;
        }
        
        _animBgmController.StartPlay();
        _isPlaying = true;
        TimeLineCtr.RefreshKeyFrameItemStateByData(_animFrameData);
        TimeLineCtr.StartPlay(startIndex, endIndex, _isLoop);
        RefreshOPMode();
    }
    
    private void OnBtnExitClick()
    {
        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetText("确认保存","保存当前的创作进度吗？", "保存", "不保存");
        commonConfirmPanel.SetIsCloseSelf(false);
        commonConfirmPanel.SetOnClickAction(() =>
        {
            commonConfirmPanel.SetConfirmLoadingVisible(true);

            SaveUgcAnimationData((isSuccess) =>
            {
                TipPanel.ShowToast(isSuccess ? "保存成功:D" : "保存失败");
                if (isSuccess)
                {
                    if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                    {
                        commonConfirmPanel.Close();
                    }

                    GameController.ExitGame(() =>
                    {
                        AnimDataManager.Inst.ClearData();
                        UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, true);
                        UIManager.Inst.BackToLastWindow();
                        MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                        CloseSelf();
                    });
                }
            });
        }, () =>
        {
            if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
            {
                commonConfirmPanel.Close();
            }

            GameController.ExitGame(() =>
            {
                AnimDataManager.Inst.ClearData();
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, true);
                UIManager.Inst.BackToLastWindow();
                MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                CloseSelf();
            });
        });
    }

    private void SaveUgcAnimationData(Action<bool> saveCallBack, bool saveCover = true)
    {
        var animInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<AnimInfo>();

        if (animInfo.propList != null && animInfo.propList.Count > 5)
        {
            LoggerUtils.LogError("animInfo.propList.Count is over 5",animInfo.propList.Count);
            Dictionary<int, AnimPropData> tempDic = new Dictionary<int, AnimPropData>();
            foreach (var propData in animInfo.propList)
            {
                tempDic[propData.index] = propData;
            }

            var propList = tempDic.Values.ToList();
            propList.Sort((a, b) => a.index.CompareTo(b.index));
            animInfo.propList = propList;
        }
        
        if (animInfo.animBgmTrackInfos.Count > 3)
        {
            LoggerUtils.LogError(AccountDataManager.Inst.Uid +  " animInfo.animBgmTrackInfos.Count is over 3");
            var newAnimBgmTrackInfos = new List<AnimBgmTrackInfo>();
            newAnimBgmTrackInfos.Add(animInfo.animBgmTrackInfos[0]);
            newAnimBgmTrackInfos.Add(animInfo.animBgmTrackInfos[1]);
            newAnimBgmTrackInfos.Add(animInfo.animBgmTrackInfos[2]);
            animInfo.animBgmTrackInfos = newAnimBgmTrackInfos;
        }
        
        var draftInfo = UGCAnimAssetManager.Inst.GetOrCreateDraftInfo(animInfo);
        
        
        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG);//重启编辑时长
        //获取动画总时长
        var curMaxFrameIndex = _animFrameData.animFrames.Last().frame;
        var curAnimTime = curMaxFrameIndex / 10.0f;
        draftInfo.SetAnimTime(curAnimTime);
        var skinType = 0;



        switch ((EmoteSubType)animInfo.animType)
        {
            case EmoteSubType.Single:
            case EmoteSubType.Double:
                skinType = 0;
                break;
            
            case EmoteSubType.PetSingle:
            case EmoteSubType.PetWithPlayer:
                    skinType = 1;
                break;
        }
        draftInfo.SetSkinType(skinType);
        draftInfo.SetMetaData(JsonConvert.SerializeObject(_animFrameData));
        draftInfo.editTime += curEditTime;

        //没有自定义保存过 - 则自动保存
        if (draftInfo.baseInfo.coverAutoSaved != CoverSaveStatus.ManualSaved)
        {
            if (saveCover)
            {
                
                StartShot();
                var shotData = GetShotData();
                EndShot();
                draftInfo.SetCover(shotData);
                draftInfo.baseInfo.coverAutoSaved = CoverSaveStatus.AutoSaved;
            }
        }

        draftInfo.UploadAndSave((info, isSuccess) => {
            if (info != null && info.baseInfo.propList != null && info.baseInfo.propList.Count > 5)
            {
                LoggerUtils.LogError("---info.propList.Count is over 5",animInfo.propList.Count);
            }

            if (info.baseInfo.animBgmTrackInfos.Count > 3)
            {
                LoggerUtils.LogError(AccountDataManager.Inst.Uid +  " info.baseInfo.animBgmTrackInfos.Count is over 3");
            }
            saveCallBack?.Invoke(isSuccess);
        });
        
        LoggerUtils.Log("###原编辑总时长：" + draftInfo.editTime + "  当次编辑时长：" + curEditTime);
    }

    private void OnBtnSaveClick()
    {
        Btn_Save.SetLoadingVisible(true);
        SaveUgcAnimationData((isSuccess) =>
        {
            TipPanel.ShowToast(isSuccess ? "保存成功:D" : "保存失败");
            Btn_Save.SetLoadingVisible(false);
        });
    }
    
    private void OnBtnEditCoverClick()
    {
        BaseLayout2D.gameObject.SetActive(false);
        BaseLayout3D.gameObject.SetActive(false);
        animNode.SetActive(false);
        UIManager.Inst.OpenPanel<AnimationStudioEditCoverPanel>(PanelId.AnimationStudioEditCoverPanel, _curAnimInfo.Clone(), _animFrameData);
    }

    private void OnBtnEditMusicClick()
    {
        BaseLayout2D.gameObject.SetActive(false);
        UIManager.Inst.OpenPanel<AnimationStudioMusicEditPanel>(PanelId.AnimationStudioMusicEditPanel, _curAnimInfo, _animFrameData, _animIKController, _animBgmController);
        OnBtnStopClick();
    }
    
    private void OnBtnAddEmptyKeyFrameSlotClick()
    {
        if (this._curSelectKeyFrameItem == null)
        {
            LoggerUtils.LogError("OnBtnAddEmptyKeyFrameSlotClick _curNearestKFItem Is Null" );
            return;
        }

        var curIndex = this._curSelectKeyFrameItem.GetIndex();
        TimeLineCtr.AddKeyFrameData(_animFrameData, curIndex);
        TimeLineCtr.BindTimeLineData(_animFrameData);
        TimeLineCtr.RefreshKeyFrameItemStateByData(_animFrameData);
        RefreshOPMode();
        TimeLineCtr.SnapToItem(this._curSelectKeyFrameItem);
    }

    private void OnBtnDelKeyFrameItemClick()
    {
        if (this._curSelectKeyFrameItem == null)
        {
            LoggerUtils.LogError("OnBtnAddKeyFrameClick _curSelectKeyFrameItem Is Null" );
            return;
        }
        
        var curIndex = this._curSelectKeyFrameItem.GetIndex();
        TimeLineCtr.DeleteKeyFrameData(_animFrameData, curIndex);
        TimeLineCtr.BindTimeLineData(_animFrameData);
        TimeLineCtr.RefreshKeyFrameItemStateByData(_animFrameData);
        
        RefreshOPMode();
        TimeLineCtr.SnapToNearestItem();
    }

    private void OnBtnClearFrameClick()
    {
        if (this._curSelectKeyFrameItem == null)
        {
            LoggerUtils.LogError("OnBtnAddKeyFrameClick _curSelectKeyFrameItem Is Null" );
            return;
        }
        
        var curIndex = this._curSelectKeyFrameItem.GetIndex();
        TimeLineCtr.ClearKeyFrameData(_animFrameData, curIndex);
        TimeLineCtr.BindTimeLineData(_animFrameData);
        TimeLineCtr.RefreshKeyFrameItemStateByData(_animFrameData);
        
        RefreshOPMode();
    }
    
    private void OnBtnEditAnimClick()
    {
        if(_curSelectKeyFrameItem == null)
            return;
        
        if (!UgcAnimVipChecker.Inst.CanAddKeyFramesInSegment(_animFrameData, _curSelectKeyFrameItem.GetIndex()))
        {
            return;
        }
        
        //数据组装
        var poseInfo = new PoseInfo();
        poseInfo.id = "EnterByStudio";
        poseInfo.poseType = _curAnimInfo.animType;
        var skinType = 0;
        switch ((EmoteSubType)_curAnimInfo.animType)
        {
            case EmoteSubType.Single:
            case EmoteSubType.Double:
                skinType = 0;
                break;
            
            case EmoteSubType.PetSingle:
            case EmoteSubType.PetWithPlayer:
                skinType = 1;
                break;
        }

        poseInfo.skinType = skinType;
        
        AnimDataManager.Inst.enterMode = EnterPanelMode.AnimEnter;
        AnimDataManager.Inst.animPose = poseInfo;
        KeyFrameData editFrameData = _curSelectKeyFrameItem.GetData();
        AnimDataManager.Inst.CurOPFrame = editFrameData.frame;
        if (editFrameData.isEmptySlot == 1)
        {
            var curFrameIndex = _curSelectKeyFrameItem.GetIndex();
            var nearKeyData = TimeLineCtr.GetNearestAnimData(_animFrameData, curFrameIndex);
            if (nearKeyData != null)
            {
                editFrameData.keyFrame = nearKeyData.keyFrame;
                editFrameData.items = nearKeyData.items;
            }
        }
        poseInfo.poseData = JsonConvert.SerializeObject(editFrameData);
        SaveUgcAnimationData((isSuccess) =>
        {
            GameController.ExitGame(() =>
            {
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCItemEditWindow, true);
                UIManager.Inst.BackToLastWindow();
                MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                CloseSelf();
                AnimDataManager.Inst.animInfo = _curAnimInfo;
                GameController.StartGame(EnterGameModel.AnimPoseEmpty, poseInfo, true, "AnimatedScene");
            });
            var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel, WindowId.CommonWindow);
            var sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(
                "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas",
                "UGCAnim_" + poseInfo.poseType, this.gameObject);
            p.Init(poseInfo, null, LoadingType.UGCAnim, s: sp);
            AkSoundManager.Inst.StopBGSound();
        }, false);
    }

    private void OnBtnZoomClick(bool IsZoomIn)
    {
        TimeLineCtr.ZoomTimeLine(IsZoomIn);
        TimeLineCtr.SnapToItem(_curSelectKeyFrameItem);
    }
    #endregion

    private void OnSnapItem(KeyFrameItem item)
    {
        if(item == null)
            return;
        
        this._curSelectKeyFrameItem = item;

        TimeLineCtr.RefreshKeyFrameItemStateByData(_animFrameData);
        if (this._curSelectKeyFrameItem.GetData() != null)
        {
            this._curSelectKeyFrameItem.RefreshItemBg(FrameSelectMode.Selected);
        }
        
        var curFrameIndex = this._curSelectKeyFrameItem.GetIndex();
        var curTime = curFrameIndex / _curAnimInfo.frameFrequency;
        GameUtils.CovertTimeLineTextFormat(Txt_CurTime, curTime);
    }

    private void OnStopAction()
    {
        _isPlaying = false;
        _animBgmController.StopPlay();
        RefreshOPMode();
    }

    private void OnLoopEnd()
    {
        _animBgmController.StartPlay();
    }

    public void SyncMuiscData(List<AnimBgmTrackInfo> animBgmTrackInfos)
    {
        //1.保存到animInfo当中
        this._curAnimInfo.animBgmTrackInfos = animBgmTrackInfos;
        //2.保存到KFData中
        this._animFrameData.animBgmTrackInfos = animBgmTrackInfos;
    }
    
    public void ReShowPanel()
    {
        BaseLayout2D.gameObject.SetActive(true);
        BaseLayout3D.gameObject.SetActive(true);
        animNode.SetActive(true);
    }

    private void DataCheck(AnimFrameData data)
    {
        if (data.framefrequency == 0)
            data.framefrequency = 10;
        
        if (_curAnimInfo.frameFrequency == 0)
            _curAnimInfo.frameFrequency = 10;
        
        if (data == null || data.animFrames == null || data.animFrames.Count == 0)
            return;

        var toRemove = new List<KeyFrameData>();
        foreach (var animFrame in data.animFrames)
        {
            if (animFrame.frame < 0)
            {
                toRemove.Add(animFrame);
            }
        }

        foreach (var frame in toRemove)
        {
            data.animFrames.Remove(frame);
        }
    }
}