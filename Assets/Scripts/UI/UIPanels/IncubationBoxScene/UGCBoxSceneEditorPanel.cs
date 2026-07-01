using Basic;
using DG.Tweening;
using EventTracking;
using Game.Avatar;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.IncubationBoxScene;
using Game.UGCEditor;
using Game.Utils;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UGCAsset;
using UGCAsset.Draft;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 盒子场景（孵化箱）UGC 编辑面板。
/// 继承自 UGCBaseEditorPanel，负责盒子贴图的绘制、面切换、模型预览及发布流程。
/// 与服装/材质编辑器的主要区别：
///   1. 不依赖 UgcPartData / UGCClothEditorConfig 配置表，直接按面数创建纹理；
///   2. 不支持裁剪，aRT（opacity mask）不写入 _opacity_texmask；
///   3. 使用 AvatarCamera（透视模式）+ DPreviewRT 渲染预览，而非正交 UI 相机直渲；
///   4. 切换面时绕 Y 轴旋转 ClothesModelParent，每面间隔 90°。
/// </summary>
public class UGCBoxSceneEditorPanel : UGCBaseEditorPanel<UGCBoxSceneEditorPanel>
{
    // ──────────────────────────────────────────────
    // 左上角操作栏
    // ──────────────────────────────────────────────

    [Header("左上角")]
    /// <summary>退出编辑按钮（带 Loading 状态，点击时执行保存/放弃询问）。</summary>
    public LoadingButton exitBtn;

    /// <summary>发布按钮（带 Loading 状态，点击后上传并发布盒子场景）。</summary>
    public LoadingButton publishBtn;

    /// <summary>保存草稿按钮（带 Loading 状态，点击后仅保存不发布）。</summary>
    public LoadingButton saveBtn;

    /// <summary>切换"穿戴预览"模式的 Toggle（开启时左侧预览显示角色穿戴效果）。</summary>
    public Toggle showModeToggle;

    /// <summary>重置预览相机位置/旋转到默认视角的按钮。</summary>
    public CButton resetCameraBtn;

    /// <summary>贴图/文字/照片等元素工具面板的根节点，用于动态显隐子工具栏。</summary>
    public Transform elementPanelRoot;

    /// <summary>当前编辑面的部件名称文本（盒子场景暂不更新，保留字段供后续扩展）。</summary>
    public Text PartName;

    /// <summary>当前编辑面的部件图标（盒子场景暂不更新，保留字段供后续扩展）。</summary>
    public Image PartIconImage;

    /// <summary>UV 画布遮罩层 RawImage，服装编辑器用于显示不可绘区域；盒子场景在 OnShow 中强制隐藏。</summary>
    public RawImage GenerateClothesMask;

    /// <summary>右侧小预览图 RawImage，显示当前面的 finalRT（合成后的最终贴图缩略图）。</summary>
    public RawImage ShowGenerateImage;

    // ──────────────────────────────────────────────
    // 手势 / 输入
    // ──────────────────────────────────────────────

    /// <summary>
    /// 预览区域的手势输入接收器，负责将触摸/鼠标事件转发给 UgcPreviewInputHandler。
    /// 盒子场景 inputHandler 为 null，接收器注册后实际不触发任何模型点击逻辑。
    /// </summary>
    public UGCPreviewInputReceiver previewInputReceiver;

    // ──────────────────────────────────────────────
    // 运行时状态（私有）
    // ──────────────────────────────────────────────

    /// <summary>默认从第 1 面开始编辑（ugcType 从 1 起）。</summary>
    private int defaultUGCType = 1;

    /// <summary>
    /// 草稿进入时，等待各面图片异步加载完成后逐面重渲 finalRT 的协程句柄。
    /// 面板隐藏 / 销毁时需停止，防止关闭后仍回调。
    /// </summary>
    private Coroutine _rebakeFacesCor;

    /// <summary>当前正在编辑的盒子场景元数据，对应 MaterialInfo 在材质编辑器中的角色。</summary>
    private BoxSceneInfo currentBoxSceneInfo;

    /// <summary>各面图标所在目录的资源路径前缀。</summary>
    private const string FaceIconDir = "Assets/Loadable/Avatar/UGCRolePart/IconSprite/UGCBoxScene/";

    // ──────────────────────────────────────────────
    // 切换部件按钮
    // ──────────────────────────────────────────────

    [Header("切换部件")]
    /// <summary>切换到下一个面的箭头按钮（等同于点击 ClothesIconBtn）。</summary>
    public CButton SwitchPartBtn;

    /// <summary>
    /// 当前面的缩略图按钮，点击后循环切换到下一个面（1→2→…→faceCount→1）。
    /// 绑定 OnSwitchPartBtnClick，内部调用 SwitchPart()。
    /// </summary>
    public CButton ClothesIconBtn;

    // ──────────────────────────────────────────────
    // 左侧预览区域
    // ──────────────────────────────────────────────

    [Header("左侧预览")]
    /// <summary>
    /// AvatarCamera 下的 ClothModelTarget 节点。
    /// 双重职责：
    ///   1. 盒子 3D 主模型（clothesModel）和网格高亮模型（clothesMeshModel）的实例化父节点；
    ///   2. 切换面时绕 Y 轴旋转的目标（每面 90°，面1=0°，面2=90°…）。
    /// </summary>
    public Transform BoxModelParent;

    public Transform BoxModelRotateRoot;


    /// <summary>
    /// Avatar 预览模型的挂载父节点（开启"穿戴预览"模式时，角色模型挂在此节点下）。
    /// 对应 AvatarCamera 或独立预览节点，由 CreateAvatarPreview 使用。
    /// </summary>
    public Transform AvatarModelParent;

    /// <summary>
    /// Box壳子 预览模型的挂载父节点（开启"穿戴预览"模式时，角色模型挂在此节点下）。
    /// </summary>
    public Transform ShellModelParent;

    /// <summary>
    /// Box屏幕 预览模型的挂载父节点（开启"穿戴预览"模式时，角色模型挂在此节点下）。
    /// </summary>
    public Transform ScreenModelParent;

    /// <summary>触摸/点击判定区域的 GameObject，用于 inputHandler 注册可交互区域。</summary>
    public GameObject TouchAreaObj;

    /// <summary>
    /// 预览区域的 RectTransform，兼作 previewRawImage 的容器（GetComponent 获取 RawImage）。
    /// AvatarCamera 渲染到 DPreviewRT，DPreviewRT 显示在此 RawImage 上。
    /// </summary>
    public RectTransform DragRect;

    /// <summary>
    /// 透视预览相机 GameObject（对应场景中的 AvatarCamera 节点）。
    /// 持有 Camera 组件，cullingMask = Terrain 层，targetTexture = DPreviewRT。
    /// InitAvatarHandler 中设置 cullingMask 并将模型节点切换到 Terrain 层。
    /// </summary>
    public Camera AvatarCamera;

    // ──────────────────────────────────────────────
    // 外观切换（BoxModeChange）
    // ──────────────────────────────────────────────

    [Header("外观切换")]
    /// <summary>
    /// 外观切换按钮，循环切换隐藏盒子 / 展示盒子 / 展示BOX 三种显示模式。
    /// </summary>
    public Button BoxModeChangeBtn;

    /// <summary>外观切换按钮的图标 Image，随状态切换对应的 Sprite。</summary>
    public Image BoxModeChangeIcon;

    /// <summary>外观切换按钮的文字 Text，随状态切换显示不同标签。</summary>
    public Text BoxModeChangeText;

    /// <summary>
    /// 三个状态对应的图标 Sprite 数组，下标与 BoxModeState 枚举值一一对应。
    /// [0]=隐藏盒子，[1]=展示盒子，[2]=展示BOX。
    /// 在 Prefab Inspector 中绑定：icn_editor_box_hidebox / icn_editor_box / icn_editor_box_showbox。
    /// </summary>
    public Sprite[] BoxModeChangeSprites;

    /// <summary>盒子 3D 主模型实例（从 qiye_ugcBreedingFarm_1_3d.prefab 实例化），挂在 ClothesModelParent 下。</summary>
    private GameObject clothesModel;

    /// <summary>
    /// 盒子面高亮网格模型实例（从 qiye_ugcBreedingFarm_1_mesh.prefab 实例化）。
    /// 子节点列表即 clothPartsMesh，切换面时仅激活对应下标的子节点。
    /// </summary>
    private GameObject clothesMeshModel;

    /// <summary>截图用模型实例（OnShow 时克隆自 clothesModel，挂在 screenShotNode 下），发布封面时使用。</summary>
    private GameObject shotModel;

    /// <summary>
    /// 预览手势处理器（负责点击选面）。
    /// 盒子场景无 UgcPartData 配置表，不调用 SetPreviewTarget，
    /// 仅用于简单点击检测，不启用拖拽旋转/缩放手势。
    /// </summary>
    private UgcPreviewInputHandler inputHandler;

    /// <summary>
    /// 盒子各面的网格高亮子节点列表（clothesMeshModel.GetAllChildren() 的结果）。
    /// 下标从 0 起，对应 ugcType 从 1 起；切换面时仅激活 [partIndex-1] 号节点。
    /// </summary>
    private List<GameObject> clothPartsMesh;

    /// <summary>
    /// Renderer 名称到面索引的映射（key = Renderer.gameObject.name，value = ugcType 1~5）。
    /// 在 InitBoxSceneModel 中构建，由 OnClickModel 用于识别点击的是哪一面。
    /// </summary>
    private readonly Dictionary<string, int> _rendererNameToPartIndex = new Dictionary<string, int>();

    /// <summary>操作提示按钮，点击后打开 UGCEditTipsPanel 说明弹窗。</summary>
    public CButton tipsBtn;

    /// <summary>编辑开始时间戳（秒），用于统计本次编辑总时长并上报埋点。</summary>
    private float startTime;

    /// <summary>
    /// 各面的像素颜色字典，key = ugcType（1起），value = 坐标→颜色映射。
    /// 新建时每面为空字典；从服务器恢复时由 GetClothesPartDicByID 填充。
    /// </summary>
    private Dictionary<int, Dictionary<Vector2Int, Color>> serverDataDir;

    /// <summary>DragRect 上的 RawImage，显示 AvatarCamera 渲染到 DPreviewRT 的透视预览画面。</summary>
    private RawImage previewRawImage;

    private const string TAG = "UGCBoxSceneEditorPanel";

    /// <summary>调试日志开关：true=输出，false=静默。排查问题时打开，发布前关闭。</summary>
    private const bool DEBUG_BOX_SCENE = true;

    /// <summary>
    /// 当前编辑中的盒子场景服务端数据。
    /// 编辑已有盒子场景时由 BoxSceneDetailView 通过 args[2] 传入；
    /// 新建场景时为 null，首次调用 SetCharacterBox(Create) 成功后由服务端返回值填充。
    /// </summary>
    private CharacterBoxInfo _currentCharacterBoxInfo;

    /// <summary>
    /// 角色预览模型封装，在 InitCharacterPreview() 中创建，OnDestroy() 中销毁。
    /// </summary>
    private CharacterWrap _characterWrap;

    /// <summary>
    /// 盒子场景专用草稿信息，以 BoxSceneInfo 为底层数据模型，
    /// 负责封面图和元数据 JSON 的 COS 上传，不调用 /ugc/skin/set 接口。
    /// 在 OnShow 中随 currentBoxSceneInfo 一起初始化。
    /// </summary>
    private BoxSceneDraftInfo _boxSceneDraftInfo;

    /// <summary>
    /// 各面名称，下标 0 对应 partIndex 1（正面），依次为左面/右面/上/下。
    /// 切换面时用于更新 PartName 文本。
    /// </summary>
    private static readonly string[] FaceNames = { "正面", "左面", "右面", "上面", "下面" };

    /// <summary>
    /// 各面图标的文件名（含扩展名），顺序与 FaceNames 一致。
    /// 完整路径 = FaceIconDir + FaceIconSpriteNames[nameIndex]。
    /// </summary>
    private static readonly string[] FaceIconSpriteNames =
    {
        "ugcboxscene_iconsprite_1.png",  // 正面
        "ugcboxscene_iconsprite_2.png",  // 左面
        "ugcboxscene_iconsprite_3.png",  // 右面
        "ugcboxscene_iconsprite_4.png",  // 上面
        "ugcboxscene_iconsprite_5.png",  // 下面
    };

    /// <summary>
    /// 统一调试日志入口，受 DEBUG_BOX_SCENE 开关控制。
    /// </summary>
    /// <param name="msg">日志内容</param>
    private void BoxLog(string msg)
    {
        if (!DEBUG_BOX_SCENE)
            return;

        Debug.Log($"[BoxScene] {msg}");
    }

    /// <summary>是否为面部零件（盒子场景固定为 false，保留字段与父类接口保持一致）。</summary>
    private bool isFacePart = false;

    /// <summary>资源加载路径前缀（盒子场景使用完整路径，此字段固定为空字符串）。</summary>
    private string assetsDir;

    // ──────────────────────────────────────────────
    // 外观切换状态
    // ──────────────────────────────────────────────

    /// <summary>
    /// 盒子模型外观显示模式枚举。
    /// </summary>
    private enum BoxModeState
    {
        /// <summary>隐藏盒子：ShellModelParent 和 clothesModel 均关闭。</summary>
        HideAll = 0,

        /// <summary>展示盒子：仅显示玻璃壳（ShellModelParent），clothesModel 关闭。</summary>
        ShowGlass = 1,

        /// <summary>展示BOX：玻璃壳和 clothesModel 均开启，旋转锁定至正面固定坐标。</summary>
        ShowAll = 2,
    }

    /// <summary>当前外观显示状态，初始为 HideAll。</summary>
    private BoxModeState _currentBoxMode = BoxModeState.HideAll;

    /// <summary>三个状态对应的按钮文字标签，下标与 BoxModeState 枚举值一一对应。</summary>
    private static readonly string[] BoxModeStateTexts = { "隐藏盒子", "展示盒子", "展示BOX" };

    /// <summary>展示BOX状态下 BoxModelRotateRoot 的固定本地位置。</summary>
    private static readonly Vector3 BoxShowAllPosition = new Vector3(0f, 0.32f, 0f);

    /// <summary>展示BOX状态下 BoxModelRotateRoot 的固定本地缩放（均匀缩放 0.87）。</summary>
    private static readonly Vector3 BoxShowAllScale = new Vector3(0.87f, 0.87f, 0.87f);

    /// <summary>首次打开 UGC 编辑面板的 PlayerPrefs Key（含 UID，防止多账号互相影响）。</summary>
    string key = "FirstOpenUGCEditPanel" + AccountDataManager.Inst.Uid;
    public override void OnCreate()
    {
        base.OnCreate();
        previewRawImage = DragRect.GetComponent<RawImage>();
        CurrentPartIndex = defaultUGCType;
        elementPanelRoot = GameObjectEx.FindChildByName(transform, "ElementPanel");
        exitBtn.onClick.AddListener(OnExitClick);
        saveBtn.onClick.AddListener(OnSaveClicked);
        publishBtn.onClick.AddListener(OnPublishBtnClick);
        resetCameraBtn.onClick.AddListener(ResetCamera);
        SwitchPartBtn.onClick.AddListener(OnSwitchPartBtnClick);
        ClothesIconBtn.onClick.AddListener(OnSwitchPartBtnClick);
        showModeToggle.onValueChanged.AddListener(OnShowModeToggleChanged);
        BoxModeChangeBtn.onClick.AddListener(OnBoxModeChangeBtnClick);
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
        // 新建盒子场景时 ugcPartTextureDatas 为空，跳过贴图显示
        if (!ugcPartTextureDatas.ContainsKey(partIndex))
            return;

        drawCanvas.SetRawImage(ugcPartTextureDatas[partIndex].mRT);
        drawCanvas.SetTransparentMat(currentBoxSceneInfo.templateId, ugcPartTextureDatas[partIndex].mRT,
            ugcPartTextureDatas[partIndex].aRT);

        ShowGenerateImage.texture = ugcPartTextureDatas[partIndex].finalRT;
        SetFinalTargetTexture(ugcPartTextureDatas[partIndex].finalRT);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        UIManager.Inst.ClosePanel(PanelId.BoxSceneStudioMainPanel);
        //if (!PlayerPrefs.HasKey(key) && !BootPanel.isPlaying && SignInPanel.isNewPlayer)
        //{
        //    PlayerPrefs.SetInt(key, 1);
        //    PlayerPrefs.Save();
        //    UIManager.Inst.OpenPanel(PanelId.BootPanel, WindowId.UGCResourceEditWindow, 112);
        //}

        BoxLog("OnShow 开始");

        // 仿照 UGCMaterialEditorPanel：args[0] 为 UGCBoxSceneData，args[1] 为 BoxSceneInfo
        var cData = args[0] as UGCBoxSceneData;
        currentClothesData = cData;
        currentBoxSceneInfo = args[1] as BoxSceneInfo;
        BoxLog($"参数解析完成 | cData={cData != null} boxSceneInfo={currentBoxSceneInfo != null}");

        // 读取可选的 CharacterBoxInfo（编辑已有盒子场景时由 BoxSceneDetailView 通过 args[2] 传入）
        _currentCharacterBoxInfo = args.Length > 2 ? args[2] as CharacterBoxInfo : null;

        // 初始化盒子场景专用草稿信息（以 BoxSceneInfo 为底层数据模型，替代 SkinDraftInfo）
        // currentBoxSceneInfo.id / cover / metaDataUrl 在 BoxSceneDetailView 中已通过 args[1] 传入
        _boxSceneDraftInfo = new BoxSceneDraftInfo(currentBoxSceneInfo);

        isFacePart = false;  // 盒子场景不是面部零件
        assetsDir = "";       // 盒子模型使用完整路径，无需前缀目录

        _canvasType = (CanvasType)currentBoxSceneInfo.canvasType;
        BoxLog($"画布类型={_canvasType}");

        // 初始化 GamePropNodeManager（建立 secondCachePool），
        // 使 UGCImage / UGCText 的删除与撤销功能正常工作。
        // 退出时 GameController.ExitGame() 会自动 Release，无需手动清理。
        GamePropNodeManager.Inst.Init(new EcsSceneWorld());

        BoxLog("初始化绘图画布...");
        mapCanvas.OnCreate(_canvasType);
        mapCanvas.OnAddRecordEvent = AddRecord;

        // 必须先加载模型，InititalMapCanvas 新建分支用 clothPartsMesh.Count 确定面数
        BoxLog("加载盒子场景模型...");
        InitBoxSceneModel();
        BoxLog($"主模型={clothesModel != null} 网格模型={clothesMeshModel != null} 部件数量={clothPartsMesh?.Count ?? -1}");

        // 模型实例化完毕后，设置 AvatarCamera 透视渲染参数并切换模型 layer
        BoxLog("初始化透视相机...");
        InitAvatarHandler();
        BoxLog("透视相机初始化完成");

        // 初始化外观切换按钮状态为"隐藏盒子"（须在 InitAvatarHandler 之后，inputHandler 已创建）
        ApplyBoxMode(BoxModeState.HideAll);

        // 初始化角色预览模型（须在 InitAvatarHandler 之后，确保 AvatarCamera cullingMask 已设置）
        InitCharacterPreview();
        BoxLog("角色预览初始化完成");

        // 盒子场景无遮罩区域，空白像素应显示纯白而非透明。
        // 服装编辑器的 mDrawCamera 默认背景 alpha=0（透明），
        // 此处改为白色纯色清屏，保证空画布渲染到 finalRT 时输出白色而非黑色/透明。
        drawCanvas.mDrawCamera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
        drawCanvas.mDrawCamera.backgroundColor = Color.white;

        BoxLog("初始化地图画布...");
        InititalMapCanvas();

        // 盒子场景不依赖 UgcPartData/UGCClothEditorConfig 表，不调用服装编辑器的 InitPreviewHandler；
        // 点击选面逻辑已在 InitAvatarHandler 中通过 UgcPreviewInputHandler 单独初始化。

        BoxLog("初始化模型部件材质...");
        InitModelPartsMats();

        BoxLog($"切换到部件 {CurrentPartIndex}...");
        SwitchPart(CurrentPartIndex);

        // 草稿（已有数据）进入时图片是异步加载的，init 循环里那次渲染早于图片到达，
        // 导致除当前面外其它面的 finalRT 烤成空白。此处启动协程：等图片加载完成后逐面重烤。
        if (currentClothesData?.parts is { Count: > 0 })
        {
            _rebakeFacesCor = StartCoroutine(RebakeAllFacesWhenLoaded());
        }

        BoxLog("初始化截图相机模型...");
        InitCameraShotModel();
        BoxLog($"截图模型={shotModel != null}");

        BoxLog("获取原始材质...");
        GetOriginalMaterials(clothesModel, shotModel);

        editorTool.SetEraserToggle(false);
        //editorTool.SetScissorsToggle(true);
        editorTool.SetAdjustVisible(false);
        showModeToggle.gameObject.SetActive(true);
        copyEditPanel.isCharacter = true;

        GameTimeUtils.Inst.StartCollect(TAG);

        var curStyle = currentBoxSceneInfo.ugcStyle == (int)UgcShaderStyle.Normal
            ? MISource.Source.Bud
            : MISource.Source.Create;
        tipsBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.UGCEditTipsPanel);
        });

        BoxLog("OnShow 完成");
    }

    /// <summary>
    /// 重写复制按钮点击，为 CopySelectPartPanel 的 5 个面预加载固定 mask 描边贴图，
    /// 使面选择界面上每个缩略图都能显示对应面的可编辑区域描边。
    /// </summary>
    protected override void OnCopyBtnClick()
    {
        var maskTextures = new List<Texture>();

        for (int i = 1; i <= 5; i++)
        {
            var path = $"Assets/Loadable/Avatar/UGCRolePart/IconSprite/UGCBoxScene/UGCBreedingFarm_1/qiye_ugcBreedingFarm_1_{i}_mask{pngExt}";
            var tex = Loader.Load<Texture>(path, this.gameObject);
            maskTextures.Add(tex);
        }

        copyEditPanel.SelectPartPanel.OverrideMaskTextures = maskTextures;

        base.OnCopyBtnClick();
    }

    public override void OnHidden()
    {
        base.OnHidden();
        GameTimeUtils.Inst.StopCollect(TAG);

        // 停止逐面重烤协程，防止面板关闭后仍回调
        if (_rebakeFacesCor != null)
        {
            StopCoroutine(_rebakeFacesCor);
            _rebakeFacesCor = null;
        }

        // 面板关闭时解锁旋转，防止旋转锁定状态残留
        if (inputHandler != null)
        {
            inputHandler.IsRotationLocked = false;
        }
    }



    private void InititalMapCanvas()
    {
        ugcPartTextureDatas = new Dictionary<int, UGCPartTextures>();
        serverDataDir = new Dictionary<int, Dictionary<Vector2Int, Color>>();

        if (currentClothesData.parts != null && currentClothesData.parts.Count > 0)
        {
            // 已有数据：从服务器保存的像素数据恢复各面贴图
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
                drawCanvas.SetTransparentMat(currentBoxSceneInfo.templateId, ugcPartTextureDatas[partIndex].mRT,
                    ugcPartTextureDatas[partIndex].aRT);
                SetFinalTargetTexture(texData.finalRT);
            }
        }
        else
        {
            // 新建盒子场景：盒子场景不依赖 UgcPartData 表，仿照材质编辑器直接为每个面
            // 创建空白像素字典和对应的 RenderTexture，ugcType 从 1 开始与面的下标对应。
            // clothPartsMesh 在此函数调用前由 InitBoxSceneModel 填充，Count = 面数（当前为 5）。
            int faceCount = clothPartsMesh != null ? clothPartsMesh.Count : 5;
            for (int i = 0; i < faceCount; i++)
            {
                int partIndex = i + 1;  // ugcType 从 1 起
                serverDataDir.Add(partIndex, LoadDefaultPartPixels(partIndex) ?? new());
                var texData = mapCanvas.GenerateNoFilterTexture2D(serverDataDir[partIndex]);
                texData.mRT = mapCanvas.GenerateFilterTexture(texData.mTex);
                texData.aRT = mapCanvas.GenerateFilterTexture(texData.aTex);
                texData.finalRT = mapCanvas.GenerateFinalTexture();
                ugcPartTextureDatas.Add(partIndex, texData);

                // 逐一渲染以初始化 finalRT（模板数据加载成功则显示默认颜色，否则为空白画布）。
                // mDrawCamera.backgroundColor 已设为白色，避免未切换过的面因 RT 未初始化而显示黑色。
                drawCanvas.SetRawImage(texData.mRT);
                drawCanvas.SetTransparentMat(currentBoxSceneInfo.templateId, texData.mRT, texData.aRT);
                SetFinalTargetTexture(texData.finalRT);
            }

            // 将画布绑定回第一个面（ugcType=1）
            drawCanvas.SetRawImage(ugcPartTextureDatas[defaultUGCType].mRT);
            drawCanvas.SetTransparentMat(currentBoxSceneInfo.templateId,
                ugcPartTextureDatas[defaultUGCType].mRT,
                ugcPartTextureDatas[defaultUGCType].aRT);
            SetFinalTargetTexture(ugcPartTextureDatas[defaultUGCType].finalRT);
        }

        UGCImportPhotoManager.Inst.SetElementHandleCanUse(false);
        UGCImportTextManager.Inst.SetElementHandleCanUse(false);
    }

    /// <summary>
    /// 草稿进入时各面图片为异步加载，InititalMapCanvas 中的首次渲染早于图片到达，
    /// 致使除当前面外其它面的 finalRT 被烤成空白（要点击才显示）。
    /// 本协程等待所有图片元素加载到终态后，逐面重新渲染一次 finalRT，
    /// 把已加载的图片就地烤进各面材质，使 5 个面无需点击即可自动显示。
    /// </summary>
    private IEnumerator RebakeAllFacesWhenLoaded()
    {
        // 超时保护：避免存在 photoUrl 为空、loadState 永远停在 Loading 的元素导致死等。
        // 约 5 秒（按 60 帧/秒折算为 300 帧），超时后照常重烤一遍。
        const int maxWaitFrames = 300;
        int waitedFrames = 0;

        // 1. 等待所有图片元素加载完成（进入 Success / Error 终态）
        while (waitedFrames < maxWaitFrames)
        {
            if (IsAllPhotosLoaded())
            {
                break;
            }

            waitedFrames++;
            yield return null;
        }

        if (waitedFrames >= maxWaitFrames)
        {
            BoxLog("RebakeAllFacesWhenLoaded: 等待图片加载超时，仍执行重烤");
        }

        // 2. 逐面重烤：切到该面的元素可见性并把图片渲染进对应 finalRT
        foreach (var kv in ugcPartTextureDatas)
        {
            int partIndex = kv.Key;
            var texData = kv.Value;

            // 切换该面元素可见性（与 init 循环、点击切面保持一致）
            UGCImportPhotoManager.Inst.OnChangePart(partIndex);
            UGCImportTextManager.Inst.OnChangePart(partIndex);

            // 让画布与相机指向该面，触发一次渲染把图片烤进 finalRT
            drawCanvas.SetRawImage(texData.mRT);
            drawCanvas.SetTransparentMat(currentBoxSceneInfo.templateId, texData.mRT, texData.aRT);
            SetFinalTargetTexture(texData.finalRT);
        }

        // 3. 恢复到当前正在编辑的面（重烤循环后元素可见性停在了最后一个面）
        UGCImportPhotoManager.Inst.OnChangePart(CurrentPartIndex);
        UGCImportTextManager.Inst.OnChangePart(CurrentPartIndex);
        ShowSmallGenerateTexture(CurrentPartIndex);

        BoxLog($"RebakeAllFacesWhenLoaded: 完成 {ugcPartTextureDatas.Count} 面重烤，已恢复到面 {CurrentPartIndex}");
        _rebakeFacesCor = null;
    }

    /// <summary>
    /// 判断当前所有图片元素是否都已加载到终态（Success 或 Error）。
    /// 仅 Loading 状态视为未完成，None（无 photoUrl 等）不阻塞重烤。
    /// </summary>
    /// <returns>无元素或全部进入终态时返回 true。</returns>
    private bool IsAllPhotosLoaded()
    {
        var elementList = UGCImportPhotoManager.Inst.GetElementList();

        if (elementList == null || elementList.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < elementList.Count; i++)
        {
            if (elementList[i] is UGCPhotoBehaviour photo && photo.loadState == TexLoadState.Loading)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 加载指定面（partIndex）的默认模板像素数据，用于新建场景时替代空白像素字典，避免画布显示灰色。
    /// 模板文件路径格式：Assets/Loadable/Avatar/UGCRolePart/Configs/UGCBox/template_{N}.json
    /// 文件结构为 UGCBoxSceneData JSON，每个文件对应一个面（template_1 = ugcType 1，以此类推）。
    /// </summary>
    /// <param name="partIndex">面索引（从 1 起，对应 ugcType）</param>
    /// <returns>像素坐标到颜色的映射字典；加载或解析失败时返回 null，由调用方回退到空字典</returns>
    private Dictionary<Vector2Int, Color> LoadDefaultPartPixels(int partIndex)
    {
        var path = GameConsts.ClothesAssetDir + "Configs/UGCBox/template.json";
        var textAsset = Loader.Load<TextAsset>(path, gameObject);

        if (textAsset == null)
        {
            LoggerUtils.LogError($"[UGCBoxSceneEditorPanel] 加载默认模板失败，路径：{path}");
            return null;
        }

        var templateData = JsonConvert.DeserializeObject<UGCBoxSceneData>(textAsset.text);

        if (templateData?.parts == null || templateData.parts.Count == 0)
        {
            LoggerUtils.LogError($"[UGCBoxSceneEditorPanel] 模板数据为空或无 parts：{path}");
            return null;
        }

        // 复用基类方法，支持 32→64 画布自动扩展
        var partDic = GetClothesPartDicByID(templateData);

        if (!partDic.TryGetValue(partIndex, out var pixelDic))
        {
            LoggerUtils.LogError($"[UGCBoxSceneEditorPanel] 模板中未找到 partIndex={partIndex}，路径：{path}");
            return null;
        }

        return pixelDic;
    }

    protected override void OnColorToggleChange(bool isSelected)
    {
        base.OnColorToggleChange(isSelected);
        // previewInputReceiver.enabled = !isSelected;
    }

    public void OnSelectHandle(int partIndex)
    {
        // 切换画布到当前面，传空列表表示无锁定区域（传 null 会导致 IsCanDrawArea 崩溃）
        if (ugcPartTextureDatas.ContainsKey(partIndex) && serverDataDir.ContainsKey(partIndex))
        {
            mapCanvas.ChangeClothesPart(ugcPartTextureDatas[partIndex], new List<Vector2Int>(),
                serverDataDir[partIndex]);
            UGCImportPhotoManager.Inst.OnChangePart(partIndex);
            UGCImportTextManager.Inst.OnChangePart(partIndex);
        }

        // partIndex 从 1 起，转为数组下标
        var nameIndex = partIndex - 1;

        // 更新面名称文本
        if (PartName != null && nameIndex >= 0 && nameIndex < FaceNames.Length)
        {
            PartName.text = FaceNames[nameIndex];
        }

        // 更新面图标，从资源路径按文件名加载单张 Sprite
        if (PartIconImage != null && nameIndex >= 0 && nameIndex < FaceIconSpriteNames.Length)
        {
            var path = FaceIconDir + FaceIconSpriteNames[nameIndex];
            var sprite = XAssetLoaderMgr.Inst.LoadResource<Sprite>(path, this.gameObject);

            if (sprite != null)
            {
                PartIconImage.sprite = sprite;
            }
            else
            {
                LoggerUtils.LogError($"[UGCBoxSceneEditorPanel] 面图标加载失败：{path}");
            }
        }

        // 根据 partIndex 加载对应面的遮罩贴图，直接按路径拼接，不走配置表
        var maskPath = $"Assets/Loadable/Avatar/UGCRolePart/IconSprite/UGCBoxScene/UGCBreedingFarm_1/qiye_ugcBreedingFarm_1_{partIndex}_mask{pngExt}";
        var maskTex = Loader.Load<Texture>(maskPath, this.gameObject);

        if (GridMapClothesMask != null)
        {
            GridMapClothesMask.texture = maskTex;
        }

        if (GenerateClothesMask != null)
        {
            GenerateClothesMask.texture = maskTex;
        }
    }

    public List<Vector2Int> GetInoperableArea(List<Vector2Int> inoperableArea)
    {
        if (inoperableArea == null)
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
            inoperableArea64.Add(new Vector2Int(inoperableArea[i].x * 2, inoperableArea[i].y * 2));
            inoperableArea64.Add(new Vector2Int(inoperableArea[i].x * 2, inoperableArea[i].y * 2 + 1));
            inoperableArea64.Add(new Vector2Int(inoperableArea[i].x * 2 + 1, inoperableArea[i].y * 2 + 1));
            inoperableArea64.Add(new Vector2Int(inoperableArea[i].x * 2 + 1, inoperableArea[i].y * 2));
        }
        return inoperableArea64;

    }


    private void OnExitClick()
    {
        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("确认保存", "保存当前的创作进度吗？", "保存", "不保存");
        commonConfirmPanel.SetIsCloseSelf(false);
        commonConfirmPanel.SetOnClickAction(() =>
        {
            commonConfirmPanel.SetConfirmLoadingVisible(true);

            // 走与 saveBtn 完全一致的完整保存流程（含 SetCharacterBox 写服务端记录）。
            // 之前此处仅调用 UploadClothesData，只上传封面/元数据到 COS，未调用 SetCharacterBox，
            // 且退出回调在上传完成前就被同步触发，导致退出时点"保存"实际并未保存上。
            // 保存流程结束（成功或失败）后再退出。
            SaveClothesData(() =>
            {
                if (commonConfirmPanel != null && commonConfirmPanel.gameObject != null)
                {
                    commonConfirmPanel.Close();
                }
                GameController.ExitGame(() =>
                {
                    CloseSelf();
                    UIManager.Inst.OpenPanel(PanelId.BoxSceneStudioMainPanel, BoxSceneStudioType.Draft);
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
                CloseSelf();
                UIManager.Inst.OpenPanel(PanelId.BoxSceneStudioMainPanel, BoxSceneStudioType.Draft);
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
        // 同步当前风格到 currentBoxSceneInfo，确保元数据中风格字段与实际一致
        currentBoxSceneInfo.ugcStyle = (int)curAnimeStyle;

        var draftInfo = _boxSceneDraftInfo;
        var clothesData = GetClothesData();
        draftInfo.SetMetaData(JsonConvert.SerializeObject(clothesData));

        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG); // 重启编辑时长
        LoggerUtils.Log("###原编辑总时长：" + draftInfo.editTime + "  当次编辑时长：" + curEditTime);
        draftInfo.editTime += curEditTime;

        GetCover(tmp =>
        {
            if (tmp == null)
            {
                TipPanel.ShowToast("保存失败");
                callBack?.Invoke();
                return;
            }

            draftInfo.SetCover(tmp);
            draftInfo.UploadAndSave((info, isSuccess) =>
            {
                if (!isSuccess)
                {
                    TipPanel.ShowToast("保存失败");
                    callBack?.Invoke();
                    return;
                }

                // COS 上传成功，currentBoxSceneInfo.cover / metaDataUrl 已由 BaseDraftInfo.RefreshUploadInfo 更新
                var charBoxInfo = BuildOrUpdateCharacterBoxInfo();
                var setType = string.IsNullOrEmpty(charBoxInfo.id) ? SetType.Create : SetType.Edit;

                LoggerUtils.Log($"[UGCBoxSceneEditorPanel] 调用 SetCharacterBox setType={setType} id={charBoxInfo.id}");

                CabinBoxSceneNetManager.Inst.SetCharacterBox(setType, charBoxInfo, (success, resultInfo) =>
                {
                    if (success)
                    {
                        // 保存服务端返回的最新 CharacterBoxInfo（含服务端分配的 id）
                        _currentCharacterBoxInfo = resultInfo;

                        // 将服务端分配的 id 同步到 currentBoxSceneInfo，后续保存使用 Edit 而非 Create
                        if (!string.IsNullOrEmpty(resultInfo?.id))
                        {
                            currentBoxSceneInfo.id = resultInfo.id;
                        }

                        TipPanel.ShowToast("保存成功");
                    }
                    else
                    {
                        TipPanel.ShowToast("保存失败");
                    }

                    callBack?.Invoke();
                });
            });
        });
    }

    private void UploadClothesData(Action<bool> uploadCallBack)
    {
        UploadClothesData(null, uploadCallBack);
    }

    private void UploadClothesData(Action saveCallBack, Action<bool> uploadCallBack = null)
    {
        // 同步当前风格到 currentBoxSceneInfo，确保元数据中风格字段与实际一致
        currentBoxSceneInfo.ugcStyle = (int)curAnimeStyle;

        var draftInfo = _boxSceneDraftInfo;
        var clothesData = GetClothesData();
        draftInfo.SetMetaData(JsonConvert.SerializeObject(clothesData));

        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG); // 重启编辑时长
        LoggerUtils.Log("###原编辑总时长：" + draftInfo.editTime + "  当次编辑时长：" + curEditTime);
        draftInfo.editTime += curEditTime;

        GetCover(tmp =>
        {
            if (tmp == null)
            {
                TipPanel.ShowToast("保存失败");
                return;
            }

            draftInfo.SetCover(tmp);
            draftInfo.UploadAndSave((info, isSuccess) =>
            {
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
                LoggerUtils.LogError("UGCBoxSceneEditorPanel GetCover fail", e);
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

    private void OnPublishClicked(CurrencyType currencyType)
    {
        publishBtn.ShowLoading();

        UploadClothesData((isSuccess) =>
        {
            if (!isSuccess)
            {
                publishBtn.HideLoading();
                TipPanel.ShowToast("保存草稿失败");
                return;
            }

            // VIP 检查：使用了相册图片但非 VIP 时阻止发布
            string[] photoImages = UGCImportPhotoManager.Inst.GetUrlArr();
            bool isVip = VipDataManager.Inst.isVip;

            if (photoImages != null && photoImages.Length > 0 && !isVip)
            {
                publishBtn.HideLoading();
                string titleStr = LocalizationManager.Inst.GetLocalizedText("您正在使用的VIP功能：添加手机相册图片");
                UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, titleStr, new List<JoinVipType>
                {
                    JoinVipType.Image
                });
                return;
            }

            var charBoxInfo = BuildOrUpdateCharacterBoxInfo();

            if (string.IsNullOrEmpty(charBoxInfo.id))
            {
                // 新建场景：先 Create 获取服务端 id，再打开发布面板让用户填写名称/描述/价格后发布
                LoggerUtils.Log("[UGCBoxSceneEditorPanel] 新建场景发布：先 Create 获取 id，再打开 BoxScenePublishPanel");

                CabinBoxSceneNetManager.Inst.SetCharacterBox(SetType.Create, charBoxInfo, (createSuccess, createResult) =>
                {
                    publishBtn.HideLoading();

                    if (!createSuccess)
                    {
                        TipPanel.ShowToast("发布失败，请稍后重试");
                        return;
                    }

                    _currentCharacterBoxInfo = createResult;
                    UIManager.Inst.OpenPanel(PanelId.BoxScenePublishPanel, createResult,
                        (Action)(() => { OnBoxScenePublishSuccess(BoxSceneStudioType.Published); }),
                        (Action)(() => { OnBoxScenePublishSuccess(BoxSceneStudioType.Draft); }));
                });
            }
            else
            {
                // 已有草稿：直接打开发布面板（携带最新的 cover / metaDataUrl）
                LoggerUtils.Log($"[UGCBoxSceneEditorPanel] 已有草稿，打开 BoxScenePublishPanel id={charBoxInfo.id}");

                publishBtn.HideLoading();
                UIManager.Inst.OpenPanel(PanelId.BoxScenePublishPanel, charBoxInfo, 
                    (Action)(() => { OnBoxScenePublishSuccess(BoxSceneStudioType.Published); }),
                    (Action)(() => { OnBoxScenePublishSuccess(BoxSceneStudioType.Draft); }));
            }
        });
    }

    #region 衣服模型预览     //TODO:处理模型Vivo花屏问题

    private void InitAvatarHandler()
    {
        // 盒子场景用 AvatarCamera 做透视预览，保留在原位不移走
        // 设置 cullingMask 只渲染 Terrain 层（盒子模型所在层）
        var cam = AvatarCamera;
        cam.cullingMask = LayerMask.GetMask("Terrain");

        // 将模型节点及其子节点切换到 Terrain 层，使透视相机可见
        if (BoxModelParent != null)
        {
            ChangeLayer(BoxModelParent, "Terrain");
        }
        int index = 0;
        if (GlobalCameraManager.Inst.UICamera != null)
        {
            index = GlobalCameraManager.Inst.IndexOf(GlobalCameraManager.Inst.UICamera);
        }
        GlobalCameraManager.Inst.Insert(index, AvatarCamera);
        // 初始化点击选面处理器（仅用于简单点击选面，不传入 SetPreviewTarget，不启用拖拽旋转/缩放）
        inputHandler = new UgcPreviewInputHandler(0);
        inputHandler.SetClickArea(TouchAreaObj);
        inputHandler.SetPreviewTarget(BoxModelRotateRoot.gameObject, null);
        inputHandler.SetDragRect(DragRect);
        inputHandler.SetRaycastConfig(cam, LayerMask.GetMask("Terrain"));
        inputHandler.SetSimpleClickAction(OnClickModel);
        previewInputReceiver.SetHandle(inputHandler);
        BoxLog("点击选面检测处理器初始化完成");
    }

    /// <summary>
    /// 初始化角色预览模型。使用当前登录用户的外观数据创建 UI 预览角色，
    /// 挂载到 AvatarModelParent 下，切换到 Terrain 层以供 AvatarCamera 渲染。
    /// 默认隐藏，由 showModeToggle 控制显示。
    /// </summary>
    private void InitCharacterPreview()
    {
        // 防止重复初始化：先销毁旧实例再重建
        if (_characterWrap != null && _characterWrap.Avatar != null)
        {
            LoggerUtils.Log($"[UGCBoxSceneEditorPanel] 检测到残留角色实例，先销毁再重建");
            UnityEngine.Object.Destroy(_characterWrap.Avatar.gameObject);
            _characterWrap = null;
        }

        var avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;

        if (avatarInfo == null)
        {
            LoggerUtils.LogError($"[UGCBoxSceneEditorPanel] avatarInfo 为空，无法创建角色预览");
            return;
        }

        _characterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
        _characterWrap.SetParent(AvatarModelParent, true);

        // 将角色所有子节点切换到 Terrain 层，与 AvatarCamera（cullingMask=Terrain）保持一致
        if (AvatarModelParent != null)
        {
            ChangeLayer(AvatarModelParent, "Terrain");
        }

        // 根据当前 Toggle 状态决定初始显隐（处理面板热重载后 Toggle 仍为 on 的情况）
        _characterWrap.Avatar.gameObject.SetActive(showModeToggle.isOn);

        LoggerUtils.Log($"[UGCBoxSceneEditorPanel] 角色预览模型创建完成，显示状态={showModeToggle.isOn}");
    }

    /// <summary>
    /// showModeToggle 状态变更回调：控制角色预览模型的显隐。
    /// </summary>
    /// <param name="isOn">true = 显示角色，false = 隐藏角色</param>
    private void OnShowModeToggleChanged(bool isOn)
    {
        LoggerUtils.Log($"[UGCBoxSceneEditorPanel] OnShowModeToggleChanged isOn={isOn}");

        if (_characterWrap == null || _characterWrap.Avatar == null)
            return;

        _characterWrap.Avatar.gameObject.SetActive(isOn);
    }

    private void ChangeLayer(Transform node, string layerName)
    {
        for (int i = 0; i < node.childCount; i++)
        {
            var childNode = node.GetChild(i);
            childNode.gameObject.layer = LayerMask.NameToLayer(layerName);
            ChangeLayer(childNode, layerName);
        }
    }

    /// <summary>
    /// 处理点击预览模型回调，根据点击的 Renderer 名称快速切换到对应面进行编辑。
    /// 由 UgcPreviewInputHandler 短触摸检测触发，通过 AvatarCamera + Terrain 层射线命中。
    /// </summary>
    /// <param name="hitTrans">射线命中的 Transform。</param>
    private void OnClickModel(Transform hitTrans)
    {
        // 复制模式下不响应点击
        if (copyEditPanel.gameObject.activeSelf)
            return;

        // 通过 Renderer 名称查找对应面索引
        if (!_rendererNameToPartIndex.TryGetValue(hitTrans.name, out int partIndex))
            return;

        // 已是当前编辑面则忽略
        if (partIndex == CurrentPartIndex)
            return;

        SwitchPart(partIndex, false);
    }

    private void ResetCamera()
    {
        if (BoxModelRotateRoot == null)
        {
            return;
        }

        BoxModelRotateRoot.DOLocalRotate(Vector3.zero, 0.3f);
    }

    // ──────────────────────────────────────────────
    // 外观切换（BoxModeChange）
    // ──────────────────────────────────────────────

    /// <summary>
    /// BoxModeChange 按钮点击回调。
    /// 循环切换外观显示状态：HideAll → ShowGlass → ShowAll → HideAll。
    /// </summary>
    private void OnBoxModeChangeBtnClick()
    {
        int nextMode = ((int)_currentBoxMode + 1) % 3;
        ApplyBoxMode((BoxModeState)nextMode);
    }

    /// <summary>
    /// 应用指定的外观显示模式。
    /// 控制 ShellModelParent（玻璃壳）和 clothesModel（box外观）的显隐，
    /// 以及旋转锁定和 BoxModelRotateRoot 坐标（展示BOX模式下固定至正面）。
    /// </summary>
    /// <param name="state">目标显示状态</param>
    private void ApplyBoxMode(BoxModeState state)
    {
        _currentBoxMode = state;

        bool showGlass = state == BoxModeState.ShowGlass || state == BoxModeState.ShowAll;
        bool showBox = state == BoxModeState.ShowAll;
        bool lockRot = state == BoxModeState.ShowAll;

        // 控制玻璃壳（ShellModelParent）显隐
        if (ScreenModelParent != null)
        {
            ScreenModelParent.gameObject.SetActive(showGlass);
        }

        // 控制 box 外观（clothesModel）显隐
        if (ShellModelParent != null)
        {
            ShellModelParent.gameObject.SetActive(showBox);
        }

        // 控制旋转锁定
        if (inputHandler != null)
        {
            inputHandler.IsRotationLocked = lockRot;
        }

        // 处理 BoxModelRotateRoot 的本地坐标（DOTween duration=0 实现即时变换）
        if (BoxModelRotateRoot != null)
        {
            if (state == BoxModeState.ShowAll)
            {
                // 展示BOX：固定位置(0,0.32,0)、旋转归零、缩放0.87
                BoxModelRotateRoot.DOLocalMove(BoxShowAllPosition, 0f);
                BoxModelRotateRoot.DOLocalRotate(Vector3.zero, 0f);
                BoxModelRotateRoot.DOScale(BoxShowAllScale, 0f);
            }
            else
            {
                // 退出展示BOX：还原位置和缩放，旋转恢复为当前编辑面角度
                BoxModelRotateRoot.DOLocalMove(Vector3.zero, 0f);
                BoxModelRotateRoot.DOScale(Vector3.one, 0f);

                var faceRot = PartRotateArr[CurrentPartIndex - 1];
                BoxModelRotateRoot.DOLocalRotate(faceRot, 0f);
            }
        }

        UpdateBoxModeChangeBtnUI(state);

        LoggerUtils.Log($"[UGCBoxSceneEditorPanel] ApplyBoxMode → {state}，玻璃={showGlass}，box外观={showBox}，锁定旋转={lockRot}");
    }

    /// <summary>
    /// 根据当前状态更新 BoxModeChange 按钮的图标 Sprite 和文字标签。
    /// </summary>
    /// <param name="state">当前显示状态</param>
    private void UpdateBoxModeChangeBtnUI(BoxModeState state)
    {
        int idx = (int)state;

        if (BoxModeChangeText != null)
        {
            BoxModeChangeText.text = BoxModeStateTexts[idx];
        }

        if (BoxModeChangeIcon != null
            && BoxModeChangeSprites != null
            && idx < BoxModeChangeSprites.Length
            && BoxModeChangeSprites[idx] != null)
        {
            BoxModeChangeIcon.sprite = BoxModeChangeSprites[idx];
            BoxModeChangeIcon.SetNativeSize();
        }
    }

    /// <summary>
    /// 加载盒子场景 3D 模型（固定路径），替代服装编辑器中的 InitClothModel。
    /// 使用 UGCBreedingFarm_1 的 prefab，不依赖数据表动态查找。
    /// </summary>
    private void InitBoxSceneModel()
    {
        var modelPath = CabinBoxSceneNetManager.modelPath;
        var meshPath = CabinBoxSceneNetManager.meshPath;

        BoxLog($"加载主模型：{modelPath}");
        var model = Loader.Load<GameObject>(modelPath);
        BoxLog($"主模型加载结果：{model != null}");

        if (model == null)
        {
            Debug.LogError($"[BoxScene] 找不到主模型，路径：{modelPath}");
            return;
        }

        clothesModel = model.Instantiate(BoxModelParent);
        clothesModel.name = "UGCModel";
        clothesModel.transform.localPosition = Vector3.zero;
        BoxLog($"主模型已实例化，挂载到：{BoxModelParent?.name}");

        // 构建 Renderer 名称 → 面索引映射（与 SetPartsMats 中 renderers[i] 对应 ugcType=i+1 的规则一致）
        _rendererNameToPartIndex.Clear();
        var renderers = clothesModel.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            _rendererNameToPartIndex[renderers[i].gameObject.name] = i + 1;
        }
        BoxLog($"已构建面映射，共 {renderers.Length} 个 Renderer");

        BoxLog($"加载网格模型：{meshPath}");
        var modelMesh = Loader.Load<GameObject>(meshPath);
        BoxLog($"网格模型加载结果：{modelMesh != null}");

        if (modelMesh == null)
        {
            Debug.LogError($"[BoxScene] 找不到网格模型，路径：{meshPath}");
            return;
        }

        clothesMeshModel = modelMesh.Instantiate(BoxModelParent);
        clothesMeshModel.name = "UGCModelMesh";
        clothesMeshModel.transform.localPosition = Vector3.zero;

        clothPartsMesh = clothesMeshModel.GetChildrens();
        BoxLog($"网格模型已实例化，子部件数量：{clothPartsMesh?.Count ?? 0}");
    }

    private void SetClothPartMeshShow(int partUgcType)
    {
        if (clothPartsMesh is not { Count: > 0 })
            return;

        // 盒子场景按下标高亮对应面的网格（partUgcType 从 1 起，数组从 0 起），
        // 不依赖 UgcPartData 配置表。
        int targetIndex = partUgcType - 1;
        for (int i = 0; i < clothPartsMesh.Count; i++)
        {
            clothPartsMesh[i].SetActive(i == targetIndex);
        }
    }



    #endregion

    #region 模型接收画笔更新
    private void SetPartsMats()
    {
        // 盒子场景按 Renderer 顺序映射贴图，不依赖 UgcPartData 配置表。
        // partIndex 从 1 起，与 ugcPartTextureDatas 的 key 保持一致。
        if (clothesModel == null)
            return;

        var renderers = clothesModel.GetComponentsInChildren<Renderer>(true);
        var clothesModelMaterials = new Dictionary<int, Material>();

        for (int i = 0; i < renderers.Length; i++)
        {
            int partIndex = i + 1;
            clothesModelMaterials[partIndex] = renderers[i].material;
        }

        if (ugcPartTextureDatas is { Count: > 0 })
        {
            foreach (var kv in ugcPartTextureDatas)
            {
                int partIndex = kv.Key;
                var texData = kv.Value;

                if (clothesModelMaterials.ContainsKey(partIndex))
                {
                    // 盒子场景不支持裁剪，不给 _opacity_texmask 赋值，使用材质球默认白色
                    ClothModelOnDraw(partIndex, texData.finalRT, null, clothesModelMaterials);
                }
            }
        }
    }
    private void ClothModelOnDraw(int partIndex, RenderTexture finalRt, RenderTexture filterAlphaRt, Dictionary<int, Material> clothesModelMaterials)
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

            if (filterAlphaRt != null)
            {
                clothesPartMats.SetTexture("_opacity_texmask", filterAlphaRt);
            }
        }
    }

    /// <summary>
    /// 为盒子场景模型各面分配渲染贴图。
    /// </summary>
    private void InitModelPartsMats()
    {
        SetPartsMats();
    }

    #endregion

    /// <summary>
    /// 克隆截图用模型。盒子场景无 UGCClothEditorConfig 配置表条目，不调整相机位置。
    /// </summary>
    private void InitCameraShotModel()
    {
        if (!clothesModel)
            return;

        shotModel = GameObject.Instantiate(clothesModel, screenShotNode);
        shotModel.transform.localScale = Vector3.one;
        shotModel.transform.localPosition = Vector3.zero;
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
            // 盒子场景按面数循环切换，不依赖 UgcPartData 配置表
            int faceCount = clothPartsMesh != null ? clothPartsMesh.Count : 5;
            newPartUgcType = (CurrentPartIndex % faceCount) + 1;  // 1→2→...→faceCount→1
            BoxLog($"SwitchPart: CurPart={CurrentPartIndex} → newPart={newPartUgcType}（共 {faceCount} 面）");
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
        // 右侧 UV 画布切换
        OnSelectHandle(CurrentPartIndex);
        // 高亮网格显示
        SetClothPartMeshShow(CurrentPartIndex);
        //旋转盒子模型，使当前面朝向摄像机（每面间隔 90° Y 轴）
        RotateClothModelTarget(CurrentPartIndex);
    }

    private Vector3[] PartRotateArr = new Vector3[]
    {
        new Vector3(0,0,0),
        new Vector3(0,45,0),
        new Vector3(0,-45,0),
        new Vector3(-50,0,0),
        new Vector3(50,0,0),
    };

    /// <summary>
    /// 根据部件索引旋转 ClothModelTarget，使对应面朝向摄像机。
    /// 每面间隔 90°（Y 轴），面 1 = 0°，面 2 = 90°，依此类推。
    /// </summary>
    /// <param name="partIndex">当前部件索引，从 1 起。</param>
    private void RotateClothModelTarget(int partIndex)
    {
        if (BoxModelRotateRoot == null)
            return;

        // 展示BOX模式下旋转已锁定至正面，切换编辑面时不改变旋转角度
        if (_currentBoxMode == BoxModeState.ShowAll)
            return;

        // partIndex 从 1 起，面 1 对应 0°，面 2 对应 90°，以此类推
        var targetVec3 = PartRotateArr[(partIndex - 1)];
        BoxModelRotateRoot.localEulerAngles = targetVec3;
        BoxLog($"RotateClothModelTarget: 面 {partIndex} → Y={targetVec3}°");
    }

    #endregion

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // 停止逐面重烤协程，防止销毁后仍回调
        if (_rebakeFacesCor != null)
        {
            StopCoroutine(_rebakeFacesCor);
            _rebakeFacesCor = null;
        }

        // 计算总时长（分钟）
        float totalTime = Time.time - startTime;
        float totalMinutes = totalTime / 60f;
        LoadEvent.ReportTask(154, (int)totalMinutes);

        // 销毁角色预览模型，防止 GameObject 泄漏
        if (_characterWrap != null && _characterWrap.Avatar != null)
        {
            Destroy(_characterWrap.Avatar.gameObject);
            _characterWrap = null;
        }

        if (AvatarCamera != null)
        {
            GlobalCameraManager.Inst.Remove(AvatarCamera);
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

    #region CabinBoxScene 网络接入辅助

    /// <summary>
    /// 根据当前 COS 上传完成后的 URL 构建或更新 CharacterBoxInfo。
    /// BaseDraftInfo.RefreshUploadInfo 在上传完毕后会自动将 URL 写入
    /// currentBoxSceneInfo.cover 与 currentBoxSceneInfo.metaDataUrl，
    /// 因此本方法需在 UploadAndSave 回调内调用。
    /// </summary>
    /// <returns>可直接传给 CabinBoxSceneNetManager.SetCharacterBox 的数据对象</returns>
    private CharacterBoxInfo BuildOrUpdateCharacterBoxInfo()
    {
        var info = _currentCharacterBoxInfo ?? new CharacterBoxInfo();

        // 同步封面 URL（COS 上传后已写入 currentBoxSceneInfo.cover）
        if (!string.IsNullOrEmpty(currentBoxSceneInfo.cover))
        {
            info.cover = currentBoxSceneInfo.cover;
        }

        // 同步元数据 URL（设计 JSON 的 COS 地址，写入 currentBoxSceneInfo.metaDataUrl）
        if (!string.IsNullOrEmpty(currentBoxSceneInfo.metaDataUrl))
        {
            info.metaDataUrl = currentBoxSceneInfo.metaDataUrl;
        }

        // 新建场景时设置默认名称（服务端要求 name 不为空）
        if (string.IsNullOrEmpty(info.name))
        {
            info.name = "我的盒子场景";
        }

        // 填充模板 ID
        if (string.IsNullOrEmpty(info.templateId))
        {
            info.templateId = currentBoxSceneInfo.templateId;
        }

        return info;
    }

    /// <summary>
    /// 盒子场景发布成功后的统一退出逻辑：退出游戏场景，打开工作室主面板并默认展示已发布页签。
    /// </summary>
    private void OnBoxScenePublishSuccess(BoxSceneStudioType boxSceneStudioType)
    {
        GameController.ExitGame(() =>
        {
            // 发布成功后打开工作室主面板，并直接跳转到已发布页签
            UIManager.Inst.OpenPanel(PanelId.BoxSceneStudioMainPanel, boxSceneStudioType);
            MessageHelper.Broadcast(DraftMessage.RefreshDraft);
        });
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
            Debug.LogError("[UGCBoxSceneEditorPanel] 编辑器工具未初始化");
            return;
        }

        // 强制切换到油漆桶工具
        editorTool.SwitchToBucketTool();

        Debug.Log("[UGCBoxSceneEditorPanel] 已强制切换到油漆桶工具");
    }

    #endregion
}
