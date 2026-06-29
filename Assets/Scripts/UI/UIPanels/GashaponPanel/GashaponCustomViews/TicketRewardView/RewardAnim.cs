using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardAnim : MonoBehaviour
{
    [SerializeField]
    private float delay = 0.5f;
    [SerializeField]
    private float duration = 0.5f;

    private void Start()
    {
        Invoke(nameof(PlayAnim), delay);
    }

    private void PlayAnim()
    {
        var rectTransform = transform.GetComponent<RectTransform>();
        var localPos = rectTransform.anchoredPosition;
        var targetPos = new Vector3(localPos.x, localPos.y + 30);
        var sequence = DOTween.Sequence();
        sequence.Append(rectTransform.DOAnchorPos(targetPos, duration));
        sequence.Append(rectTransform.DOAnchorPos(localPos, duration));
        sequence.SetLoops(-1, LoopType.Yoyo);
        sequence.Play();
    }
}
