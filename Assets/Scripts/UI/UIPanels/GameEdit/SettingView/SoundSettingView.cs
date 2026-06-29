using System;
using System.Collections.Generic;
using Basic.Extensions;
using Es;
using Game.Audio;
using Game.Base;
using Game.MapSetting;
using Game.Props.PropsComponents;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using UIAgent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit.SettingView
{
    public class SoundSettingView : BaseSettingView {

        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField]
        private Transform bgAudioRoot;

        [SerializeField]
        private Transform noiseAudioRoot;

        [SerializeField] private Transform bgContainer;
        [SerializeField] private Transform noiseContainer;

        [SerializeField] private GameObject soundSettingItemObj;

        [SerializeField] private GameObject contentObj;

        private readonly List<SoundSettingItem> bgSettingItems = new List<SoundSettingItem>();

        private readonly List<SoundSettingItem> noiseSettingItems = new List<SoundSettingItem>();

        private SoundSettingItem selectedBgSettingItem;
        private SoundSettingItem emptyBgSettingItem;
        private SoundSettingItem selectedNoiseSettingItem;
        private int _audioLimit = 120;



        private void Awake()
        {
            var bgAudioList = Es.DataTables.GetGameBgAudioDataList();
            var noiseAudioList = Es.DataTables.GetGameNoiseAudioDataList();

            var bgMusicComp =  GameMapSettingManager.Inst.settingEntity.GetComp<BgMusicComponent>();
            var ugcSettingItem = Instantiate(soundSettingItemObj, bgAudioRoot).GetComponent<SoundSettingItem>();
            var ugcAudioData = new GameBgAudioData();
            ugcAudioData.SetUGCUrl(bgMusicComp.ugcMusicUrl, bgMusicComp.loudness);
            ugcSettingItem.SetData(ugcAudioData, OnItemSelected);
            ugcSettingItem.SetDeleteUGCMusic(OnDeleteUGCMusic);
            bgSettingItems.Add(ugcSettingItem);


            foreach (var tmpAudioData in bgAudioList)
            {
                var item = Instantiate(soundSettingItemObj, bgAudioRoot);
                var settingItem = item.GetComponent<SoundSettingItem>();
                settingItem.SetData(tmpAudioData, OnItemSelected);
                bgSettingItems.Add(settingItem);
                if (string.IsNullOrEmpty(tmpAudioData.State)) {
                    emptyBgSettingItem = settingItem;
                }
            }

            foreach (var tmpNoiseAudioData in noiseAudioList)
            {
                var item = Instantiate(soundSettingItemObj, noiseAudioRoot);
                var settingItem = item.GetComponent<SoundSettingItem>();
                settingItem.SetData(tmpNoiseAudioData, OnItemSelected);
                noiseSettingItems.Add(settingItem);
            }

            soundSettingItemObj.SetActive(false);
        }

        private void RefreshLayout() {

            LayoutRebuilder.ForceRebuildLayoutImmediate(bgAudioRoot.GetComponent<RectTransform>());
            bgAudioRoot.GetComponent<AutoLayoutPreferredVertical>().LayoutChildrenObjectsSync();
            bgAudioRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(1385, bgAudioRoot.GetComponent<RectTransform>().sizeDelta.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(noiseAudioRoot.GetComponent<RectTransform>());
            // noiseAudioRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(1385, noiseAudioRoot.GetComponent<GridLayoutGroup>().preferredHeight);
            noiseAudioRoot.GetComponent<AutoLayoutPreferredVertical>().LayoutChildrenObjectsSync();
            noiseAudioRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(1385, noiseAudioRoot.GetComponent<RectTransform>().sizeDelta.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bgContainer.GetComponent<RectTransform>());
            bgContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(1385, bgContainer.GetComponent<VerticalLayoutGroup>().preferredHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(noiseContainer.GetComponent<RectTransform>());
            noiseContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(1385, noiseContainer.GetComponent<VerticalLayoutGroup>().preferredHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentObj.GetComponent<RectTransform>());
            contentObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1385, contentObj.GetComponent<VerticalLayoutGroup>().preferredHeight);
        }

        private void DelayRefreshLayout() {
          // 延迟2帧 刷新，确保在列表排列后执行
            canvasGroup.alpha = 0;
            this.SetFrameCallBack(2, () => {
                RefreshLayout();
                canvasGroup.alpha = 1;
            });
        }

        public override void OnTabSelected()
        {
            // RefreshLayout();
            var bgMusicComp =  GameMapSettingManager.Inst.settingEntity.GetComp<BgMusicComponent>();
            foreach (var soundSettingItem in bgSettingItems)
            {
                var audioData = soundSettingItem.GetData();
                if (audioData is GameBgAudioData bgAudioData)
                {
                    if (bgAudioData.Id == bgMusicComp.bgId)
                    {
                        selectedBgSettingItem = soundSettingItem;
                        soundSettingItem.SetSelect(true);
                    }
                    else
                    {
                        soundSettingItem.SetSelect(false);
                    }
                }
            }

            foreach (var soundSettingItem in noiseSettingItems)
            {
                var audioData = soundSettingItem.GetData();
                if (audioData is GameNoiseAudioData noiseAudioData)
                {

                    if (noiseAudioData.Id == bgMusicComp.noiseId)
                    {
                        selectedNoiseSettingItem = soundSettingItem;
                        soundSettingItem.SetSelect(true);
                    }
                    else
                    {
                        soundSettingItem.SetSelect(false);
                    }
                }
            }

            DelayRefreshLayout();
        }




        public void OnDeleteUGCMusic(SoundSettingItem item) {
            // 删除UGC
            OnItemSelected(emptyBgSettingItem);
            DelayRefreshLayout();
        }

        public void OnItemSelected(SoundSettingItem item)
        {
            var bgMusicComp =  GameMapSettingManager.Inst.settingEntity.GetComp<BgMusicComponent>();
            var audioData = item.GetData();
            AkSoundManager.Inst.StopGameMusic();
            AkSoundManager.Inst.StopNoiseSound();
            if (audioData is GameBgAudioData bgAudioData)
            {
                if (selectedBgSettingItem == item && selectedBgSettingItem.isPlaying)
                {
                    return;
                }
                if (bgAudioData.IsUGC) {

                    if (DeviceInfoManager.Inst.DeviceBaseData.version == "1.0.1" ||
                        DeviceInfoManager.Inst.DeviceBaseData.version == "1.0.0")
                    {
                        UIAgentManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, (int)ForceUpdate.NeedUpdateFeature);
                        return;
                    }

                    if (string.IsNullOrEmpty(bgAudioData.UgcMusicUrl)) {
                        bool isCancel = false;
                        Action onCancel = ()=>{
                            isCancel = true;
                        };

                        UIManager.Inst.OpenPanel<BgMusicUploadingPanel>(PanelId.BgMusicUploadingPanel, onCancel);
                        int musicLoudness = 0;
                        AlbumUtils.Inst.UploadMusic(_audioLimit, (remoteUrl) => {
                            if (isCancel) {
                                return;
                            }
                            if (!string.IsNullOrEmpty(remoteUrl)) {
                                bgAudioData.SetUGCUrl(remoteUrl, musicLoudness);
                                item.RefreshData();
                                DelayRefreshLayout();
                                AkSoundManager.Inst.StopGameMusic();
                                BgMusicManager.Inst.SetUGCMusic(bgAudioData.UgcMusicUrl, bgAudioData.Loudness);
                                BgMusicManager.Inst.PlayUGCMusic();
                                selectedBgSettingItem.SetSelect(false);
                                selectedBgSettingItem = item;
                                selectedBgSettingItem.SetSelect(true, true);
                                selectedNoiseSettingItem.SetSelect(true);
                            }
                            UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
                        }, err => {
                            UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
                        }, null, loudness => {
                            musicLoudness = loudness;
                        });
                    } else {
                        AkSoundManager.Inst.StopGameMusic();
                        BgMusicManager.Inst.SetUGCMusic(bgAudioData.UgcMusicUrl, bgAudioData.Loudness);
                        BgMusicManager.Inst.PlayUGCMusic();
                        selectedBgSettingItem.SetSelect(false);
                        selectedBgSettingItem = item;
                        selectedBgSettingItem.SetSelect(true, true);
                        selectedNoiseSettingItem.SetSelect(true);
                    }
                    return;
                }
                bgMusicComp.bgId = bgAudioData.Id;
                bgMusicComp.loudness = 0;
                BgMusicManager.Inst.SetUGCMusic("");
                BgMusicManager.Inst.PlayPGCMusic();
                selectedBgSettingItem.SetSelect(false);
                selectedBgSettingItem = item;
                selectedBgSettingItem.SetSelect(true, true);
                selectedNoiseSettingItem.SetSelect(true);

            } else if (audioData is GameNoiseAudioData noiseAudioData)
            {
                if (selectedNoiseSettingItem == item && selectedNoiseSettingItem.isPlaying)
                {
                    return;
                }
                bgMusicComp.noiseId = noiseAudioData.Id;
                if (!string.IsNullOrEmpty(noiseAudioData.State))
                {
                    AkSoundManager.Inst.PlayNoiseSound(noiseAudioData.State);
                }
                else
                {
                    AkSoundManager.Inst.StopNoiseSound();
                }
                selectedNoiseSettingItem.SetSelect(false);
                selectedNoiseSettingItem = item;
                selectedNoiseSettingItem.SetSelect(true, true);
                selectedBgSettingItem.SetSelect(true);
            }

        }

        private void OnDisable()
        {
            AkSoundManager.Inst.StopGameMusic();
            AkSoundManager.Inst.StopNoiseSound();
        }


    }
}
