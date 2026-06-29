/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-09-01 15:26:14
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-06 17:37:16
 * @ Description: UGC素材封面截图
 */

using System;
using Game.Utils;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine; 
using UnityEngine.UI; 

public class UgcItemCoverPanel : BasePanel<UgcItemCoverPanel>
{
    [SerializeField]private Text titleTxt;
    [SerializeField]private LoadingButton loadingBtn;
    [SerializeField]private CButton backBtn;
    [SerializeField]private RectTransform photoTF;
    [SerializeField]private GameObject loadingMaskGo;

    Action onReturnCallback;
    Action<bool, byte[]> onComplete;
    int originLayer;
    Color originBgColor;
    Camera mainCamera;

    public override void OnCreate()
    {
        backBtn.onClick.AddListener(OnBackClick);
        loadingBtn.onClick.AddListener(OnConfirmClick);
        loadingMaskGo.SetActive(false);
    }

    public void AddScreenShotComplete(Action<bool, byte[]> complete)
    {
        onComplete = complete;
    }

    public void AddOnCloseListener(Action callback)
    {
        onReturnCallback += callback;
    }

    public void SetTitle(string title)
    {
        this.titleTxt.text = title;
    }

    void OnBackClick()
    {
        UIManager.Inst.ClosePanel(this);
    }

    void OnConfirmClick()
    {
        ShowLoading(true);
        var bytes = ScreenShotUtils.TakeShotWithAlphaUIRectTF(mainCamera, photoTF);
        if (bytes == null || bytes.Length == 0)
        {
            LoggerUtils.LogError("Cover Save Fail");
            onComplete(false, bytes);
            ShowLoading(false);
        } else {
            LoggerUtils.Log("Cover Save Success");
            onComplete(true, bytes);
        }
    }

    public void ShowLoading(bool isShow)
    {
        if (isShow)
        {
            loadingBtn.ShowLoading();
        } else {
            loadingBtn.HideLoading();
        }
        loadingMaskGo.SetActive(isShow);
    }

    public override void OnShow(params object[] args)
    {
        UIManager.Inst.HideAllOtherPanelInWindow(this);
        InputHandlerManager.Inst.SetIsCanSelect(false);
        var shotIncludeLayer = LayerMask.NameToLayer("ShotInclude");
        var defaultLayer = LayerMask.NameToLayer("Default");

        // 设置截图环境
        mainCamera = GameCameraUtils.Inst.GetMainCamera();
        originLayer = mainCamera.cullingMask;
        mainCamera.cullingMask = (1 << shotIncludeLayer) + (1 << defaultLayer);
        originBgColor = Color.clear;

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        originBgColor = mainCamera.backgroundColor;
        mainCamera.backgroundColor = Color.clear;
}

    public override void OnHidden()
    {
        UIManager.Inst.ShowAllOtherPanelInWindow(this);
        onReturnCallback?.Invoke();
        InputHandlerManager.Inst.SetIsCanSelect(true);

        // 重置截图环境
        mainCamera.clearFlags = CameraClearFlags.Skybox;
        mainCamera.backgroundColor = originBgColor;
        mainCamera.cullingMask = originLayer;
    }

    protected override void OnDestroy()
    {
        onReturnCallback = null;
        onComplete = null;
    }

    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }

}