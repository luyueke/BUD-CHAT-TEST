using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TreasureHuntSkinPanel : BasePanel<CatGiftSkinShowPanel>
{
    [SerializeField] private CButton backBtn;
    [SerializeField] private Button blankBtn;

    public override void OnCreate()
    {
        backBtn.onClick.AddListener(CloseSelf);
        blankBtn.onClick.AddListener(CloseSelf);
    }

}
