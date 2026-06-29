using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TheatreLoadingBoard : MonoBehaviour{

    [SerializeField] private Slider loadingSlider;
    [SerializeField] private Text loadingText;
    [SerializeField] private float progressLerpSpeed = 1.8f;
    [SerializeField] private float minSegmentDuration = 1.5f;

    private float targetProgress;

    public float TargetProgress
    {
        get => targetProgress;
        set => targetProgress = Mathf.Clamp01(value);
    }

    public float CurrentProgress => loadingSlider == null ? 0f : loadingSlider.value;

    public void Show(string text = null)
    {
        gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(text))
        {
            SetText(text);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetText(string text)
    {
        if (loadingText == null) return;
        loadingText.SetLocalText(text ?? string.Empty);
    }

    public void ResetProgress(float progress = 0f)
    {
        SetProgressImmediate(progress);
    }

    public void SetProgressImmediate(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
        if (loadingSlider != null)
        {
            loadingSlider.value = targetProgress;
        }
    }

    public void TickProgress()
    {
        if (loadingSlider == null) return;
        loadingSlider.value = Mathf.MoveTowards(
            loadingSlider.value,
            targetProgress,
            Time.deltaTime * progressLerpSpeed);
    }

    public IEnumerator SmoothToTarget(Func<bool> continuePredicate = null)
    {
        if (loadingSlider == null) yield break;
        float elapsed = 0f;
        float minDuration = Mathf.Max(0f, minSegmentDuration);
        while ((continuePredicate == null || continuePredicate()) &&
               (loadingSlider.value < targetProgress - 0.0001f || elapsed < minDuration))
        {
            if (loadingSlider.value < targetProgress - 0.0001f)
            {
                TickProgress();
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetProgressImmediate(targetProgress);
    }
}