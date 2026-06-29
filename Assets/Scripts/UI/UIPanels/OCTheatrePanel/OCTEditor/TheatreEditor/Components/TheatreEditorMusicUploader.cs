using System;
using System.Collections.Generic;
using System.IO;
using GameData.Base;
using Network;
using Network.Http;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorMusicUploader : MonoBehaviour
{
    [SerializeField] private Button uploadBtn;
    [SerializeField] private Text musicNameText;
    [SerializeField] private GameObject loadingObj;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button modifyBtn;
    [SerializeField] private Button deleteBtn;
    [SerializeField] private int maxMusicLenSeconds = 300;

    private Action<string> onMusicUploaded;
    private string currentUrl = "";

    public void Init(Action<string> onUploaded)
    {
        onMusicUploaded = onUploaded;

        uploadBtn?.onClick.RemoveAllListeners();
        uploadBtn?.onClick.AddListener(OnUploadBtnClicked);

        modifyBtn?.onClick.RemoveAllListeners();
        modifyBtn?.onClick.AddListener(OnModifyClicked);

        deleteBtn?.onClick.RemoveAllListeners();
        deleteBtn?.onClick.AddListener(OnDeleteClicked);

        optionsPanel?.SetActive(false);
        SetLoading(false);
    }

    public void SetDisplayUrl(string url)
    {
        currentUrl = url ?? "";
        if (musicNameText != null)
            musicNameText.text = string.IsNullOrEmpty(currentUrl) ? "上传音频" : Path.GetFileName(currentUrl);
    }

    private void SetLoading(bool isLoading)
    {
        loadingObj?.SetActive(isLoading);
    }

    private void OnUploadBtnClicked()
    {
        if (!string.IsNullOrEmpty(currentUrl))
            optionsPanel?.SetActive(true);
        else
            StartUpload();
    }

    private void OnModifyClicked()
    {
        optionsPanel?.SetActive(false);
        StartUpload();
    }

    private void OnDeleteClicked()
    {
        optionsPanel?.SetActive(false);
        SetDisplayUrl("");
        onMusicUploaded?.Invoke("");
    }

    private void StartUpload()
    {
#if UNITY_EDITOR
        string localPath = UnityEditor.EditorUtility.OpenFilePanel(
            "选择音频文件", "",
            "mp3,wav,ogg,mp4,mov,MP3,WAV,OGG,MP4,MOV");
        if (string.IsNullOrEmpty(localPath)) return;
        string url = "file://" + localPath;
        SetDisplayUrl(url);
        onMusicUploaded?.Invoke(url);
#else
        SetLoading(true);
        AlbumUtils.Inst.UploadMusic(maxMusicLenSeconds,
            onSuccess: OnUploadSuccess,
            onFail: err =>
            {
                SetLoading(false);
                TipPanel.ShowToast("音乐上传失败，请重试");
            });
#endif
    }

#if !UNITY_EDITOR
    private void OnUploadSuccess(string url)
    {
        SetLoading(false);
        SetDisplayUrl(url);
        onMusicUploaded?.Invoke(url);
    }
#endif
}
