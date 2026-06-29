using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.Config;
using Game.Utils;
using GameData.BaseInfo;
using UI.Base;
using UnityEngine;

public class BaseAnimEditPanel : BasePanel<BaseAnimEditPanel>
{
    [Header("截屏")]
    public Material ShotMaterial;
    public Camera Cam_ShotCam;
    protected AnimIKController _animIKController;
    protected AnimBgmController _animBgmController; //动画BGM控制器
    protected PoseRoleCreater creater = new PoseRoleCreater();
    protected GameObject animNode;
    protected GameObject shotAnimNode;
    #region Datas
    protected AnimInfo _curAnimInfo;
    //动画数据
    protected AnimFrameData _animFrameData;
    #endregion

    public virtual void InitWidget()
    {
 
    }

    public virtual void StartPreview()
    {
        if (_poseModeConfig == null)
        {
            _poseModeConfig = DataTables.GetPoseModeConfig((int)_curAnimInfo.animType);
        }
    }
    
    public virtual void OnSetAnimtion(float time)
    {
        if(time < 0 )
            return;
        
        if(time > GetTotalTime())
            return;
        
        _animIKController?.SetTimeStamp(time, _curAnimInfo.frameFrequency);
    }
    
    public float GetTotalTime()
    {
        var curMaxFrameIndex = _animFrameData.animFrames.Last().frame;
        var curTime = curMaxFrameIndex * 1.0f / _curAnimInfo.frameFrequency;

        return curTime;
    }
    
    protected List<Vector3> orgPositions = new List<Vector3>();
    protected PoseModeConfig _poseModeConfig;
    
    protected virtual void StartShot()
    {
        EnterShotMode();
        SetShotPos();
    }

    protected virtual void EnterShotMode()
    {
        orgPositions.Clear();
        creater.ChangeImageModeAction(PoseImageType.WhiteBody);
        var whtieIkController = creater.GetIkController(PoseImageType.WhiteBody);
        whtieIkController.ChangeShotMaterial(true);
        SetShotCamPos();
    }

    protected void SetShotCamPos()
    {
        Cam_ShotCam.transform.localPosition = _poseModeConfig.ShotCamPos;
        Cam_ShotCam.orthographicSize = _poseModeConfig.ShotCamSize;
    }

    protected void SetShotPos()
    {
        var optNodes = creater.OptNodes;
        for (var i = 0; i < optNodes.Count; i++)
        {
            orgPositions.Add(optNodes[i].transform.localPosition);
            optNodes[i].transform.localPosition = _poseModeConfig.ShotEditPos[i];
        }
    }
    
    protected byte[] GetShotData()
    {
        Cam_ShotCam.enabled = true;
        //开始截屏
        var shotData = ScreenShotUtils.TakeShot(Cam_ShotCam, GameConsts.UGCPoseShotSize, true);
        Cam_ShotCam.enabled = false;
        return shotData;
    }

    protected void EndShot()
    {
        var whtieIkController = creater.GetIkController(PoseImageType.WhiteBody);
        whtieIkController.ChangeShotMaterial(false);
        var optNodes = creater.OptNodes;
        for (var i = 0; i < optNodes.Count; i++)
        {
            optNodes[i].transform.localPosition = orgPositions[i];
        }
        creater.ChangeImageModeAction(PoseImageType.Current);
    }
}
