using System;
using System.IO;
using Game.Base;
using Game.Scene.ModeController;
using Game.UGCEditor;
using Game.Utils;
using GameData;
using GameData.Base;
using GameData.Manager;
using GameData.BaseInfo;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Author:Jaywill
/// Desc:封面截图
/// Date:23-07-31 17:11:41
/// </summary>
public class CoverPanel : BasePanel<CoverPanel>
{
    private CButton backBtn;
    private LoadingButton confirmBtn;
    private Action onReturnCallback;
    // private Image refImg;
    // private Image centerMask;//镂空部分
    // private Image centerImg;//白色外框
    [SerializeField] private RectTransform MaskArea;//镂空部分
    [SerializeField] private RectTransform OutImgArea;//白色外框
    [SerializeField] private RectTransform PanelRectransform;

    
    private const float screenShotW = 2033f;
    private const float screenShotH = 1046f;
    private Vector2 frameSize = new Vector2(7, 5);

    private Rect screenShotRec = new Rect();
    public override void OnCreate()
    {
        backBtn = GameObjectEx.FindChildByName(transform, "BackBtn").GetComponent<CButton>();
        confirmBtn = GameObjectEx.FindChildByName(transform, "ConfirmBtn").GetComponent<LoadingButton>();
        backBtn.onClick.AddListener(OnBackBtnClick);
        confirmBtn.onClick.AddListener(OnConfirmBtnClick);

        ResizeShotArea();
    }
    
    private void ResizeShotArea()
    {
        float shotRatio = screenShotW/screenShotH;
        float shotW = OutImgArea.rect.width;
        float shotH = shotW / shotRatio;

        if(shotH > PanelRectransform.rect.height)
        {
            shotH = PanelRectransform.rect.height;
            shotW = shotH * shotRatio;

            float stretchWidth = PanelRectransform.rect.width - shotW;
            OutImgArea.sizeDelta = new Vector2(-stretchWidth, shotH);
            MaskArea.sizeDelta = new Vector2(-(stretchWidth + frameSize.x * 2), shotH - frameSize.y * 2);
        }
        else
        {
            //设置UI区域&边框
            OutImgArea.sizeDelta = new Vector2(OutImgArea.sizeDelta.x, shotH);
            MaskArea.sizeDelta = new Vector2(MaskArea.sizeDelta.x, shotH - frameSize.y * 2);
        }

        Canvas canvas = this.GetComponentInParent<Canvas>();
        ScreenSpaceConvert convert = new ScreenSpaceConvert(canvas);
        screenShotRec = convert.FindScreenRect(OutImgArea);
    }

    public override void OnShow(params object[] args)
    {
        InputHandlerManager.Inst.SetIsCanSelect(false);
        if (args.Length > 0)
        {
            Action callback = args[0] as Action;
            onReturnCallback += callback;
        }

        var camera = GameCameraUtils.Inst.GetMainCamera();
        camera.RemoveLayer(LayerMask.NameToLayer("ShotExclude"));
    }


    public override void OnHidden()
    {
        onReturnCallback?.Invoke();
        InputHandlerManager.Inst.SetIsCanSelect(true);

        var camera = GameCameraUtils.Inst.GetMainCamera();
        camera.AddLayer(LayerMask.NameToLayer("ShotExclude"));
    }

    private void OnBackBtnClick()
    {
        CloseSelf();
    }

    private void OnConfirmBtnClick()
    {
        confirmBtn.ShowLoading();
        var bytes = ScreenShotUtils.ScreenShot(GameCameraUtils.Inst.GetMainCamera(), screenShotRec);
        if (bytes == null || bytes.Length == 0)
        {
            OnFail("保存封面失败");
            return;
        }

        var draftInfo = MapAssetManager.Inst.GetOrCreateDraftInfo(GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>());
        draftInfo.baseInfo.coverAutoSaved = CoverSaveStatus.ManualSaved;
        draftInfo.SetCover(bytes);
        draftInfo.UploadAndSave((info, isSuccess) => {
            if (isSuccess) {
                OnSuccess();
            } else {
                OnFail(draftInfo.GetUploadMessage());
            }
        });
    }

    /// <summary>
    /// 测试代码,保存到本地
    /// </summary>
    /// <param name="img"></param>
    /// <param name="type"></param>
    private void SaveCoverLocal(byte[] img, string saveType = "jpg")
    {
        string DraftPath = Application.streamingAssetsPath + "/U3D/Test/";
        string fileName = "cover." + saveType.ToString().ToLower();
        if (!Directory.Exists(DraftPath))
        {
            Directory.CreateDirectory(DraftPath);
        }
        if (File.Exists(DraftPath + fileName))
        {
            File.Delete(DraftPath + fileName);
        }
        FileStream stream = new FileStream(DraftPath + fileName, FileMode.Create);
        stream.Write(img, 0, img.Length);
        LoggerUtils.Log("local save cover success -- desPath = " + DraftPath + fileName);
        stream.Flush();
        stream.Close();
    }

    private void OnFail(string err)
    {
        TipPanel.ShowToast(err);
        LoggerUtils.LogError("Cover Save Fail");
        confirmBtn.HideLoading();
    }

    private void OnSuccess()
    {
        TipPanel.ShowToast("封面保存成功");
        LoggerUtils.Log("Cover Save Success");
        confirmBtn.HideLoading();
        CloseSelf();
    }


    protected override void OnDestroy()
    {
        onReturnCallback = null;
    }

    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }
}
