using System;
using System.Collections.Generic;
using System.IO;
using Game.Audio;
using Game.Config;
using Game.MapSetting;
using GameData;
using GameData.Base;
using GameData.Manager;
using Message;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.GameSetting
{
    public class GlobalSettingManager : GlobalInstance<GlobalSettingManager>
    {
        private GlobalSettingData data;
        // private BGMusicBehaviour bgBehv;

        public void ReadSaveParams()
        {
            try
            {
                data = GetGlobalSetting(AccountDataManager.Inst.Uid ?? string.Empty);
            }
            catch (Exception e)
            {
                LoggerUtils.LogError(e);
            }

            if (data == null)
            {
                data = new GlobalSettingData();
            }

        }

        public void Init()
        {
            MessageHelper.AddListener<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, InitGame);
            ReadSaveParams();
            SettingOldLogic oldLogic = new SettingOldLogic();
            oldLogic.OldLogic();
            InitSoundVolume();
            InitGraphics();

        }
        //进入地图时使用地图设置
        public void InitGame(EnterGameModel enterGameModel, UgcBaseInfo info)
        {
            ReadSaveParams();
            if (enterGameModel == EnterGameModel.GuestScene
                ||enterGameModel == EnterGameModel.UpdatePublishTest
                ||enterGameModel == EnterGameModel.PublishTest
                ||enterGameModel == EnterGameModel.CreateEmptyScene
                ||enterGameModel == EnterGameModel.ContinueEditScene)
            {
                OnFootStepChange?.Invoke(data.footStep == 0);
                OnViewDistanceChange?.Invoke(data.viewDistance);
                GameDataManager.Inst.globalSettingData = data;
            }
        }
        public void Save()
        {
            if (data != null && !string.IsNullOrEmpty(AccountDataManager.Inst.Uid))
            {
                SetGlobalSetting(AccountDataManager.Inst.Uid, data);
            }
        }

        public void SyncGameView(GameView gameView)
        {
            data.gameView = gameView;
        }

        #region 单一获取属性方法

        private void EnsureData()
        {
            if (data == null)
            {
                ReadSaveParams();
            }

            //以防万一
            if (data == null)
            {
                data = new GlobalSettingData();
            }
        }

        public bool GetAssistMode()
        {
            EnsureData();
            return data.AssistMode == 0;
        }

        public GameView GetGameView()
        {
            EnsureData();
            return data.gameView;
        }

        public bool IsAutoRunningOpen()
        {
            EnsureData();
            return data.automaticRunning == 0;
        }

        public bool IsLockMoveStick()
        {
            EnsureData();
            return data.lockMoveStick == 0;
        }

        public float GetCameraSensitive()
        {
            EnsureData();
            //海外版兼容，防止数值太大
            if (data.cameraPanSensitivity > 1)
            {
                data.cameraPanSensitivity = 0.31f;
            }
            return data.cameraPanSensitivity;
        }

        public float GetViewDistance()
        {
            EnsureData();
            return data.viewDistance;
        }

        public bool IsShowUserName()
        {
            EnsureData();
            return data.showUserName == 0;
        }

        public bool IsFriendRequestOpen()
        {
            EnsureData();
            return data.friendRequestNew == 0;
        }

        public int GetFps()
        {
            EnsureData();
            return data.FPS == 0 ? 60 : 30;
        }

        public bool IsBloomOpen()
        {
            EnsureData();
            if (data.bloom == -1) {
                data.bloom = QualityManager.IsQualityLow() ? 1 : 0;
            }

            return data.bloom == 0;
        }

        public bool IsShadowOpen()
        {
            EnsureData();
            if (data.shadow == -1)
            {
                data.shadow = QualityManager.IsQualityLow() ? 1 : 0;
            }

            return data.shadow == 0;
        }

        public bool IsFootstepOpen()
        {
            EnsureData();
            return data.footStep == 0;
        }

        public float GetBgmVolume()
        {
            EnsureData();
            return data.bgm;
        }

        public float GetSoundEffectVolume()
        {
            EnsureData();
            return data.soundEffect;
        }
        public int GetGraphics()
        {
            EnsureData();
            return data.graphics;

        }
        public float GetMicrophoneVolume()
        {
            EnsureData();
            return data.microPhone;
        }

        public float GetSpeakerVolume()
        {
            EnsureData();
            return data.speaker;
        }

        // public VoiceEffect GetVoiceEffect()
        // {
        //     EnsureData();
        //     return data.voiceEffect;
        // }

        #endregion

        #region 监听事件

        public Action<GameView> OnGameViewChange;
        public Action<bool> OnAutomaticRunChange;
        public Action<bool> OnLockMoveStickChange;
        public Action<float> OnCameraPanSensitivityChange;
        public Action<float> OnViewDistanceChange;
        public Action<bool> OnShowUserNameChange;
        public Action<bool> OnFriendRequestChange;
        public Action<int> OnFPSChange;
        public Action<bool> OnBloomChange;
        public Action<bool> OnShadowChange;
        public Action<bool> OnFootStepChange;
        public Action<float> OnBgmChange;
        public Action<float> OnSoundEffectChange;
        public Action<float> OnMicrophoneChange;
        public Action<float> OnSpeakerChange;
        // public Action<VoiceEffect> OnVoiceChangerChange;

        #endregion

        #region 给Panel调用的方法

        public void AssistModeChange(int index)
        {
            data.AssistMode = index;
            Save();
        }

        public void GameViewChange(GameView gameView)
        {
            data.gameView = gameView;
            OnGameViewChange?.Invoke(gameView);
        }

        public void AutomaticRunChange(int index)
        {
            data.automaticRunning = index;
            Save();
        }

        public void LockMoveStickChange(int index)
        {
            data.lockMoveStick = index;
            OnLockMoveStickChange?.Invoke(index == 0);
            Save();
        }

        public void CameraPanSensitivityChange(float value)
        {
            data.cameraPanSensitivity = value;
            OnCameraPanSensitivityChange?.Invoke(value);
            Save();
        }

        public void ViewDistanceChange(float value)
        {
            data.viewDistance = value;
            OnViewDistanceChange?.Invoke(value);
            Save();
        }

        public void ShowUserNameChange(int index)
        {
            data.showUserName = index;
            OnShowUserNameChange?.Invoke(index == 0);
            Save();
        }

        public void FriendRequestChange(int index)
        {
            data.friendRequestNew = index;
            OnFriendRequestChange?.Invoke(index == 0);
            Save();
        }

        public void FPSChange(int index)
        {
            data.FPS = index;
            OnFPSChange?.Invoke(index == 0 ? 60 : 30);
            Save();
        }


        /// <summary>
        /// 0 是开、 1 是关
        /// </summary>
        /// <param name="index"></param>
        public void BloomChange(int index)
        {
            data.bloom = index;
            OnBloomChange?.Invoke(index == 0);
            PostProcessManager.Inst.SetBloomActive(index);
            Save();
        }

        public void ShadowChange(int index)
        {
            data.shadow = index;
            OnShadowChange?.Invoke(index == 0);

            Save();
        }

        public void FootStepChange(int index)
        {
            data.footStep = index;
            OnFootStepChange?.Invoke(index == 0);
            Save();
        }

        public void GraphicsChange(int level)
        {
            data.graphics = level;
            GraphicsManager.Inst.SetGraphics(level);
            Save();
        }


        public void BgmChange(float value)
        {
            data.bgm = value;
            OnBgmChange?.Invoke(value);

            //设置 WWise BGM 音量
            AkSoundManager.Inst.SetBGMAudioVolume(value);
            // 设置自定义 BGM 音量
            SetBGMBehaviorVolume(value / 100.0f);
            Save();
        }
        public void SoundEffectChange(float value)
        {
            data.soundEffect = value;
            OnSoundEffectChange?.Invoke(value);
            float volume = value / 100;
            // // 设置 H5 视频声音
            // VideoNodeManager.Inst.SetVideoVolume(volume);
            // // 设置声音按钮声音
            // SoundManager.Inst.SetSoundVolume(volume);
            // //设置 WWise 音效音量
            AkSoundManager.Inst.SetSFXAudioVolume(value);
            Save();
        }
        public void MicroPhoneChange(float value)
        {
            data.microPhone = value;
            OnMicrophoneChange?.Invoke(value);
        }

        public void SpeakerChange(float value)
        {
            data.speaker = value;
            OnSpeakerChange?.Invoke(value);
        }

        // public void VoiceChangerChange(int index)
        // {
        //     data.voiceEffect = (VoiceEffect)index;
        //     OnVoiceChangerChange?.Invoke(data.voiceEffect);
        //     Save();
        // }
        #endregion

        // private BGMusicBehaviour GetBgBav()
        // {
        //     if (bgBehv == null)
        //     {
        //         if (SceneBuilder.Inst != null && SceneBuilder.Inst.BGMusicEntity != null)
        //         {
        //             var sceneEntity = SceneBuilder.Inst.BGMusicEntity;
        //             var bindGo = sceneEntity.Get<GameObjectComponent>().bindGo;
        //             if (bindGo != null)
        //             {
        //                 bgBehv = bindGo.GetComponent<BGMusicBehaviour>();
        //             }
        //         }
        //     }
        //
        //     return bgBehv;
        // }

        public void SetBGMBehaviorVolume(float volume)
        {
            // var bgmBehv = GetBgBav();
            // if (bgmBehv != null)
            // {
            //     bgmBehv.SetAudioVolume(volume);
            // }
        }

        /// <summary>
        /// 根据设置数据初始化音量
        /// </summary>
        public void InitSoundVolume()
        {
            if (data == null)
                return;
            AkSoundManager.Inst.SetBGMAudioVolume(data.bgm);
            // AudioController.Inst.SetSFXAudioVolume(data.soundEffect);
            // float volume = data.soundEffect / 100.0f;
            AkSoundManager.Inst.SetSFXAudioVolume(data.soundEffect);
            // VideoNodeManager.Inst.SetVideoVolume(volume);
        }

        public void InitGraphics()
        {
            SetGraphics(data.graphics);
            OnShadowChange?.Invoke(data.shadow == 0);
            OnFPSChange?.Invoke(data.FPS == 0 ? 60 : 30);
        }

        private void SetGraphics(int graphValue)
        {
            var graphics = (GraphicsEnum)graphValue;
            if (graphics == GraphicsEnum.None)
            {
                graphics = GraphicsEnum.High;
                var mobineQuality = GameDataManager.Inst.GetMobileQuality();
                switch (mobineQuality)
                {
                    case ModelClassificationType.Middle:
                        graphics = GraphicsEnum.Medium;
                        break;
                    case ModelClassificationType.Low:
                        graphics = GraphicsEnum.Low;
                        break;
                }
                GraphicsChange((int) graphics);
            }
            else
            {
                GraphicsManager.Inst.SetGraphics(data.graphics);
            }
        }

        public static SettingLocalInfo ReadSettingInfo()
        {
            string filePath = GameConsts.InfoDir + GameConsts.SettingLocalInfo;
            SettingLocalInfo info = null;

            try
            {
                if (!Directory.Exists(GameConsts.InfoDir))
                {
                    Directory.CreateDirectory(GameConsts.InfoDir);
                }

                if (File.Exists(filePath))
                {
                    string jsonStr = File.ReadAllText(filePath);
                    if(!string.IsNullOrEmpty(jsonStr))
                    {
                        info = JsonConvert.DeserializeObject<SettingLocalInfo>(jsonStr);
                    }

                }
            }
            catch (Exception e)
            {
                LoggerUtils.LogError(" SettingLocalInfo ReadSettingInfo Error:" + e.StackTrace);
            }

            if (info == null)
            {
                info = new SettingLocalInfo();
            }
            return info;
        }

        public static void SaveSettingInfo(SettingLocalInfo info)
        {
            string filePath = GameConsts.InfoDir + GameConsts.SettingLocalInfo;
            if (!Directory.Exists(GameConsts.InfoDir))
            {
                Directory.CreateDirectory(GameConsts.InfoDir);
            }
            string json = JsonConvert.SerializeObject(info);
            File.WriteAllText(filePath, json);
        }

        public static void SetGlobalSetting(string id, GlobalSettingData data)
        {

            var info = ReadSettingInfo();
            info.PlayerSettingInfoDict[id] = data;
            SaveSettingInfo(info);
        }

        public GlobalSettingData GetGlobalSetting(string id)
        {
            var settingInfo = ReadSettingInfo();
            GlobalSettingData data;
            if (!settingInfo.PlayerSettingInfoDict.TryGetValue(id, out data))
            {
                data = new GlobalSettingData();
                settingInfo.PlayerSettingInfoDict[id] = data;
            }
            SetGlobalSetting(id, data);
            //视角暂时永远设置为第三人称
            data.gameView = GameView.ThirdPerson;
            return data;
        }

        public override void Release()
        {
            base.Release();
            MessageHelper.RemoveListener<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, InitGame);
        }
    }

    [Serializable]
    public class SettingLocalInfo
    {
        public Dictionary<string, GlobalSettingData> PlayerSettingInfoDict = new Dictionary<string, GlobalSettingData>();
    }
}
