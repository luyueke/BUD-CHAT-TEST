using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GameData;
using Message;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class WaterCameraEffectPanel : BasePanel<WaterCameraEffectPanel>
{
    private Image effectImg;
    private CanvasGroup _canvasGroup;
    private Tween showAnim;

    public override void OnCreate()
    {
        base.OnCreate();
        effectImg = transform.Find("effect").GetComponent<Image>();
        _canvasGroup = effectImg.GetComponent<CanvasGroup>();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        var color = (Color)args[0];
        SetEffectImg(color);
        ClearAnim();
        _canvasGroup.alpha = 0.7f;
        showAnim = _canvasGroup.DOFade(0.96f,0.5f);
    }


    public void SetEffectImg(Color color)
    {
        effectImg.material.SetColor("_MainTex_Color",color);
    }

    private void ClearAnim()
    {
        if (showAnim != null)
        {
            showAnim.Kill();
            showAnim = null;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearAnim();
    }
}