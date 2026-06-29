using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
namespace Game
{
    /// <summary>
    /// 创建结果item
    /// </summary>
    public class ChatCreatingResultItem : MonoBehaviour
    {
        public RectTransform imageRect;
        Action completeCb;

        private const float BarMinWidth = 20f;
        private const float BarMaxWidth = 603f;
        private const float BarStallWidth = 590f;   // 未收到 EndProgress 时停在此处
        private const float NormalDuration = 35f;   // 从 20 到 590 的正常时长（秒）
        private const float FinishDuration = 0.4f;  // EndProgress 后冲到满的时长（秒）

        private Coroutine _progressCoroutine;
        private bool _endCalled = false;

        void Awake()
        {
            imageRect.sizeDelta = new(BarMinWidth, imageRect.sizeDelta.y);
        }

        /// <summary>
        /// 进度条从 20 缓慢增长到 590（约 35 秒），未收到 EndProgress 时停在 590
        /// </summary>
        public void BeginProgress()
        {
            _endCalled = false;
            if (_progressCoroutine != null)
                StopCoroutine(_progressCoroutine);
            _progressCoroutine = StartCoroutine(RunProgress());
        }

        /// <summary>
        /// 调用后进度条从当前位置加速冲到 603
        /// </summary>
        public void EndProgress(Action endAc)
        {
            completeCb = endAc;
            _endCalled = true;
            if (_progressCoroutine != null)
            {
                StopCoroutine(_progressCoroutine);
                _progressCoroutine = null;
            }
            _progressCoroutine = StartCoroutine(FinishProgress());
        }

        private IEnumerator RunProgress()
        {
            float startWidth = imageRect.sizeDelta.x;
            float elapsed = 0f;
            while (elapsed < NormalDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / NormalDuration);
                float w = Mathf.Lerp(startWidth, BarStallWidth, t);
                imageRect.sizeDelta = new Vector2(w, imageRect.sizeDelta.y);
                if (_endCalled) yield break;
                yield return null;
            }
            imageRect.sizeDelta = new Vector2(BarStallWidth, imageRect.sizeDelta.y);
        }

        private IEnumerator FinishProgress()
        {
            float startWidth = imageRect.sizeDelta.x;
            float elapsed = 0f;
            while (elapsed < FinishDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FinishDuration);
                float w = Mathf.Lerp(startWidth, BarMaxWidth, t);
                imageRect.sizeDelta = new Vector2(w, imageRect.sizeDelta.y);
                yield return null;
            }
            imageRect.sizeDelta = new Vector2(BarMaxWidth, imageRect.sizeDelta.y);
            completeCb?.Invoke();
        }
    }
}
