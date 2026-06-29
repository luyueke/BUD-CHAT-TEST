using Es;
using Game.Audio;
using GameData.PgcData;
using Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UIAgent;
using UnityEngine;
using xasset;
using Random = UnityEngine.Random;

namespace Game.Pet
{
    public class PetAnimationCtrl : PlayerAnimationCtrl
    {
        public override void PlayCurEyeAni()
        {
            var eyePartData = Wrap.GetPartData(UniqueType.GetPGCPetAvatar(AvatarSubType.Eyes));
            ClearClip(1);
            Wrap.RsetFaceMat();
        }

        private List<PlayAniType> singleLoopAni = new List<PlayAniType>() { PlayAniType.PetSingleLoopStart, PlayAniType.PetSingleLooping, PlayAniType.PetSingleLoopEnd };
        protected override List<PlayAniType> GetSingleLoopAni()
        {
            return singleLoopAni;
        }

        protected readonly PlayAniType[] PetWithPlayerPetA = new PlayAniType[] { PlayAniType.PetWithPlayerPetA };
        protected readonly PlayAniType[] PetWithPlayerPlayerA = new PlayAniType[] { PlayAniType.PetWithPlayerPlayerA };
        protected readonly PlayAniType[] PetWithPlayerLoopPetA = new PlayAniType[] { PlayAniType.PetWithPlayerLoopPetAStart, PlayAniType.PetWithPlayerLoopPetALoop };
        protected readonly PlayAniType[] PetWithPlayerLoopPlayerA = new PlayAniType[] { PlayAniType.PetWithPlayerLoopPlayerAStart, PlayAniType.PetWithPlayerLoopPlayerALoop };

        /// <summary>
        /// 用于UI界面播放双人Emote
        /// </summary>
        /// <param name="emoteID"></param>
        /// <param name="playerBAnimCtrl"></param>
        /// <param name="OnCompleteDoubleEmote"></param>
        /// <param name="isPlaySound"></param>
        public void PlayPetWithPlayerEmoteForUICharacter(string emoteID, PlayerAnimationCtrl playerBAnimCtrl, Action OnCompleteDoubleEmote = null, bool isPlaySound = true, Action OnDownloadOver = null, Func<bool> loopNeedFinish = null)
        {
            playerId = AccountDataManager.Inst.Uid;
            playerBAnimCtrl.SetPlayerID(AccountDataManager.Inst.Uid);

            ClearExpression(uiEmoteExpressionGo);
            ResetAnimation();

            DownloadAnimationAB(emoteID, (success) =>
            {
                OnDownloadOver?.Invoke();
                if (!success) return;

                var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteID);
                if (emoAniDataList == null || emoAniDataList.Count == 0)
                {
                    LoggerUtils.LogError($"获取Emote配置失败，请确认配置表上是否存在EmoteID : {emoteID}");
                    return;
                }

                var aniConfigList = emoAniDataList.ConvertToAniConfig();

                bool isLoop = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(PlayAniType.PetWithPlayerLoopPetALoop.ToString())) != null;
                if (isLoop)
                {
                    PlayPetADoubleEmoteList(aniConfigList, PetWithPlayerLoopPetA, isPlaySound);
                    playerBAnimCtrl.PlayPlayerADoubleEmoteList(transform, aniConfigList, PetWithPlayerLoopPlayerA, OnCompleteDoubleEmote, isPlaySound,loopNeedFinish);
                }
                else
                {
                    PlayPetADoubleEmoteList(aniConfigList, PetWithPlayerPetA, isPlaySound);
                    playerBAnimCtrl.PlayPlayerADoubleEmoteList(transform, aniConfigList, PetWithPlayerPlayerA, OnCompleteDoubleEmote, isPlaySound);
                }
            });
        }

        /// <summary>
        /// 用于UI界面播放双人Emote
        /// </summary>
        /// <param name="emoteID"></param>
        /// <param name="playerBAnimCtrl"></param>
        /// <param name="OnCompleteDoubleEmote"></param>
        /// <param name="isPlaySound"></param>
        public void PlayPetWithPlayerEmoteForHall(string emoteID, PlayerAnimationCtrl playerBAnimCtrl, Action OnCompleteDoubleEmote = null, bool isPlaySound = true, Action OnDownloadOver = null, Func<bool> loopNeedFinish = null)
        {
            playerId = AccountDataManager.Inst.Uid;
            playerBAnimCtrl.SetPlayerID(AccountDataManager.Inst.Uid);

            ClearExpression(uiEmoteExpressionGo);
            ResetAnimation();

            DownloadAnimationAB(emoteID, (success) =>
            {
                OnDownloadOver?.Invoke();
                if (!success) return;

                var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteID);
                if (emoAniDataList == null || emoAniDataList.Count == 0)
                {
                    LoggerUtils.LogError($"获取Emote配置失败，请确认配置表上是否存在EmoteID : {emoteID}");
                    return;
                }

                var aniConfigList = emoAniDataList.ConvertToAniConfig();

                bool isLoop = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(PlayAniType.PetWithPlayerLoopPetALoop.ToString())) != null;
                if (isLoop)
                {
                    PlayPetADoubleEmoteList(aniConfigList, PetWithPlayerLoopPetA, isPlaySound);
                    playerBAnimCtrl.PlayPlayerADoubleEmoteListForHall(transform, aniConfigList, PetWithPlayerLoopPlayerA, OnCompleteDoubleEmote, isPlaySound);
                }
                else
                {
                    PlayPetADoubleEmoteList(aniConfigList, PetWithPlayerPetA, isPlaySound);
                    playerBAnimCtrl.PlayPlayerADoubleEmoteListForHall(transform, aniConfigList, PetWithPlayerPlayerA, OnCompleteDoubleEmote, isPlaySound);
                }
            });
        }

        public void PlayTeleportingAni(Action OnCompleteSingleEmote = null, bool isPlaySound = true)
        {
            ResetEmoteForUICharacter();

            var featAniConfig = DataTables.GetFeatAniConfigList().Find((aniConfig) => aniConfig.id == 5).ConvertToAniConfig();
            uiEmoteExpressionGo = CreateExpression(featAniConfig);
            PlayConfigAni(featAniConfig, () =>
            {
                ResetEmoteForUICharacter();
                OnCompleteSingleEmote?.Invoke();
            }, false);

            AkSoundManager.Inst.PlaySound("Pet_Group", "Appear", "Play_Pet_Locomotion", gameObject);
        }

        public void PlayChangeClothAni(Action OnCompleteSingleEmote = null, bool isPlaySound = true, string curPlayerId = "")
        {
            ResetEmoteForUICharacter();

            if(string.IsNullOrEmpty(curPlayerId))
                curPlayerId = AccountDataManager.Inst.Uid;

            playerId = curPlayerId;
            var featAniConfig = DataTables.GetFeatAniConfigList().Find((aniConfig) => aniConfig.id == 6).ConvertToAniConfig();
            uiEmoteExpressionGo = CreateExpression(featAniConfig);
            PlayConfigAni(featAniConfig, () =>
            {
                ResetEmoteForUICharacter();
                OnCompleteSingleEmote?.Invoke();
            }, isPlaySound);
        }

        public void PlayInstrumentIdleAni(string curPlayerId)
        {
            if(string.IsNullOrEmpty(curPlayerId))
                curPlayerId = AccountDataManager.Inst.Uid;
            
            ResetEmoteForUICharacter();

            playerId = curPlayerId;
            var instrumentIdleAniConfig = DataTables.GetFeatAniConfigList().Find((aniConfig) => aniConfig.id == 7).ConvertToAniConfig();
            uiEmoteExpressionGo = CreateExpression(instrumentIdleAniConfig);
            PlayConfigAni(instrumentIdleAniConfig, () =>
            {
                PlayInstrumentIdleAni(curPlayerId);
            });
        }
    }
    
    
}
