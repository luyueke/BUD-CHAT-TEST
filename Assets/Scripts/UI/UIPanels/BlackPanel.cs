using UI.Base;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using System;

/// <summary>
/// Author:
/// Desc:
/// Date:23-08-16 17:25:06
/// </summary>
public class BlackPanel : BasePanel<BlackPanel>
{
    public Image BlackImage;

    private Sequence panelSequence;
    private Action callBack;

    void PlayTransitionAnim()
    {

        if (panelSequence != null)
        {
            return;
        }
        InitColor();
        panelSequence = DOTween.Sequence();
        panelSequence.Append(DOTween.ToAlpha(() => BlackImage.color, x => BlackImage.color = x, 1, 0.5f).SetTarget(BlackImage));
        panelSequence.Append(DOTween.ToAlpha(() => BlackImage.color, x => BlackImage.color = x, 0, 0.8f).SetTarget(BlackImage));
        panelSequence.AppendCallback(() => OnComplete());
    }

    void InitColor()
    {
        BlackImage.color = new Color(0, 0, 0, 0);
    }

    void OnComplete()
    {
        UIManager.Inst.ClosePanel(this);
    }

    public override void OnCreate()
    {
    }

    public override void OnShow(params object[] args)
    {
        if (args != null && args.Length > 0)
        {
            try
            {
                bool isAutoPlayAnim = (bool)args[0];
                if (isAutoPlayAnim)
                {
                    PlayTransitionAnim();
                }
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("BlackPanel OnShow Error:"+e.StackTrace);
            }
        }
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
        if (panelSequence != null)
        {
            panelSequence.Kill();
        }
        panelSequence = null;
        callBack = null;
    }
    
    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }
}