using Basic.Extensions;
using Basic.Utils;
using Cinemachine;
using Game.Base;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using GameSync.Manager;
using SceneController.Attribute;
using UnityEngine;

namespace Game.Scene.ModeController
{
    [GameModel(GameMode.AIGuset)]
    public class AIGusetModeController : BaseModeController
    {
        public override void EnterMode()
        {
            base.EnterMode();
            EnterGuestMode();
            LoggerUtils.Log("Enter Play Mode");
            foreach (var modeManager in GetAllModeManager())
            {
                modeManager.OnGuest();
            }
        }

        private void EnterGuestMode()
        {
            GameCameraUtils.Inst.GetEditVirtualCamera().enabled = false;
            var virtualCamera = GameCameraUtils.Inst.GetPlayVirtualCamera();
            virtualCamera.enabled = true;
            virtualCamera.m_Lens.FieldOfView = 50;
            var camTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            camTransposer.m_FollowOffset.z = -4.5f;
            
            if (GameController.enterGameModel == EnterGameModel.AIYandere)
            {
                virtualCamera.Follow.transform.localEulerAngles = new Vector3(14, 163, 0);
            }
            else if (GameController.enterGameModel == EnterGameModel.AIHospital)
            {
                virtualCamera.Follow.transform.localEulerAngles = new Vector3(-3.263f, 0.793f, 0);
            }
        }
    }
}

