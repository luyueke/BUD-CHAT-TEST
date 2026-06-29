using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class ShapeHintPanel : BasePanel<ShapeHintPanel>
{
    [SerializeField] private Text panelTitle;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private CButton backBtn;
    [SerializeField] private Button blankBtn;
    [SerializeField] private CButton enterBtn;
    public override void OnCreate()
    {
        base.OnCreate();
        backBtn.onClick.AddListener(OnBackBtnClick);
        blankBtn.onClick.AddListener(OnBackBtnClick);
        enterBtn.onClick.AddListener(OnBackBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

    }

    private void OnBackBtnClick()
    {
        CloseSelf();
    }
}
