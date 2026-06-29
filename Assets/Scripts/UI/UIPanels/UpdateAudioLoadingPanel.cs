using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class UpdateAudioLoadingPanel : BasePanel<UpdateAudioLoadingPanel>
{
    public CButton Btn_Close;
    public Transform LoadingNode;
    private Tween _loadingTween;
    private Action _onCloseAct;
    
    public override void OnCreate()
    {
        base.OnCreate();
        Btn_Close.onClick.AddListener(OnBtnCloseClick);
        _loadingTween?.Kill();
        _loadingTween = LoadingNode.DOLocalRotate(new Vector3(0, 0, -720), 2f).SetLoops(-1);
    }

    public void SetOnCloseAct(Action act)
    {
        this._onCloseAct = act;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _loadingTween?.Kill();
    }

    private void OnBtnCloseClick()
    {
        this._onCloseAct?.Invoke();
        CloseSelf();
    }
}
