using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Extensions;
using Es;
using Game.Audio;
using UI.UIPanels.GameEdit.SettingView;
using UnityEngine;
using UnityEngine.UI;

public class VehicleAudioItem : MonoBehaviour
{
    [SerializeField] private GameObject noMusicIconObj;

    [SerializeField] private GameObject musicIconObj;

    [SerializeField] private GameObject selectIconObj;

    [SerializeField] private GameObject importIconObj;
    [SerializeField] private GameObject vipIconObj;

    [SerializeField] private GameObject selectMaskObj;

    [SerializeField] private Text nameText;

    [SerializeField] private Button deleteBtn;

    private VehicleAudioData audioData;

    private bool _isSelected;
    private bool _isPlaying;

    private Action<VehicleAudioItem> onClickedCallBack;
    private Action<VehicleAudioItem> onDeleteUGCCallBack;

    private bool _needVip = false;

    public bool NeedVip => _needVip;

    private void Awake()
    {
        noMusicIconObj.SetActive(false);
        musicIconObj.SetActive(false);
        selectIconObj.SetActive(false);
        selectMaskObj.SetActive(false);
        importIconObj.SetActive(false);
        vipIconObj.SetActive(false);
        deleteBtn.gameObject.SetActive(false);
        GetComponent<Button>().onClick.AddListener(() => {
            onClickedCallBack?.Invoke(this);
        });
        deleteBtn.onClick.AddListener(OnDeleteUGCMusic);
    }

    public void SetData(VehicleAudioData data, Action<VehicleAudioItem> callBack = null,bool needVip = false)
    {
        _needVip = needVip;

        audioData = data;
        RefreshData();
        onClickedCallBack = callBack;
    }

    public void RefreshData()
    {
        if (audioData == null)
        {
            LoggerUtils.LogError("声音文件为空!");
            return;
        }
        vipIconObj.SetActive(_needVip);
        noMusicIconObj.SetActive(false);
        if (audioData.isUGC)
        {
            if (string.IsNullOrEmpty(audioData.MusicUrl))
            {
                nameText.text = "本地上传"; 
            }
            else
            {
                if (string.IsNullOrEmpty(audioData.name))
                {
                    audioData.name = $"提取音乐_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
                }
                nameText.text = audioData.name;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(gameObject.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(gameObject.transform.parent.GetComponent<RectTransform>());
            UpdateVisualState(_isSelected, _isPlaying);
            return;
        }
        importIconObj.gameObject.SetActive(false);
        deleteBtn.gameObject.SetActive(false);
        musicIconObj.SetActive(true);
        nameText.text = audioData.name;
        LayoutRebuilder.ForceRebuildLayoutImmediate(gameObject.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(gameObject.transform.parent.GetComponent<RectTransform>());
        UpdateVisualState(_isSelected, _isPlaying);
    }

    public void SetDeleteUGCMusic(Action<VehicleAudioItem> callBack)
    {
        onDeleteUGCCallBack = callBack;
    }

    public VehicleAudioData GetData()
    {
        return audioData;
    }

    public void Setselect(bool isSelect,bool isPlay = false)
    {
        _isSelected = isSelect;
        _isPlaying = isPlay;
        UpdateVisualState(isSelect, isPlay);
    }

    private void UpdateVisualState(bool isSelect, bool isPlay)
    {
        // selection visuals
        selectMaskObj.SetActive(isSelect);

        if (audioData == null)
        {
            importIconObj.SetActive(false);
            musicIconObj.SetActive(false);
            selectIconObj.SetActive(false);
            deleteBtn.gameObject.SetActive(false);
            return;
        }

        bool hasUrl = !string.IsNullOrEmpty(audioData.MusicUrl);

        if (audioData.isUGC)
        {
            // 需求：url为空 -> 显示import，隐藏selectMusic和BtnDelete
            //      url非空 -> 隐藏import，显示selectMusic和BtnDelete（选中播放时显示selectIcon）
            importIconObj.SetActive(!hasUrl);
            deleteBtn.gameObject.SetActive(hasUrl);

            if (!hasUrl)
            {
                musicIconObj.SetActive(false);
                selectIconObj.SetActive(false);
            }
            else
            {
                bool showSelected = isSelect && isPlay;
                selectIconObj.SetActive(showSelected);
                // musicIconObj 作为“已导入/可播放”的图标（未选中播放时显示）
                musicIconObj.SetActive(!showSelected);
            }
            return;
        }

        // PGC/预置音效：不显示导入/删除；选中播放时显示selectIcon，否则显示musicIcon
        importIconObj.SetActive(false);
        deleteBtn.gameObject.SetActive(false);
        bool showPgcSelected = isSelect && isPlay;
        selectIconObj.SetActive(showPgcSelected);
        musicIconObj.SetActive(!showPgcSelected);
    }

    public void OnDeleteUGCMusic()
    {
        if (!audioData.isUGC)
        {
            return;
        }
        audioData.MusicUrl = null;
        _isSelected = false;
        _isPlaying = false;
        RefreshData();
        UpdateVisualState(false, false);
        onDeleteUGCCallBack?.Invoke(this);
    }
}
