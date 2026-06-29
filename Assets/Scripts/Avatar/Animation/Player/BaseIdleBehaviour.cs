
    using System.Collections.Generic;
    using BUD.AnimPose;
    using Es;
    using Game.Pet;
    using Game.Vehicle.PGCVehicle;
    using GameData.BaseInfo;
    using GameData.PgcData;
    using UnityEngine;

    public class BaseIdleBehaviour : MonoBehaviour
    {
        // 秒
        public const float MinLoopTime = 15;
        public const string IdleDefaultName = "default";
        public const string IdleLeisureName = "leisure";

        public PlayerAnimationCtrl animationCtrl;
        protected IdleData mData;
        protected List<EmoAniConfig> emoAniDataList;
        protected EmoteSubType emoteSubType;
        protected float time;
        protected bool isMainPlaying;
        protected bool isNeedSound;

        public void Init(PlayerAnimationCtrl ctrl, bool needSound = false)
        {
            isNeedSound = needSound;
            animationCtrl = ctrl;
        }

        public void SetData(IdleData data)
        {
            if (data == null)
            {
                data = new IdleData()
                {
                    mainIdle = IdleLeisureName,
                    subIdle = new()
                };
            }
            mData = data;
            ResetEmoteForUICharacter(animationCtrl);
            PlayMain();
        }

        public IdleData GetData()
        {
            return mData;
        }

        public void PreviewSub(string emoteId)
        {
            isMainPlaying = false;
            animationCtrl.ResetEmoteForUICharacter();
            PlayerSubAnim(emoteId);
        }

        public virtual void ChangeOcAni()
        {
            isMainPlaying = false;
            time = 0;
        }

        public void ResetEmoteAnim()
        {
            animationCtrl.ResetEmoteForUICharacter();
        }

        private void OnEnable()
        {
            if (mData != null)
            {
                if (animationCtrl != null && PGCVehicleManager.Inst.HasHallUIVehicleFor(animationCtrl))
                    return;
                ResetEmoteForUICharacter(animationCtrl);
                PlayMain();
            }
        }

        protected void ResetEmoteForUICharacter(PlayerAnimationCtrl ctrl)
        {
            if (ctrl != null)
            {
                ctrl.ResetEmoteForUICharacter();
            }
        }

        private void SetPlayerState(PlayerAnimationCtrl ctrl, PlayerState state)
        {
            if (ctrl != null)
            {
                ctrl.SetPlayerState(state);
            }
        }


        protected virtual bool LoopNeedFinish()
        {
            return false;
        }

        public void PlayMain()
        {
            if (mData == null)
            {
                LoggerUtils.LogError("mData is Null");
                return;
            }

            var emoConfigData = Es.DataTables.GetEmoUIConfig(mData.mainIdle);
            if (emoConfigData == null)
            {
                if (mData.mainIdle == IdleLeisureName)
                {
                    SetPlayerState(animationCtrl, PlayerState.Leisure);
                }
                else if (mData.mainIdle == IdleDefaultName)
                {
                    SetPlayerState(animationCtrl, PlayerState.Default);
                }
                ResetEmoteForUICharacter(animationCtrl);
            }
            else
            {
                emoAniDataList = Es.DataTables.GetEmoAniConfigList()
                    .FindAll((emoAniData) => emoAniData.emoId == mData.mainIdle);
                var uiEmoData = DataTables.GetEmoUIConfig(mData.mainIdle);
                if (uiEmoData != null)
                {
                    emoteSubType = (EmoteSubType) uiEmoData.emoType;
                    PlayUIEmoAnim();
                }
            }
            isMainPlaying = true;
        }

        protected virtual void PlayUIEmoAnim()
        {
        }

        protected virtual void PlayerSubAnim(string emoteID)
        {
           
        }

        protected void PlaySub()
        {
            if (mData != null && mData.subIdle != null && mData.subIdle.Count != 0)
            {
                string emoteID = mData.subIdle[Random.Range(0, mData.subIdle.Count)];
                PlayerSubAnim(emoteID);
            }
        }

        protected void OnSingleOnceEmoteFinish()
        {
            PlayMain();
        }

        private bool IsNotEmote()
        {
            return mData.mainIdle == IdleDefaultName || mData.mainIdle == IdleLeisureName;
        }

        private void Update()
        {
            if (mData == null || mData.subIdle == null || mData.subIdle.Count < 1 || !isMainPlaying) return;
            time += Time.deltaTime;

            if (IsNotEmote() && time > MinLoopTime)
            {
                PlaySub();
            }
        }
    }

