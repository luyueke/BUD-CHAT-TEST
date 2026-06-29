using Message;
using UnityEngine;
using UnityEngine.UI;

public class CameraModeCameraMenu: CameraModeMenuBase
{
    private CameraModeToggle lens_toggle;
    private CameraModeToggle view_toggle;
    private CameraModeToggle filter_toggle;
    private CameraModeToggle frame_toggle;

    private CameraModeViewMenu cameraModeViewMenu;
    private CameraModeFilterMenu cameraModeFilterMenu;
    private CameraModeFrameMenu cameraModeFrameMenu;

    private bool isLensMode = false;
    
    protected override void OnInit(){

        lens_toggle = GetComponentByName<CameraModeToggle>("ToggleLens");
        view_toggle = GetComponentByName<CameraModeToggle>("ToggleView");
        filter_toggle = GetComponentByName<CameraModeToggle>("ToggleFilter");
        frame_toggle = GetComponentByName<CameraModeToggle>("ToggleFrame");

        cameraModeViewMenu = transform.GetComponentInChildren<CameraModeViewMenu>(true);
        cameraModeFilterMenu = transform.GetComponentInChildren<CameraModeFilterMenu>(true);
        cameraModeFrameMenu = transform.GetComponentInChildren<CameraModeFrameMenu>(true);

        lens_toggle.Init();
        view_toggle.Init();
        filter_toggle.Init();
        frame_toggle.Init();

        cameraModeViewMenu.Init();
        cameraModeFilterMenu.Init();
        cameraModeFrameMenu.Init();

        lens_toggle.onValueChanged.AddListener(OnLensChange);
        view_toggle.onValueChanged.AddListener(OnViewChange);
        filter_toggle.onValueChanged.AddListener(OnFilterChange);
        frame_toggle.onValueChanged.AddListener(OnFrameChange);

        gameObject.SetActive(false); //默认隐藏
    }

    private void OnLensChange(bool isOn){
        HideAllSubMenu();
        if(isOn && !isLensMode){
            isLensMode = true;
            MessageHelper.Broadcast(MessageName.UICameraModeOpenLensControl);
        }else if (!isOn && isLensMode){
            isLensMode = false;
            MessageHelper.Broadcast(MessageName.UICameraModeCloseLensControl);
        }
    }

    private void OnViewChange(bool isOn){
        HideAllSubMenu();
        if(isOn){
            cameraModeViewMenu.Show();
        }
    }
    
    private void OnFilterChange(bool isOn){
        HideAllSubMenu();
        if(isOn){
            cameraModeFilterMenu.Show();
        }
    }

    private void OnFrameChange(bool isOn){
        HideAllSubMenu();
        if(isOn){
            cameraModeFrameMenu.Show();
        }
    }

    private void HideAllSubMenu(){
        cameraModeViewMenu.Hide();
        cameraModeFilterMenu.Hide();
        cameraModeFrameMenu.Hide();
    }

    protected override void OnShow()
    {
        MessageHelper.AddListener(MessageName.UICameraModeOpenLensControl, OnUICameraModeOpenLensControl);
    }

    protected override void OnHide()
    {
        MessageHelper.RemoveListener(MessageName.UICameraModeOpenLensControl, OnUICameraModeOpenLensControl);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.UICameraModeOpenLensControl, OnUICameraModeOpenLensControl);
    }

    private void OnUICameraModeOpenLensControl(){
        isLensMode = true;
        lens_toggle.isOn = true;
    }
}
