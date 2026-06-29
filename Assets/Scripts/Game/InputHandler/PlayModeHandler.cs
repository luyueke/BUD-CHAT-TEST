using Cinemachine;
using System;
using Game.Config;
using Game.Utils;
using UnityEngine;

public class PlayModeHandler : InputHandler
{
    private Transform camFollow;
    private CinemachineVirtualCamera VirtualCam;
    private CinemachineTransposer camTransposer;
    private Camera eCamera;
    public void InitCamera()
    {
        SetCamera(GameCameraUtils.Inst.GetMainCamera(), GameCameraUtils.Inst.GetEditVirtualCamera());
    }
    
    public void SetCamera(Camera cam, CinemachineVirtualCamera vCam)
    {
        eCamera = cam;
        VirtualCam = vCam;
        camFollow = vCam.Follow;
        GameCameraUtils.Inst.SetMainCameraCinemachineBrain(true);
    }
    
}
