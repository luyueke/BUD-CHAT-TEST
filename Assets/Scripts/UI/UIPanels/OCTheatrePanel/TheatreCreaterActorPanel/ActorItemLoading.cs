using DG.Tweening;
using UnityEngine;

public class ActorItemLoading : MonoBehaviour
{
    void Awake()
    {
        transform.DOLocalRotate(new Vector3(0f, 0f, -360f), 1f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    void OnDestroy()
    {
        transform.DOKill();
    }
}
