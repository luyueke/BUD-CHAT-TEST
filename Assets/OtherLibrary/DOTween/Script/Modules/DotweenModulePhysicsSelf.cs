using UnityEngine;

namespace DG.Tweening
{
    public static class DotweenModulePhysicsSelf
    {
        public static void DoPingPongMoveY(this Transform transform, float from, float to, float duration, Ease upEase, Ease downEase)
        {
            transform.DOLocalMoveY(to, duration).SetEase(upEase)
                    .OnComplete(() => transform.DoPingPongMoveY(to, from, duration, downEase, upEase));
        }
        
    }
}