using System.Collections.Generic;
using Game.UGCEditor;
using GameData.UGCData;
using System;
using Game.Base;
using Game.Utils;
using GameData.BaseInfo;
using Message;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class UGCMaterialEditorPanel : UGCBaseEditorPanel<UGCMaterialEditorPanel> {
    [Header("左上角")] public LoadingButton exitBtn;
    public LoadingButton publishBtn;
    public LoadingButton saveBtn;

    /// <summary>
    /// 截屏相机
    /// </summary>
    public Camera shotCamera;

    public GameObject TargetModel;
    public UIDragUtil uDragUtil;
    public RawImage ShowGenerateImage;

    public Action PublishAction;
    private const string TAG = "UGCMaterialEditorPanel";
    private Transform ShotTargetModel;
    private MaterialInfo currentMaterialInfo;
    private MeshRenderer shotMeshRenderer;
    private Dictionary<int, Dictionary<Vector2Int, Color>> serverDataDir;

    public override void OnCreate() {
        base.OnCreate();
        CurrentPartIndex = 1;
        exitBtn.onClick.AddListener(OnExitClick);
        saveBtn.onClick.AddListener(OnSaveClicked);
        publishBtn.onClick.AddListener(OnPublishBtnClick);
        editorTool.SetScissorsToggle(false);
        uDragUtil.RotateTarget = TargetModel.transform;
        ShotTargetModel = TargetModel.transform.parent.parent;
        InitCameraShotModel();
        var wrapper = Loader.Load<Material>("Assets/Arts/Game/BaseMatMaterial/AnimeStyleMatte.mat");
        StyleMaterial = wrapper.RetainAsset(this.gameObject);
    }
    
    private void OnPublishBtnClick()
    {
        // PublishCurrencyPanel publishCurrencyPanel = UIManager.Inst.OpenPanel<PublishCurrencyPanel>(PanelId.PublishCurrencyPanel);
        // publishCurrencyPanel.SetCallback((type =>
        // {
        //     OnPublishClicked(type);
        // }));
        OnPublishClicked(CurrencyType.PinkCoin);
    }

    private void InitCameraShotModel()
    {
        if (ShotTargetModel)
        {
            var shotModel = screenShotNode.Find("ClothModelTarget");
            shotMeshRenderer = shotModel.Find("Cube/Mesh1").GetComponent<MeshRenderer>();
            var targetMeshRenderer = ShotTargetModel.Find("Cube/Mesh1").GetComponent<MeshRenderer>();
            shotMeshRenderer.material = targetMeshRenderer.material;
            var designRatio =  2436/ 1125;
            var realRatio = Screen.width / (float)Screen.height;
            var tarSize = realRatio / designRatio;
            ScreenShotCamera.orthographicSize = tarSize;
        }
    }

    public override void OnShow(params object[] args) {
        base.OnShow(args);
        var cData = args[0] as UGCMaterialData;
        currentClothesData = cData;
        currentMaterialInfo = args[1] as MaterialInfo;
        _canvasType = (CanvasType)currentMaterialInfo.canvasType;
        mapCanvas.OnCreate(_canvasType);
        mapCanvas.OnAddRecordEvent = AddRecord;
        InititalMapCanvas();
        GetOriginalMaterials(TargetModel,shotMeshRenderer.gameObject);
        GameTimeUtils.Inst.StartCollect(TAG);
        TransformInteractorController.Inst.InterActor.ResetInfo();
        var curStyle = currentMaterialInfo.ugcStyle == (int) UgcShaderStyle.Normal
            ? MISource.Source.Bud
            : MISource.Source.Create;
        StyleSource.DefualtOn(curStyle);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        GameTimeUtils.Inst.StopCollect(TAG);
    }


    #region 初始化

    public void InititalMapCanvas() {
        ugcPartTextureDatas = new Dictionary<int, UGCPartTextures>();
        if (currentClothesData.parts != null) {
            serverDataDir = GetClothesPartDicByID(currentClothesData);
            for (int i = 0; i < currentClothesData.parts.Count; i++) {
                var partData = currentClothesData.parts[i];
                int partIndex = currentClothesData.parts[i].type;
                var texData = mapCanvas.GenerateNoFilterTexture2D(serverDataDir[partIndex]);
                texData.mRT = mapCanvas.GenerateFilterTexture(texData.mTex);
                texData.aRT = mapCanvas.GenerateFilterTexture(texData.aTex);
                texData.finalRT = mapCanvas.GenerateFinalTexture();
                ugcPartTextureDatas.Add(partIndex, texData);
                if (partData != null) {
                
                    GenerateImportUGCClothes(partIndex, partData, drawCanvas.mDrawPanel);
                    HierarchicalSort();
                }
                drawCanvas.SetRawImage(texData.mRT);
                SetFinalTargetTexture(texData.finalRT);
            }
        }

        ShowSmallGenerateTexture(CurrentPartIndex);
        var modelMaterial = TargetModel.GetComponent<MeshRenderer>().material;
        modelMaterial.SetTexture("_BaseMap", ugcPartTextureDatas[CurrentPartIndex].finalRT);

        mapCanvas.ChangeClothesPart(ugcPartTextureDatas[CurrentPartIndex], new List<Vector2Int>(),
            serverDataDir[CurrentPartIndex]);

        UGCImportPhotoManager.Inst.SetElementHandleCanUse(false);
        UGCImportTextManager.Inst.SetElementHandleCanUse(false);
    }


    private void ShowSmallGenerateTexture(int partIndex) {
        drawCanvas.SetRawImage(ugcPartTextureDatas[partIndex].mRT);
        ShowGenerateImage.texture = ugcPartTextureDatas[partIndex].finalRT;
        SetFinalTargetTexture(ugcPartTextureDatas[partIndex].finalRT);
    }

    #endregion


    #region 数据存储

    private void OnExitClick() {
        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetIsCloseSelf(false);
        commonConfirmPanel.SetLocalText("确认保存", "保存当前的创作进度吗？", "保存", "不保存");
        commonConfirmPanel.SetOnClickAction(() => {
            commonConfirmPanel.SetConfirmLoadingVisible(true);
            UploadUGCData(() => {
                if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                {
                    commonConfirmPanel.Close();
                }
                GameController.ExitGame(() => {
                    UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCResourceEditWindow, true);
                    UIManager.Inst.BackToLastWindow();
                    MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                });
            });
        }, () => {
            if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
            {
                commonConfirmPanel.Close();
            }
            GameController.ExitGame(() => {
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCResourceEditWindow, true);
                UIManager.Inst.BackToLastWindow();
                MessageHelper.Broadcast(DraftMessage.RefreshDraft);
            });
        });
        commonConfirmPanel.SetOnCloseAction(() => {
        });
    }

    private void OnSaveClicked() {
        saveBtn.ShowLoading();
        SaveUGCData(() => {
            saveBtn.HideLoading();
        });
    }

    private void SaveUGCData(Action callBack = null) {
        string[] imgs = UGCImportPhotoManager.Inst.GetUrlArr();
        currentMaterialInfo.imgs = imgs;
        currentMaterialInfo.ugcStyle = (int)curAnimeStyle;
        var draftInfo = MaterialAssetManager.Inst.GetOrCreateDraftInfo(currentMaterialInfo);
        var ugcData = GetUGCData();
        draftInfo.SetMetaData(JsonConvert.SerializeObject(ugcData));

        var partTextures = GetPartTextures();
        draftInfo.SetParts(partTextures.Item2[0]);
        draftInfo.imgs = UGCImportPhotoManager.Inst.GetUrlArr();
        
        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG);//重启编辑时长
        LoggerUtils.Log("###原编辑总时长："+draftInfo.editTime + "  当次编辑时长："+ curEditTime);
        draftInfo.editTime += curEditTime;
        
        GetCover(pngBytes =>
        {
            draftInfo.SetCover(pngBytes);
            draftInfo.UploadAndSave((info, isSuccess) => {
                callBack?.Invoke();
                TipPanel.ShowToast("保存成功:D");
            });
        });
    }

    private void UploadUGCData(Action<bool> uploadCallBack) {
        UploadUGCData(null, uploadCallBack);
    }

    private void UploadUGCData(Action saveCallBack = null, Action<bool> uploadCallBack = null) {
        string[] imgs = UGCImportPhotoManager.Inst.GetUrlArr();
        currentMaterialInfo.imgs = imgs;
        currentMaterialInfo.ugcStyle = (int)curAnimeStyle;
        var draftInfo = MaterialAssetManager.Inst.GetOrCreateDraftInfo(currentMaterialInfo);
        var ugcData = GetUGCData();
        draftInfo.SetMetaData(JsonConvert.SerializeObject(ugcData));
        var partTextures = GetPartTextures();
        draftInfo.SetParts(partTextures.Item2[0]);
        draftInfo.imgs = UGCImportPhotoManager.Inst.GetUrlArr();
        
        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG);//重启编辑时长
        LoggerUtils.Log("###原编辑总时长："+draftInfo.editTime + "  当次编辑时长："+ curEditTime);
        draftInfo.editTime += curEditTime;
                
        GetCover(pngBytes =>
        {
            draftInfo.SetCover(pngBytes);
            draftInfo.UploadAndSave((info, isSuccess) => {
                if (isSuccess) {
                    currentMaterialInfo.materialUrl = info.GetPartsRemoteUrl();
                    currentMaterialInfo.metaDataUrl = info.GetMetadataRemoteUrl();
                    currentMaterialInfo.cover = info.GetCoverRemoteUrl();
                }

                uploadCallBack?.Invoke(isSuccess);
            });
            saveCallBack?.Invoke();
        });
    }


    private void GetCover(Action<byte[]> callBack) {
        var lastRenderTexture = shotCamera.targetTexture;
        var tmpRenderTexture = new RenderTexture(960, 960, 24, RenderTextureFormat.ARGB32);
        shotCamera.targetTexture = tmpRenderTexture;
        shotCamera.Render();

        GlobalCoroutineUtils.Inst.WaitForEndOfFrame(() => {
            var pngBytes = ScreenShotUtils.TakeShot(shotCamera, new Rect(0, 0, 960, 960));
            shotCamera.targetTexture = lastRenderTexture;
            Destroy(tmpRenderTexture);
            tmpRenderTexture = null;
            callBack?.Invoke(pngBytes);
        });
    }

    private UGCMaterialData GetUGCData() {
        var data = new UGCMaterialData {
            id = currentClothesData.id,
            parts = new List<UGCPartData>()
        };

        foreach (var parts in serverDataDir) {
            int partIndex = parts.Key;
            var partData = new UGCPartData {
                type = partIndex
            };
            var pixels = new List<UGCPixelData>();
            foreach (var pixel in parts.Value) {
                var pixelData = new UGCPixelData {
                    col = FormatUtils.ColorRGBAToString(pixel.Value),
                    p = FormatUtils.Vector2IntToString(pixel.Key)
                };
                pixels.Add(pixelData);
            }

            SetHierarchicalSort();
            partData.photos = UGCImportPhotoManager.Inst.GetPartPhotoDatas(partIndex);
            partData.texts = UGCImportTextManager.Inst.GetPartTextDatas(partIndex);
            partData.pixels = pixels;
            data.parts.Add(partData);
        }

        return data;
    }


    private void OnPublishClicked(CurrencyType currencyType) {
        publishBtn.ShowLoading();
        UploadUGCData((Action<bool>)((isSuccess) => {
            publishBtn.HideLoading();
            if (isSuccess)
            {
                string[] photoImages = currentMaterialInfo.imgs;
                bool isVip = VipDataManager.Inst.isVip;
                if (photoImages != null && photoImages.Length > 0 && !isVip)
                {
                    string titleStr = LocalizationManager.Inst.GetLocalizedText("您正在使用的VIP功能：添加手机相册图片");
                    var panel = UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel,titleStr, new List<JoinVipType>
                    {
                        JoinVipType.Image
                    });
                    return;
                }
                var publishMachine = new UGCPublishStateMachine();
                publishMachine.SetStates(new List<UGCPublishStateBase>() {
                    new UGCMaterialDetailState()
                });

                publishMachine.SetEditData(new MaterialEditData()
                {
                    draftInfo = MaterialAssetManager.Inst.GetOrCreateDraftInfo(currentMaterialInfo.Clone()),
                    currencyType = currencyType
                });
                publishMachine.SetFinishCallBack(() => {
                    GameController.ExitGame(() =>
                    {
                        UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCResourceEditWindow, true);
                        UIManager.Inst.BackToLastWindow();
                        if (UIManager.Inst.TryFindPanel(PanelId.AssetStudioDraftCommonPanel, out AssetStudioDraftCommonPanel panel))
                        {
                            panel.OnPublishCallBack(true);
                        }
                    });
                });
   
                publishMachine.Start();
            }
            else
            {
                TipPanel.ShowToast("保存草稿失败");
            }
        }));
    }

    #endregion
}
