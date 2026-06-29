// using System;
// using SavingData;
//
// /// <summary>
// /// 每日挑战进图结算处理
// /// </summary>
// public class PassLevelDailyChallengeHandler : PassLevelHandlerBase
// {
//     public override void HandlePassed()
//     {
//         PassLevelPanel.OpenPanel(true);
//         RequestData((rewardInfo) =>
//         {
//             // 刷新通关数据
//             DailyChallengeManager.Inst.RequestForceRefresh();
//             if (PassLevelPanel.Instance) PassLevelPanel.Instance.SetRewards(rewardInfo);
//         });
//
//         var haveNextLevel = DailyChallengeManager.Inst.NextChallengeMapData != null;
//
//         if (haveNextLevel)
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
//                 BtnText = "Next Level",
//                 BtnStyle = PassLevelPanel.PassLevelBtnStyleType.Yellow,
//                 ClickAction = () =>
//                 {
//                     PassLevelPanel.Hide();
//                     //下一关
//                     var nextM = DailyChallengeManager.Inst.NextChallengeMapData;
//                     DailyChallengeManager.Inst.CurrentChallengeStep += 1;
//                     PassLevelManager.Inst.PlayNextLevel(new MapPublishTestModeInfo()
//                     {
//                         mapId = nextM.mapInfo.mapId,
//                         mapName = nextM.mapInfo.mapName,
//                         enterGameMode = EnterGameMode.DailyChallenge,
//                         isInWhiteList = nextM.mapInfo.isInWhiteList,
//                         mapInfo = null
//                     });
//                 }
//             });
//         }
//         else
//         {
//             PassLevelPanel.Instance.SetWinStr(
//                 "You have reached the last level!\nCome back tomorrow for a new set of challenges!");
//             PassLevelPanel.Instance.SetButtons(null, new PassLevelPanel.ButtonSetting()
//             {
//                 BtnText = "Back to Lobby",
//                 BtnStyle = PassLevelPanel.PassLevelBtnStyleType.Blue,
//                 UButtonConfig = new PassLevelPanel.UButtonBaseConfig()
//                 {
//                     UISoundType = UISoundType.UI_Return_C1,
//                 },
//                 ClickAction = () =>
//                 {
//                     PassLevelPanel.Hide();
//                     PassLevelManager.Inst.ExitCurrentMap();
//                 }
//             });
//         }
//     }
//
//     public override void HandleFailed()
//     {
//         PassLevelPanel.OpenPanel(false);
//         RequestData((rewardInfo) =>
//         {
//             if (PassLevelPanel.Instance) PassLevelPanel.Instance.SetRewards(rewardInfo);
//         });
//
//         PassLevelPanel.Instance.SetButtons(new PassLevelPanel.ButtonSetting()
//         {
//             BtnText = "Back to Lobby",
//             BtnStyle = PassLevelPanel.PassLevelBtnStyleType.Blue,
//             ClickAction = () =>
//             {
//                 PassLevelPanel.Hide();
//                 PassLevelManager.Inst.ExitCurrentMap();
//             }
//         }, new PassLevelPanel.ButtonSetting()
//         {
//             BtnText = "Try Again",
//             BtnStyle = PassLevelPanel.PassLevelBtnStyleType.Yellow,
//             ClickAction = () =>
//             {
//                 PassLevelPanel.Hide();
//                 PassLevelManager.Inst.PlayAgainOffline(true);
//             }
//         });
//     }
//
//     public void RequestData(Action<PassLevelManager.PassLevelRewardInfo> callback)
//     {
//         PassLevelManager.Inst.SendPassLevelDataReq((rewardInfo) => { callback?.Invoke(rewardInfo); },
//             (error) => { callback?.Invoke(null); });
//     }
// }