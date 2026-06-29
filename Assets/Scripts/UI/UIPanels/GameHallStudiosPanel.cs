using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class GameHallStudiosPanel : BasePanel<GameHallStudiosPanel>
{
    public CButton Btn_Close;
    public CButton Btn_AvatarStudio;
    public CButton Btn_PetStudio;
    public CButton Btn_GameStudio;
    public CButton Btn_InstrumentStudio;
    public CButton Btn_AnimStudio;
    public CButton Btn_NpcStudio;
    public CButton Btn_VehicleStudio;
    public CButton Btn_TheatreStudio;
    public CButton Btn_BudBoxStudio;
    public override void OnCreate()
    {
        base.OnCreate();
        
        Btn_Close.onClick.AddListener(CloseSelf);
        
        Btn_AvatarStudio.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel,CharacterStyle.Avatar);
            CloseSelf();
        });

        Btn_PetStudio.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel,CharacterStyle.Pet);
            CloseSelf();
        });

        Btn_GameStudio.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AssetStudioCategoryPanel);
            CloseSelf();
        });

        Btn_InstrumentStudio.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.InstrumentStudioCategoryPanel);
            CloseSelf();
        });
        
        Btn_AnimStudio.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AnimationStudioCategoryPanel);
            CloseSelf();
        });
        
        Btn_NpcStudio.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.AINpcStudioMainPanel);
            CloseSelf();
        });

        Btn_VehicleStudio.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.VehicleStudioCategoryPanel);
            CloseSelf();
        });
        Btn_TheatreStudio.onClick.AddListener((() =>
        {
            UIManager.Inst.OpenPanel(PanelId.TheatreStudioCategoryPanel);
            CloseSelf();
        }));
        Btn_BudBoxStudio.onClick.AddListener((() =>
        {
            UIManager.Inst.OpenPanel(PanelId.BudBoxStudioPanel);
            CloseSelf();
        }));
    }
}
