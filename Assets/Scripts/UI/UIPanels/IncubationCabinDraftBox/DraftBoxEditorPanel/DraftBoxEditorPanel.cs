using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.COSXML;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 草稿箱角色编辑器面板
    ///       - 实时预览：PhotoCamera → RenderTexture → PreviewItem RawImage，每帧自动更新，无截图开销
    ///       - 退出时：从 RenderTexture 读像素上传 COS，保存草稿后关闭
    /// Date:26-04-01
    /// </summary>
    public class DraftBoxEditorPanel : BasePanel<DraftBoxEditorPanel>
    {
        [Header("左侧卡片预览")]
        private IncubationDraftBaseItem PreviewItem;    // 左侧预览卡片，实时显示 RenderTexture
        [SerializeField] private CabinCharacterCardItem PreviewItem1;    // 左侧预览卡片，实时显示 RenderTexture
        [SerializeField] private CabinSkinCardItem PreviewItem2;    // 左侧预览卡片，实时显示 RenderTexture
        [SerializeField] private Transform CharacterRoot;               // 角色模型的挂载节点
        [SerializeField] private Camera PhotoCamera;                    // 专用拍照相机，输出到 RenderTexture（预制件上配置好，直接拖拽）

        [Header("右侧编辑")]
        [SerializeField] private DraftBoxEditorRightPanel RightPanel;   // 右侧编辑面板（角色姿势/细节/卡面）

        [Header("返回 / 保存")]
        [SerializeField] private CButton Btn_Back;                      // 返回按钮
        [SerializeField] private CButton Btn_Save;                      // 保存按钮，触发截图上传和创建/编辑接口
        [SerializeField] private Button Btn_Last;                      // 返回编辑伙伴
        [SerializeField] private Button Btn_LastEx;                      // 返回编辑伙伴
        [SerializeField] internal Text backTxt;
        [SerializeField] internal Text creatText;
        [SerializeField] internal Text cardText;
        [SerializeField] internal Text editorCharacterText;

        private CharacterWrap _characterWrap;       // 当前生成的 UI 角色包装器
        private PlayerAnimationCtrl _animCtrl;      // 角色动画控制器（PGC 动作，暂保留引用）
        private AnimIKController _ikController;     // 角色 IK 控制器，用于应用 UGC 姿势（KeyFrameData）

        private Vector3 _ikInitialLocalPos;        // 角色创建瞬间（Animator 尚未 tick）缓存的 avatar 根节点初始局部位移，用于回调里还原 Root Motion 造成的漂移
        private Quaternion _ikInitialLocalRot;     // 同上，缓存初始局部旋转
        private bool _hasIkInitialTransform;       // 是否已成功缓存初始 transform（防止回调被同步触发时读到空引用/已偏移值）
        private CabinCharacterBaseInfo _data;             // 当前编辑的草稿数据（深拷贝，所有编辑直接写入此对象）
        private string tokenID;

        private RenderTexture _renderTexture;       // 相机输出纹理，直接引用至 PreviewItem，GPU 逐帧更新
        private bool _isUploading = false;          // 防止保存按钮重复触发上传
        private bool _isModelLoaded = false;        // 角色所有部件加载完成后置为 true

        private CabinEntryType _entryType = CabinEntryType.Edit;  // 进入类型（创建/编辑）
        private string _originalDataJson;           // OnShow 时的数据快照，用于 Back 脏检测（编辑模式）
        private string _cabinOriginalDataJson;      // IncubationCabinPanel 首次打开时的原始快照，返回时透传回去保持脏检测基准
        private string _originalSkinPackJson;       // 进入编辑器前的皮肤快照，用于 OnLastClick 截图判断（皮肤变更也需更新卡面封面）

        // ──────────────── 生命周期 ────────────────

        public override void OnCreate()
        {
            Btn_Back.onClick.AddListener(OnBackClick);
            Btn_Save.onClick.AddListener(OnSaveClick);
            Btn_Last.onClick.AddListener(OnLastClick);
            Btn_LastEx.onClick.AddListener(OnLastClick);
            RightPanel.InitUI();
        }

        public override void OnShow(params object[] args)
        {
            MessageHelper.AddListener<string>(MessageName.OnCabinEditorPoseSelected, ApplyPose);
            MessageHelper.AddListener<float>(MessageName.OnCabinEditorScaleChanged, ApplyScale);
            MessageHelper.AddListener<float>(MessageName.OnCabinEditorPosXChanged, ApplyPosX);
            MessageHelper.AddListener<float>(MessageName.OnCabinEditorPosYChanged, ApplyPosY);

            if (args != null && args.Length >= 3 && args[0] is CabinCharacterBaseInfo data && args[1] is CabinEntryType entryType && args[2] is string tokenid)
            {
                // 第4个参数：IncubationCabinPanel 首次打开时的原始快照，返回时透传回去
                _cabinOriginalDataJson = (args.Length >= 4 && args[3] is string cabinOriginalJson) ? cabinOriginalJson : null;

                var dataJson = JsonConvert.SerializeObject(data);
                if (data is CabinCharacterUgcInfo)
                {
                    _data = JsonConvert.DeserializeObject<CabinCharacterUgcInfo>(dataJson);
                }
                else if (data is CabinCharacterPackInfo)
                {
                    _data = JsonConvert.DeserializeObject<CabinCharacterPackInfo>(dataJson);
                }
                else
                {
                    _data = JsonConvert.DeserializeObject<CabinCharacterBaseInfo>(dataJson);
                }
                _entryType = entryType;
                tokenID = tokenid;
                PreviewItem1.gameObject.SetActive(false);
                PreviewItem2.gameObject.SetActive(false);
                if (_data is CabinCharacterUgcInfo)
                {
                    PreviewItem = PreviewItem1;
                }
                else if (_data is CabinCharacterPackInfo)
                {
                    PreviewItem = PreviewItem2;
                }
                PreviewItem.gameObject.SetActive(true);
                PreviewItem.SetData(_data);
                RightPanel.SetData(_data);

                // 记录原始数据快照，供 Btn_Back 脏检测使用
                _originalDataJson = JsonConvert.SerializeObject(_data);

                // 记录皮肤快照：优先使用 IncubationCabinPanel 首次打开时的皮肤（_cabinOriginalDataJson），
                // 若不存在则以当前 _data.skinPack 作为基准（无 IncubationCabinPanel 时不检测皮肤变更）
                if (!string.IsNullOrEmpty(_cabinOriginalDataJson))
                {
                    var cabinOriginal = JsonConvert.DeserializeObject<CabinCharacterBaseInfo>(_cabinOriginalDataJson);
                    _originalSkinPackJson = JsonConvert.SerializeObject(cabinOriginal?.skinPack);
                }
                else
                {
                    _originalSkinPackJson = JsonConvert.SerializeObject(_data.skinPack);
                }

                CreateCharacter();
                ApplyInitialPose();

                bool isUgc = _data is CabinCharacterUgcInfo;

                string str = _entryType == CabinEntryType.Create ? "创建" : "编辑";
                string str1 = isUgc ? "伙伴" : "皮肤";
                backTxt.text = $"{str}{str1}";
                creatText.text = $"{str}{str1}";
                cardText.text = $"{str}卡片";
                editorCharacterText.text = $"{str}{str1}";
            }
        }

        public override void OnHidden()
        {
            MessageHelper.RemoveListener<string>(MessageName.OnCabinEditorPoseSelected, ApplyPose);
            MessageHelper.RemoveListener<float>(MessageName.OnCabinEditorScaleChanged, ApplyScale);
            MessageHelper.RemoveListener<float>(MessageName.OnCabinEditorPosXChanged, ApplyPosX);
            MessageHelper.RemoveListener<float>(MessageName.OnCabinEditorPosYChanged, ApplyPosY);

            _isUploading = false;
            DestroyAll();
        }

        protected override void OnDestroy()
        {
            DestroyAll();
        }

        // ──────────────── 角色模型 & RenderTexture ────────────────

        /// <summary>根据草稿皮肤数据创建 UI 角色，并建立 RenderTexture 实时预览链路</summary>
        private void CreateCharacter()
        {
            DestroyAll();
            _isModelLoaded = false;

            var characterData = AccountDataManager.Inst.UserInfo.avatarInfo;
            var defaultSkin = CabinTools.GetDefaultSkin(_data?.skinPack);
            if (defaultSkin != null && !string.IsNullOrEmpty(defaultSkin.avatarJson))
                characterData = CharacterData.DeserializeObject(defaultSkin.avatarJson);

            _characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(
                characterData, CharacterRoot,
                callback: () =>
                {
                    _isModelLoaded = true;
                    var animator = _ikController != null ? _ikController.GetComponent<Animator>() : null;
                    if (animator != null)
                    {
                        // 真机上模型加载回调触发前 Animator 已 tick 数帧，把骨骼从绑定姿势推到了动画中段；
                        // 直接禁用只会把骨骼冻结在被推偏的那一帧（编辑器加载快几乎无偏移，真机加载慢偏移明显）。
                        // 先 Rebind 把整套骨骼重置回初始绑定姿势，再 Update(0f) 保证当帧立即采样生效，最后禁用，
                        // 这样冻结的是初始姿势而非动画中段，使真机表现与编辑器一致。
                        animator.Rebind();
                        animator.Update(0f);
                        animator.enabled = false;
                    }

                    // Rebind 只重置骨骼姿态，不一定回退 Root Motion 累加到 avatar 根节点 Transform 上的位移/旋转，
                    // 用同步创建阶段缓存的初始值显式还原，确保角色回到最初位置（而非动画跑了几帧后的位置）
                    if (_hasIkInitialTransform && _ikController != null)
                    {
                        _ikController.transform.localPosition = _ikInitialLocalPos;
                        _ikController.transform.localRotation = _ikInitialLocalRot;
                    }
                });
            _animCtrl = _characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            _ikController = _characterWrap.Avatar.GetComponent<AnimIKController>();

            // 在同步创建阶段（此刻 Animator 还未 tick）缓存 avatar 根节点的初始局部位移/旋转，
            // 供上面的异步加载回调还原 —— 回调触发时 transform 已被 Root Motion 推偏，读不到真正的初始值
            if (_ikController != null)
            {
                _ikInitialLocalPos = _ikController.transform.localPosition;
                _ikInitialLocalRot = _ikController.transform.localRotation;
                _hasIkInitialTransform = true;
            }

            ApplyTransform();

            // 创建支持 Alpha 的 RenderTexture，相机渲染到它，RawImage 直接引用它
            // GPU 每帧自动更新，不需要任何协程或截图
            _renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            // 相机背景设为完全透明，确保截图时角色立绘无灰色底色
            PhotoCamera.clearFlags = CameraClearFlags.SolidColor;
            PhotoCamera.backgroundColor = Color.clear;
            PhotoCamera.targetTexture = _renderTexture;
            PreviewItem.SetLocalCoverTexture(_renderTexture);
        }

        /// <summary>销毁角色 GameObject 并释放 RenderTexture</summary>
        private void DestroyAll()
        {
            if (PhotoCamera != null)
                PhotoCamera.targetTexture = null;

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
            if (_characterWrap != null)
            {
                Destroy(_characterWrap.CustomAvatar);
                _characterWrap = null;
                _animCtrl = null;
                _ikController = null;
            }
        }

        // ──────────────── 姿势 & 细节 ────────────────

        /// <summary>
        /// 应用 UGC 姿势：切换 IK 模式并将 KeyFrameData 写入各关节，角色立刻摆出对应姿势
        /// poseData 为 KeyFrameData 的 JSON 字符串，来自 PoseInfo.poseData
        /// </summary>
        private void ApplyPose(string poseData)
        {
            if (_ikController == null || string.IsNullOrEmpty(poseData)) return;
            var keyFrameData = JsonConvert.DeserializeObject<KeyFrameData>(poseData);
            if (keyFrameData == null) return;
            _ikController.ChangeAnimResType(AnimResType.UGC);
            _ikController.SetKeyFrameData(UgcPoseSubType.Single, keyFrameData);
            //var animator = _ikController.GetComponent<Animator>();
            //if (animator != null) animator.enabled = true;
            _animCtrl?.PlayCurEyeAni();
        }

        /// <summary>
        /// 进入界面时根据 coverInfo.detail 中存储的 poseId/poseResourceType 查找并应用姿势，
        /// 官方姿势从 cabinofficialPose.json 缓存中同步查找，UGC 姿势从背包异步查找
        /// </summary>
        private void ApplyInitialPose()
        {
            var detail = _data.coverInfo?.GetDetail();
            if (detail == null || string.IsNullOrEmpty(detail.poseId)) return;

            if (detail.poseResourceType == (int)ResourceType.Pose)
            {
                // 官方姿势直接从静态缓存查询，由 DraftBoxCharacterPosePanel.LoadOfficialPoses() 在 SetData 阶段预先填充
                var poseData = DraftBoxCharacterPosePanel.FindOfficialPoseData(detail.poseId);

                if (!string.IsNullOrEmpty(poseData))
                {
                    ApplyPose(poseData);
                }
            }
            else if (detail.poseResourceType == (int)ResourceType.UgcPose)
            {
                // UGC 姿势（社区购买或我创作）背包缓存中 UgcInfo.UgcInfo 不保证填充，
                // 需通过 GetPoseInfo 异步获取完整 PoseInfo（与 LoadMinePoses/LoadCommunityPoses 保持一致）
                AssetsDataManager.GetPoseInfo(detail.poseId, (success, recommendItem) =>
                {
                    if (!success)
                        return;

                    var poseInfo = recommendItem?.UgcInfo as PoseInfo;

                    if (poseInfo == null)
                        return;

                    ApplyPose(poseInfo.poseData);
                });
            }
        }

        /// <summary>缩放变更：直接写入 _data.coverInfo，再同步到 CharacterRoot</summary>
        private void ApplyScale(float scale)
        {
            if (_data.coverInfo == null)
                _data.coverInfo = new CabinCoverInfo();
            var detail = _data.coverInfo.GetDetail();
            detail.sizeVec3 = new Vector3(scale, scale, scale);
            _data.coverInfo.SetDetail(detail);
            ApplyTransform();
        }

        /// <summary>横向位移变更：直接写入 _data.coverInfo，再同步到 CharacterRoot</summary>
        private void ApplyPosX(float posX)
        {
            if (_data.coverInfo == null)
                _data.coverInfo = new CabinCoverInfo();
            var detail = _data.coverInfo.GetDetail();
            detail.posVec3 = new Vector3(posX, detail.posVec3.y, 0f);
            _data.coverInfo.SetDetail(detail);
            ApplyTransform();
        }

        /// <summary>纵向位移变更：直接写入 _data.coverInfo，再同步到 CharacterRoot</summary>
        private void ApplyPosY(float posY)
        {
            if (_data.coverInfo == null)
                _data.coverInfo = new CabinCoverInfo();
            var detail = _data.coverInfo.GetDetail();
            detail.posVec3 = new Vector3(detail.posVec3.x, posY, 0f);
            _data.coverInfo.SetDetail(detail);
            ApplyTransform();
        }

        /// <summary>从 _data.coverInfo 读取最新缩放和位移，同步到 CharacterRoot</summary>
        private void ApplyTransform()
        {
            if (CharacterRoot == null)
                return;
            var detail = _data?.coverInfo?.GetDetail();
            float scale = (detail != null && detail.sizeVec3.x > 0) ? detail.sizeVec3.x : 1f;
            float posX = detail?.posVec3.x ?? 0f;
            float posY = detail?.posVec3.y ?? 0f;
            CharacterRoot.localScale = Vector3.one * scale;
            CharacterRoot.localPosition = new Vector3(posX, posY, CharacterRoot.localPosition.z);
        }

        // ──────────────── 退出：从 RenderTexture 读像素上传 ────────────────

        /// <summary>
        /// 返回按钮点击：
        /// - 创建模式：直接弹窗询问是否保存
        /// - 编辑模式：序列化比对数据，有改动才弹窗，无改动直接关闭
        /// </summary>
        private void OnBackClick()
        {
            if (_entryType == CabinEntryType.Create)
            {
                ShowSaveDialog();
            }
            else
            {
                // 所有编辑数据已实时写入 _data，直接比对 JSON 快照检测变更
                bool dataChanged = JsonConvert.SerializeObject(_data) != _originalDataJson;
                // 皮肤有变更（皮肤变更同样影响卡面外观，也需要重新截图）
                bool skinChanged = JsonConvert.SerializeObject(_data.skinPack) != _originalSkinPackJson;
                if (dataChanged || skinChanged)
                {
                    ShowSaveDialog();
                }
                else
                {
                    CloseSelf();
                }
            }
        }

        /// <summary>弹窗询问用户是否保存当前编辑，确认后触发截图上传并发送服务器请求</summary>
        private void ShowSaveDialog()
        {
            var panel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            panel.SetTextAndAction(
                "提示",
                "是否保存当前编辑？",
                "保存",
                "丢弃",
                confirmClick: () => { OnSaveClick(); },
                cancelClick: () => { CloseSelf(); }
            );
        }

        /// <summary>保存按钮点击（Create / Edit 通用）：防重入后启动截图-提交协程</summary>
        private void OnSaveClick()
        {
            if (_isUploading) return;
            if (_characterWrap == null || _renderTexture == null)
            {
                CloseSelf();
                return;
            }
            if (!_isModelLoaded)
            {
                TipPanel.ShowToast("资源正在加载，请稍后再试");
                return;
            }
            _isUploading = true;
            Btn_Save.SetClickAble(false);
            StartCoroutine(UploadAndSave());
        }

        private void OnLastClick()
        {
            if (_isUploading)
                return;

            // 卡面数据（姿势/缩放/位移/颜色）有变更
            bool dataChanged = JsonConvert.SerializeObject(_data) != _originalDataJson;
            // 皮肤有变更（皮肤变更同样影响卡面外观，也需要重新截图）
            bool skinChanged = JsonConvert.SerializeObject(_data.skinPack) != _originalSkinPackJson;

            if ((dataChanged || skinChanged) && _renderTexture != null && _isModelLoaded)
            {
                _isUploading = true;
                StartCoroutine(UploadCoverAndGoBack());
            }
            else
            {
                NavigateToCompanionEditor();
            }
        }

        /// <summary>
        /// 卡面数据（姿势/缩放/位移/颜色）有变更时，通过工具类从 RenderTexture 截图上传 COS，
        /// 用新封面 URL 更新 _data.cover，完成后返回编辑伙伴界面
        /// </summary>
        private IEnumerator UploadCoverAndGoBack()
        {
            string uploadedUrl = null;

            // 调用工具类完成截图 + 上传，内部包含 WaitForEndOfFrame
            yield return StartCoroutine(CabinCoverPhotoHelper.UploadFromRenderTexture(_renderTexture, url =>
            {
                uploadedUrl = url;
            }));

            if (!string.IsNullOrEmpty(uploadedUrl))
            {
                // 将新封面 URL 写入 _data，返回 IncubationCabinPanel 后立即生效
                _data.cover = uploadedUrl;
                var defaultSkin = CabinTools.GetDefaultSkin(_data.skinPack);
                if (defaultSkin != null)
                    defaultSkin.cover = uploadedUrl;
            }
            else
            {
                LoggerUtils.LogError("DraftBoxEditorPanel - 返回时封面上传失败");
            }

            _isUploading = false;
            NavigateToCompanionEditor();
        }

        /// <summary>关闭卡面编辑器，跳转回编辑伙伴界面，并透传原始快照保持脏检测基准</summary>
        private void NavigateToCompanionEditor()
        {
            CloseSelf();
            UIManager.Inst.OpenPanel<IncubationCabinPanel>(PanelId.IncubationCabinPanel, _data, _entryType, tokenID, _cabinOriginalDataJson);
        }

        /// <summary>截图上传协程，完成后根据进入类型调用创建或编辑接口，完成后关闭面板</summary>
        private IEnumerator UploadAndSave()
        {
            string uploadedUrl = null;

            // 调用工具类完成截图 + 上传，内部包含 WaitForEndOfFrame
            yield return StartCoroutine(CabinCoverPhotoHelper.UploadFromRenderTexture(_renderTexture, url =>
            {
                uploadedUrl = url;
            }));

            if (string.IsNullOrEmpty(uploadedUrl))
            {
                // 封面上传失败，直接关闭
                LoggerUtils.LogError("DraftBoxEditorPanel - 封面上传失败，保存中止");
                CloseSelf();
                yield break;
            }

            // 封面上传成功，调用服务器接口保存草稿数据
            bool serverDone = false;
            OnSaveUploadSuccess(uploadedUrl, () => serverDone = true);
            while (!serverDone)
            {
                yield return null;
            }

            if (_entryType == CabinEntryType.Create && _data is CabinCharacterUgcInfo)
            {
                ShowSaveSuccessDialog();
            }
            else
            {
                CloseSelf();
            }
        }

        /// <summary>
        /// 封面上传成功：序列化 coverInfo、设置封面 URL，
        /// 按进入类型调用创建（SetType.Create）或编辑（SetType.Edit）接口。
        /// 创建成功后额外关闭 IncubationCabinPanel 并广播列表刷新。
        /// </summary>
        private void OnSaveUploadSuccess(string url, System.Action onComplete)
        {
            // 所有编辑数据（姿势/缩放/位移/颜色）已在操作时直接写入 _data，无需在此处手动合并
            if (_data.coverInfo == null)
                _data.coverInfo = new CabinCoverInfo();

            var info = _data;
            info.cover = url;
            var defaultSkin = CabinTools.GetDefaultSkin(info.skinPack);
            if (defaultSkin != null) defaultSkin.cover = url;

            if (_entryType == CabinEntryType.Create)
            {
                if (info is CabinCharacterUgcInfo)
                {
                    CabinNetManager.Inst.SetCabinCharacterInfo((CabinCharacterUgcInfo)info, SetType.Create, (isSuccess, returnedData) =>
                    {
                        if (isSuccess)
                        {
                            _data = returnedData;
                            MessageHelper.Broadcast(MessageName.OnCabinDraftListChange);
                        }
                        else
                        {
                            LoggerUtils.LogError("DraftBoxEditorPanel - 创建草稿失败");
                        }
                        UIManager.Inst.ClosePanel(PanelId.IncubationCabinPanel);
                        onComplete?.Invoke();
                    });
                }
                else if (info is CabinCharacterPackInfo)
                {
                    CabinNetManager.Inst.SetCabinCharacterPackInfo((CabinCharacterPackInfo)info, SetType.Create, (isSuccess, data) =>
                    {
                        UIManager.Inst.ClosePanel(PanelId.IncubationCabinPanel);
                        onComplete?.Invoke();
                    });
                }

            }
            else
            {
                if (info is CabinCharacterUgcInfo)
                {
                    CabinNetManager.Inst.SetCabinCharacterInfo((CabinCharacterUgcInfo)info, SetType.Edit, (isSuccess, returnedData) =>
                    {
                        if (!isSuccess)
                            LoggerUtils.LogError("DraftBoxEditorPanel - 保存草稿失败");
                        else
                        {
                            _data = returnedData;
                            MessageHelper.Broadcast(MessageName.OnCabinCharacterUpdated, info.id, CabinCharacterUpdateType.All);
                        }
                        onComplete?.Invoke();
                    });
                }
                else if (info is CabinCharacterPackInfo)
                {
                    CabinNetManager.Inst.SetCabinCharacterPackInfo((CabinCharacterPackInfo)info, SetType.Edit, (isSuccess, data) =>
                    {
                        if (!isSuccess)
                            LoggerUtils.LogError("DraftBoxEditorPanel - 保存草稿失败");
                        else
                            MessageHelper.Broadcast(MessageName.OnCabinCharacterUpdated, info.id, CabinCharacterUpdateType.All);
                        onComplete?.Invoke();
                    });
                }

            }
        }

        /// <summary>
        /// 保存成功后弹窗：提示用户选择返回草稿箱或前往创建皮肤
        /// </summary>
        private void ShowSaveSuccessDialog()
        {
            var panel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            panel.SetText("保存成功", "是否前往创建皮肤？", "前往创建皮肤", "返回草稿箱");
            panel.SetOnClickAction(cancelClick: () => { CloseSelf(); });
            panel.SetOnCloseAction(() => { CloseSelf(); });
            panel.SetAsyncConfirmAction(done => OnGoToCreateExtPack(done));
        }

        /// <summary>
        /// 关闭当前面板，以 Btn_CreateExtPack 等效逻辑创建新皮肤包并打开 IncubationCabinPanel
        /// </summary>
        private void OnGoToCreateExtPack(Action onDone = null)
        {
            if (!(_data is CabinCharacterUgcInfo))
            {
                onDone?.Invoke();
                return;
            }

            var defConfigList = DataTables.GetCabinDefCharacterConfigList();
            var defConfig = defConfigList != null && defConfigList.Count > 0 ? defConfigList[0] : null;

            string characterId = _data is CabinCharacterPackInfo packData ? packData.characterId : _data.id;

            var packInfo = new CabinCharacterPackInfo
            {
                characterId = characterId,
                name = _data.name,
            };
            packInfo.skinPack = new System.Collections.Generic.List<SkinPackInfo>()
            {
                new SkinPackInfo()
                {
                    packId = "",
                    avatarJson = CharacterData.SerializeObject(AccountDataManager.Inst.UserInfo.avatarInfo as CharacterData),
                    isDefault = 1,
                    cover = "",
                }
            };
            packInfo.coverInfo = CabinTools.BuildDefaultCoverInfo(defConfig);

            // 尝试获取 IncubationCabinDraftBox 的拍照委托，使新皮肤包有默认封面
            UIManager.Inst.TryFindPanel<IncubationCabinDraftBox>(PanelId.IncubationCabinDraftBox, out var draftBox);
            System.Action<CabinCharacterUgcInfo, System.Action<string>> photoAction =
                draftBox != null ? draftBox.TakePhotoForNewRole : null;

            CloseSelf();
            CabinTools.ApplyDefaultDataToExtPackAndOpenPanel(packInfo, tokenID, photoAction, onDone);
        }

        public override void OnWindowBeFocused() { }
        public override void OnWindowPop() { }
    }
}

