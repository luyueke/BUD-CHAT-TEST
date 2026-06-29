using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorBGUploader : MonoBehaviour
{
    [SerializeField] private Button uploadBtn;
    [SerializeField] private RemoteImageBehaviour previewImage;
    [SerializeField] private GameObject uploadHint;
    [SerializeField] private GameObject loadingObj;

    private string remoteFolder;
    private Action<string> onUploadSuccess;
    private Action onUploadFail;
    private Func<TheatreEditorDataCenter> _getDataCenter;

    public void Init(string folder, Action<string> onSuccess, Action onFail = null, Func<TheatreEditorDataCenter> getDataCenter = null)
    {
        remoteFolder = folder;
        onUploadSuccess = onSuccess;
        onUploadFail = onFail;
        _getDataCenter = getDataCenter;
        uploadBtn?.onClick.RemoveAllListeners();
        uploadBtn?.onClick.AddListener(OnUploadClicked);
        SetLoading(false);
    }

    public void SetDisplayUrl(string url)
    {
        bool hasImage = !string.IsNullOrEmpty(url);
        if (previewImage != null)
        {
            if (hasImage) previewImage.Load(url);
            previewImage.gameObject.SetActive(hasImage);
        }
        uploadHint?.SetActive(!hasImage);
    }

    private void SetLoading(bool isLoading)
    {
        loadingObj?.SetActive(isLoading);
    }

    private void OnUploadClicked()
    {
#if UNITY_EDITOR
        string localPath = UnityEditor.EditorUtility.OpenFilePanelWithFilters(
            "选择背景图片", "",
            new[] { "图片文件", "png,jpg,jpeg,PNG,JPG,JPEG", "所有文件", "*" });
        if (string.IsNullOrEmpty(localPath)) return;
        OpenImageEditPanel("file://" + localPath);
#else
        var albumParams = new OpenSystemAlbumParams { albumType = 1, isCrop = 0 };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.openSystemAlbum, OnAlbumResult);
        MobileInterface.Instance.OpenSystemAlbum(JsonConvert.SerializeObject(albumParams));
#endif
    }

    private void OnAlbumResult(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.openSystemAlbum);
        if (string.IsNullOrEmpty(msg)) return;
        var albumRes = JsonConvert.DeserializeObject<AlbumResData>(msg);
        if (albumRes == null || string.IsNullOrEmpty(albumRes.localUrl)) return;
        OpenImageEditPanel(albumRes.localUrl);
    }

    private void OpenImageEditPanel(string localPath)
    {
        UIManager.Inst.OpenPanel(PanelId.TheatreEditorImageEditPanel, localPath,
            (Action<string>)(url =>
            {
                SetLoading(false);
                SetDisplayUrl(url);
                onUploadSuccess?.Invoke(url);
            }),
            remoteFolder,
            (Action)(() => SetLoading(true)),
            _getDataCenter?.Invoke());
    }
}
