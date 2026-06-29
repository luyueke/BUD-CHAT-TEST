using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Audio;
using Game.MusicalInstrument;
using GameData.BaseInfo;
using Message;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BUD.AnimPose
{
    public class AnimBgmController : MonoBehaviour
    {
        private bool _isInMapScene = false;
        private bool _isUIPreviewMode = false;
        private GameObject _soundPreviewObj
        {
            get
            {
                if (!_isInMapScene)
                {
                    if (_globalSoundObj == null)
                    {
                        var mainCameraNode = GameObject.Find("GlobalMainCamera");
                        var soundNode = new GameObject("SoundNode");
                        if (soundNode != null)
                        {
                            soundNode.transform.SetParent(mainCameraNode.transform);
                            soundNode.transform.localPosition = Vector3.zero;
                            _globalSoundObj = soundNode;
                        }
                    }

                    return _globalSoundObj;
                }
                else
                {
                    return this.gameObject;
                }
            }
        }
        private GameObject _globalSoundObj;
        // 供外部（如 FittingRoomPanel）在游戏场景下强制使用 2D 音效
        public void SetUIPreviewMode(bool isUIPreview)
        {
            _isUIPreviewMode = isUIPreview;
        }

        private PreviewAudioType _previewAudioType
        {
            get
            {
                if (_isInMapScene && !_isUIPreviewMode)
                {
                    return PreviewAudioType.ThreeD;
                }
                else
                {
                    return PreviewAudioType.TwoD;
                }
            }
        }

        // 存储当前正在播放的音频项
        private List<AnimMusicInfo> _curPlayingInfos = new List<AnimMusicInfo>();
        // 存储帧率（每秒多少帧）
        private float _frameFrequency = 10.00f; // 假设帧率为 10，可以根据您的实际情况调整
        private bool _canPlayBgm;

        private void Awake()
        {
            _isInMapScene = SceneManager.GetActiveScene().name != "GameHall";
        }

        public void StartPlay()
        {
            // 在开始播放时，停止所有可能残留的声音
            StopAllSounds();
            _canPlayBgm = true;
        }
        
        public void StopPlay()
        {
            if(!_canPlayBgm)
                return;
            
            // 在停止播放时，停止所有正在播放的声音
            StopAllSounds();
            _canPlayBgm = false;
        }

        /// <summary>
        /// 停止所有正在播放的声音
        /// </summary>
        private void StopAllSounds()
        {
            // 停止所有 PGC 音频
            foreach (var animMusicInfo in _curPlayingInfos)
            {
                if (animMusicInfo.isPgc == 1)
                {
                    StopPgcTone(animMusicInfo.id);
                }
            }
            // 停止所有 UGC 音频
            StopUgcTone();

            // 清空当前正在播放的音频项列表
            _curPlayingInfos.Clear();
        }

        /// <summary>
        /// 在指定时间点预览音轨中的音频
        /// </summary>
        /// <param name="animBgmTrackInfos">音轨列表</param>
        /// <param name="time">当前时间（秒）</param>
        public void PlayAnimBgmTrackByTime(List<AnimBgmTrackInfo> animBgmTrackInfos, float time)
        {
            if(!_canPlayBgm)
                return;
            
            if(animBgmTrackInfos == null || animBgmTrackInfos.Count == 0)
                return;
            
            // 存储此时应该播放的音频项
            var audioToPlay = new HashSet<AnimMusicInfo>();

            foreach (var trackInfo in animBgmTrackInfos)
            {
                foreach (var animMusicInfo in trackInfo.animMusicList)
                {
                    // 计算音频项的开始和结束时间
                    float startTime = animMusicInfo.startFrame / _frameFrequency;
                    float endTime = startTime + animMusicInfo.frameLen / _frameFrequency;
                    // 判断当前时间是否在音频项的播放时间范围内
                    if (time >= startTime && time < endTime)
                    {
                        audioToPlay.Add(animMusicInfo);

                        if (!_curPlayingInfos.Contains(animMusicInfo))
                        {
                            // 需要开始播放音频
                            float playbackOffset = time - startTime;

                            if (animMusicInfo.isPgc == 1)
                            {
                                // 使用 WWISE 播放 PGC 音频
                                PlayPgcTone(animMusicInfo, playbackOffset);
                            }
                            else
                            {
                                // 使用 Unity AudioSource 播放 UGC 音频
                                PlayUgcTone(animMusicInfo, playbackOffset, trackInfo.trackId);
                            }
                            
                            _curPlayingInfos.Add(animMusicInfo);
                        }
                    }
                }
            }

            // 停止不再需要播放的音频项
            var infosToRemove = _curPlayingInfos.Where(info => !audioToPlay.Contains(info)).ToList();

            foreach (var animMusicInfo in infosToRemove)
            {
                if (animMusicInfo.isPgc == 1)
                {
                    // 停止 PGC 音频
                    // StopPgcTone(animMusicInfo.id);
                }
                else
                {
                    // 停止 UGC 音频
                    // StopUgcTone();
                }
                _curPlayingInfos.Remove(animMusicInfo);
            }
        }
        
        /// <summary>
        /// 播放PGC音色，传入info和开始时间
        /// </summary>
        /// <param name="info"></param>
        /// <param name="playbackOffset"></param>
        private void PlayPgcTone(AnimMusicInfo info, float playbackOffset)
        {
            if (playbackOffset < 0.1f)
                playbackOffset = 0;
            
            var startEventName = UgcAnimToneUtils.GetPgcTonePlayEventName(info.id);
            var pgcToneConfig = UgcAnimToneUtils.GetPgcToneConfig(info.id);
            float pgcToneTimeLen = pgcToneConfig.bgmLength / _frameFrequency;
            
            // 当前时间是这个Pgc音频的百分之多少
            var in_fPercent = playbackOffset / pgcToneTimeLen;
            in_fPercent = Mathf.Clamp01(in_fPercent); // 确保百分比在 0 到 1 之间
            // 使用 AkSoundManager 播放音效，并处理异步事件
            AkSoundManager.Inst.PlaySoundWithCb("", "", startEventName, _soundPreviewObj, in_fPercent);
        }

        /// <summary>
        /// 停止播放 PGC 音频, 传入AnimMusicInfo 的 id
        /// </summary>
        private void StopPgcTone(string pgcId)
        {
            var stopEventName = UgcAnimToneUtils.GetPgcToneStopEventName(pgcId);
            AkSoundManager.Inst.PlaySound("", "",  stopEventName, _soundPreviewObj);
        }

        /// <summary>
        /// 播放UGC音色，传入info和开始时间
        /// </summary>
        /// <param name="info"></param>
        /// <param name="playbackOffset"></param>
        private void PlayUgcTone(AnimMusicInfo info, float playbackOffset, int track)
        {
            var audioUrl = info.metaDataUrl;
            if (string.IsNullOrEmpty(audioUrl))
                return;
            
            UgcToneLoaderBehaviour ugcToneLoaderBehaviour = _soundPreviewObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
            ugcToneLoaderBehaviour.LoadAudioClipAndSeekPlay(_previewAudioType, info.metaDataUrl, playbackOffset, track);
        }
        
        /// <summary>
        /// 停止播放 UGC 音频
        /// </summary>
        private void StopUgcTone()
        {
            var audioSources = _soundPreviewObj.GetComponentsInChildren<AudioSource>().ToList();
            audioSources?.ForEach(x =>
            {
                x.Stop();
                x.clip = null;
            });
        }
    }
}
