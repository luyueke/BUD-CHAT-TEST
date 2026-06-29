// @Author: YangJie
// @Description:
// @Date:  2023/07/12
// @Modify:

using Basic.Extensions;
using Basic.Utils;
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
    [GameModel(GameMode.Guest)]
    public class GuestModeController : BaseModeController
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
            GameCameraUtils.Inst.GetPlayVirtualCamera().enabled = true;
            UGCCommonReq.Inst.UGCBuyReq(GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().id, isSuccess =>
            {
                LoggerUtils.Log($"消费地图 [{GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().id}]: {isSuccess}");
            });
            MainThreadDispatcher.Init();
            //开始请求连接服务器
            ClientManager.Inst.ConnectServer();
        }
    }
}
