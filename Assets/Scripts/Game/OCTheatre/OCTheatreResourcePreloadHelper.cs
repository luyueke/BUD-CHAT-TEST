using System;
using System.Collections.Generic;
using Game.Audio;
using GameData.BaseInfo;
using UGCAsset;
using UnityEngine;
using xasset;

namespace GameData.Manager
{
    /// <summary>
    /// OCTheatre 资源预缓存帮助类，负责图片/动作/音频资源预下载。
    /// </summary>
    public class OCTheatreResourcePreloadHelper
    {
        private readonly List<RemoteImageWrapper> imageWrappers = new List<RemoteImageWrapper>();
        private readonly List<RemoteAssetRequest> remoteAssetRequests = new List<RemoteAssetRequest>();
        private readonly List<AudioRequest> audioRequests = new List<AudioRequest>();
        private readonly HashSet<string> preloadedTheatreIds = new HashSet<string>();
        private GameObject audioPreloadNode;
        private UgcToneLoaderBehaviour ugcToneLoader;
        private int preloadVersion;

        public void PreloadAll(
            OCTheatreInfo theatreInfo,
            OCTDetailInfoRuntime detailInfo,
            Dictionary<string, OCTheatreAvatarInfo> avatarDict,
            Action<float> onProgress = null,
            Action<bool> onDone = null)
        {
            if (theatreInfo == null || detailInfo == null)
            {
                onDone?.Invoke(false);
                return;
            }

            if (!string.IsNullOrEmpty(theatreInfo.id) && preloadedTheatreIds.Contains(theatreInfo.id))
            {
                onProgress?.Invoke(1f);
                onDone?.Invoke(true);
                return;
            }

            if (!string.IsNullOrEmpty(theatreInfo.id))
            {
                preloadedTheatreIds.Add(theatreInfo.id);
            }

            var imageUrls = new HashSet<string>();
            var pgcEmoteIds = new HashSet<string>();
            var ugcEmoteIds = new HashSet<string>();
            var audioUrlWithType = new List<(string url, int type)>();
            CollectPreloadTargets(detailInfo, avatarDict, imageUrls, pgcEmoteIds, ugcEmoteIds, audioUrlWithType);

            var totalTaskCount = imageUrls.Count + pgcEmoteIds.Count + ugcEmoteIds.Count + audioUrlWithType.Count;
            if (totalTaskCount <= 0)
            {
                onProgress?.Invoke(1f);
                onDone?.Invoke(true);
                return;
            }

            var currentVersion = ++preloadVersion;
            var completedTaskCount = 0;
            var finalResult = true;
            var isDone = false;

            void CompleteOne(bool success)
            {
                if (currentVersion != preloadVersion || isDone)
                {
                    return;
                }

                finalResult &= success;
                completedTaskCount++;
                onProgress?.Invoke(completedTaskCount * 1f / totalTaskCount);

                if (completedTaskCount >= totalTaskCount)
                {
                    isDone = true;
                    onDone?.Invoke(finalResult);
                }
            }

            onProgress?.Invoke(0f);

            foreach (var imageUrl in imageUrls)
            {
                PreloadImage(imageUrl, CompleteOne);
            }

            foreach (var emoteId in pgcEmoteIds)
            {
                PreloadPgcEmote(emoteId, CompleteOne);
            }

            foreach (var emoteId in ugcEmoteIds)
            {
                PreloadUgcEmote(emoteId, currentVersion, CompleteOne);
            }

            foreach (var audioInfo in audioUrlWithType)
            {
                PreloadAudio(audioInfo.url, audioInfo.type, CompleteOne);
            }
        }

        public void Cancel()
        {
            preloadVersion++;
        }

        public void Clear()
        {
            Cancel();

            foreach (var wrapper in imageWrappers)
            {
                wrapper?.request?.Release();
            }

            foreach (var request in remoteAssetRequests)
            {
                request?.Release();
            }

            foreach (var request in audioRequests)
            {
                request?.Release();
            }

            imageWrappers.Clear();
            remoteAssetRequests.Clear();
            audioRequests.Clear();
            preloadedTheatreIds.Clear();

            if (audioPreloadNode != null)
            {
                UnityEngine.Object.Destroy(audioPreloadNode);
                audioPreloadNode = null;
                ugcToneLoader = null;
            }
        }

        private static void CollectPreloadTargets(
            OCTDetailInfoRuntime detailInfo,
            Dictionary<string, OCTheatreAvatarInfo> avatarDict,
            HashSet<string> imageUrls,
            HashSet<string> pgcEmoteIds,
            HashSet<string> ugcEmoteIds,
            List<(string url, int type)> audioUrlWithType)
        {
            if (detailInfo.sections != null)
            {
                foreach (var section in detailInfo.sections.Values)
                {
                    if (section == null) continue;

                    if (!string.IsNullOrEmpty(section.backgroundURL))
                    {
                        imageUrls.Add(section.backgroundURL);
                    }

                    CollectEmote(section.emote, pgcEmoteIds, ugcEmoteIds);
                    CollectAudio(section.audio, audioUrlWithType);
                }
            }

            if (detailInfo.allAvatars != null && avatarDict != null)
            {
                foreach (var runtimeAvatar in detailInfo.allAvatars)
                {
                    if (runtimeAvatar == null || string.IsNullOrEmpty(runtimeAvatar.playerId)) continue;
                    if (!avatarDict.TryGetValue(runtimeAvatar.playerId, out var avatarInfo)) continue;
                    if (avatarInfo?.expressions == null) continue;

                    foreach (var expression in avatarInfo.expressions)
                    {
                        if (expression == null || string.IsNullOrEmpty(expression.expressionURL)) continue;
                        imageUrls.Add(expression.expressionURL);
                    }
                }
            }
        }

        private static void CollectEmote(OCTheatreEmote emote, HashSet<string> pgcEmoteIds, HashSet<string> ugcEmoteIds)
        {
            if (emote == null) return;
            var emoteId = emote.emoteID;
            if (string.IsNullOrEmpty(emoteId)) return;

            // PGC emoteId 纯数字；UGC emoteId 可能包含字母。
            if (int.TryParse(emoteId, out _))
            {
                pgcEmoteIds.Add(emoteId);
            }
            else
            {
                ugcEmoteIds.Add(emoteId);
            }
        }

        private static void CollectAudio(OCTheatreAudio audio, List<(string url, int type)> audioUrlWithType)
        {
            if (audio == null || string.IsNullOrEmpty(audio.audioURL)) return;

            // 约定: 0=商城音效, 1=上传音效URL
            if (audio.type == 0 || audio.type == 1)
            {
                audioUrlWithType.Add((audio.audioURL, audio.type));
            }
        }

        private void PreloadImage(string imageUrl, Action<bool> onComplete)
        {
            var wrapper = Loader.LoadRemoteImageAsync(imageUrl);
            if (wrapper == null || wrapper.request == null)
            {
                Debug.LogWarning($"[OCTheatrePreload] Preload image failed, invalid request: {imageUrl}");
                onComplete?.Invoke(false);
                return;
            }

            imageWrappers.Add(wrapper);
            if (wrapper.request.isDone)
            {
                onComplete?.Invoke(wrapper.request.result == Request.Result.Success);
                return;
            }

            wrapper.completed += success => { onComplete?.Invoke(success); };
        }

        private static void PreloadPgcEmote(string emoteId, Action<bool> onComplete)
        {
            var preloadGo = new GameObject($"OCTheatrePgcPreload_{emoteId}");
            var ctrl = preloadGo.AddComponent<PlayerAnimationCtrl>();
            ctrl.DownloadAnimationAB(emoteId, success =>
            {
                onComplete?.Invoke(success);
                UnityEngine.Object.Destroy(preloadGo);
            });
        }

        private void PreloadUgcEmote(string emoteId, int currentVersion, Action<bool> onComplete)
        {
            UGCAnimAssetManager.Inst.GetAssetInfo<UGCAnimGetResponse>(emoteId, animInfo =>
            {
                if (currentVersion != preloadVersion)
                {
                    return;
                }

                if (animInfo == null || string.IsNullOrEmpty(animInfo.metaDataUrl))
                {
                    Debug.LogWarning($"[OCTheatrePreload] Resolve UGC emote metaDataUrl failed, emoteId={emoteId}");
                    onComplete?.Invoke(false);
                    return;
                }

                var request = Asset.LoadRemoteAssetAsync(animInfo.metaDataUrl);
                if (request == null)
                {
                    Debug.LogWarning($"[OCTheatrePreload] Preload UGC emote failed, request is null, emoteId={emoteId}");
                    onComplete?.Invoke(false);
                    return;
                }

                remoteAssetRequests.Add(request);
                if (request.isDone)
                {
                    onComplete?.Invoke(request.result == Request.Result.Success);
                    return;
                }

                request.completed += _ => { onComplete?.Invoke(request.result == Request.Result.Success); };
            });
        }

        private void PreloadAudio(string audioUrl, int audioType, Action<bool> onComplete)
        {
            EnsureUgcToneLoader();
            if (ugcToneLoader == null)
            {
                Debug.LogWarning($"[OCTheatrePreload] Preload audio failed, tone loader is null. type={audioType}, url={audioUrl}");
                onComplete?.Invoke(false);
                return;
            }

            // 走 UgcToneLoaderBehaviour 的公开下载链路，保持与现有业务一致。
            ugcToneLoader.LoadUgcAudioByOriginUrl(audioUrl, (success, request) =>
            {
                if (request != null)
                {
                    audioRequests.Add(request);
                }

                onComplete?.Invoke(success);
            });
        }

        private void EnsureUgcToneLoader()
        {
            if (ugcToneLoader != null)
            {
                return;
            }

            audioPreloadNode = new GameObject("OCTheatreAudioPreloadNode");
            ugcToneLoader = audioPreloadNode.AddComponent<UgcToneLoaderBehaviour>();
            ugcToneLoader.MaxAudioSourceCount = 0;
        }
    }
}
