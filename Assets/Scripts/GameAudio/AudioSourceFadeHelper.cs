using System;
using System.Collections;
using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// 对 AudioSource 进行线性渐消停止，替代 AudioSource.Stop() 的波形截断式停止。
    /// 仅用于乐器音色停止路径，不影响其他音频模块。
    ///
    /// 设计要点：
    /// 1. 渐消协程运行在 CoroutineManager 上，与调用方生命周期解耦。
    /// 2. 通过 clip 引用监测 AudioSource 是否被复用——若新播放已替换 clip，
    ///    协程自动退出，不干扰新播放，也不修改音量。
    /// 3. 渐消完成后恢复原始音量，保证下次播放时音量正确。
    /// </summary>
    public static class AudioSourceFadeHelper
    {
        /// <summary>
        /// 乐器音色停止的默认渐消时长（60 ms）。
        /// 足以消除 PCM 截断噪音，且在最高 BPM 场景下不影响节奏感知。
        /// </summary>
        public const float InstrumentFadeDuration = 0.06f;

        /// <summary>
        /// 对指定 AudioSource 执行渐消并在完成后停止。
        /// 若 source 未在播放则立即触发 onStopped 回调并返回。
        /// </summary>
        /// <param name="source">目标 AudioSource</param>
        /// <param name="fadeDuration">渐消时长（秒），默认 60 ms</param>
        /// <param name="onStopped">渐消完成且 Stop 调用后的回调（可空）</param>
        public static void FadeOutAndStop(AudioSource source,
            float fadeDuration = InstrumentFadeDuration,
            Action onStopped = null)
        {
            if (source == null)
            {
                onStopped?.Invoke();
                return;
            }

            if (!source.isPlaying)
            {
                onStopped?.Invoke();
                return;
            }

            CoroutineManager.Inst.StartCoroutine(FadeOutCoroutine(source, fadeDuration, onStopped));
        }

        private static IEnumerator FadeOutCoroutine(AudioSource source, float fadeDuration, Action onStopped)
        {
            AudioClip originalClip = source.clip;
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                if (source == null)
                    yield break;

                // source 已被复用于新的播放（clip 被替换），立即退出，
                // 不修改音量，不调用 Stop，让新播放正常运行。
                if (source.clip != originalClip)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }

            if (source != null && source.clip == originalClip)
            {
                source.Stop();
                source.volume = startVolume; // 恢复音量，供下次 Play() 使用
            }

            onStopped?.Invoke();
        }
    }
}
