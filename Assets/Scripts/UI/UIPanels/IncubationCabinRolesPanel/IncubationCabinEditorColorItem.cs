// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using BUD.AnimPose;
// using BUD.AnimPose;
// using Com.TheFallenGames.OSA.DataHelpers;
// using Com.TheFallenGames.OSA.Util.IO;
// using Es;
// using EventTracking;
// using EventTracking;
// using Game.AINPCStudio;
// using Game.AnimationStudio;
// using Game.AnimationStudio;
// using Game.Audio;
// using Game.Audio;
// using Game.Avatar;
// using Game.Base;
// using Game.Config;
// using Game.Config;
// using Game.COSXML;
// using Game.Event;
// using Game.KinematicCharacter;
// using Game.MusicalInstrument;
// using Game.Pet;
// using Game.Store;
// using Game.Utils;
// using Game.Vehicle.PGCVehicle;
// using Game.Vehicle.PGCVehicle.KVC;
// using GameData;
// using GameData.BaseInfo;
// using GameData.Manager;
// using GameData.PgcData;
// using GameData.UGCData;
// using GameSync.Manager;
// using GameUI;
// using Message;
// using Message;
// using Network;
// using Network.Http;
// using Newbie;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
// using Pb.Game;  
// using UnityEngine;
// using UnityEngine.UI;

// namespace UI.UIPanels.IncubationCabin
// {

//     public class IncubationCabinEditorColorItem : MonoBehaviour
//     {

//         [SerializeField] private Button selectBtn;
//         [SerializeField] private Button[] colorBtns;
//         [SerializeField] internal Button setDefaultBtn;




//         internal CharacterWrap otherCharacterWrap;
//         internal BaseAvatarWrapper avatarWrapper;
//         [Header("人物形象")]
//         [SerializeField] internal Transform characterRoot;
//         [SerializeField] internal AvatarCameraController avatarCameraController;
//         #region 人物相关


//         internal PlayerAnimationCtrl animationCtrl;
//         internal AnimIKController animationCtrlIK;
//         internal PlayerHoldBehaviour playerHold;
//         internal PlayerAnimationCtrl otherAnimationCtrl;
//         internal AnimIKController otherAnimationCtrlIK;
//         internal PlayMusicScoreBev playMusicScoreBev;

//         #endregion

//         public override void OnShow(params object[] args)
//         {
//             InitCharacterWrapper();

//             InitBtns();
//         }

//         private void InitCharacterWrapper()
//         {
//             var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;

//             var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
//             //characterWrapper.SetParent(characterRoot, true);
//             animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
//             animationCtrlIK = characterWrapper.Avatar.GetComponent<AnimIKController>();
//             playerHold = characterWrapper.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
//             avatarCameraController.RotateTarget = characterRoot;

//             otherCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(AccountDataManager.Inst.UserInfo.otherAvatarInfo, characterRoot);
//             //otherCharacterWrap.SetParent(characterRoot, true);
//             otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
//             otherAnimationCtrlIK = otherCharacterWrap.Avatar.GetComponent<AnimIKController>();
//             avatarWrapper = characterWrapper;
//             otherCharacterWrap.Avatar.gameObject.SetActive(false);
//         }

//         void InitBtns()
//         {
//             setDefaultBtn.onClick.AddListener(OnSetDefaultBtnClick);
//             backBtn.onClick.AddListener(OnBackBtnClick);
//             if (colorBtns != null && colorBtns.Length > 0)
//             {
//                 for (int i = 0, C = colorBtns.Length; i < C; i++)
//                 {
//                     int index = i;
//                     var colorBtn = colorBtns[index];
//                     colorBtn.onClick.AddListener(() =>
//                     {
//                         OnColorBtnClick(index);
//                     });
//                 }
//             }
//         }

//         void OnBackBtnClick()
//         {
//             CloseSelf();
//         }

//         void OnColorBtnClick(int index)
//         {
//             //TODO: 设置颜色

//         }

//         void OnSetDefaultBtnClick()
//         {
//             //TODO: 设置默认形象
//         }
//     }


// }
