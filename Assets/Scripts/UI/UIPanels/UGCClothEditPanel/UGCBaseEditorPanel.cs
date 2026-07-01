using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Basic.UndoRedo;
using Game.Audio;
using Game.Base;
using Game.UGCEditor;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using RTG;
using UnityEngine.UI;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UndoSystem;
using UnityEngine;
using Utils.Extensions;

public class UGCBaseEditorPanel<T>: BasePanel<T> where T : BasePanel<T>
{
    [Header("左下角")] public CButton undoBtn;
    public CButton redoBtn;
    public Image UndoIcon;
    public Image RedoIcon;

    [Header("工具栏")] public Toggle colorToggle;

    [Header("颜色选择")] public ClothesColorPickerView colorPickerView;
    public ColorPinkerPanel hsvPickerView;
    
    public DynamicDrawCanvas PartMirrorDrawCanvas;
    public RectTransform DrawBoard;
    public RawImage GridMapClothesMask;
    public GridMapCanvas mapCanvas;
    public Camera ScreenShotCamera;
    [Header("风格化")] 
    public MISource StyleSource;
    protected Material StyleMaterial;

    protected UGCEditorTool editorTool;
    protected DynamicDrawCanvas drawCanvas;
    
    [NonSerialized]
    public int CurrentPartIndex = 0;
    protected const string pngExt = ".png";

    private Transform importCanvasParent;
    private GameObject transformPanel;
    private Slider zoomSlider;
    private ZoomScrollBar zoomScrollBar;
    private UGCEditorInputReceiver zoomInputReceiver;
    private UGCCanvasZoomHandler zoomHandler;
    private Transform zoomPanel;

  
    private Transform trasformMask;
    private RawImage frameMask;
    private Transform mapElementParent;
    private RawImage ugcImagePrefab;
    private Text ugcTextPrefab;

    protected Transform screenShotNode;
    // private UGCPhotoSelectPanel selectPhotoPanel;
    private Material mirrorMat;
    
    protected Dictionary<int, UGCPartTextures> ugcPartTextureDatas;
    protected MainUGCResCopyPanel copyEditPanel;
    protected UGCResData currentClothesData;
    protected CanvasType _canvasType;
    
    private const string dynamicPath = "Assets/Loadable/UI/UIPanel/UGCResourceEditPanel/UGCClothesMask/";
    private int undoId = 0;
    
    private AmbientLightSetting _srcLightSetting = new AmbientLightSetting();
    private bool _srcHallLightVisible;
    private bool _srcGameSceneLightVisible;
    private bool _srcPreviewSceneLightVisible;
    
    protected string mainTexName = "_BaseMap";
    protected string alphaTexName = "_opacity_texmask";

    protected UgcShaderStyle curAnimeStyle = UgcShaderStyle.Normal;
    private List<Material> orgMaterials = new List<Material>();
    private List<Material> animeStyleMaterials = new List<Material>();
    private List<Renderer> targetModelRenderers = new List<Renderer>();
    private  List<Renderer> shotModelRenderers = new List<Renderer>();
    protected const string removeRendererName = "ugchat_2004_3";
    public override void OnCreate()
    {
        base.OnCreate();
        //TODO:不需要使用RTG
        var rtg = RTGApp.Get;
        if (rtg != null)
        {
            rtg.gameObject.SetActive(false);
        }
        UIManager.Inst.ForceSetOtherWindowTransInStack(WindowId.UGCResourceEditWindow, false);
        
        colorPickerView.AddListener(OnColorPickerSelected);
        hsvPickerView.Init();
        hsvPickerView.SetElementColor = OnHsvPickerSelected;
        colorToggle.onValueChanged.AddListener(OnColorToggleChange);
        
        frameMask = this.transform.Find("Panel/DrawingView/FramesMask").GetComponent<RawImage>();
        mapElementParent = mapCanvas.transform.Find("ZoomMask/ZoomPanel/RawImage/ElementPanel/Recovery");
        ugcTextPrefab = mapElementParent.Find("UGCText").GetComponent<Text>();
        ugcImagePrefab = mapElementParent.Find("UGCImage").GetComponent<RawImage>();
        // selectPhotoPanel = transform.Find("SelectPhoto").GetComponent<UGCPhotoSelectPanel>();
        transformPanel = this.transform.Find("Panel/TransformPanel").gameObject;
        trasformMask = transformPanel.transform.Find("ReSizeNode/TrasformMask");
        zoomPanel = DrawBoard.transform.Find("ZoomPanel").GetComponent<Transform>();
        copyEditPanel = this.transform.Find("Panel/CopyEditPanel").GetComponent<MainUGCResCopyPanel>();
        var backBtn = transformPanel.transform.Find("BackButton").GetComponent<Button>();
        screenShotNode = ScreenShotCamera.transform.parent;
        TransformInteractorController.Inst.InitializeInteractor(zoomPanel, UpdateUndoBtnView);
        mirrorMat = Loader.Load<Material>("Assets/Loadable/UI/UIPanel/UGCResourceEditPanel/Shaders/mirrorBG.mat", this.gameObject);
        
        importCanvasParent = new GameObject("ImportCanvas").transform;
        importCanvasParent.localPosition = new Vector3(300, 0, 0);
        UGCImportPhotoManager.Create();
        UGCImportTextManager.Create();
        AddInputHandler();
        AddImportHandler();
        CreatePartMirrorDrawCanvas();
        
        undoBtn.onClick.AddListener(OnUndoBtnClick);
        redoBtn.onClick.AddListener(OnRedoBtnClick);
        backBtn.onClick.AddListener(OnBackBtnClick);
        SetLight(true);

        if (StyleSource != null)
            StyleSource.SetCallback(SetUgcStyle);
    }

    protected virtual void SetUgcStyle(MISource.Source source)
    {
        if (source == MISource.Source.Create)
        {
            GetOrCreateNewAnimTypeMaterials();
        }

        curAnimeStyle = source == MISource.Source.Bud ? UgcShaderStyle.Normal : UgcShaderStyle.Anime;
        for (var i = 0; i < targetModelRenderers.Count; i++)
        {
            var curMat= curAnimeStyle == UgcShaderStyle.Normal ? orgMaterials[i] : animeStyleMaterials[i];
            targetModelRenderers[i].material = curMat;
            shotModelRenderers[i].material = curMat;
        }
    }


    protected void GetOriginalMaterials(GameObject model,GameObject shotModel)
    {
        orgMaterials.Clear();
        var tRenderers = model.GetComponentsInChildren<Renderer>(true);
        var sRenderers = shotModel.GetComponentsInChildren<Renderer>(true);
        if (tRenderers != null)
        {
            targetModelRenderers = tRenderers.ToList();
            targetModelRenderers.RemoveAll(x => x.name.Contains(removeRendererName));
            for (var i = 0; i < targetModelRenderers.Count; i++)
            {
                orgMaterials.Add(tRenderers[i].material);
            }
        }

        if (sRenderers != null)
        {
            shotModelRenderers = sRenderers.ToList();
            shotModelRenderers.RemoveAll(x => x.name.Contains(removeRendererName));
        }
    }

    private void GetOrCreateNewAnimTypeMaterials()
    {
        if (animeStyleMaterials.Count == 0)
        {
            var count = orgMaterials.Count;
            for (int i = 0; i < count; i++)
            {
                var newMat = new Material(StyleMaterial);
                newMat.SetTexture(mainTexName,orgMaterials[i].mainTexture);
                if (orgMaterials[i].HasTexture(alphaTexName))
                {
                    var alphaTex = orgMaterials[i].GetTexture(alphaTexName);
                    newMat.SetTexture(alphaTexName, alphaTex);
                }
                animeStyleMaterials.Add(newMat);
            }
        }
    }


    /// <summary>
    /// 提供各部件画布克隆，确保最终效果表现
    /// </summary>
    /// <param name="index"></param>
    /// <param name="rt"></param>
    /// <returns></returns>
    private void CreatePartMirrorDrawCanvas()
    {
        drawCanvas = Instantiate(PartMirrorDrawCanvas, importCanvasParent);
        drawCanvas.transform.localPosition = new Vector3(30, 0, 0);
        var UICanvas = UIManager.Inst.Canvas.GetComponent<CanvasScaler>();
        drawCanvas.InitCanvas(DrawBoard, UICanvas,mapCanvas.FinalTextureSize);
    }
    
    protected void SetFinalTargetTexture(RenderTexture rt)
    {
        drawCanvas.SetCameraTargetTexture(rt);
    }
    
    public Dictionary<int, Dictionary<Vector2Int, Color>> GetClothesPartDicByID(UGCResData data)
    {
        var tempDic = new Dictionary<int, Dictionary<Vector2Int, Color>>();
        for (int i = 0; i < data.parts.Count; i++)
        {
            Dictionary<Vector2Int, Color> vColors = new Dictionary<Vector2Int, Color>();
            //如果模版信息是32*32的但是画板为64，则进行一次转换
            bool is32Canvas = data.parts[i].pixels.Count == mapCanvas.GetCanvasTypeCount(CanvasType.Canvas_32)* mapCanvas.GetCanvasTypeCount(CanvasType.Canvas_32);
            bool isNeedTrans32To64 = is32Canvas && _canvasType == CanvasType.Canvas_64;
            for (int j = 0; j < data.parts[i].pixels.Count; j++)
            {
                var pos = DataUtil.DeSerializeVector2Int(data.parts[i].pixels[j].p);
                var col = DataUtil.DeSerializeColor(data.parts[i].pixels[j].col);
                if (!isNeedTrans32To64)
                { 
                    vColors.Add(pos,col);
                }
                else
                {
                    vColors.Add(new Vector2Int(pos.x*2, pos.y*2),col);
                    vColors.Add(new Vector2Int(pos.x*2, pos.y*2+1),col);
                    vColors.Add(new Vector2Int(pos.x*2+1, pos.y*2+1),col);
                    vColors.Add(new Vector2Int(pos.x*2+1, pos.y*2),col);
                }
            }
            tempDic.Add(data.parts[i].type, vColors);
        }

        return tempDic;
    }
    
    #region 画布放大缩小操作

    private void AddInputHandler()
    {
        zoomInputReceiver = new GameObject("zoomInputReceiver").AddComponent<UGCEditorInputReceiver>();
        zoomSlider = this.transform.Find("Panel/DrawingView/ZoomBar/ZoomSlider").GetComponent<Slider>();
        zoomSlider.onValueChanged.AddListener(OnZoomValueChange);

        zoomScrollBar = this.transform.Find("Panel/DrawingView/DrawScrollBar").GetComponent<ZoomScrollBar>();
        zoomHandler = new UGCCanvasZoomHandler();
        zoomHandler.SetData(UIManager.Inst.Canvas, zoomPanel.gameObject);
        zoomHandler.AddZoomScaleListener(OnZoomHandlerValueChange);
        zoomHandler.AddZoomMoveListener(OnZoomHandlerMoveChange);
        zoomInputReceiver.SetHandle(zoomHandler);
    }

    //根据当前缩放值调整slider
    private void OnZoomHandlerValueChange(float value)
    {
        zoomSlider.SetValueWithoutNotify(value);
        zoomScrollBar.OnContentResize();
    }

    private void OnZoomHandlerMoveChange()
    {
        zoomScrollBar.OnContentMove();
    }

    //主动拖动zoom slider
    private void OnZoomValueChange(float value)
    {
        LoggerUtils.Log("###l OnZoomValueChange:" + value);
        zoomHandler.SetZoomScale(value);
        zoomScrollBar.OnContentResize();
    }

    #endregion
    
    
    #region ColorPick

    protected virtual void OnColorToggleChange(bool isSelected)
    {
        hsvPickerView.gameObject.SetActive(isSelected);
        if (isSelected)
        {
            hsvPickerView.SetSelectColor(colorPickerView.GetSelectColor());
        }
    }

    private void OnColorPickerSelected(Color color)
    {
        colorToggle.isOn = false;
        SetTextColor(color);
    }
    
    public void SetTextColor(Color col)
    {
        if (UGCPaintToolSettings.Current.ToolType == EditorToolType.Text)
        {
            UGCImportTextManager.Inst.SetColor(col);
        }
    }

    private void OnHsvPickerSelected(Color color)
    {
        colorPickerView.SetSelectColor(color);
        SetTextColor(color);
    }

    #endregion
    
    
    #region Photo操作

    private void AddImportHandler()
    {
        var toolNode = this.transform.Find("Panel/DrawTool");
        editorTool = toolNode.GetComponent<UGCEditorTool>();
        editorTool.OnInit();
        editorTool.OnFrameChange = OnFrameChange;
        editorTool.OnToolChange = OnEditorToolChange;
        editorTool.OnCopyAct = OnCopyBtnClick;
    }

    private void OnEditorToolChange()
    {
        UGCImportTextManager.Inst.SetRaycastTarget(false);
        UGCImportPhotoManager.Inst.SetRaycastTarget(false);
        switch (UGCPaintToolSettings.Current.ToolType)
        {
            case EditorToolType.Photo:
                editorTool.OnSelectPhoto = OnSelectPhoto;
                editorTool.OnCopyPhoto = OnCopyPhoto;
                editorTool.OnAddNew = () => CreateNewPhoto();
                editorTool.OnDeletePhoto = OnDesPhoto;
                editorTool.OnTransformPhoto = OnTransformPhoto;
                editorTool.OnReplacePhoto = OnReplacePhoto;
                UGCImportTextManager.Inst.SetRaycastTarget(true);
                UGCImportPhotoManager.Inst.SetRaycastTarget(true);
                break;
            case EditorToolType.Text:
                colorPickerView.SetSelectColor(Color.black);
                editorTool.OnSelectText = OnSelectText;
                editorTool.OnAddNew = () => CreateNewText();
                UGCImportTextManager.Inst.SetRaycastTarget(true);
                UGCImportPhotoManager.Inst.SetRaycastTarget(true);
                break;
        }
    }
    //画布外框切换
    public void OnFrameChange()
    {
        GridMapClothesMask.material = null;
        var toolType = UGCPaintToolSettings.Current.ToolType;
        var drawMode = UGCPaintToolSettings.Current.PaintingMode;
        string maskName = string.Empty;
        if (toolType == EditorToolType.Painting && drawMode == UGCDrawMode.Mirror)
        {
            maskName = "mirror_mask";
            GridMapClothesMask.material = mirrorMat;
        }
        else if (toolType == EditorToolType.Scissors && drawMode == UGCDrawMode.Mirror)
        {
            maskName = "scissors_mirror_mask";
            GridMapClothesMask.material = mirrorMat;
        }
        else if (toolType == EditorToolType.Scissors && drawMode == UGCDrawMode.Normal)
        {
            maskName = "scissors_mask";
        }

        if (string.IsNullOrEmpty(maskName))
        {
            frameMask.gameObject.SetActive(false);
        }
        else
        {
            frameMask.gameObject.SetActive(true);
            var tempTex = Loader.Load<Texture>(dynamicPath + maskName + pngExt, this.gameObject);
            frameMask.texture = tempTex;
        }
    }
    public void OnSelectPhoto()
    {
        var behaviour = UGCImportPhotoManager.Inst.GetDefaultElement(CurrentPartIndex);
        if (behaviour == null)
        {
            behaviour = CreateNewPhoto();
        }

        // CreateNewPhoto 可能因权限不足返回 null（如子类做了 VIP 拦截）
        if (behaviour == null)
            return;

        UGCImportPhotoManager.Inst.CurrentSelectBehaviour = behaviour;
        TransformInteractorController.Inst.InterActor.Show(behaviour.rectTrans, behaviour.OnTransformChange,
            behaviour.Init);
    }

    public virtual UGCPhotoBehaviour CreateNewPhoto()
    {
        var behaviour = UGCImportPhotoManager.Inst.CreatEmptyElement(CurrentPartIndex, mapElementParent,
            drawCanvas.mDrawPanel, ugcImagePrefab.gameObject);
        if (behaviour != null)
        {
            BindPhotoAttr(behaviour);
        }

        return behaviour;
    }

    public void OnCopyPhoto()
    {
        var selectBehaviour = UGCImportPhotoManager.Inst.CurrentSelectBehaviour as UGCPhotoBehaviour;
        if (selectBehaviour != null)
        {
            selectBehaviour.data.pos = selectBehaviour.GetOffetPosition();
            var cloneBehaviour = CreatePhotoByData(CurrentPartIndex, selectBehaviour.data.Clone(), drawCanvas.mDrawPanel);
            UGCImportPhotoManager.Inst.CurrentSelectBehaviour = cloneBehaviour;
        }
    }

    protected UGCPhotoBehaviour CreatePhotoByData(int partType, PhotoData data, Transform parent)
    {
        var behaviour = UGCImportPhotoManager.Inst.CreatElement<UGCPhotoBehaviour,PhotoData>(partType, data, mapElementParent, parent, ugcImagePrefab.gameObject);
        if (behaviour != null)
        {
            BindPhotoAttr(behaviour);
        }
        return behaviour;
    }
    
    protected void GenerateImportUGCClothes(int partIndex ,UGCPartData data, Transform parent)
    {
        var tList = data.texts;
        if (tList != null)
        {
            foreach (var t in tList)
            {
                CreateTextByData(partIndex, t, parent);
            }
        }

        var pList = data.photos;
        if (pList != null)
        {
            foreach (var p in pList)
            {
                CreatePhotoByData(partIndex, p, parent);
            }
        }
    }

    private void BindPhotoAttr(UGCPhotoBehaviour behaviour)
    {
        behaviour.OnSelectAction = OpenPhotoPanel;
        behaviour.OnCopyAction = OnCopyPhoto;
        behaviour.OnDestoryBehaviour = OnDesPhoto;
        UGCImportPhotoManager.Inst.AddCreateRecord(CurrentPartIndex,behaviour.gameObject);
        UpdateUndoBtnView();
        UGCImportPhotoManager.Inst.CurrentSelectBehaviour = behaviour;
        TransformInteractorController.Inst.InterActor.Show(behaviour.rectTrans, behaviour.OnTransformChange,
            behaviour.Init);
    }
    protected void HierarchicalSort()
    {
        List<ElementBaseBehaviour> gather = new List<ElementBaseBehaviour>();
        
        gather.AddRange(UGCImportPhotoManager.Inst.GetElementList());
        gather.AddRange(UGCImportTextManager.Inst.GetElementList());
        gather.Sort(SortCompare);
        for (int i = 0; i < gather.Count; i++)
        {
            gather[i].rectTrans.SetSiblingIndex(i);
            gather[i].SetMirSiblingIndex();
        }
    }
    protected void SetHierarchicalSort()
    {
        var elementList = mapElementParent.GetComponentsInChildren<ElementBaseBehaviour>();
        if (elementList!=null)
        {
            for (int i = 0; i < elementList.Length; i++)
            {
                elementList[i].hierarchy = i;
            }
        }
    }
    private static int SortCompare(ElementBaseBehaviour info1, ElementBaseBehaviour info2)
    {
        return info1.hierarchy.CompareTo(info2.hierarchy);
    }
    private void OnDesPhoto()
    {
        var selectBehaviour = UGCImportPhotoManager.Inst.CurrentSelectBehaviour;
        if (selectBehaviour == null)
        {
            return;
        }
        UGCPhotoBehaviour behaviour = selectBehaviour as UGCPhotoBehaviour;
        behaviour.loader.gameObject.SetActive(false);

        GamePropNodeManager.Inst.DestroyNodeToSecondCache(behaviour.gameObject);
        TransformInteractorController.Inst.InterActor.AddDestroyRecord(behaviour.gameObject);
        UpdateUndoBtnView();

        if (behaviour.photoDynamicMir)
        {
            GamePropNodeManager.Inst.DestroyNodeToSecondCache(behaviour.photoDynamicMir.gameObject);
        }

        UGCImportPhotoManager.Inst.RemoveElement(CurrentPartIndex, behaviour);
        var compCount = UGCImportPhotoManager.Inst.GetCompCountByPartIndex(CurrentPartIndex);
        if (compCount == 0)
        {
            editorTool.PaintToggle.isOn = true;
        }
        UGCImportPhotoManager.Inst.CurrentSelectBehaviour = null;
        TransformInteractorController.Inst.InterActor.Hide();
    }

    private void OnTransformPhoto()
    {
        if (TransformInteractorController.Inst.InterActor.targetGameObject == null)
        {
            UGCPhotoBehaviour newBehav = UGCImportPhotoManager.Inst.GetLastElement(CurrentPartIndex) as UGCPhotoBehaviour;
            if (newBehav == null) return;
            UGCImportPhotoManager.Inst.CurrentSelectBehaviour = newBehav;
            TransformInteractorController.Inst.InterActor.Show(newBehav.rectTrans, newBehav.OnTransformChange,
                newBehav.Init);
        }

        TransformInteractorController.Inst.InterActor.InteMode = InteratorMode.Transform;
        transformPanel.gameObject.SetActive(true);
        zoomSlider.SetValueWithoutNotify(0);
        OnZoomValueChange(1f);

        zoomPanel.transform.SetParent(trasformMask);
        zoomPanel.localPosition = Vector3.zero;
        zoomPanel.localScale = Vector3.one;
        TransformInteractorController.Inst.InterActor.transform.SetParent(trasformMask.parent);
        TransformInteractorController.Inst.InterActor.ShowDetailTransform();
    }

    private void OnReplacePhoto()
    {
        UGCPhotoBehaviour behaviour = UGCImportPhotoManager.Inst.CurrentSelectBehaviour as UGCPhotoBehaviour;
        OpenPhotoPanel(behaviour);
    }

    private void OnBackBtnClick()
    {
        transformPanel.gameObject.SetActive(false);
        TransformInteractorController.Inst.InterActor.transform.SetParent(zoomPanel);
        zoomPanel.transform.SetParent(DrawBoard);
        zoomPanel.localPosition = Vector3.zero;
        zoomPanel.localScale = Vector3.one;
        TransformInteractorController.Inst.InterActor.InteMode = InteratorMode.Normal;
        TransformInteractorController.Inst.InterActor.HideDetailTransform();
    }

    private void OpenPhotoPanel(UGCPhotoBehaviour behaviour)
    {
        if (behaviour != null)
        {
            behaviour.OpenPhotoPanel();
        }

        // selectPhotoPanel.gameObject.SetActive(true);
        // selectPhotoPanel.ShowPanel(behaviour);
        //
    }

    public void OnSelectText()
    {
        var behaviour = UGCImportTextManager.Inst.GetDefaultElement(CurrentPartIndex);
        if (behaviour == null)
        {
            behaviour = CreateNewText();
        }
        UGCImportTextManager.Inst.CurrentSelectBehaviour = behaviour;
        TransformInteractorController.Inst.InterActor.Show(behaviour.rectTrans, behaviour.OnTransformChange,
            behaviour.Init);
    }

    public UGCTextBehaviour CreateNewText()
    {
        var behaviour = UGCImportTextManager.Inst.CreatEmptyElement(CurrentPartIndex, mapElementParent,
            drawCanvas.mDrawPanel, ugcTextPrefab.gameObject);
        if (behaviour != null)
        {
            BindTextAttr(behaviour);
        }

        return behaviour;
    }

    public void OnCopyText()
    {
        var selectBehaviour = UGCImportTextManager.Inst.CurrentSelectBehaviour as UGCTextBehaviour;
        if (selectBehaviour != null)
        {
            selectBehaviour.data.pos = selectBehaviour.GetOffetPosition();
            var cloneBehaviour = CreateTextByData(CurrentPartIndex, selectBehaviour.data.Clone(),
                drawCanvas.mDrawPanel);
            UGCImportTextManager.Inst.CurrentSelectBehaviour = cloneBehaviour;
        }
    }

    protected UGCTextBehaviour CreateTextByData(int partType, TextData data, Transform parent)
    {
        var behaviour = UGCImportTextManager.Inst.CreatElement<UGCTextBehaviour,TextData>(partType, data, mapElementParent, parent, ugcTextPrefab.gameObject);
        if (behaviour != null)
        {
            BindTextAttr(behaviour);
        }
        return behaviour;
    }

   


    private void BindTextAttr(UGCTextBehaviour behaviour)
    {
        behaviour.OnCopyAction = OnCopyText;
        behaviour.OnDestoryBehaviour = OnDesText;
        UGCImportTextManager.Inst.AddCreateRecord(CurrentPartIndex,behaviour.gameObject);
        UpdateUndoBtnView();
        UGCImportTextManager.Inst.CurrentSelectBehaviour = behaviour;
        TransformInteractorController.Inst.InterActor.Show(behaviour.rectTrans, behaviour.OnTransformChange,
            behaviour.Init);
    }

    private void OnDesText()
    {
        var selectBehaviour = UGCImportTextManager.Inst.CurrentSelectBehaviour;
        if (selectBehaviour == null)
        {
            return;
        }
        UGCTextBehaviour behaviour = selectBehaviour as UGCTextBehaviour;
        GamePropNodeManager.Inst.DestroyNodeToSecondCache(behaviour.gameObject);
        TransformInteractorController.Inst.InterActor.AddDestroyRecord(behaviour.gameObject);
        UpdateUndoBtnView();

        if (behaviour.textDynamicMir)
        {
            GamePropNodeManager.Inst.DestroyNodeToSecondCache(behaviour.textDynamicMir.gameObject);
        }

        UGCImportTextManager.Inst.RemoveElement(CurrentPartIndex, behaviour);
        var compCount = UGCImportTextManager.Inst.GetCompCountByPartIndex(CurrentPartIndex);
        if (compCount == 0)
        {
            editorTool.PaintToggle.isOn = true;
        }
        UGCImportTextManager.Inst.CurrentSelectBehaviour = null;
        TransformInteractorController.Inst.InterActor.Hide();
    }
    #endregion
    
    
    #region 撤销重做

    private void OnUndoBtnClick()
    {
        UndoRedoManager.Inst.Undo();
        UpdateUndoBtnView();
    }
    
    private void OnRedoBtnClick()
    {
        UndoRedoManager.Inst.Redo();
        UpdateUndoBtnView();
    }
    private int GetUndoId()
    {
        undoId += 1;
        return undoId;
    }

    /// <summary>
    /// 笔刷、油漆桶的undo
    /// </summary>
    public void AddRecord(Dictionary<Vector2Int, Color> beforeDrawGridPairs, Dictionary<Vector2Int, Color> afterDrawGridPairs)
    {
        UndoRecord record = new UndoRecord(UndoHelperName.UGCClothDrawUndoHelper);
        UGCClothDrawUndoData beginData = CreateUndoData(beforeDrawGridPairs);
        UGCClothDrawUndoData endData = CreateUndoData(afterDrawGridPairs);
        record.BeginData = beginData;
        record.EndData = endData;
        int undoId = GetUndoId();
        beginData.undoId = undoId;
        endData.undoId = undoId;
        switch (mapCanvas.curMode)
        {
            case UGCDrawMode.Mirror:
                beginData.type = UGCDrawMode.Mirror;
                endData.type = UGCDrawMode.Mirror;
                break;
            case UGCDrawMode.Flip:
                beginData.type = UGCDrawMode.Flip;
                endData.type = UGCDrawMode.Flip;
                break;

            case UGCDrawMode.Copy:
                beginData.type = UGCDrawMode.Copy;
                endData.type = UGCDrawMode.Copy;
                break;
        }
        UndoRecordPool.Inst.PushRecord(record);
        UpdateUndoBtnView();
    }
    protected virtual UGCClothDrawUndoData CreateUndoData(Dictionary<Vector2Int, Color> gridList)
    {
        UGCClothDrawUndoData data = new UGCClothDrawUndoData();
        Dictionary<Vector2Int, Color> pairs = new Dictionary<Vector2Int, Color>();
        foreach (var item in gridList)
        {
            pairs.Add(item.Key, item.Value);
        }
        data.mapCanvas = mapCanvas;
        data.drawGridPairs = pairs;
        return data;
    }

    private void UpdateUndoBtnView()
    {
        LoggerUtils.Log("UpdateUndoBtnView");
        Color EnableColor = Color.white;
        Color UnEnableColor = new Color(1, 1, 1, 0.5f);
        bool hasUndo = (UndoRecordPool.Inst.GetUndoCount() > 0);
        UndoIcon.color = hasUndo ? EnableColor : UnEnableColor;
        bool hasRedo = (UndoRecordPool.Inst.GetRedoCount() > 0);
        RedoIcon.color = hasRedo ? EnableColor : UnEnableColor;
    }

    #endregion

    #region 按鍵音
    public override void OnHidden()
    {
        base.OnHidden();
    }

    public override void OnWindowBeFocused()
    {
        base.OnWindowBeFocused();
    }

    #endregion

    #region 数据保存

    protected virtual Tuple<int[], byte[][], byte[][]> GetPartTextures()
    {
        var ugcTypes = ugcPartTextureDatas.Keys;
        var finalRenderTextures = ugcPartTextureDatas.Values.Select(tmp => ScreenShotUtils.TakeRenderTexture(tmp.finalRT) );
        var alphaRenderTextures = ugcPartTextureDatas.Values.Select(tmp => ScreenShotUtils.TakeRenderTexture(tmp.aRT));
        return new Tuple<int[], byte[][], byte[][]>(ugcTypes.ToArray(), finalRenderTextures.ToArray(), alphaRenderTextures.ToArray());
    }
    protected virtual Texture2D GetBigTexture()
    { 
        List<Texture2D> texture2Ds = new List<Texture2D>();
        var ugcTypes = ugcPartTextureDatas.Keys;
        foreach (var items in ugcTypes)
        {
            Texture2D texture = GenTexturePNG(ugcPartTextureDatas[items].finalRT);
            texture2Ds.Add(texture);
            Texture2D alphaTexture = GenTexturePNG(ugcPartTextureDatas[items].aRT);
            texture2Ds.Add(alphaTexture);
        }
        var mergeTexture2D = MergeTexture2D(texture2Ds);
        return mergeTexture2D;
    }


    #endregion

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SetLight(false);
        if (importCanvasParent != null)
        {
            GameObject.Destroy(importCanvasParent.gameObject);
        }

        if (zoomInputReceiver != null)
        {
            Destroy(zoomInputReceiver.gameObject);
            zoomInputReceiver = null;
        }
        
        var rtg = RTGApp.Get;
        if (rtg != null)
        {
            GameObject.Destroy(rtg.gameObject);
        }
        
        UGCImportPhotoManager.Inst.Dispose();
        UGCImportTextManager.Inst.Dispose();
        UGCPaintToolSettings.Dispose();
    }

    private void SetLight(bool isEnter)
    {
        if (isEnter)
        {
            _srcLightSetting.ambientSkyColor = new Color(1f, 0.835f, 0.768f);
            _srcLightSetting.ambientEquatorColor = new Color(0.93f, 0.93f, 0.93f);
            _srcLightSetting.ambientGroundColor = new Color(0.95f, 0.86f, 0.86f);
            
            _srcPreviewSceneLightVisible = AmbientLightManager.Inst.HidePreviewDirLight();
            _srcLightSetting = AmbientLightManager.Inst.OpenUILight(_srcLightSetting);
            _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
            _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();

        }
        else
        {
            AmbientLightManager.Inst.CloseUILight(_srcLightSetting);
            AmbientLightManager.Inst.RevertHallLight(_srcHallLightVisible);
            AmbientLightManager.Inst.RevertGameSceneLight(_srcGameSceneLightVisible);
            AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);
        }
    }
    
    
    protected virtual void OnCopyBtnClick()
    {
        zoomInputReceiver.gameObject.SetActive(false);
        List<RenderTexture> allFinalRenderTextures = new List<RenderTexture>();
        if (ugcPartTextureDatas != null)
        {
            foreach (var ugcPartTextureData in ugcPartTextureDatas.Values)
            {
                allFinalRenderTextures.Add(ugcPartTextureData.finalRT);
            }
        }
        var ugcDatas = UgcPartDataManager.Inst.GetUgcPartDataList(currentClothesData.id);
        copyEditPanel.Show(mapCanvas.gridOffset,mapCanvas.GridCountX,allFinalRenderTextures,ugcDatas);
        copyEditPanel._OnCopy = GetCopyInfo;
        copyEditPanel._OnBack = BackCopyState;
        copyEditPanel._OnPartSelect = OnCopySelect;
        copyEditPanel._OnDone = StartPaste;
        zoomSlider.SetValueWithoutNotify(0);
        OnZoomValueChange(1f);
        zoomPanel.SetParent(copyEditPanel.mask);
        zoomPanel.localPosition = Vector3.zero;
        GridMapClothesMask.gameObject.SetActive(false);
        zoomPanel.localScale = Vector3.one;
    }
    public void OnCopySelect(int id)
    {
        SwitchPart(id);
        if (GridMapClothesMask.texture!=null)
        {
            GridMapClothesMask.gameObject.SetActive(true);
        }
    }

    public virtual void SwitchPart(int partIndex = 0, bool isSwitchCamera = true)
    {
        
    }
    //
    // public override void ChangeParts(int partId)
    // {
    //     OnSelectHandler(allParts[partId-1]);
    //     StartUGCTween(ugcDatas[curClothesIndex].rotAngle,ugcDatas[curClothesIndex].rotAxie);
    // }
    public void StartPaste(int[] indexList,int angle)
    {
        mapCanvas.SetPaste(indexList,angle);
    }
    private void BackCopyState()
    {
        zoomInputReceiver.gameObject.SetActive(true);
        zoomSlider.SetValueWithoutNotify(0);
        OnZoomValueChange(1f);
        zoomPanel.SetParent(DrawBoard);
        zoomPanel.localPosition = Vector3.zero;
        zoomPanel.localScale = Vector3.one;
        if (GridMapClothesMask.texture!=null)
        {
            GridMapClothesMask.gameObject.SetActive(true);
        }
    }
    public void GetCopyInfo(int[] indexList)
    {
        mapCanvas.GetCopyInfoOnGrid(indexList);
    }
    protected Texture2D GenTexturePNG(RenderTexture rt)
    {
        Texture2D tex2D = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        RenderTexture.active = rt;
        tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex2D.Gamma();
        return tex2D;
    }
    public Texture2D MergeTexture2D(List<Texture2D> texture2Ds, bool destroy = true)
    {
        if (texture2Ds == null || texture2Ds.Count == 0)
        {
            return null;
        }

        var smallTextureSize = 256;
        var textureSize = Loader.GetUGCPartTextureSize(texture2Ds.Count, new Vector2Int(smallTextureSize, smallTextureSize));
        var bigTexture = new Texture2D(textureSize.x, textureSize.y);
        int colCount = bigTexture.width / smallTextureSize;
        for (int i = 0; i < texture2Ds.Count; i++)
        {
            var pixels = texture2Ds[i].Scale( smallTextureSize, smallTextureSize);
            bigTexture.SetPixels(i%colCount*smallTextureSize, i/colCount*smallTextureSize, smallTextureSize, smallTextureSize,pixels);
            if (destroy)
            {
                UnityEngine.Object.Destroy(texture2Ds[i]);
            }
        }
        bigTexture.Apply();
        return bigTexture;
    }
   
}