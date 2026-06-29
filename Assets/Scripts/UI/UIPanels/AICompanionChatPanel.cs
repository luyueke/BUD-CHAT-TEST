using System;
using Game;
using Sirenix.OdinInspector;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AI伴侣聊天面板（竖屏）
/// 打开/关闭时的截帧+旋转均由调用方通过 ScreenOrientationHelper.Inst.Switch 完成，
/// 面板本身只负责设置竖屏 UI；关闭按钮自行触发横屏切换。
/// </summary>
public class AICompanionChatPanel : BasePanel<AICompanionChatPanel>
{
    private GameObject LandscapeContainer;
    private Image LandscapeImage;

    private GameObject PortraitContainer;
    private Image PortraitImage;

    public CButton BtnClose;

    private bool _landscapeRestoreStarted;
    private GameObject _prevPanelGO;

    public ChatMainPanel chatMainPanel;

    private CanvasScaler _rootCanvasScaler;
    private Vector2 _landscapeReferenceResolution;
    private float _landscapeMatchWidthOrHeight;

    public override void OnCreate()
    {
        base.OnCreate();
        //LandscapeContainer = transform.Find("LandscapeContainer")?.gameObject;
        //PortraitContainer = transform.Find("PortraitContainer")?.gameObject;
        //if (LandscapeContainer != null)
        //    LandscapeImage = LandscapeContainer.transform.Find("LandscapeImage")?.GetComponent<Image>();
        //if (PortraitContainer != null)
        //    PortraitImage = PortraitContainer.transform.Find("PortraitImage")?.GetComponent<Image>();
        if (BtnClose != null)
            BtnClose.onClick.AddListener(OnCloseClick);
    }

    /// <summary>
    /// 调用方已通过 ScreenOrientationHelper 切换到竖屏，OnShow 直接建立竖屏 UI。
    /// </summary>
    private void RestorePrevPanel()
    {
        if (_prevPanelGO == null) return;
        _prevPanelGO.SetActive(true);
        _prevPanelGO = null;
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _landscapeRestoreStarted = false;
        // args[0]：调用方传入自己的 GameObject，打开竖屏面板时隐藏，关闭时恢复
        _prevPanelGO = args?.Length > 0 ? args[0] as GameObject : null;
        // 调用方已通过 onPreRotate 在 Overlay 就位后隐藏自己，此处不重复 SetActive

        //try
        //{
        //    CanvasScaler cs = GameObject.Find("Canvas").GetComponent<CanvasScaler>();
        //    cs.referenceResolution = new Vector2(1125, 2436);
        //}
        //catch (Exception ex)
        //{
        //    Debug.LogError(ex);
        //    CloseSelf();
        //    return;
        //}

        //var rt = PortraitContainer.GetComponent<RectTransform>();
        //rt.anchorMin = Vector2.zero;
        //rt.anchorMax = Vector2.one;
        //rt.offsetMin = Vector2.zero;
        //rt.offsetMax = Vector2.zero;

        //SetContainerVisible(showLandscape: false);
        chatMainPanel?.ShowDefault();
    }

    private void OnCloseClick()
    {
        _landscapeRestoreStarted = true;
        ScreenOrientationHelper.Inst.Switch(
            ScreenOrientation.LandscapeLeft,
            onComplete: () =>
            {
                RestorePrevPanel();
                CloseSelf();
                //try
                //{
                //    CanvasScaler cs = GameObject.Find("Canvas").GetComponent<CanvasScaler>();
                //    cs.referenceResolution = new Vector2(2436, 1125);
                //}
                //catch (Exception ex) { Debug.LogError(ex); }
            }
        );
    }

    //public override void OnHidden()
    //{
    //    base.OnHidden();
    //    RestorePrevPanel();
    //    if (_landscapeRestoreStarted) return;
    //    _landscapeRestoreStarted = true;
    //    // 取消进行中的旋转（如 Switch 过程中被外部强制关闭），同步恢复横屏
    //    ScreenOrientationHelper.Inst.Cancel();
    //    //SetLandscape();
    //    try
    //    {
    //        CanvasScaler cs = GameObject.Find("Canvas").GetComponent<CanvasScaler>();
    //        cs.referenceResolution = new Vector2(2436, 1125);
    //    }
    //    catch (Exception ex) { Debug.LogError(ex); }
    //}

    //protected override void OnDestroy()
    //{
    //    RestorePrevPanel();
    //    if (!_landscapeRestoreStarted)
    //    {
    //        ScreenOrientationHelper.Inst.Cancel();
    //        //SetLandscape();
    //    }
    //    base.OnDestroy();
    //}

    //private void SetContainerVisible(bool showLandscape)
    //{
    //    LandscapeContainer?.SetActive(showLandscape);
    //    PortraitContainer?.SetActive(!showLandscape);
    //}

    //private void SetLandscape()
    //{
    //    Screen.orientation = ScreenOrientation.LandscapeLeft;
    //    Screen.autorotateToPortrait = false;
    //    Screen.autorotateToPortraitUpsideDown = false;
    //    Screen.autorotateToLandscapeLeft = true;
    //    Screen.autorotateToLandscapeRight = true;
    //}

    public void BeginCreateRoleChat()
    {
        chatMainPanel?.BeginCreateRoleChat();
    }
}
