using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorBGItem : MonoBehaviour
{
    [SerializeField] private Button selectBtn;
    [SerializeField] private RemoteImageBehaviour bgImg;
    [SerializeField] private GameObject hintObj;
    [SerializeField] private USwitchToggle beSelectedToggle;
    [SerializeField] private GameObject loadingObj;

    private string remoteFolder;
    private Action<string> onUploadSuccess;
    private bool isFilled;
    private string currentUrl = "";
    private bool isLoading;

    public bool IsSelected => isFilled && beSelectedToggle != null && beSelectedToggle.isOn;

    // 空槽：点击触发上传
    public void InitAsEmpty(string folder, Action<string> onSuccess)
    {
        isFilled = false;
        remoteFolder = folder;
        onUploadSuccess = onSuccess;

        selectBtn?.onClick.RemoveAllListeners();
        selectBtn?.onClick.AddListener(OnUploadClicked);

        SetDisplayUrl(null);

        beSelectedToggle?.Init();
        if (beSelectedToggle != null) beSelectedToggle.isOn = false;
        beSelectedToggle?.gameObject.SetActive(false);
        SetLoading(false);
    }

    public void SetSelectCallback(Action onClicked)
    {
        selectBtn?.onClick.AddListener(() => onClicked?.Invoke());
    }

    // 已填充槽：展示图片，无上传行为
    public void InitAsFilled(string url)
    {
        isFilled = true;
        selectBtn?.onClick.RemoveAllListeners();

        SetDisplayUrl(url);

        beSelectedToggle?.Init();
        if (beSelectedToggle != null) beSelectedToggle.isOn = false;
        beSelectedToggle?.gameObject.SetActive(false);
        SetLoading(false);
    }

    public void SetEditMode(bool editMode)
    {
        if (beSelectedToggle != null)
            beSelectedToggle.gameObject.SetActive(editMode && isFilled);

        if (!editMode && beSelectedToggle != null)
            beSelectedToggle.isOn = false;
    }

    private void SetDisplayUrl(string url)
    {
        currentUrl = url ?? "";
        bool hasImage = !string.IsNullOrEmpty(currentUrl);
        if (bgImg != null)
        {
            if (hasImage) bgImg.Load(currentUrl);
            bgImg.gameObject.SetActive(hasImage);
        }
        RefreshHintObj();
    }

    private void SetLoading(bool loading)
    {
        isLoading = loading;
        loadingObj?.SetActive(isLoading);
        RefreshHintObj();
    }

    private void RefreshHintObj()
    {
        hintObj?.SetActive(!isLoading && string.IsNullOrEmpty(currentUrl));
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
            (Action)(() => SetLoading(true)));
    }
}
