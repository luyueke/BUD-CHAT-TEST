using System.Collections.Generic;
using Es;
using Game.UGCEditor;
using GameData.UGCData;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Basic;
using Game.Avatar;
using Game.Base;
using Game.Config;
using Game.Pet;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.UI;
using Newbie;
using EventTracking;

public class UGCResourceEditPanel :UGCBaseEditorPanel<UGCResourceEditPanel>
{
    [Header("左上角")] public LoadingButton exitBtn;
    public LoadingButton publishBtn;
    public LoadingButton saveBtn;
    public Toggle showModeToggle;
    public Toggle showPetModeToggle;
    public CButton resetCameraBtn;
    public Transform elementPanelRoot;
    public Text PartName;
    public Image PartIconImage;
    public RawImage GenerateClothesMask;
    public RawImage ShowGenerateImage;

    public UGCPreviewInputReceiver previewInputReceiver;

    private int defaultUGCType = 1;
    private SkinInfo currentClothesInfo;

    private SpriteAtlas iconAtlas;

    [Header("切换部件")] public CButton SwitchPartBtn;
    public CButton ClothesIconBtn;

    [Header("左侧预览")]
    public Transform ClothesModelParent;
    public Transform AvatarModelParent;
    public GameObject TouchAreaObj; //手指触摸区域
    public RectTransform DragRect;
    public GameObject PreviewCamera;
    public GameObject AvatarCamera;
    private GameObject clothesModel;
    private GameObject clothesMeshModel;
    private GameObject shotModel;
    private UgcPreviewInputHandler inputHandler;
    private List<GameObject> clothPartsMesh;
    private CharacterWrap characterWrap;
    private CharacterWrap adjustCharacterWrap;
    private bool isShowAvatar = false; //预览衣服(false)/人物
    public CButton tipsBtn;
    private PetWrap petWrap;
    private PetWrap adjustPetWrap;
    private float startTime;

    private Dictionary<int, Dictionary<Vector2Int, Color>> serverDataDir;
    private RawImage previewRawImage;
    private const string TAG = "UGCResourceEditPanel";
    private bool isFacePart = false;
    private string assetsDir;

    private List<Material> avatarOrgMaterials = new List<Material>();
    private List<Material> avatarAnimeStyleMaterials = new List<Material>();
    private List<Renderer> avatarTargetModelRenderers = new List<Renderer>();
    string key = "FirstOpenUGCEditPanel" + AccountDataManager.Inst.Uid;
    public override void OnCreate()
    {
        base.OnCreate();
        previewRawImage = DragRect.GetComponent<RawImage>();
        CurrentPartIndex = defaultUGCType;
        iconAtlas = XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(
            "Assets/Loadable/UI/UIPanel/UGCResourceEditPanel/ugcIconAtlas.spriteatlas", this.gameObject);
        elementPanelRoot = GameObjectEx.FindChildByName(transform,"ElementPanel");
        exitBtn.onClick.AddListener(OnExitClick);
        saveBtn.onClick.AddListener(OnSaveClicked);
        publishBtn.onClick.AddListener(OnPublishBtnClick);
        showModeToggle.onValueChanged.AddListener(OnPlayerModeChange);
        showPetModeToggle.onValueChanged.AddListener(OnPlayerModeChange);
        resetCameraBtn.onClick.AddListener(ResetCamera);
        SwitchPartBtn.onClick.AddListener(OnSwitchPartBtnClick);
        ClothesIconBtn.onClick.AddListener(OnSwitchPartBtnClick);
        editorTool.OnAdjustAct = OnAdjustClick;
        var wrapper = Loader.Load<Material>("Assets/Arts/Game/BaseMatMaterial/UgcAnimeStyle.mat");
        StyleMaterial = wrapper.RetainAsset(this.gameObject);
        //记录打开时间
        startTime = Time.time;
    }
    
    private void OnPublishBtnClick()
    {
        OnPublishClicked(CurrencyType.PinkCoin);

    }


    private void ShowSmallGenerateTexture(int partIndex)
    {
        drawCanvas.SetRawImage(ugcPartTextureDatas[partIndex].mRT);
        drawCanvas.SetTransparentMat(currentClothesInfo.templateId, ugcPartTextureDatas[partIndex].mRT,
            ugcPartTextureDatas[partIndex].aRT);

        ShowGenerateImage.texture = ugcPartTextureDatas[partIndex].finalRT;
        SetFinalTargetTexture(ugcPartTextureDatas[partIndex].finalRT);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (!PlayerPrefs.HasKey(key) && !BootPanel.isPlaying && SignInPanel.isNewPlayer)
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            UIManager.Inst.OpenPanel(PanelId.BootPanel, WindowId.UGCResourceEditWindow, 112);
        }
        var cData = args[0] as UGCClothesData;
        currentClothesData = cData;
        currentClothesInfo = args[1] as SkinInfo;
        assetsDir = IsCurSkinTypeAvatar() ? GameConsts.ClothesAssetDir : GameConsts.PetClothesAssetDir;
        isFacePart = drawCanvas.IsTransparentMat(currentClothesInfo.templateId);
        InitSkinDetailInfo(isFacePart);
        StyleSource.gameObject.SetActive(!isFacePart);
        _canvasType = (CanvasType)currentClothesInfo.canvasType;
        mapCanvas.OnCreate(_canvasType);
        mapCanvas.OnAddRecordEvent = AddRecord;
        InititalMapCanvas();
        InitClothModel();
        InitPreviewHandler();
        InitAvatarHandler();
        InitCharacterPreview();
        InitModelPartsMats();
        SwitchPart(CurrentPartIndex);
        InitCameraShotModel();
        InitPreviewLayer();
        if (!isFacePart)
        {
            GetOriginalMaterials(clothesModel,shotModel);
        }
        //修改图片文字在纯透明模版上的层级
        if (isFacePart)
        {
            elementPanelRoot.SetParent(elementPanelRoot.parent.parent);
            elementPanelRoot.SetSiblingIndex(1);
            AdjustCharacter();
        }
        editorTool.SetEraserToggle(isFacePart);
        editorTool.SetScissorsToggle(!isFacePart);
        editorTool.SetAdjustVisible(isFacePart);
        GameTimeUtils.Inst.StartCollect(TAG);
        showModeToggle.gameObject.SetActive(IsCurSkinTypeAvatar());
        showPetModeToggle.gameObject.SetActive(!IsCurSkinTypeAvatar());
        copyEditPanel.isCharacter = IsCurSkinTypeAvatar();
        var curStyle = currentClothesInfo.ugcStyle == (int) UgcShaderStyle.Normal
            ? MISource.Source.Bud
            : MISource.Source.Create;
        StyleSource.DefualtOn(curStyle);
        tipsBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.UGCEditTipsPanel);
        });
    }

    public override void OnHidden()
    {
        base.OnHidden();
        GameTimeUtils.Inst.StopCollect(TAG);
    }
    
    protected override void SetUgcStyle(MISource.Source source)
    {
        base.SetUgcStyle(source);
        
        if (source == MISource.Source.Create)
        {
            GetOrCreateAvatarNewAnimTypeMaterials();
        }

        curAnimeStyle = source == MISource.Source.Bud ? UgcShaderStyle.Normal : UgcShaderStyle.Anime;
        for (var i = 0; i < avatarTargetModelRenderers.Count; i++)
        {
            var curMat= curAnimeStyle == UgcShaderStyle.Normal ? avatarOrgMaterials[i] : avatarAnimeStyleMaterials[i];
            avatarTargetModelRenderers[i].material = curMat;
        }
    }
    
    private void GetOrCreateAvatarNewAnimTypeMaterials()
    {
        if (avatarAnimeStyleMaterials.Count == 0)
        {
            var count = avatarOrgMaterials.Count;
            for (int i = 0; i < count; i++)
            {
                var newMat = new Material(StyleMaterial);
                newMat.SetTexture(mainTexName,avatarOrgMaterials[i].mainTexture);
                if (newMat.HasTexture(alphaTexName))
                {
                    var alphaTex = avatarOrgMaterials[i].GetTexture(alphaTexName);
                    newMat.SetTexture(alphaTexName, alphaTex);
                }
                avatarAnimeStyleMaterials.Add(newMat);
            }
        }
    }

    private void GetAvatarOriginalMaterials(Transform avatar1, Transform avatar2)
    {
        var ugcClothNode1 = GameObjectEx.FindChildByName(avatar1,
                GetUGCMeshNodeName((AvatarSubType) currentClothesInfo.subType,currentClothesInfo.skinType));

        var ugcClothNode2 = GameObjectEx.FindChildByName(avatar2,
            GetUGCMeshNodeName((AvatarSubType) currentClothesInfo.subType,currentClothesInfo.skinType));
        avatarOrgMaterials.Clear();
        
        var renderers1 = ugcClothNode1.GetComponentsInChildren<Renderer>(true);
        if (renderers1 != null && renderers1.Length != 0)
        {
            avatarTargetModelRenderers.AddRange(renderers1);
            for (var i = 0; i < renderers1.Length; i++)
            {
                avatarOrgMaterials.Add(renderers1[i].material);
            }
        }
        
        var renderers2 = ugcClothNode2.GetComponentsInChildren<Renderer>(true);
        if (renderers2 != null && renderers2.Length != 0)
        {
            avatarTargetModelRenderers.AddRange(renderers2);
            avatarTargetModelRenderers.RemoveAll(x => x.name.Contains(removeRendererName));
            for (var i = 0; i < renderers2.Length; i++)
            {
                avatarOrgMaterials.Add(renderers2[i].material);
            }
        }
    }

    private void InitSkinDetailInfo(bool isFacePart)
    {
        if (isFacePart && currentClothesInfo.skinDetailInfo == null)
        {
            string templateId = currentClothesInfo.templateId;
            var oriConfigData = IsCurSkinTypeAvatar()
                ? DataTables.GetAvatarCommonData(templateId)
                : DataTables.GetPetAvatarCommonData(templateId);
            var detailInfo = new SkinDetailInfo();
            detailInfo.pDef = oriConfigData.pDef;
            detailInfo.rDef = oriConfigData.rDef;
            detailInfo.sDef = oriConfigData.sDef;
            currentClothesInfo.skinDetailInfo = detailInfo;
        }
    }

    private void InititalMapCanvas()
    {
        ugcPartTextureDatas = new Dictionary<int, UGCPartTextures>();
        if (currentClothesData.parts != null)
        {
            serverDataDir = GetClothesPartDicByID(currentClothesData);
            for (int i = 0; i < currentClothesData.parts.Count; i++)
            {
                var partData = currentClothesData.parts[i];
                int partIndex = currentClothesData.parts[i].type;
                var texData = mapCanvas.GenerateNoFilterTexture2D(serverDataDir[partIndex]);
                texData.mRT = mapCanvas.GenerateFilterTexture(texData.mTex);
                texData.aRT = mapCanvas.GenerateFilterTexture(texData.aTex);
                texData.finalRT = mapCanvas.GenerateFinalTexture();
                ugcPartTextureDatas.Add(partIndex, texData);
                if (partData != null)
                {
                    UGCImportPhotoManager.Inst.OnChangePart(partIndex);
                    UGCImportTextManager.Inst.OnChangePart(partIndex);
                    GenerateImportUGCClothes(partIndex, partData, drawCanvas.mDrawPanel);
                    HierarchicalSort();
                }
                drawCanvas.SetRawImage(texData.mRT);
                drawCanvas.SetTransparentMat(currentClothesInfo.templateId, ugcPartTextureDatas[partIndex].mRT,
                    ugcPartTextureDatas[partIndex].aRT);
                SetFinalTargetTexture(texData.finalRT);
            }
        }
        UGCImportPhotoManager.Inst.SetElementHandleCanUse(false);
        UGCImportTextManager.Inst.SetElementHandleCanUse(false);

    }

    protected override void OnColorToggleChange(bool isSelected)
    {
        base.OnColorToggleChange(isSelected);
        // previewInputReceiver.enabled = !isSelected;
    }

    public  void OnSelectHandle(int partIndex)
    {
        var staticData = UgcPartDataManager.Inst.GetUgcPartData(currentClothesData.id, partIndex);
        var inoperableArea = GetInoperableArea(staticData.inoperableArea);
        mapCanvas.ChangeClothesPart(ugcPartTextureDatas[partIndex],inoperableArea ,
            serverDataDir[partIndex]);
        PartName.SetLocalText(staticData.partName);
        PartIconImage.sprite = iconAtlas.GetSprite(staticData.iconSpriteName);
        var tempTex = Loader.Load<Texture>(assetsDir + staticData.maskSpriteName, this.gameObject);
        GridMapClothesMask.texture = tempTex;
        GenerateClothesMask.texture = tempTex;
        UGCImportPhotoManager.Inst.OnChangePart(partIndex);
        UGCImportTextManager.Inst.OnChangePart(partIndex);
    }

    public List<Vector2Int> GetInoperableArea(List<Vector2Int> inoperableArea)
    {
        if (inoperableArea==null)
        {
            return null;
        }
        if (_canvasType == CanvasType.Canvas_32)
        {
            return inoperableArea;
        }
        List<Vector2Int> inoperableArea64 = new List<Vector2Int>();
        for (int i = 0; i < inoperableArea.Count; i++)
        {
            inoperableArea64.Add(new Vector2Int(inoperableArea[i].x*2, inoperableArea[i].y*2));
            inoperableArea64.Add(new Vector2Int(inoperableArea[i].x*2, inoperableArea[i].y*2+1));
            inoperableArea64.Add(new Vector2Int(inoperableArea[i].x*2+1, inoperableArea[i].y*2+1));
            inoperableArea64.Add(new Vector2Int(inoperableArea[i].x*2+1, inoperableArea[i].y*2));
        }
        return inoperableArea64;

    }


    private void OnExitClick()
    {
        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("确认保存","保存当前的创作进度吗？", "保存", "不保存");
        commonConfirmPanel.SetIsCloseSelf(false);
        commonConfirmPanel.SetOnClickAction(() =>
        {
            commonConfirmPanel.SetConfirmLoadingVisible(true);
            UploadClothesData(() =>
            {
                if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                {
                    commonConfirmPanel.Close();
                }
                GameController.ExitGame(() =>
                {
                    ContestDataManager.Inst.SkinContest = null;
                    UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCResourceEditWindow, true);
                    UIManager.Inst.BackToLastWindow();
                    MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                });

            });

        }, () =>
        {
            if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
            {
                commonConfirmPanel.Close();
            }
            GameController.ExitGame(() =>
            {
                ContestDataManager.Inst.SkinContest = null;
                UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCResourceEditWindow, true);
                UIManager.Inst.BackToLastWindow();
                MessageHelper.Broadcast(DraftMessage.RefreshDraft);
            });
        });
    }

    private void OnSaveClicked()
    {
        saveBtn.ShowLoading();
        SaveClothesData(() =>
        {
            saveBtn.HideLoading();
        });
        // 保存进度
    }


    private void SaveClothesData(Action callBack = null)
    {
        string[] imgs = UGCImportPhotoManager.Inst.GetUrlArr();
        currentClothesInfo.imgs = imgs;
        currentClothesInfo.ugcStyle = (int)curAnimeStyle;
        var draftInfo = SkinAssetManager.Inst.GetOrCreateDraftInfo(currentClothesInfo);
        var clothesData = GetClothesData();
        draftInfo.SetMetaData(JsonConvert.SerializeObject(clothesData));
        var clothesBigTexture = GetBigTexture();
        draftInfo.SetClothesSaveTexture2D(clothesBigTexture);

        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG);//重启编辑时长
        LoggerUtils.Log("###原编辑总时长："+draftInfo.editTime + "  当次编辑时长："+ curEditTime);
        draftInfo.editTime += curEditTime;
        // draftInfo.photoUrls = UGCImportPhotoManager.Inst.GetUrlArr();

        Destroy(clothesBigTexture);
        GetCover(tmp =>
        {
            if (tmp == null)
            {
                TipPanel.ShowToast("保存失败");
                callBack?.Invoke();
                return;
            }

            draftInfo.SetCover(tmp);
            draftInfo.UploadAndSave((info, isSuccess) => {
                callBack?.Invoke();
                TipPanel.ShowToast("保存成功:D");
            });
        });

    }

    private void UploadClothesData(Action<bool> uploadCallBack)
    {
        UploadClothesData(null, uploadCallBack);
    }

    private void UploadClothesData(Action saveCallBack, Action<bool> uploadCallBack = null)
    {
        string[] imgs = UGCImportPhotoManager.Inst.GetUrlArr();
        currentClothesInfo.imgs = imgs;
        currentClothesInfo.ugcStyle = (int)curAnimeStyle;
        var draftInfo = SkinAssetManager.Inst.GetOrCreateDraftInfo(currentClothesInfo);
        var clothesData = GetClothesData();
        draftInfo.SetMetaData(JsonConvert.SerializeObject(clothesData));
        var clothesBigTexture = GetBigTexture();
        draftInfo.SetClothesSaveTexture2D(clothesBigTexture);

        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG);//重启编辑时长
        LoggerUtils.Log("###原编辑总时长："+draftInfo.editTime + "  当次编辑时长："+ curEditTime);
        draftInfo.editTime += curEditTime;

        Destroy(clothesBigTexture);
        GetCover(tmp =>
        {
            if (tmp == null)
            {
                TipPanel.ShowToast("保存失败");
                return;
            }
            draftInfo.SetCover(tmp);
            draftInfo.UploadAndSave((info, isSuccess) => {
                uploadCallBack?.Invoke(isSuccess);
            });
            saveCallBack?.Invoke();
        });


    }




    private void GetCover(Action<byte[]> callBack)
    {

        var tmpCamera = ScreenShotCamera;
        var lastRenderTexture = tmpCamera.targetTexture;
        var tmpRenderTexture = new RenderTexture(960, 960, 24, RenderTextureFormat.ARGB32);
        tmpCamera.targetTexture = tmpRenderTexture;
        tmpCamera.Render();
        byte[] pngBytes = null;
        GlobalCoroutineUtils.Inst.WaitForEndOfFrame(() =>
        {
            try
            {
                pngBytes = ScreenShotUtils.TakeShot(tmpCamera, new Rect(0, 0, 960, 960));
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("UgcResourceEditPanel GetCover fail",e);
                callBack?.Invoke(null);
                return;
            }
            tmpCamera.targetTexture = lastRenderTexture;
            Destroy(tmpRenderTexture);
            tmpRenderTexture = null;
            callBack?.Invoke(pngBytes);
        });


    }



    private UGCClothesData GetClothesData()
    {
        var data = new UGCClothesData
        {
            id = currentClothesData.id,
            parts = new List<UGCPartData>()
        };

        foreach (var parts in serverDataDir)
        {
            int partIndex = parts.Key;
            var partData = new UGCPartData
            {
                type = partIndex
            };
            var pixels = new List<UGCPixelData>();
            foreach (var pixel in parts.Value)
            {
                var pixelData = new UGCPixelData
                {
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

    private void OnAdjustClick()
    {
        var editData = new SkinEditData()
        {
            draftInfo = new SkinDraftInfo(currentClothesInfo),
        };
        var adjustPanel = UIManager.Inst.OpenPanel<UGCSkinAdjustPanel>(PanelId.UGCSkinAdjustPanel,
            currentClothesInfo.skinType == (int)SkinType.Avatar?adjustCharacterWrap:adjustPetWrap,editData);
        adjustPanel.SyncAdjustAct = AdjustCharacter;
    }

    private void AdjustCharacter()
    {
        if (IsCurSkinTypeAvatar())
        {
            var classType = UniqueType.GetUgcAvatar((AvatarSubType) currentClothesInfo.subType);
            var detailInfo = currentClothesInfo.skinDetailInfo;
            characterWrap.Scale(classType, detailInfo.sDef);
            characterWrap.Move(classType, detailInfo.pDef);
            characterWrap.Rotate(classType, detailInfo.rDef);
        }
        else
        {
            var classType = UniqueType.GetUGCPetAvatar((AvatarSubType) currentClothesInfo.subType);
            var detailInfo = currentClothesInfo.skinDetailInfo;
            petWrap.Scale(classType, detailInfo.sDef);
            petWrap.Move(classType, detailInfo.pDef);
            petWrap.Rotate(classType, detailInfo.rDef);
        }
    }


    private void OnPublishClicked(CurrencyType currencyType)
    {
        publishBtn.ShowLoading();
        UploadClothesData((Action<bool>)((isSuccess) =>
        {
            publishBtn.HideLoading();
            if (isSuccess)
            {
                var ugcInfo = currentClothesInfo;
                string[] photoImages = ugcInfo.imgs;
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
                var draftInfo = SkinAssetManager.Inst.GetOrCreateDraftInfo(ugcInfo);
                var publishMachine = new UGCPublishStateMachine();
                var stateList = new List<UGCPublishStateBase>();
                if (isFacePart)
                {
                    stateList.Add(new UGCPublishStateBase(IsCurSkinTypeAvatar()?UGCPublishState.SetPropSkinAdjust:UGCPublishState.SetPetSkinAdjust));
                }
                stateList.Add(new UGCSkinDetailState());
                publishMachine.SetStates(stateList);
                publishMachine.SetEditData(new SkinEditData() {
                    draftInfo = draftInfo,
                    currencyType = currencyType
                });
                publishMachine.SetFinishCallBack(() => {
                    GameController.ExitGame(() =>
                    {
                        UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCResourceEditWindow, true);
                        UIManager.Inst.BackToLastWindow();
                        MessageHelper.Broadcast(MessageName.ClothPublishInEditSuccess);
                    },false);

                });
                publishMachine.SetCancelCallBack(() => {
                });
                publishMachine.Start();

            }
            else
            {
                TipPanel.ShowToast("保存草稿失败");
            }
        }));


    }

    #region 衣服模型预览     //TODO:处理模型Vivo花屏问题
    private void InitPreviewHandler()
    {
        var defaultPartData = UgcPartDataManager.Inst.GetUgcPartData(currentClothesData.id, CurrentPartIndex);
        inputHandler = new UgcPreviewInputHandler(currentClothesInfo.skinType);
        var uiCamera = GlobalCameraManager.Inst.UICamera;
        inputHandler.SetClickArea(TouchAreaObj);
        inputHandler.SetDragRect(DragRect);
        inputHandler.SetPreviewCamera(uiCamera);
        inputHandler.SetSimpleClickAction(OnClickModel);
        inputHandler.SetPreviewTarget(ClothesModelParent.gameObject, defaultPartData, false);

        previewInputReceiver.SetHandle(inputHandler);
    }

    private void InitAvatarHandler()
    {
        AvatarCamera.transform.SetParent(null);
        AvatarCamera.transform.localScale = Vector3.one;
        AvatarCamera.transform.localPosition = new Vector3(5000, 0, 0);
    }

    private void InitPreviewLayer()
    {
        ChangeLayer(PreviewCamera.transform,"UI");
        previewRawImage.enabled = false;
    }

    private void ChangeLayer(Transform node,string layerName)
    {
        for (int i = 0; i < node.childCount; i++)
        {
            var childNode = node.GetChild(i);
            childNode.gameObject.layer = LayerMask.NameToLayer(layerName);
            ChangeLayer(childNode,layerName);
        }
    }

    private void OnClickModel(Transform hitTrans)
    {
        //如果在复制模式不可点击模型
        if (copyEditPanel.gameObject.activeSelf)
        {
            return;
        }
        // Debug.Log("hitTrans===" + hitTrans.name);
        var curPartData = UgcPartDataManager.Inst.GetUgcPartData(currentClothesData.id, CurrentPartIndex);
        var curPartName = curPartData.partsName;
        if (!curPartName.Equals(hitTrans.name))
        {
            var pData = UgcPartDataManager.Inst.GetPartDataByPartName(currentClothesData.id, hitTrans.name);
            if (pData!=null)
            {
                SwitchPart(pData.ugcType, false);
            }
        }
    }

    private void ResetCamera()
    {
        inputHandler?.ResetPreview();
    }

    protected void InitClothModel()
    {
        var templateId = currentClothesData.id;
        var templateCfg =IsCurSkinTypeAvatar()? Es.DataTables.GetClothesTemplate(templateId):Es.DataTables.GetPetClothesTemplate(templateId);
        var editConfig =IsCurSkinTypeAvatar()?Es.DataTables.GetUGCClothEditorConfig(templateId):Es.DataTables.GetPetUGCClothEditorConfig(templateId);
        if (templateCfg == null)
        {
            LoggerUtils.LogError($"Can not find template id : {templateId}");
            return;
        }

        var model = Loader.Load<GameObject>(assetsDir + templateCfg.ClothModelPath);
        clothesModel = model.Instantiate(ClothesModelParent);
        clothesModel.name = "UGCModel";
        clothesModel.transform.localPosition = editConfig.DefalutChildPos;

        var modelMesh = Loader.Load<GameObject>(assetsDir +templateCfg.ClothMeshModelPath);
        clothesMeshModel = modelMesh.Instantiate(ClothesModelParent);
        clothesMeshModel.name = "UGCModelMesh";
        clothesMeshModel.transform.localPosition = editConfig.DefalutChildPos;
        clothPartsMesh = clothesMeshModel.GetAllChildren();
    }

    private void SetClothPartMeshShow(int partUgcType)
    {
        if (clothPartsMesh is not {Count: > 0}) return;
        var targetPartData = UgcPartDataManager.Inst.GetUgcPartData(currentClothesData.id, partUgcType);
        var targetPartName = targetPartData.partsName;
        foreach (var partMesh in clothPartsMesh)
        {
            partMesh.SetActive(partMesh.name.Equals(targetPartName + "_mesh"));
        }
    }
    


    #endregion

    #region 人物模型预览

    private void OnPlayerModeChange(bool isOn)
    {
        GameObject playerModeBg = GameObjectEx.FindChildByName(IsCurSkinTypeAvatar()?showModeToggle.gameObject:showPetModeToggle.gameObject, "Background").gameObject;
        playerModeBg.gameObject.SetActive(!isOn);
        isShowAvatar = isOn;
        characterWrap?.Avatar.gameObject.SetActive(isOn);
        petWrap?.Avatar.gameObject.SetActive(isOn);
        clothesModel.gameObject.SetActive(!isOn);
        clothesMeshModel.SetActive(!isOn);
        previewRawImage.enabled = isOn;
        SwitchPreviewTarget(true);
    }

    private void InitCharacterPreview()
    {
        if (IsCurSkinTypeAvatar())
        {
            characterWrap = CreateCharacter();
            adjustCharacterWrap = CreateCharacter();
            adjustCharacterWrap.Avatar.gameObject.SetActive(false);
        }
        else
        {
            petWrap = CreatePet();
            adjustPetWrap = CreatePet();
            adjustPetWrap.Avatar.gameObject.SetActive(false);
        }
    }
    private PetWrap CreatePet()
    {
        var avatarInfo = AccountDataManager.Inst.PetInfo.avatarInfo;
        var wrap = PetAvatarController.Inst.CreateUIAvatar(avatarInfo);
        wrap.SetParent(AvatarModelParent, true);
        var subType = UniqueType.GetUGCPetAvatar((AvatarSubType) currentClothesInfo.subType);
        wrap.ChangeUGCPart(subType, currentClothesData.id,string.Empty,string.Empty);
        var config = DataTables.GetAvatarCommonData(currentClothesData.id);
        if (config != null)
        {
            wrap.Move(subType, config.pDef);
            wrap.Rotate(subType, config.rDef);
            wrap.Scale(subType, config.sDef);
            wrap.HVScale(subType, config.vhSDef);
            // wrap.SetLeftOrRight(subType, config.leftRightType);
        }
        wrap.Avatar.gameObject.SetActive(false);
        var editConfig = Es.DataTables.GetPetUGCClothEditorConfig("avatar_" + currentClothesInfo.templateId);
        wrap.Avatar.transform.localPosition = editConfig.DefalutChildPos;
        return wrap;
    }
    private CharacterWrap CreateCharacter()
    {
        var avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
        var wrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
        wrap.SetParent(AvatarModelParent, true);
        // wrap.TakeOff(UniqueType.GetAvatar((AvatarSubType)currentClothesInfo.subType));
        var subType = UniqueType.GetUgcAvatar((AvatarSubType) currentClothesInfo.subType);
        wrap.ChangeUGCPart(subType, currentClothesData.id,string.Empty,string.Empty);
        var config = DataTables.GetAvatarCommonData(currentClothesData.id);
        if (config != null)
        {
            wrap.Move(subType, config.pDef);
            wrap.Rotate(subType, config.rDef);
            wrap.Scale(subType, config.sDef);
            wrap.HVScale(subType, config.vhSDef);
            wrap.SetLeftOrRight(subType, config.leftRightType);
        }

        wrap.Avatar.gameObject.SetActive(false);
        var editConfig = Es.DataTables.GetUGCClothEditorConfig("avatar_" + currentClothesInfo.templateId);
        wrap.Avatar.transform.localPosition = editConfig.DefalutChildPos;
        return wrap;
    }

    #endregion

    #region 模型接收画笔更新
    public static string GetUGCMeshNodeName(AvatarSubType type,int skinType = 0)
    {
        switch (type)
        {
            case AvatarSubType.Backpack:
                return GameConsts.bagMeshNodeName;
            case AvatarSubType.Hand:
                return GameConsts.handMeshNodeName;
            case AvatarSubType.Shoe:
                return GameConsts.shoeMeshNodeName;
            case AvatarSubType.Hats:
                return GameConsts.hatMeshNodeName;
            case AvatarSubType.Glasses:
                return GameConsts.glassesMeshNodeName;
            case AvatarSubType.Mouth:
                return GameConsts.mouthMeshNodeName;
            case AvatarSubType.FacePaint:
                if (skinType == (int)SkinType.Avatar)
                {
                    return GameConsts.facePaintMeshNodeName;
                }
                else
                {
                    return GameConsts.petFacePaintMeshNodeName;
                }
               
            case AvatarSubType.Tail:
                return GameConsts.tailMeshNodeName;
            case AvatarSubType.Skin:
                return GameConsts.bodyNodeName;
            case AvatarSubType.Clothes:
            default:
                return GameConsts.clothMeshNodeName;
        }
    }
    private void SetPartsMats()
    {
        var clothesModelMaterials = new Dictionary<int, Material>();
        var allPartsData= UgcPartDataManager.Inst.GetUgcPartDataList(currentClothesData.id);
        foreach (var partData in allPartsData)
        {
            //衣服模型
            var clothModelMat = FindPartMat(clothesModel.transform, partData.partsName,false);
            var result = clothesModelMaterials.TryAdd(partData.ugcType, clothModelMat);
            if (!result)
            {
                LoggerUtils.LogError($"Add ugc cloth mat error , ugcType:{partData.ugcType}");
            }
        }
        if (ugcPartTextureDatas is {Count:>0})
        {
            foreach (var kv in ugcPartTextureDatas)
            {
                var partIndex = kv.Key;
                var texData = kv.Value;
                ClothModelOnDraw(partIndex,texData.finalRT, texData.aRT,clothesModelMaterials);
            }
        }
    }
    private void SetAvatarPartsMats(CharacterWrap warp)
    {
        var clothesModelMaterials = new Dictionary<int, Material>();
        var allPartsData = UgcPartDataManager.Inst.GetUgcPartDataList(currentClothesData.id);
        foreach (var partData in allPartsData)
        {
            Transform ugcClothNode = null;
            Material playerModelMat = null;
            if (currentClothesInfo.subType == (int)AvatarSubType.Eyes)
            {
                playerModelMat = FindPartMat(warp.Avatar.transform, partData.partsName,true);
            }
            else if (currentClothesInfo.subType == (int)AvatarSubType.FacePaint)
            {
                playerModelMat = FindPartMat(warp.Avatar.transform, GetUGCMeshNodeName((AvatarSubType)currentClothesInfo.subType),true);
            }
            else
            {
                ugcClothNode =
                    GameObjectEx.FindChildByName(warp.Avatar.transform, GetUGCMeshNodeName((AvatarSubType)currentClothesInfo.subType));
                playerModelMat = FindPartMat(ugcClothNode.transform, partData.partsName,true);
            }
            var result = clothesModelMaterials.TryAdd(partData.ugcType, playerModelMat);
            if (!result)
            {
                LoggerUtils.LogError($"Add ugc cloth mat error , ugcType:{partData.ugcType}");
            }
        }
        if (ugcPartTextureDatas is {Count:>0})
        {
            foreach (var kv in ugcPartTextureDatas)
            {
                var partIndex = kv.Key;
                var texData = kv.Value;
                ClothModelOnDraw(partIndex,texData.finalRT, texData.aRT,clothesModelMaterials);
            }
        }
    }
    
   
    
    private void SetPetPartsMats(PetWrap warp)
    {
        var clothesModelMaterials = new Dictionary<int, Material>();
        var allPartsData = UgcPartDataManager.Inst.GetUgcPartDataList(currentClothesData.id);
        foreach (var partData in allPartsData)
        {
            Transform ugcClothNode = null;
            Material playerModelMat = null;
            if (currentClothesInfo.subType == (int)AvatarSubType.Eyes)
            {
                playerModelMat = FindPartMat(warp.Avatar.transform, partData.partsName,true);
            }
            else if (currentClothesInfo.subType == (int)AvatarSubType.FacePaint)
            {
                playerModelMat = FindPartMat(warp.Avatar.transform, GetUGCMeshNodeName((AvatarSubType)currentClothesInfo.subType,currentClothesInfo.skinType),true);
            }
            else
            {
                ugcClothNode =
                    GameObjectEx.FindChildByName(warp.Avatar.transform, GetUGCMeshNodeName((AvatarSubType)currentClothesInfo.subType,currentClothesInfo.skinType));
                playerModelMat = FindPartMat(ugcClothNode.transform, partData.partsName,true);
            }
            var result = clothesModelMaterials.TryAdd(partData.ugcType, playerModelMat);
            if (!result)
            {
                LoggerUtils.LogError($"Add ugc cloth mat error , ugcType:{partData.ugcType}");
            }
        }
        if (ugcPartTextureDatas is {Count:>0})
        {
            foreach (var kv in ugcPartTextureDatas)
            {
                var partIndex = kv.Key;
                var texData = kv.Value;
                ClothModelOnDraw(partIndex,texData.finalRT, texData.aRT,clothesModelMaterials);
            }
        }
    }
    private void ClothModelOnDraw(int partIndex,RenderTexture finalRt, RenderTexture filterAlphaRt,Dictionary<int, Material> clothesModelMaterials)
    {
        if (clothesModelMaterials is not { Count: > 0 }) return;
        var clothesPartMats = clothesModelMaterials[partIndex];
        if (clothesPartMats.shader.name.Equals("Universal Render Pipeline/Lit"))
        {
            clothesPartMats.SetTexture("_BaseMap", finalRt);
        }
        else if (clothesPartMats.shader.name.Equals("bud/patterns_ugc_ARI"))
        {
            clothesPartMats.SetTexture("_patterns_tex", finalRt);
        }
        else
        {
            clothesPartMats.SetTexture("_MainTex", finalRt);
            clothesPartMats.SetTexture("_opacity_texmask", filterAlphaRt);
        }
    }

    private void InitModelPartsMats()
    {
        SetPartsMats();
        
        if (IsCurSkinTypeAvatar())
        {
            SetAvatarPartsMats(characterWrap);
            SetAvatarPartsMats(adjustCharacterWrap);
            if (!isFacePart)
            {
                GetAvatarOriginalMaterials(characterWrap.Avatar.transform, adjustCharacterWrap.Avatar.transform);
            }
        }
        else
        {
            SetPetPartsMats(petWrap);
            SetPetPartsMats(adjustPetWrap);
            if (!isFacePart)
            {
                GetAvatarOriginalMaterials(petWrap.Avatar.transform, adjustPetWrap.Avatar.transform);
            }
        }
    }

    private void SetPartMat(Transform modelTrans,Transform avatarClothesTrans, string partPath)
    {
        var part = GameObjectEx.FindChildByName(modelTrans.transform, partPath);
        var avatarPart = GameObjectEx.FindChildByName(avatarClothesTrans.transform, partPath);
        part.gameObject.SetActive(true);
        SkinnedMeshRenderer sr = part.GetComponent<SkinnedMeshRenderer>();
        SkinnedMeshRenderer avatarSr = avatarPart.GetComponent<SkinnedMeshRenderer>();
        if (sr!=null)
        {
            avatarSr.material = sr.material;
        }
        else
        {
            MeshRenderer mr =  part.GetComponent<MeshRenderer>();
            MeshRenderer avatarMr =  part.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                avatarMr.material = mr.material;
            }
        }
    }

    private Material FindPartMat(Transform modelTrans, string partPath,bool isAvatar)
    {
        var part = GameObjectEx.FindChildByName(modelTrans.transform, partPath);
        part.gameObject.SetActive(true);
        Material mat = null;
        SkinnedMeshRenderer sr = part.GetComponent<SkinnedMeshRenderer>();
        if (sr!=null)
        {
            mat = sr.material;
        }
        else
        {
            MeshRenderer mr =  part.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mat = part.GetComponent<MeshRenderer>().material;
            }
        }
        //动态打开MeshCollider
        var meshCollider = part.GetComponentInChildren<MeshCollider>();
        if (meshCollider)
        {
            meshCollider.enabled = true;
        }

        return mat;
    }



    #endregion

    private void InitCameraShotModel()
    {
        if (clothesModel)
        {
            var config = IsCurSkinTypeAvatar()?DataTables.GetUGCClothEditorConfig(currentClothesInfo.templateId):DataTables.GetPetUGCClothEditorConfig(currentClothesInfo.templateId);
            shotModel = GameObject.Instantiate(clothesModel, screenShotNode);
            shotModel.transform.localScale = Vector3.one;
            shotModel.transform.localPosition = Vector3.zero;
            ScreenShotCamera.transform.localPosition = config.ShotCameraPos;
            ScreenShotCamera.transform.localEulerAngles = config.ShotCameraRot;
        }
    }

    private bool IsCurSkinTypeAvatar()
    {
        return currentClothesInfo.skinType == (int)SkinType.Avatar;
    }
    #region 切换部件

    public void OnSwitchPartBtnClick()
    {
        SwitchPart();
    }

    public override void SwitchPart(int partIndex = 0, bool isSwitchCamera = true)
    {
        base.SwitchPart(partIndex, isSwitchCamera);
        var newPartUgcType = 0;
        if (partIndex == 0)
        {
            var allPartsData = UgcPartDataManager.Inst.GetUgcPartDataList(currentClothesData.id);
            newPartUgcType = GetNextUgcType(CurrentPartIndex, allPartsData);
            LoggerUtils.Log($"SwitchPart==== CurPart:{CurrentPartIndex}, newPartUgc:{newPartUgcType}");
        }
        else
        {
            newPartUgcType = partIndex;
        }

        TransformInteractorController.Inst.InterActor.ResetInfo();
        UGCImportTextManager.Inst.CurrentSelectBehaviour = null;
        UGCImportPhotoManager.Inst.CurrentSelectBehaviour = null;

        CurrentPartIndex = newPartUgcType;
        ShowSmallGenerateTexture(CurrentPartIndex);
        //右侧切换
        OnSelectHandle(CurrentPartIndex);
        //左侧切换
        SwitchPreviewTarget(!isShowAvatar);
        //高亮网格显示
        SetClothPartMeshShow(CurrentPartIndex);
    }

    private void SwitchPreviewTarget(bool isSwitchCamera)
    {
        var curPartData = UgcPartDataManager.Inst.GetUgcPartData(currentClothesData.id, CurrentPartIndex);
        var targetNode = isShowAvatar ? AvatarModelParent : ClothesModelParent;
        inputHandler?.SetPreviewTarget(targetNode.gameObject, curPartData, isShowAvatar, isSwitchCamera);
    }

    private int GetNextUgcType(int ugcType, List<UgcPartData> data)
    {
        for (int i = 0; i < data.Count; i++)
        {
            var d = data[i];
            var nextIndex = i + 1;
            if (d.ugcType == ugcType && nextIndex < data.Count)
            {
                return data[nextIndex].ugcType;
            }
        }

        return defaultUGCType;
    }

    #endregion

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 计算总时长（分钟）
        float totalTime = Time.time - startTime;
        float totalMinutes = totalTime / 60f;
        LoadEvent.ReportTask(154, (int)totalMinutes);
        
        if (AvatarCamera != null)
        {
            Destroy(AvatarCamera);
            AvatarCamera = null;
        }
    }
    protected override UGCClothDrawUndoData CreateUndoData(Dictionary<Vector2Int, Color> gridList)
    {
        UGCClothDrawUndoData data = new UGCClothDrawUndoData();
        Dictionary<Vector2Int, Color> pairs = new Dictionary<Vector2Int, Color>();
        foreach (var item in gridList)
        {
            pairs.Add(item.Key, item.Value);
        }
        data.mapCanvas = mapCanvas;
        data.drawGridPairs = pairs;
        data.changePartAction = OnUndoChangePart;
        data.partId = CurrentPartIndex;
        return data;
    }

    public void OnUndoChangePart(int partIndex)
    {
        if (partIndex != CurrentPartIndex)
        {
            SwitchPart(partIndex);
            // StartUGCTween(ugcDatas[curClothesIndex].rotAngle,ugcDatas[curClothesIndex].rotAxie);
        }
    }


    #region 衣服大赛相关逻辑

    public void setContestInfo(ContestInfo info)
    {
        SetContestUI(info);
    }

    private void SetContestUI(ContestInfo info)
    {
        var bgParent = GameObjectEx.FindChildByName(transform, "BG");
        ContestEventManager.Inst.SetCustomBg(bgParent, info.backgroundIconUrlList, info.backgroundColor);
    }

    #endregion

    #region 外部调用功能
    
    /// <summary>
    /// 外部调用功能 - 强制切换到油漆桶工具
    /// </summary>
    public void ForceSwitchToBucketTool()
    {
        // 检查编辑器工具是否存在
        if (editorTool == null)
        {
            Debug.LogError("[UGCResourceEditPanel] 编辑器工具未初始化");
            return;
        }
        
        // 强制切换到油漆桶工具
        editorTool.SwitchToBucketTool();
        
        Debug.Log("[UGCResourceEditPanel] 已强制切换到油漆桶工具");
    }
    
    #endregion
}
