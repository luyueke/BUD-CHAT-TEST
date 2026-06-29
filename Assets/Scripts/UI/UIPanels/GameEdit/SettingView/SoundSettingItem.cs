// @Author: YangJie
// @Description:
// @Date:  2023/08/07
// @Modify:

using System;
using Es;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit.SettingView {
    public class SoundSettingItem : MonoBehaviour {
        [SerializeField] private GameObject noMusicIconObj;

        [SerializeField] private GameObject musicIconObj;

        [SerializeField] private GameObject selectIconObj;

        [SerializeField] private GameObject importIconObj;

        [SerializeField] private GameObject selectMaskObj;

        [SerializeField] private Text nameText;

        [SerializeField] private Button deleteBtn;


        private Action<SoundSettingItem> onClickedCallBack;
        private Action<SoundSettingItem> onDeleteUGCCallBack;

        public bool isPlaying = false;

        private object audioData;

        private void Awake() {
            noMusicIconObj.SetActive(false);
            musicIconObj.SetActive(false);
            selectIconObj.SetActive(false);
            selectMaskObj.SetActive(false);
            GetComponent<Button>().onClick.AddListener(() => {
                onClickedCallBack?.Invoke(this);
            });
            deleteBtn.onClick.AddListener(OnDeleteUGCMusic);
        }


        public void SetData(GameNoiseAudioData data, Action<SoundSettingItem> callBack = null) {

            audioData = data;
            RefreshData();
            onClickedCallBack = callBack;
        }

        public void SetData(GameBgAudioData data, Action<SoundSettingItem> callBack = null) {
            audioData = data;
            RefreshData();
            onClickedCallBack = callBack;
        }

        public void RefreshData() {
            if (audioData is GameBgAudioData data) {
                nameText.SetLocalText(data.Name);
                if (data.IsUGC) {
                    noMusicIconObj.SetActive(false);
                    if (string.IsNullOrEmpty(data.UgcMusicUrl)) {
                        importIconObj.SetActive(true);
                        musicIconObj.SetActive(false);
                        deleteBtn.gameObject.SetActive(false);
                    } else {
                        importIconObj.SetActive(false);
                        musicIconObj.SetActive(true);
                        deleteBtn.gameObject.SetActive(true);
                    }
                } else {
                    importIconObj.gameObject.SetActive(false);
                    deleteBtn.gameObject.SetActive(false);
                    if (string.IsNullOrEmpty(data.State)) {
                        noMusicIconObj.SetActive(true);
                    } else {
                        musicIconObj.SetActive(true);
                    }
                }
            } else if (audioData is GameNoiseAudioData noiseAudioData) {
                if (string.IsNullOrEmpty(noiseAudioData.State)) {
                    noMusicIconObj.SetActive(true);
                } else {
                    musicIconObj.SetActive(true);
                }
                importIconObj.SetActive(false);
                deleteBtn.gameObject.SetActive(false);
                nameText.SetLocalText(noiseAudioData.Name);
            }
        }


        public object GetData() {
            return audioData;
        }


        public void SetSelect(bool isSelect, bool playing = false) {
            selectMaskObj.SetActive(isSelect);
            isPlaying = playing;
            if (!noMusicIconObj.activeSelf) {
                selectIconObj.SetActive(playing && isSelect);
                if (audioData is GameBgAudioData bgAudioData) {
                    if (bgAudioData.IsUGC) {
                        if (string.IsNullOrEmpty(bgAudioData.UgcMusicUrl)) {
                            musicIconObj.SetActive(false);
                        } else {
                            musicIconObj.SetActive(!playing || !isSelect);
                        }
                    } else {
                        musicIconObj.SetActive(!playing || !isSelect);
                    }
                } else {
                    musicIconObj.SetActive(!playing || !isSelect);
                }
            }
        }

        public void SetDeleteUGCMusic(Action<SoundSettingItem> callBack) {
            onDeleteUGCCallBack = callBack;
        }

        public void OnDeleteUGCMusic() {
            if (audioData is GameBgAudioData data) {
                if (data.IsUGC) {
                    data.SetUGCUrl(null, 0);
                    RefreshData();
                    onDeleteUGCCallBack?.Invoke(this);
                }
            }
        }
    }
}
