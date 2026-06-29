using Game.Audio;
using Game.MusicalInstrument;
using GameData.BaseInfo;
using System;
using UnityEngine;

public class UgcAnimToneManager : GlobalInstance<UgcAnimToneManager>
{
    private GameObject _globalSoundObj;
    public UgcAnimToneManager()
    {
        _globalSoundObj = GameObject.Find("GlobalMainCamera");
    }

    #region 预览单音色方法
    private BudTimer previewOverTimer;
    private AnimMusicInfo _curPreviewInfo;
    private string _curPgcStopName;
    private Action _onPreviewOver;
    public void PreviewTone(AnimMusicInfo info, Action onPreviewOver = null)
    {
        this._curPreviewInfo = info;

        if (onPreviewOver != null)
        {
            this._onPreviewOver = onPreviewOver;
            TimerManager.Inst.Stop(previewOverTimer);
            StartPreviewOverTimer(info);
        }

        StopPreviewTone();
        if (info.isPgc == 1)
        {
            PreviewPgcTone(info);
        }
        else
        {
            PreviewUgcTone(info);
        }
    }



    private void StartPreviewOverTimer(AnimMusicInfo info)
    {
        var curInfo = info;
        var delayTime = info.frameLen / 10.0f;
        if (delayTime == 0)
            delayTime = 2;
        previewOverTimer = TimerManager.Inst.RunOnce("previewOverTimer", delayTime, () =>
        {
            if (curInfo.id == _curPreviewInfo.id)
            {
                _onPreviewOver?.Invoke();
            }
        });
    }

    public void PreviewTone(CabinToneInfo info)
    {
        StopPreviewTone();
        if (info.isPgc == 1)
        {
            _curPgcStopName = UgcAnimToneUtils.GetPgcToneStopEventName(info.id);
            var startName = UgcAnimToneUtils.GetPgcTonePlayEventName(info.id);
            AkSoundManager.Inst.PlaySound("", "", startName, _globalSoundObj);
        }
        else
        {
            UgcToneLoaderBehaviour ugcToneLoaderBehaviour = _globalSoundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
            foreach (var item in info.languageList)
            {
                if (item.type!=0)
                {
                    continue;
                }
                ugcToneLoaderBehaviour.LoadAudioClipAndPlay(PreviewAudioType.TwoD, item.voiceUrl);
                break;
            }
        
        }
    }

    public void StopPreviewTone()
    {
        //停止UGC-Preview
        UgcToneLoaderBehaviour ugcToneLoaderBehaviour = _globalSoundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
        ugcToneLoaderBehaviour.Stop();

        //停止UPGC-Preview
        if (!string.IsNullOrEmpty(_curPgcStopName))
        {
            AkSoundManager.Inst.StopSound(_curPgcStopName, _globalSoundObj);
            _curPgcStopName = "";
        }
    }

    //播放PGC音色
    private void PreviewPgcTone(AnimMusicInfo info)
    {
        _curPgcStopName = UgcAnimToneUtils.GetPgcToneStopEventName(info.id);
        var startName = UgcAnimToneUtils.GetPgcTonePlayEventName(info.id);
        AkSoundManager.Inst.PlaySound("", "", startName, _globalSoundObj);
    }

    //播放UGC音色
    private void PreviewUgcTone(AnimMusicInfo info)
    {
        UgcToneLoaderBehaviour ugcToneLoaderBehaviour = _globalSoundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
        ugcToneLoaderBehaviour.LoadAudioClipAndPlay(PreviewAudioType.TwoD, info.metaDataUrl);
    }

    //播放UGC音色
    public void PreviewUgcTone(string metaDataUrl, PreviewAudioType audioType= PreviewAudioType.TwoD)
    {
        StopPreviewTone();
        UgcToneLoaderBehaviour ugcToneLoaderBehaviour = _globalSoundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
        ugcToneLoaderBehaviour.LoadAudioClipAndPlay(audioType, metaDataUrl);
    }
    #endregion
}
