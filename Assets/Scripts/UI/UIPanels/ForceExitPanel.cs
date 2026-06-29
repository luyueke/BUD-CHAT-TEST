using Game.Base;
using GameData.Manager;
using UI.Base;
using UI.BaseWidgets;

public class ForceExitPanel : BasePanel<ForceExitPanel>
{
    public LoadingButton ConfirmButton;
    private bool _isExiting;

    public override void OnCreate()
    {
        base.OnCreate();
        ConfirmButton.onClick.AddListener(OnExitRoom);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        try
        {
            UIManager.Inst.ClosePanel(PanelId.AlbumPanel);
            UIManager.Inst.ClosePanel(PanelId.CameraModePanel);
            UIManager.Inst.ClosePanel(PanelId.BuyAlbumVolumePanel);
            UIManager.Inst.ClosePanel(PanelId.BigPhotoImgPanel);
            UIManager.Inst.ClosePanel(PanelId.CameraExpandPanel);
            UIManager.Inst.ClosePanel(PanelId.CameraAllPhotoPanel);
            UIManager.Inst.ClosePanel(PanelId.PhotoImgShowPanel);
            UIManager.Inst.ClosePanel(PanelId.PhotoSharePanel);
            UIManager.Inst.ClosePanel(PanelId.MapPhotoShowPanel);
            UIManager.Inst.ClosePanel(PanelId.CameraNoticePanel);
        }
        catch (System.Exception)
        {
            //不处理
            LoggerUtils.LogError("退出房间时关闭面板失败,不处理");
        }
    }

    private void OnExitRoom()
    {
        if (_isExiting)
        {
            return;
        }
        _isExiting = true;
        ConfirmButton.ShowLoading();
        ConfirmButton.SetClickAble(false);
        
        GameController.ExitGame(() =>
        {
            GameDataManager.Inst.gameOnlineData.Clear();
            UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.GuestWindow, true);
            UIManager.Inst.ClosePanel(PanelId.UIOperationOnWorldPanel);
            UIManager.Inst.ClosePanel(PanelId.GameGuestPanel);
            UIManager.Inst.BackToLastWindow();
        });
    }
}
