using DG.Tweening.Core;
using DG.Tweening.Core.Enums;

namespace DG.Tweening
{
    public static class DOTweenModuleSequence
    {
        public static void InitStartValue(this Tween t)
        {
            if (t.tweenType == TweenType.Sequence)
            {
                Sequence s = (Sequence)t;

                for (int i = 0; i < s.sequencedTweens.Count; i++)
                {
                    Tween subT = s.sequencedTweens[i];

                    if (subT.tweenType == TweenType.Sequence)
                    {
                        subT.InitStartValue();
                    } else if (subT.tweenType == TweenType.Tweener){
                        // 全部执行一遍到结尾
                        TweenManager.Goto(subT, subT.duration * subT.loops, false, UpdateMode.IgnoreOnComplete);
                    } 
                }
            }
        }
    }
}