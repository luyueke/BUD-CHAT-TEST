using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Basic.Utils;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AnimationStudioMusicEditPanel : BaseAnimEditPanel
{
    [Header("UI操作")] 
    public CButton Btn_Return;
    public CButton Btn_Play;
    public CButton Btn_Stop;
    public CButton Btn_DelBgm;
    public Image Img_DelBgm;
    public Text Txt_DelBgm;
    public CButton Btn_Undo;
    public CButton Btn_Redo;
    public CButton Btn_ZoomIn;
    public CButton Btn_ZoomOut;
    public Text Txt_CurTime;
    public Text Txt_TotalTime;
    [Header("时间轴操作相关")] 
    public MusicTimeLineController TimeLineCtr;
    private CharacterWrap _characterWrap;

    #region Datas
    private const string TAG = "AnimationStudioMusicEditPanel";
    #endregion
    //当前选中的关键帧
    private KeyFrameItem _curSelectKeyFrameItem;
    private List<KeyFrameItem> _curKeyFrameItems = new List<KeyFrameItem>();
    private bool _isPlaying = false;

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        GameTimeUtils.Inst.StartCollect(TAG);

        _curAnimInfo = (AnimInfo)args[0];
        _animFrameData = (AnimFrameData)args[1];
        _animIKController = (AnimIKController)args[2];
        _animBgmController = (AnimBgmController)args[3];
        InitWidget();
    }

    public override void OnHidden()
    {
        base.OnHidden();
        _curAnimInfo.animBgmTrackInfos = TimeLineCtr.GetTrackInfo();
        var panel = UIManager.Inst.FindPanel<AnimationStudioEditPanel>(PanelId.AnimationStudioEditPanel);
        if (panel != null)
        {
            panel.SyncMuiscData(_curAnimInfo.animBgmTrackInfos);
            panel.ReShowPanel();
        }
    }

    public override void InitWidget()
    {
        base.InitWidget();
        AddListener();
        InitAnimController();
        InitBgmWidget();
        SetBtnDelBgmEnable(false);
    }

    private void AddListener()
    {
        Btn_Return.onClick.AddListener(CloseSelf);
        Btn_Play.onClick.AddListener(OnBtnPlayClick);
        Btn_Stop.onClick.AddListener(OnBtnStopClick);
        Btn_ZoomIn.onClick.AddListener(() => { OnBtnZoomClick(true);});
        Btn_ZoomOut.onClick.AddListener(() => { OnBtnZoomClick(false);});
    }

    private void InitAnimController()
    {
        TimeLineCtr.SetAction(OnSnapItem, OnSelectKeyFrame, OnSetAnimtion, OnStopAction);
        _curKeyFrameItems = TimeLineCtr.CreateTimeLine(_animFrameData);
        
        TimeLineCtr.RefreshKeyFrameItemStateByData(_animFrameData);
        TimeLineCtr.SnapToItem(_curKeyFrameItems[0]);
        TimeLineCtr.SetCurAnimStepLogic();
    }

    private void InitBgmWidget()
    {
        TimeLineCtr.InitData(_curAnimInfo.animBgmTrackInfos, OnSelectBgmKFItem);
    }

    #region TimeLineAction
    private void OnSnapItem(KeyFrameItem item)
    {
        if (item == null)
            return;
        
        this._curSelectKeyFrameItem = item;
        var curFrameIndex = this._curSelectKeyFrameItem.GetIndex();
        var curTime = curFrameIndex / _curAnimInfo.frameFrequency;
        GameUtils.CovertTimeLineTextFormat(Txt_CurTime, curTime);
    }

    //点击关键帧
    private void OnSelectKeyFrame(KeyFrameItem keyFrameItem)
    {
        this._curSelectKeyFrameItem = keyFrameItem;
        TimeLineCtr.SnapToItem(keyFrameItem);
        RefreshOPMode();
        TimeLineCtr.SetCurAnimStepLogic();
    }
    
    private void OnStopAction()
    {
        _isPlaying = false;
        RefreshOPMode();
        _animBgmController.StopPlay();
    }
    
    private void RefreshOPMode()
    {
        Btn_Play.gameObject.SetActive(!_isPlaying);
        Btn_Stop.gameObject.SetActive(_isPlaying);
        
        //获取动画总时长
        var curMaxFrameIndex = _animFrameData.animFrames.Last().frame;
        var curTime = curMaxFrameIndex / _curAnimInfo.frameFrequency;
        GameUtils.CovertTimeLineTextFormat(Txt_TotalTime, curTime);
    }
    #endregion

    private void OnSelectBgmKFItem(BgmKeyFrameItem bgmKFItem)
    {
        if(bgmKFItem == null)
            return;
        
        SetBtnDelBgmEnable(true);
        this.Btn_DelBgm.onClick.RemoveAllListeners();
        this.Btn_DelBgm.onClick.AddListener(() =>
        {
            TimeLineCtr.DeleteBgmKeyFrameItem(bgmKFItem);
            SetBtnDelBgmEnable(false);
        });
    }

    private void SetBtnDelBgmEnable(bool enable)
    {
        this.Btn_DelBgm.SetClickAble(enable);
        Img_DelBgm.color = enable ? Color.white : DataUtil.DeSerializeColorCheckHash("#6C6C6C");
        Txt_DelBgm.color = enable ? Color.white : DataUtil.DeSerializeColorCheckHash("#6C6C6C");
    }

    #region ButtonFunc

    public void OnBtnPlayClick()
    {
        _curAnimInfo.animBgmTrackInfos = TimeLineCtr.GetTrackInfo();
        var panel = UIManager.Inst.FindPanel<AnimationStudioEditPanel>(PanelId.AnimationStudioEditPanel);
        panel?.SyncMuiscData(_curAnimInfo.animBgmTrackInfos);
        
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
        TimeLineCtr.StartPlay(startIndex, endIndex);
        RefreshOPMode();
    }

    private void OnBtnStopClick()
    {
        _isPlaying = false;
        TimeLineCtr.SetCurAnimStepLogic();
    }
    
    private void OnBtnZoomClick(bool IsZoomIn)
    {
        TimeLineCtr.ZoomTimeLine(IsZoomIn);
        TimeLineCtr.SnapToItem(_curSelectKeyFrameItem);
    }
    #endregion
}
