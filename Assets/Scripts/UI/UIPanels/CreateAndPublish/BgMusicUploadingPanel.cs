using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class BgMusicUploadingPanel : BasePanel<BgMusicUploadingPanel>
{
    private Image _imgTopContent;
    private CButton closeButton;

    private Action onClose;

    protected override void Awake() {
        base.Awake();
        _imgTopContent = GameObjectEx.FindComponentByName<Image>(transform, "topPanel");
        closeButton = GameObjectEx.FindComponentByName<CButton>(transform, "Content/CloseBtn");
        closeButton.onClick.AddListener(OnCloseCallBack);
    }

    public override void OnShow(params object[] args) {
        base.OnShow(args);
        onClose = args[0] as Action;
    }

    public void SetTopColor(string colorhex)
    {
        _imgTopContent.color = DataUtil.DeSerializeColorCheckHash(colorhex);
    }

    private void OnCloseCallBack() {
        onClose?.Invoke();
        CloseSelf();
    }

}
