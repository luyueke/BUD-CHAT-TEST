using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Audio;
using Game.Config;
using Game.MusicalInstrument;
using Message;
using UnityEngine;
using UnityEngine.SceneManagement;
using xasset;

public class UgcToneLoaderBehaviour : MonoBehaviour
{
    [HideInInspector]
    public int MaxAudioSourceCount = 3;
    private int poolNum = 30; //最多缓存22个音节 一个音色最多22哥
    
    private Queue<AudioRequest> loadedPathAudioRequests = new Queue<AudioRequest>();
    private List<AudioSource> _audioSources = new List<AudioSource>();
    private Coroutine _curUgcPreviewToneCoroutinue;
    private int _roundRobinCursor;
    
    public void Stop()
    {
        if (_curUgcPreviewToneCoroutinue != null)
        {
            CoroutineManager.Inst.StopCoroutine(_curUgcPreviewToneCoroutinue);
        }
        if (_audioSources != null && _audioSources.Count > 0)
        {
            _audioSources.ForEach(x => AudioSourceFadeHelper.FadeOutAndStop(x));
        }
        RestoreMusic();
    }

    /// <summary>
    /// 检查当前是否有语音音频正在播放。
    /// </summary>
    public bool IsAudioPlaying()
    {
        return _audioSources != null && _audioSources.Any(s => s != null && s.isPlaying);
    }

    public void LoadAudioClipAndPlay(PreviewAudioType previewAudioType, string audioUrl, int audioSourceIndex = 0, Action onComplete = null, float volumeOverride = -1f)
    {
        _audioSources = GetOrAddAudioSource(previewAudioType);
        DownloadUgcAudio(audioUrl, (success,request) =>
        {
            onComplete?.Invoke();
            if (success && request != null && request.asset != null)
            {
                _audioSources = GetOrAddAudioSource(previewAudioType);
                int targetSourceIndex = ResolveAudioSourceIndex(audioSourceIndex);
                ReduceMusic();
                _audioSources[targetSourceIndex].clip = request.asset;
                _audioSources[targetSourceIndex].volume = volumeOverride >= 0f ? volumeOverride : AkSoundManager.Inst.GetSFXAudioVolume();
                _audioSources[targetSourceIndex].Play();
            }
        });
    }

    public void LoadMultipleAudioAndPlay(PreviewAudioType previewAudioType, string toneId, List<string> audioUrls)
    {
        DownloadMultipleAudio(toneId, audioUrls, (id, audioDict) =>
        {
            if (toneId != id)
            {
                return;
            }
            _curUgcPreviewToneCoroutinue = CoroutineManager.Inst.StartCoroutine(PlayUgcPreviewAudioClips(previewAudioType, audioUrls, audioDict));
        });
    }
    
    public void LoadAudioClipAndSeekPlay(PreviewAudioType previewAudioType, string audioUrl, float playbackOffset, int audioSourceIndex = 0, Action onComplete = null)
    {
        _audioSources = GetOrAddAudioSource(previewAudioType);
        DownloadUgcAudio(audioUrl, (success,request) =>
        {
            onComplete?.Invoke();
            if (success && request != null && request.asset != null)
            {
                _audioSources = GetOrAddAudioSource(previewAudioType);
                int targetSourceIndex = ResolveAudioSourceIndex(audioSourceIndex);
                ReduceMusic();
                _audioSources[targetSourceIndex].Stop();
                _audioSources[targetSourceIndex].clip = request.asset;
                _audioSources[targetSourceIndex].volume = AkSoundManager.Inst.GetSFXAudioVolume();
                _audioSources[targetSourceIndex].time = playbackOffset;
                _audioSources[targetSourceIndex].Play();
            }
        });
    }
    
    public void DownloadMultipleAudio(string toneId, List<string> downloadUrls, Action<string, Dictionary<string, AudioClip>> onComplete)
    {
        Dictionary<string, AudioClip> clipDict = new Dictionary<string, AudioClip>();
        int remainingDownloads = downloadUrls.Count;
            
        foreach (var url in downloadUrls)
        {
            var request = AudioRequest.Load(url);
            request.completed += (req) =>
            {
                request.asset.name = request.path;
                
                if (request.asset != null)
                {
                    AddRequestCache(request);
                    // 将URL和对应的请求一起存储
                    if(!clipDict.ContainsKey(url))
                        clipDict.Add(url, request.asset);
                }

                remainingDownloads--;
                
                // 如果所有音频都下载完成，调用回调
                if (remainingDownloads == 0)
                {
                    onComplete?.Invoke(toneId, clipDict);
                }
            };
        }
    }
    
    IEnumerator PlayUgcPreviewAudioClips(PreviewAudioType previewAudioType, List<string> audioUrls, Dictionary<string, AudioClip> audioDict)
    {
        _audioSources = GetOrAddAudioSource(previewAudioType);
        foreach (var audioUrl in audioUrls)
        {
            if (audioDict.ContainsKey(audioUrl))
            {
                ReduceMusic();
                // 停止播放
                _audioSources[0].Stop();
                _audioSources[0].clip = audioDict[audioUrl];
                _audioSources[0].Play();
                // 播放2秒
                yield return new WaitForSeconds(1);
            }
        }
        LoggerUtils.Log("All audio clips played.");
    }
    
    private void DownloadUgcAudio(string url, Action<bool,AudioRequest> onComplete)
    {
        if (string.IsNullOrEmpty(url))
        {
            onComplete?.Invoke(false, null);
            return;
        }
        var request = AudioRequest.Load(url);
        request.completed += (req) =>
        {
            if (req.result == Request.Result.Success)
            {
                AddRequestCache(request);
                onComplete?.Invoke(req.result == Request.Result.Success, request);
            }
            else
            {
                //if (req.error.Contains("destination host"))
                {
                    LoadUgcAudioByOriginUrl(url,onComplete);
                }
                //else
                //{
                //    LoggerUtils.Log("DownloadUgcAudio error = " + req.error);
                //    onComplete?.Invoke(false, null);
                //}
            }
        };
    }
    
    public void LoadUgcAudioByOriginUrl(string path,Action<bool,AudioRequest> callback = null)
    {
        string downloadPath = GetOriginDownloadUrl(path);
        var request = AudioRequest.Load(downloadPath);
        if (request == null)
        {
            callback?.Invoke(false,null);
            return;
        }
        request.completed += req =>
        {
            if (req.result == Request.Result.Success)
            {
                AddRequestCache(request);
            }
            else
            {
                Debug.LogError("ugc audio origin download fail! msg:" + req.error);
            }
            callback?.Invoke(req.result == Request.Result.Success, request);
        };
    }
    
    public string GetOriginDownloadUrl(string url)
    {
        if (url.EndsWith(".mp3"))
        {
            try
            {
                Uri uri = new Uri(url);
                var queryAndPath = uri.PathAndQuery;
                url = GameConsts.BusinessBaseHost + queryAndPath;
                url = url.Replace(GameConsts.BusinessBaseHost, GameConsts.BusinessCdnUrl);
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("LoadUGCPartRemoteImageAsync Origin URL is invalid: " + url + ", " + e.Message);
            }
        }
        return url;
    }
    
    //在OnDestroy的时候释放
    private void OnDestroy()
    {
        while (loadedPathAudioRequests.Count > 0)
        {
            loadedPathAudioRequests.Dequeue().Release();
        }
    }
    
    protected void AddRequestCache(AudioRequest request)
    {
        loadedPathAudioRequests.Enqueue(request);
        
        if (loadedPathAudioRequests.Count > poolNum)
        {
            loadedPathAudioRequests.Dequeue().Release();
        }
    }

    private List<AudioSource> GetOrAddAudioSource(PreviewAudioType audioType)
    {
        // 只管理本组件自己创建的 AudioSource，不用 GetComponentsInChildren 重新扫描场景，
        // 避免把 PGC VoiceController 在同一 GameObject 上创建的 AudioSource 也纳入管理
        // 从而防止 UGC 覆盖 PGC 的 3D 空间配置，或 PGC 被 UGC 的 spatialBlend 重置干扰。
        _audioSources.RemoveAll(s => s == null);
        for (int i = _audioSources.Count; i < MaxAudioSourceCount; i++)
        {
            var audioSourceComp = this.gameObject.AddComponent<AudioSource>();
            audioSourceComp.playOnAwake = false;
            audioSourceComp.rolloffMode = AudioRolloffMode.Custom;
            audioSourceComp.spatialBlend = 1;
            audioSourceComp.maxDistance = 23;
            audioSourceComp.dopplerLevel = 0;
            _audioSources.Add(audioSourceComp);
        }
        float sfxAudioVolume = AkSoundManager.Inst.GetSFXAudioVolume();
        _audioSources.ForEach(ad =>
        {
            ad.spatialBlend = (int)audioType;
            // 只对空闲源重置音量：正在播放的源可能处于渐消中，强制重置音量会造成响度突变噗音
            if (!ad.isPlaying)
                ad.volume = sfxAudioVolume;
        });
        return _audioSources;
    }

    /// <summary>
    /// 优先使用可复用空闲源，空闲不足时再按 round-robin 抢占，避免连续命中同一个 AudioSource。
    /// </summary>
    private int ResolveAudioSourceIndex(int preferredIndex)
    {
        if (_audioSources == null || _audioSources.Count == 0)
        {
            return 0;
        }

        int sourceCount = _audioSources.Count;
        int normalizedPreferred = preferredIndex >= 0 ? preferredIndex % sourceCount : -1;

        // 1) 调用方指定索引且该源空闲，优先尊重调用方意图。
        if (normalizedPreferred >= 0 && IsReusableSource(_audioSources[normalizedPreferred]))
        {
            _roundRobinCursor = (normalizedPreferred + 1) % sourceCount;
            return normalizedPreferred;
        }

        // 2) 从 round-robin 游标开始找空闲源，分散高频触发负载。
        for (int i = 0; i < sourceCount; i++)
        {
            int idx = (_roundRobinCursor + i) % sourceCount;
            if (IsReusableSource(_audioSources[idx]))
            {
                _roundRobinCursor = (idx + 1) % sourceCount;
                return idx;
            }
        }

        // 3) 都忙时按 round-robin 抢占，避免每次都抢同一源产生爆音。
        int fallbackIndex = _roundRobinCursor % sourceCount;
        _roundRobinCursor = (fallbackIndex + 1) % sourceCount;
        return fallbackIndex;
    }

    private static bool IsReusableSource(AudioSource source)
    {
        if (source == null)
        {
            return false;
        }

        if (source.clip == null)
        {
            return true;
        }

        return !source.isPlaying;
    }
    
    private void ReduceMusic()
    {
        if (SceneManager.GetActiveScene().name != "GameHall")
        {
            MessageHelper.Broadcast(MessageName.ReduceMusic, false);
        }
        else
        {
            AkSoundManager.Inst.ReduceMusic();
        }
    }

    public void RestoreMusic()
    {
        if (SceneManager.GetActiveScene().name != "GameHall")
        {
            MessageHelper.Broadcast(MessageName.RestoreMusic, false);
        }
        else
        {
            AkSoundManager.Inst.RestoreMusic();
        }
    }
}
