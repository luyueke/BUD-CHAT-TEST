// @Author: YangJie
// @Description:
// @Date:  2023/07/18
// @Modify:

using Game.Base;
using Game.Utils;
using GameData;
using SceneController.Attribute;
using UnityEngine;

namespace Game.Scene.ModeController
{
    [GameModel(GameMode.Play)]
    public class PlayModeController : BaseModeController
    {
        public override void EnterMode()
        {
            base.EnterMode();
            EnterPlayMode();
            LoggerUtils.Log("Enter Play Mode");
            foreach (var modeManager in GetAllModeManager())
            {
                modeManager.OnPlay();
            }
        }

        private void EnterPlayMode()
        {
            GameCameraUtils.Inst.GetEditVirtualCamera().enabled = false;
            GameCameraUtils.Inst.GetPlayVirtualCamera().enabled = true;
        }
    }
}