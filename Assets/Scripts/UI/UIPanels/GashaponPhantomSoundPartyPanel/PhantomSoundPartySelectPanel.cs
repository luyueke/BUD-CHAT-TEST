using System;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PhantomSoundPartySelectPanel : BasePanel<PhantomSoundPartySelectPanel>
{
    public Image selectedImg;
    [SerializeField] private CButton confirmBtn;
    [SerializeField] private CButton cancelBtn;
    [SerializeField] private Text txtTips;


    private Action _onConfirm;

    public override void OnCreate()
    {
        base.OnCreate();
        confirmBtn.onClick.AddListener(OnConfirmClick);
        cancelBtn.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _onConfirm = null;
        if (args == null || args.Length == 0) return;

        string pgcId = args[0] as string;
        if (args.Length > 1) _onConfirm = args[1] as Action;

        if (selectedImg != null && !string.IsNullOrEmpty(pgcId))
        {
            selectedImg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(
                "Assets/Loadable/UI/UIPanel/GashaponPhantomSoundPartyPanel/GashaponPhantomSoundPartyPanel.spriteatlas",
                pgcId, gameObject);
        }

        if (txtTips != null && !string.IsNullOrEmpty(pgcId))
        {
            string pgcName = Es.DataTables.GetPgcNameData(pgcId)?.Name ?? pgcId;
            txtTips.text = $"确认选择【{pgcName}】载具吗?选定后不可更改";
        }
    }

    private void OnConfirmClick()
    {
        CloseSelf();
        _onConfirm?.Invoke();
    }
}
