using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Audio;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using UIAgent;
using UnityEngine;
using UnityEngine.SceneManagement;
using xasset;

namespace Game.MusicalInstrument
{
    public enum PreviewAudioType
    {
        TwoD = 0,
        ThreeD = 1,
    }
    public class MusicalInstrumentManager : GlobalInstance<MusicalInstrumentManager>
    {
        private const string ShortWwiseName = "_YQShort_";
        private const string LongWwiseName = "_YQLong_";
        private const int PgcEmitterPoolInitialSize = 3;
        private const int PgcEmitterPoolSize = 15;
        private GameObject globalSoundObj;
        private readonly Dictionary<string, PgcEmitterPool> _pgcEmitterPoolDict = new Dictionary<string, PgcEmitterPool>();
        private readonly List<string> _invalidPgcPoolKeysBuffer = new List<string>();

        private class PgcEmitterPool
        {
            public GameObject owner;
            public readonly List<GameObject> emitters = new List<GameObject>();
            public int cursor;
        }

        public MusicalInstrumentManager()
        {
            globalSoundObj = GameObject.Find("GlobalMainCamera");
        }

        #region PreviewTone - 音色预览
        //PGC
        private Coroutine _curPgcPreviewToneCoroutinue;
        private string _curPgcStopPreviewName;

        public void PlaySingleUgcSyllable(string url, GameObject refObj)
        {
            UgcToneLoaderBehaviour ugcToneLoaderBehaviour = refObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
            ugcToneLoaderBehaviour.Stop();
            ugcToneLoaderBehaviour.LoadAudioClipAndPlay(PreviewAudioType.TwoD, url);
        }
        
        public void PreviewUgcTone(ToneInfo toneInfo, GameObject refObj, List<SyllableType> previewList = null)
        {
            if(toneInfo == null)
                return;

            if (previewList == null)
            {
                previewList = MusicalInstrumentUtils.Middle_Config;
            }

            var curUgcPreviewUrls = new List<string>();
        
            foreach (var syllableType in previewList)
            {
                if(toneInfo.toneDict.ContainsKey((int)syllableType))
                {
                    var syllableData = toneInfo.toneDict[(int)syllableType];
                    curUgcPreviewUrls.Add(syllableData.url);
                }
            }

            UgcToneLoaderBehaviour ugcToneLoaderBehaviour = refObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
            ugcToneLoaderBehaviour.Stop();
            ugcToneLoaderBehaviour.LoadMultipleAudioAndPlay(PreviewAudioType.TwoD, toneInfo.id, curUgcPreviewUrls);
        }

        public void StopPreviewUgcTone(GameObject refObj)
        {
            UgcToneLoaderBehaviour ugcToneLoaderBehaviour = refObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
            ugcToneLoaderBehaviour?.Stop();
        }

        public void PreviewPgcTone(InstrumentToneConfig instrumentToneConfig)
        {
            StopPreviewPgcTone();

            var syllableList = instrumentToneConfig.syllableList;
            if(syllableList == null || syllableList.Count == 0)
                return;

            var previewRange = MusicalInstrumentUtils.GetPreviewRange();
            List<string> eventNameList = new List<string>();
            for (int i = (int)previewRange.x - 1; i <= (int)previewRange.y - 1; i++)
            {
                eventNameList.Add(syllableList[i]);
            }
            _curPgcPreviewToneCoroutinue = CoroutineManager.Inst.StartCoroutine(PreviewPgcToneSyllable(eventNameList));
        }

        public void StopPreviewPgcTone()
        {
            if (_curPgcPreviewToneCoroutinue != null)
            {
                CoroutineManager.Inst.StopCoroutine(_curPgcPreviewToneCoroutinue);
            }

            if (!string.IsNullOrEmpty(_curPgcStopPreviewName))
            {
                AkSoundManager.Inst.StopSound(_curPgcStopPreviewName, globalSoundObj);
            }
        }

        IEnumerator PreviewPgcToneSyllable(List<string> eventNameList)
        {
            foreach (string eventName in eventNameList)
            {
                yield return CoroutineManager.Inst.StartCoroutine(PlaySinglePgcSyllable(eventName));
            }
        }

        IEnumerator PlaySinglePgcSyllable(string eventName)
        {
            var startName = "Play" + ShortWwiseName + eventName;
            var stopName = "Stop" + ShortWwiseName + eventName;
            _curPgcStopPreviewName = stopName;

            AkSoundManager.Inst.LoadPgcToneAsync(startName, (loadWwiseName) =>
            {
                if (loadWwiseName == startName)
                {
                    if (SceneManager.GetActiveScene().name != "GameHall")
                    {
                        MessageHelper.Broadcast(MessageName.ReduceMusic, true);
                    }

                    AkSoundManager.Inst.PlaySound("", "", startName, globalSoundObj);
                }
            }, OnPgcSoundBankLoadFail);

            // 播放2秒后暂停
            yield return new WaitForSeconds(1);

            if (SceneManager.GetActiveScene().name != "GameHall")
            {
                MessageHelper.Broadcast(MessageName.RestoreMusic, true);
            }
            AkSoundManager.Inst.StopSound(stopName, globalSoundObj);
        }
        #endregion
        

        /// <summary>
        ///  播放PGC音节
        /// </summary>
        /// <param name="toneInfo">pgc音色Info</param>
        /// <param name="syllableId">音节Id</param>
        /// <param name="longShortType">长短音</param>
        /// <param name="refObj"></param>
        public void PlaySingleSyllable(PreviewAudioType previewAudioType, ToneInfo toneInfo, List<int> syllableIdList, int longShortType, GameObject refObj)
        {
            if (toneInfo.IsPgc())
            {
                HandlePgcSyllableOp(previewAudioType, toneInfo.id, syllableIdList, longShortType, refObj);
            }
            else
            {
                HandleUgcSyllableOp(previewAudioType, toneInfo, syllableIdList, refObj);
            }
        }

        public void StopPreviewSyllable(PreviewAudioType audioType, ToneInfo toneInfo, GameObject refObj)
        {
            if(toneInfo == null)
                return;

            GameObject sessionObj = refObj;
            if (refObj == null || audioType == PreviewAudioType.TwoD)
                refObj = globalSoundObj;   

            if (toneInfo.IsPgc())
            {
                var pgcToneConfig = Es.DataTables.GetInstrumentToneConfig(toneInfo.id);

                if (pgcToneConfig == null)
                    return;

                var stopName = pgcToneConfig.stopName;
                bool hasPooledEmitters = StopPgcEmitterPool(audioType, refObj, sessionObj, toneInfo.id, stopName);
                if (refObj != null && !hasPooledEmitters)
                {
                    AkSoundManager.Inst.PostEventAsync(stopName, refObj);
                }

                if (SceneManager.GetActiveScene().name != "GameHall")
                {
                    MessageHelper.Broadcast(MessageName.RestoreMusic, true);
                }
            }
            else
            {
                var curRefObjAudioSources = refObj.GetComponentsInChildren<AudioSource>();
                if (curRefObjAudioSources != null)
                {
                   foreach (var src in curRefObjAudioSources)
                       AudioSourceFadeHelper.FadeOutAndStop(src);
                }
                RestoreMusic();
            }
        }

        private void HandlePgcSyllableOp(PreviewAudioType audioType, string tonePgcId, List<int> syllableIdList, int longShortType, GameObject refObj)
        {
            var pgcToneConfig = Es.DataTables.GetInstrumentToneConfig(tonePgcId);

            if (pgcToneConfig == null)
                return;

            for (int i = 0; i < syllableIdList.Count; i++)
            {
                var syllableId = syllableIdList[i];

                if (string.IsNullOrEmpty(tonePgcId))
                    return;

                if (syllableId <= 0)
                    return;

                GameObject sessionObj = refObj;
                if (refObj == null || audioType == PreviewAudioType.TwoD)
                    refObj = globalSoundObj;

                var syllableIndex = syllableId - 1;

                if (syllableIndex > pgcToneConfig.syllableList.Count)
                {
                    LoggerUtils.LogError("PlaySingleSyllable syllableId 有误");
                    return;
                }

                if (pgcToneConfig.syllableList.Count <= syllableIndex)
                {
                    LoggerUtils.LogError("音色列表索引超出");
                    return;
                }

                var syllableName = pgcToneConfig.syllableList[syllableIndex];

                LongShortType curLongShorType = (LongShortType)longShortType;

                //1.传入的是长音， 但是这个音色只支持短音
                if ((LongShortType)longShortType == LongShortType.Long &&
                    (LongShortType)pgcToneConfig.longShortType == LongShortType.Short)
                {
                    curLongShorType = LongShortType.Short;
                }

                //2.传入的是短音， 但是这个音色只支持长音
                if ((LongShortType)longShortType == LongShortType.Short &&
                    (LongShortType)pgcToneConfig.longShortType == LongShortType.Long)
                {
                    curLongShorType = LongShortType.Long;
                }

                var longShortWwiseName = curLongShorType == LongShortType.Short ? ShortWwiseName : LongWwiseName;

                var startName = "Play" + longShortWwiseName + syllableName;
                int emitterHintIndex = i;

                AkSoundManager.Inst.LoadPgcToneAsync(startName, (loadWwiseName) =>
                {
                    if (loadWwiseName == startName)
                    {
                        var targetEmitter = GetOrCreatePgcEmitter(audioType, refObj, sessionObj, tonePgcId, emitterHintIndex);
                        if (targetEmitter == null)
                        {
                            LoggerUtils.LogError("PGC emitter 创建失败，回退到 refObj 播放");
                            targetEmitter = refObj;
                        }

                        if (SceneManager.GetActiveScene().name != "GameHall")
                        {
                            MessageHelper.Broadcast(MessageName.ReduceMusic, true);
                        }

                        AkSoundManager.Inst.PlaySound("", "", startName, targetEmitter);
                    }
                    else
                    {
                        LoggerUtils.LogError("loadWwiseName == startName" + "loadWwiseName =  " + loadWwiseName + "  startName = " + startName);
                    }
                }, OnPgcSoundBankLoadFail);
            }
        }

        private void HandleUgcSyllableOp(PreviewAudioType previewAudioType, ToneInfo toneInfo, List<int> syllableIdList, GameObject refObj)
        {
            if (refObj == null)
                return;
            
            UgcToneLoaderBehaviour ugcToneLoaderBehaviour = refObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
            
            for (int i = 0; i < syllableIdList.Count; i++)
            {
                var syllableId = syllableIdList[i];

                if(i >= ugcToneLoaderBehaviour.MaxAudioSourceCount)
                    return;

                if(toneInfo == null)
                    return;

                if(toneInfo.toneDict == null || toneInfo.toneDict.Count == 0)
                    return;


                if (!toneInfo.toneDict.TryGetValue(syllableId, out SyllableData syllableData))
                {
                    return;
                }

                var downloadUrl = syllableData.url;
                
                ugcToneLoaderBehaviour.LoadAudioClipAndPlay(previewAudioType, downloadUrl, i);
            }
        }

        private bool StopPgcEmitterPool(PreviewAudioType audioType, GameObject ownerObj, GameObject sessionObj, string toneId, string stopName)
        {
            string poolKey = BuildPgcPoolKey(audioType, ownerObj, sessionObj, toneId);
            if (!_pgcEmitterPoolDict.TryGetValue(poolKey, out var pool) || pool == null || pool.emitters == null || pool.emitters.Count == 0)
            {
                return false;
            }

            bool hasAnyEmitter = false;
            for (int i = 0; i < pool.emitters.Count; i++)
            {
                var emitter = pool.emitters[i];
                if (emitter == null)
                {
                    continue;
                }

                hasAnyEmitter = true;
                var emitterSources = emitter.GetComponents<AudioSource>();
                foreach (var src in emitterSources)
                    AudioSourceFadeHelper.FadeOutAndStop(src);
            }
            return hasAnyEmitter;
        }

        private GameObject GetOrCreatePgcEmitter(PreviewAudioType audioType, GameObject owner, GameObject sessionObj, string toneId, int preferredIndex)
        {
            if (owner == null)
            {
                return null;
            }

            CleanupInvalidPgcPools();

            string poolKey = BuildPgcPoolKey(audioType, owner, sessionObj, toneId);
            if (!_pgcEmitterPoolDict.TryGetValue(poolKey, out var pool) || pool == null)
            {
                pool = new PgcEmitterPool
                {
                    owner = owner,
                    cursor = 0,
                };
                _pgcEmitterPoolDict[poolKey] = pool;
            }

            if (pool.owner == null)
            {
                pool.owner = owner;
            }

            for (int i = pool.emitters.Count - 1; i >= 0; i--)
            {
                if (pool.emitters[i] == null)
                {
                    pool.emitters.RemoveAt(i);
                }
            }

            if (pool.emitters.Count <= 0)
            {
                int targetCount = Mathf.Clamp(PgcEmitterPoolInitialSize, 1, PgcEmitterPoolSize);
                for (int i = 0; i < targetCount; i++)
                {
                    var emitter = new GameObject($"PgcToneEmitter_{audioType}_{i}");
                    emitter.transform.SetParent(owner.transform, false);
                    pool.emitters.Add(emitter);
                }
            }

            if (pool.emitters.Count == 0)
            {
                return owner;
            }

            int emitterCount = pool.emitters.Count;
            int preferred = preferredIndex >= 0 ? preferredIndex % emitterCount : -1;

            if (preferred >= 0)
            {
                var preferredEmitter = pool.emitters[preferred];
                var preferredSource = preferredEmitter != null ? preferredEmitter.GetComponent<AudioSource>() : null;
                if (preferredEmitter != null && (preferredSource == null || !preferredSource.isPlaying))
                {
                    pool.cursor = (preferred + 1) % emitterCount;
                    return preferredEmitter;
                }
            }

            for (int i = 0; i < emitterCount; i++)
            {
                int idx = (pool.cursor + i) % emitterCount;
                var emitter = pool.emitters[idx];
                if (emitter == null)
                {
                    continue;
                }

                var source = emitter.GetComponent<AudioSource>();
                if (source == null || !source.isPlaying)
                {
                    pool.cursor = (idx + 1) % emitterCount;
                    return emitter;
                }
            }

            if (pool.emitters.Count < PgcEmitterPoolSize)
            {
                int newIndex = pool.emitters.Count;
                var emitter = new GameObject($"PgcToneEmitter_{audioType}_{newIndex}");
                emitter.transform.SetParent(owner.transform, false);
                pool.emitters.Add(emitter);
                pool.cursor = (newIndex + 1) % pool.emitters.Count;
                return emitter;
            }

            int fallbackIdx = pool.cursor % emitterCount;
            pool.cursor = (fallbackIdx + 1) % emitterCount;
            return pool.emitters[fallbackIdx];
        }

        private string BuildPgcPoolKey(PreviewAudioType audioType, GameObject owner, GameObject sessionObj, string toneId)
        {
            int ownerId = owner != null ? owner.GetInstanceID() : 0;
            int sessionId = sessionObj != null ? sessionObj.GetInstanceID() : 0;
            string safeToneId = string.IsNullOrEmpty(toneId) ? "none" : toneId;
            return $"{audioType}_{ownerId}_{sessionId}_{safeToneId}";
        }

        private void CleanupInvalidPgcPools()
        {
            if (_pgcEmitterPoolDict.Count == 0)
            {
                return;
            }

            _invalidPgcPoolKeysBuffer.Clear();
            foreach (var kv in _pgcEmitterPoolDict)
            {
                if (kv.Value == null || kv.Value.owner == null)
                {
                    _invalidPgcPoolKeysBuffer.Add(kv.Key);
                }
            }

            for (int i = 0; i < _invalidPgcPoolKeysBuffer.Count; i++)
            {
                _pgcEmitterPoolDict.Remove(_invalidPgcPoolKeysBuffer[i]);
            }
        }
        //判断PGC乐器是否为15音且乐谱为22音
        public void CheckPGCInstrumentCanPlayMusicScore(string pgcId,MusicScoreInfo mInfo,Action showTips)
        {
            if (mInfo == null)
            {
                return;
            }
            var iData = DataTables.GetInstrumentConfig(pgcId);
            if (iData==null)
            {
                return;
            }
            var pgcConfig = Es.DataTables.GetInstrumentToneConfig(iData.toneId);
            if (pgcConfig.toneType == (int)ToneType.Fifteen&&mInfo.toneType == (int)ToneType.TwentyTwo)
            {
                showTips?.Invoke();
            }
        }
        //判断UGC乐器是否为15音且乐谱为22音
        public void CheckInstrumentCanPlayMusicScore(ToneInfo tInfo,MusicScoreInfo mInfo,Action showTips)
        {
            if (tInfo == null||mInfo == null)
            {
                return;
            }
            if (tInfo.toneType == (int)ToneType.Fifteen&&mInfo.toneType == (int)ToneType.TwentyTwo)
            {
                showTips?.Invoke();
            }
        }

        public void CheckInstrumentIsUgcToneAndShowToast(ToneInfo toneInfo)
        {

        }

        /// <summary>
        /// 提前下载音色中的音节
        /// </summary>
        /// <param name="toneInfo"></param>
        /// <param name="onComplete"></param>
        public void PreLoadToneSyllableFiles(ToneInfo toneInfo, Action<string> onComplete, GameObject refObj)
        {
            if (toneInfo.IsPgc())
            {
                onComplete?.Invoke(toneInfo.id);
                return;
            }

            List<string> downloadUrls = new List<string>();
            foreach (var syllableData in toneInfo.toneDict.Values)
            {
                downloadUrls.Add(syllableData.url);
            }
            
            onComplete?.Invoke(toneInfo.id);
            // UgcToneLoaderBehaviour ugcToneLoaderBehaviour = refObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
            // ugcToneLoaderBehaviour.DownloadMultipleAudio(toneInfo.id, downloadUrls,
            //     (toneId, clipDict) =>
            //     {
            //         if (toneInfo.id == toneId)
            //         {
            //             onComplete?.Invoke(toneInfo.id);
            //         }
            //     });
        }

        private void OnPgcSoundBankLoadFail()
        {
            UIAgentManager.Inst.OpenPanel(PanelId.TipPanel, "对不起>< 音频下载失败，请重试TT");
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
}
