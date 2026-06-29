using Message;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CameraMenuType
{
    None,
    Action,
    Npc,
    Oc,
    Camera
}

public class CameraModeMainMenu : CameraModeMenuBase
{
    private ToggleGroup toggle_group;
    private CameraModeToggle action_toggle;
    private CameraModeToggle npc_toggle;
    private CameraModeToggle change_toggle;
    private CameraModeToggle camera_toggle;
    private CameraMenuType curCameraMode = CameraMenuType.None;

    private CameraModeActionMenu cameraModeActionMenu;
    private CameraModeNpcMenu cameraModeNpcMenu;
    private CameraModeOcMenu cameraModeOcMenu;
    private CameraModeCameraMenu cameraModeCameraMenu;

    protected override void OnInit(){
        isShowAnim = false;

        toggle_group = GetComponentByName<ToggleGroup>("ToggleMainGroup");
        toggle_group.allowSwitchOff = true;
        toggle_group.SetAllTogglesOff();
        action_toggle = GetComponentByName<CameraModeToggle>("ToggleAction");
        npc_toggle = GetComponentByName<CameraModeToggle>("ToggleNpc");
        change_toggle = GetComponentByName<CameraModeToggle>("ToggleChange");
        camera_toggle = GetComponentByName<CameraModeToggle>("ToggleCamera");

        cameraModeActionMenu = transform.GetComponentInChildren<CameraModeActionMenu>(true);
        cameraModeNpcMenu = transform.GetComponentInChildren<CameraModeNpcMenu>(true);
        cameraModeOcMenu = transform.GetComponentInChildren<CameraModeOcMenu>(true);
        cameraModeCameraMenu = transform.GetComponentInChildren<CameraModeCameraMenu>(true);

        cameraModeActionMenu.Init();
        cameraModeNpcMenu.Init();
        cameraModeOcMenu.Init();
        cameraModeCameraMenu.Init();

        if(action_toggle != null) {
            action_toggle.Init(); 
            action_toggle.onValueChanged.AddListener(OnActionChange);
            action_toggle.isOn = false;
        }
        if(npc_toggle != null) {
            npc_toggle.Init(); 
            npc_toggle.onValueChanged.AddListener(OnNpcChange);
            npc_toggle.isOn = false;
        }
        if(change_toggle != null) {
            change_toggle.Init(); 
            change_toggle.onValueChanged.AddListener(OnOcChange);
            change_toggle.isOn = false;
        }
        if(camera_toggle != null) {
            camera_toggle.Init(); 
            camera_toggle.onValueChanged.AddListener(OnCameraChange);
            camera_toggle.isOn = false;
        }

        //cameraModeActionMenu.Show(); //默认显示Action菜单
        MessageHelper.AddListener(MessageName.UICameraModeCloseCurrent, OnUICameraModeCloseCurrent);
        MessageHelper.AddListener(MessageName.UICameraModeGlobalClose, OnUICameraModeCloseCurrent);
        MessageHelper.AddListener(MessageName.UICameraModeOpenNpc, OnUICameraModeOpenNpc);
    }

    private void OnDestroy() {
        MessageHelper.RemoveListener(MessageName.UICameraModeCloseCurrent, OnUICameraModeCloseCurrent);
        MessageHelper.RemoveListener(MessageName.UICameraModeGlobalClose, OnUICameraModeCloseCurrent);
        MessageHelper.RemoveListener(MessageName.UICameraModeOpenNpc, OnUICameraModeOpenNpc);
    }

    // 世界中点击 AI 伙伴"互动选项"时，CameraMode 下改为打开 NPC 子菜单
    private void OnUICameraModeOpenNpc()
    {
        if (npc_toggle == null) return;
        if (npc_toggle.isOn)
        {
            // 已在 NPC 菜单：确保子菜单已展开
            if (curCameraMode != CameraMenuType.Npc)
            {
                curCameraMode = CameraMenuType.Npc;
                ShowSubMenu();
            }
        }
        else
        {
            npc_toggle.isOn = true; // 触发 OnNpcChange → ShowSubMenu
        }
    }

    private void OnUICameraModeCloseCurrent(){
        toggle_group.allowSwitchOff = true;
        toggle_group.SetAllTogglesOff();
        curCameraMode = CameraMenuType.None;
        HideSubMenu();
    }


    private void OnActionChange(bool isOn){

        if(isOn && curCameraMode != CameraMenuType.Action){
            curCameraMode = CameraMenuType.Action;
            ShowSubMenu();
        }
    }

    private void OnNpcChange(bool isOn){

        if(isOn && curCameraMode != CameraMenuType.Npc){
            curCameraMode = CameraMenuType.Npc;
            ShowSubMenu();
        }
    }

    private void OnOcChange(bool isOn){

        if(isOn && curCameraMode != CameraMenuType.Oc){
            curCameraMode = CameraMenuType.Oc;
            ShowSubMenu();
        }
    }

    private void OnCameraChange(bool isOn){

        if(isOn && curCameraMode != CameraMenuType.Camera){
            curCameraMode = CameraMenuType.Camera;
            ShowSubMenu();
        }else if (!isOn && curCameraMode == CameraMenuType.Camera){
            MessageHelper.Broadcast(MessageName.UICameraModeCloseLensControl);
        }
    }

    private void ShowSubMenu(){
        HideSubMenu();
        switch(curCameraMode){
            case CameraMenuType.Action:
                cameraModeActionMenu.Show();
                break;
            case CameraMenuType.Npc:
                cameraModeNpcMenu.Show();
                break;
            case CameraMenuType.Oc:
                cameraModeOcMenu.Show();
                break;
            case CameraMenuType.Camera:
                cameraModeCameraMenu.Show();
                break;
        }

        if(toggle_group.allowSwitchOff){
            toggle_group.allowSwitchOff = false;
        }
    }

    private void HideSubMenu(){
        cameraModeActionMenu.Hide();
        cameraModeNpcMenu.Hide();
        cameraModeOcMenu.Hide();
        cameraModeCameraMenu.Hide();
    }

    protected override void OnShow()
    {
        
    }

    protected override void OnHide()
    {
        
    }
}
