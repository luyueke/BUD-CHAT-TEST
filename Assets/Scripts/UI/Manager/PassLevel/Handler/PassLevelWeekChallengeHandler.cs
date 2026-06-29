// using System;
// using SavingData;
//
// /// <summary>
// /// 每周挑战进图结算处理
// /// </summary>
// public class PassLevelWeekChallengeHandler : PassLevelHandlerBase
// {
//     public override void HandlePassed()
//     {
//         PassLevelPanel.OpenPanel(true);
//         RequestData((rewardInfo) =>
//         {
//             WeekChallengeManager.Inst.RefreshUI();
//             if (PassLevelPanel.Instance) PassLevelPanel.Instance.SetRewards(rewardInfo);
//         });
//
//         PassLevelPanel.Instance.SetWinStr(
//             "You have reached the last level!\nCome back tomorrow for a new set of challenges!");
//         PassLevelPanel.Instance.SetButtons(null, new PassLevelPanel.ButtonSetting()
//         {
//             BtnText = "Back to Lobby",
//             BtnStyle = PassLevelPanel.PassLevelBtnStyleType.Blue,
//             ClickAction = () =>
//             {
//                 PassLevelPanel.Hide();
//                 PassLevelManager.Inst.ExitCurrentMap();
//             }
//         });
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