using EventTracking;
using Game.Base;
using Game.CommunityGame;
using Game.PropStore;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

/// <summary>
/// Author:
/// Desc:
/// Date:24-07-02 19:52:35
/// </summary>
public class AssetStudioCategoryPanel : BasePanel<AssetStudioCategoryPanel>
{
    [SerializeField] private CButton BackBtn;
    [SerializeField] private CButton MapStudioBtn;
    [SerializeField] private CButton PropStudioBtn;
    [SerializeField] private CButton PropStoreBtn;
    [SerializeField] private CButton MaterialStudioBtn;
    [SerializeField] private CButton MaterialStoreBtn;
    
    public override void OnCreate()
    {
        AddListeners();
        LoadEvent.ReportTask(157, 1);
    }

    private void AddListeners()
    {
        BackBtn?.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            UIManager.Inst.ClosePanel(this);
        });
        
        MapStudioBtn?.onClick.AddListener(OnClickMapStudio);
        PropStudioBtn?.onClick.AddListener(OnClickPropStudio);
        PropStoreBtn?.onClick.AddListener(OnClickPropStore);
        MaterialStudioBtn?.onClick.AddListener(OnClickMaterialStudio);
        MaterialStoreBtn?.onClick.AddListener(OnClickMaterialStore);
    }

    private void OnClickMapStudio()
    {
        UIManager.Inst.OpenPanel(PanelId.GameStudioPanel);
    }
    
    private void OnClickPropStudio()
    {
        UIManager.Inst.OpenPanel(PanelId.AssetStudioDraftCommonPanel, MainViewType.Prop);
    }
    
    private void OnClickPropStore()
    {
        UIManager.Inst.OpenPanel<PropStorePanel>(PanelId.PropStorePanel);
    }
    
    private void OnClickMaterialStudio()
    {
        UIManager.Inst.OpenPanel(PanelId.AssetStudioDraftCommonPanel, MainViewType.Material);
    }
    
    private void OnClickMaterialStore()
    {
        UIManager.Inst.OpenPanel<MaterialStorePanel>(PanelId.MaterialStorePanel);
    }
    
    public override void OnShow(params object[] args)
    {
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
    }
    
    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }
}