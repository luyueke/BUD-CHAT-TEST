// using Newtonsoft.Json;
//
// /// <summary>
// /// 联机游玩模式
// /// </summary>
// public class PassLevelGameMixerHandler : PassLevelHandlerBase
// {
//     public override void HandlePassed()
//     {
//         OpenPassLevelPanel(true);
//         RequestRewardData();
//     }
//
//     public override void HandleFailed()
//     {
//         OpenPassLevelPanel(false);
//         RequestRewardData();
//     }
//
//     public void RequestRewardData()
//     {
//         PassLevelManager.Inst.SendPassLevelDataReq((rewardInfo) =>
//             {
//                 if (PassLevelPanel.Instance) PassLevelPanel.Instance.SetRewards(rewardInfo);
//             },
//             (error) => { LoggerUtils.LogError($"PassLevelGuestHandler SendPassLevelDataReq faild :{error}"); });
//     }
//
//     private void OpenPassLevelPanel(bool isWin)
//     {
//         PassLevelPanel.OpenPanel(isWin);
//         if (!isWin)
//         {
//             PassLevelPanel.Instance.SetButtons(new PassLevelPanel.ButtonSetting()
//             {
//                 BtnText = "Back to Lobby",
//                 BtnStyle = PassLevelPanel.PassLevelBtnStyleType.Blue,
//                 ClickAction = () =>
//                 {
//                     PassLevelPanel.Hide();
//                     PassLevelManager.Inst.ExitCurrentMap();
//                 }
//             }, new PassLevelPanel.ButtonSetting()
//             {
//                 BtnText = "Try Again",
//                 BtnStyle = PassLevelPanel.PassLevelBtnStyleType.Yellow,
//                 ClickAction = () =>
//                 {
//                     PassLevelPanel.Hide();
//                     PassLevelManager.Inst.PlayAgainOnline();
//                 }
//             });
//         }
//         else
//         {
//             var levelinfos = GameManager.Inst.gameMixerInfo.levelInfos;
//             var mapId = GlobalFieldController.CurMapInfo.mapId;
//             var mapLevel = levelinfos.Find(x => x.mapId == mapId);
//             if (mapLevel != null && mapLevel.level <levelinfos.Count)
//             {
//                 PassLevelPanel.Instance.SetButtons(new PassLevelPanel.ButtonSetting()
//                 {
//                     BtnText = "Back to Lobby",
//                     BtnStyle = PassLevelPanel.PassLevelBtnStyleType.Blue,
//                     ClickAction = () =>
//                     {
//                         PassLevelPanel.Hide();
//                         PassLevelManager.Inst.ExitCurrentMap();
//                     }
//                 }, new PassLevelPanel.ButtonSetting()
//                 {
//                     BtnText = "Next Level",
//                     BtnStyle = PassLevelPanel.PassLevelBtnStyleType.Yellow,
//                     ClickAction = () =>
//                     {
//                         PassLevelPanel.Hide();
//                         GameMixerPlayManager.Inst.StartTransfer(levelinfos[levelinfos.IndexOf(mapLevel) + 1].mapId);
//                     }
//                 });
//             }
//             else
//             {
//                 PassLevelPanel.Instance.SetButtons(null, new PassLevelPanel.ButtonSetting()
//                 {
//                     BtnText = "Back to Lobby",
//                     BtnStyle = PassLevelPanel.PassLevelBtnStyleType.Blue,
//                     ClickAction = () =>
//                     {
//                         PassLevelPanel.Hide();
//                         PassLevelManager.Inst.ExitCurrentMap();
//                     }
//                 });
//             }
//         }
//         
//         
//     }
// }