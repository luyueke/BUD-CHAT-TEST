using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkBlackPanel : BasePanel<AIParkBlackPanel>
    {
        public CanvasGroup canvasGroup;
        private Tweener fadeInTween;
        private Tweener fadeOutTween;

        public override void OnHidden()
        {
            base.OnHidden();
            fadeInTween?.Kill();
            fadeOutTween?.Kill();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            float waitTime = 1;
            float beginAlpha = 0;
            Action fadeOutCb = null;
            Action fadeInCb = null;
            if (args.Length > 0)
            {
                waitTime = args[0] is float ? (float)args[0] : 1;
            }
            if (args.Length > 1)
            {
                beginAlpha = args[1] is float ? (float)args[1] : 0;
            }
            if (args.Length > 2)
            {
                fadeOutCb = args[2] is Action ? (Action)args[2] : null;
            }
            if (args.Length > 3)
            {
                fadeInCb = args[3] is Action ? (Action)args[3] : null;
            }
            float fadeTime = 1;
            canvasGroup.alpha = beginAlpha;
            fadeInTween = canvasGroup.DOFade(1,fadeTime)
                .SetEase(Ease.OutQuart)
                .OnComplete(() =>
                {
                    TimerManager.Inst.RunOnce("AIParkBlackPanel", waitTime, () =>
                    {
                        fadeOutCb?.Invoke();
                        fadeOutTween = canvasGroup.DOFade(0, fadeTime)
                            .SetEase(Ease.InQuart)
                            .OnComplete(() =>
                            {
                                OnHidden();
                                CloseSelf();
                                fadeInCb?.Invoke();
                            });
                    });
                });
        }
    }
}