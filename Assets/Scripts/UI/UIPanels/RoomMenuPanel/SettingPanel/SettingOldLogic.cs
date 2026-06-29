using Game.Base;
using Game.Utils;
using UnityEngine;

namespace Game.GameSetting
{
    public class SettingOldLogic
    {
        public void OldLogic()
        {
            GlobalSettingManager.Inst.OnFootStepChange += OldFootStepChange;
            GlobalSettingManager.Inst.OnGameViewChange += OldGameViewChange;
            GlobalSettingManager.Inst.OnFPSChange += OnFPSChange;
            GlobalSettingManager.Inst.OnShadowChange += OnShadowChange;
            // GlobalSettingManager.Inst.OnMicrophoneChange += OldMicrophoneChange;
            // GlobalSettingManager.Inst.OnSpeakerChange += OldSpeakerChange;
            // GlobalSettingManager.Inst.OnVoiceChangerChange += OldVoiceChangerChange;
            // GlobalSettingManager.Inst.OnFriendRequestChange += OldFriendRequestChange;
            GlobalSettingManager.Inst.OnViewDistanceChange += OldViewDistanceChange;
        }
        
        private void OldViewDistanceChange(float value)
        {
            if (GameCameraUtils.HasInstance)
            {
                GameCameraUtils.Inst.GetPlayVirtualCamera().m_Lens.FarClipPlane = value;
            }
           
        }

        // private void OldFriendRequestChange(bool open)
        // {
        //     SocialNotificationManager.Inst.openNotification = open;
        // }

        private void OldFootStepChange(bool open)
        {
            if (GameSoundManager.HasInstance)
            {
                GameSoundManager.Inst.IsOpenFootSound = open;
            }
           
        }

        

        private void OnFPSChange(int index)
        {
            QualityManager.Inst.SetFps(index == 0 ? 60 : 30);
        }

        private void OnShadowChange(bool open)
        {
            QualityManager.Inst.SetTargetQualityShadow(open);
        }
        private void OldGameViewChange(GameView gameView)
        {
            // TODO: GlobalSeting
            
            // if (gameView == GameView.FirstPerson
            //     && PlayModePanel.Instance
            //     && PlayModePanel.Instance.isTps)
            // {
            //     PlayModePanel.Instance.OnChangeViewBtnClick();
            // }
            //
            // if (gameView == GameView.ThirdPerson
            //     && PlayModePanel.Instance
            //     && !PlayModePanel.Instance.isTps)
            // {
            //     PlayModePanel.Instance.OnChangeViewBtnClick();
            // }

        }

        // private void OldMicrophoneChange(float value)
        // {
        //     if (RealTimeTalkManager.Inst != null)
        //     {
        //         RealTimeTalkManager.Inst.ChangeMicLevel((int)value);
        //     }
        // }
        //
        // private void OldSpeakerChange(float value)
        // {
        //     if (RealTimeTalkManager.Inst != null)
        //     {
        //         RealTimeTalkManager.Inst.ChangeVoiceLevel((int)value);
        //     }
        // }

        // private void OldVoiceChangerChange(VoiceEffect voiceEffect)
        // {
        //     if (RoomMenuPanel.Instance != null)
        //     {
        //         AKSoundManager.Inst.StopVoiceDemoSound(RoomMenuPanel.Instance.DowntownPanel.globalSettingPanel
        //             .gameObject);
        //         if (voiceEffect != VoiceEffect.Original)
        //         {
        //             AKSoundManager.Inst.PlayVoiceDemoSound((voiceEffect).ToString(),
        //                 RoomMenuPanel.Instance.DowntownPanel.globalSettingPanel.gameObject);
        //         }
        //     }
        //
        //     if (RealTimeTalkManager.Inst != null)
        //     {
        //         RealTimeTalkManager.Inst.SetVoiceChange(voiceEffect);
        //     }
        // }
    }
}
