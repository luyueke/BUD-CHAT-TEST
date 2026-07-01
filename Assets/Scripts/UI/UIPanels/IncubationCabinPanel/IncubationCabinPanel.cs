using BUD.AnimPose;
using Game.Avatar;
using Game.COSXML;
using Game.MusicalInstrument;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    partial class IncubationCabinPanel : BasePanel<IncubationCabinPanel>
    {
        // 官方姿势配置文件路径，与 DraftBoxCharacterPosePanel.OfficialPoseConfigPath 保持一致
        private const string OfficialPoseConfigPath = "Assets/Arts/Config/CabinPoseConfig/cabinofficialPose.json";

        [SerializeField] internal GoToggle[] leftToggles;
        [SerializeField] internal Button changePreviewBtn;
        [SerializeField] internal Button editskinBtn;
        [SerializeField] internal Button backBtn;
        [SerializeField] internal Button nextBtn;
        [SerializeField] internal Button nextExBtn;
        [SerializeField] internal Text backTxt;
        [SerializeField] internal Text creatText;
        [SerializeField] internal Text cardText;
        [SerializeField] internal Text editorCardText;

        [Header("人物形象")]
        [SerializeField] internal Transform characterRoot;
        [SerializeField] internal AvatarCameraController avatarCameraController;

        [SerializeField] internal GameObject skinContentGo;
        [SerializeField] internal GameObject baseMsgContentGo;
        [SerializeField] internal GameObject actionContentGo;
        [SerializeField] internal GameObject voiceContentGo;
        [SerializeField] internal GameObject interactContentGo;
        [SerializeField] internal GameObject previewGo;
        [SerializeField] internal GameObject SkinNameRoot;
        [SerializeField] internal Text SkinName;
        [SerializeField] internal Button RestNameBtn;

        [Header("Tab 节点组件")]
        [SerializeField] internal IncubationCabinSkinNode skinNode;
        [SerializeField] internal IncubationCabinBaseMsgNode baseMsgNode;
        [SerializeField] internal IncubationCabinActionNode actionNode;
        [SerializeField] internal IncubationCabinVoiceNode voiceNode;
        [SerializeField] internal IncubationCabinInteractNode interactNode;

        public CabinCharacterBaseInfo info { get; private set; }
        public string _tokenID { get; private set; }
        private CabinEntryType _entryType = CabinEntryType.Edit;
        private string _originalDataJson;
        private bool _isSaving = false;

        // OnCabinRefreshSkinRoleList 的包装委托，用于 Add/RemoveListener 保持同一引用
        private bool _isModelLoaded = false;
        private BudTimer _previewDelayTimer;
        // 动画播完但语音仍在播放时，等待语音结束的协程
        private Coroutine _audioWaitCoroutine;
        private CabinStandbyAnimController _standbyAnimCtrl = new CabinStandbyAnimController();
        public CabinStandbyAnimController StandbyAnimCtrl => _standbyAnimCtrl;

        internal PlayerAnimationCtrl animationCtrl;
        internal AnimIKController animationCtrlIK;
        internal PlayerHoldBehaviour playerHold;
        internal PlayMusicScoreBev playMusicScoreBev;
        internal CharacterWrap characterWrapper;

        internal SkinPackInfo curSkinPack = null;
        internal string _initialSkinPackId = "";
        bool _isDefautSkinPack = true;
        public bool IsDefautSkinPack
        {
            get { return _isDefautSkinPack; }
            set
            {
                _isDefautSkinPack = value;
                //editskinBtn.gameObject.SetActive(!value);
                //defaultTxtGo.gameObject.SetActive(value);
            }
        }

        public override void OnShow(params object[] args)
        {
            if (args != null && args.Length >= 3 && args[0] is CabinCharacterBaseInfo info && args[1] is CabinEntryType entryType && args[2] is string token)
            {
                this.info = info;
                _entryType = entryType;
                _tokenID = token;
                // 第4个参数：从 DraftBoxEditorPanel 返回时透传的首次打开原始快照
                // 不为空时表示是从卡面编辑器返回的，脏检测基准应保持首次打开时的状态
                _originalDataJson = (args.Length >= 4 && args[3] is string originalJson) ? originalJson : null;
            }
            InitUI();
            if (this.info == null)
            {
                return;
            }
            InitUpdateUI();
        }

        void InitUpdateUI()
        {
            // 皮肤列表刷新时：
            // 1. 始终刷新当前面板的皮肤列表显示
            // 2. 若正在编辑本体（CabinCharacterUgcInfo），使用面板自身相机重拍卡片封面并保存；
            //    若正在编辑扩展包（CabinCharacterPackInfo），不触发本体封面更新
            MessageHelper.AddListener<CharacterData>(MessageName.OnCabinUpdateAvatar, UpdateAvatar);
            MessageHelper.AddListener<Action<bool, string>>(MessageName.OnCabinTakeMatchPhoto, OnTakeMatchPhoto);
            MessageHelper.AddListener(MessageName.OnCabinRefreshBaseMsg, baseMsgNode.RefreshBaseMsg);
            MessageHelper.AddListener(MessageName.OnCabinRefreshInteractContent, interactNode.RfreshInteractContent);
            MessageHelper.AddListener(MessageName.OnCabinRefreshInteractNode2And3, interactNode.RefreshNode2And3);
            MessageHelper.AddListener<characterInteraction>(MessageName.OnCabinBeginPreviewActivation, BeginPreviewActivation);
            MessageHelper.AddListener<voiceCommands>(MessageName.OnCabinBeginPreviewVoiceCommands, BeginPreviewVoiceCommands);

            bool isUgc = info is CabinCharacterUgcInfo characterUgcInfo;
            // 序列化当前 info 用于深拷贝（包含从 DraftBoxEditorPanel 带回的所有变更）
            var currentInfoJson = JsonConvert.SerializeObject(info);
            // 脏检测基准：仅在首次打开时设置（_originalDataJson 为 null）；
            // 从 DraftBoxEditorPanel 返回时 _originalDataJson 已在 OnShow 中设置为首次快照，不覆盖
            if (string.IsNullOrEmpty(_originalDataJson))
            {
                _originalDataJson = currentInfoJson;
            }
            baseMsgContentGo.SetActive(isUgc);
            actionContentGo.SetActive(false);
            voiceContentGo.SetActive(false);
            SkinNameRoot.SetActive(!isUgc);
            interactContentGo.SetActive(!isUgc);
            string str = _entryType == CabinEntryType.Create ? "创建" : "编辑";
            if (isUgc)
            {
                info = JsonConvert.DeserializeObject<CabinCharacterUgcInfo>(currentInfoJson);
                characterUgcInfo = (CabinCharacterUgcInfo)info;
                baseMsgNode.SetLocalInfo(characterUgcInfo);
                backTxt.text = $"{str}伙伴";
                creatText.text = $"{str}伙伴";
                cardText.text = $"{str}卡片";
                editorCardText.text = $"{str}伙伴卡片";
                SkinName.text = info.name;
            }
            else
            {
                info = JsonConvert.DeserializeObject<CabinCharacterPackInfo>(currentInfoJson);
                backTxt.text = $"{str}皮肤";
                creatText.text = $"{str}皮肤";
                cardText.text = $"{str}卡片";
                editorCardText.text = $"{str}皮肤卡片";
                SkinName.text = info.name;
            }


            SelectCabinCharacterUgcInfo(info);

        }

        /// <summary>
        /// 选择养成舱角色
        /// </summary>
        public void SelectCabinCharacterUgcInfo(CabinCharacterBaseInfo cabinCharacterUgcInfo)
        {
            if (cabinCharacterUgcInfo == null)
            {
                LoggerUtils.LogError("选择养成舱角色失败:角色信息为空");
                return;
            }

            foreach (var skinPack in cabinCharacterUgcInfo.skinPack)
            {
                if (skinPack.isDefault == 1)
                {
                    curSkinPack = skinPack;
                    _initialSkinPackId = curSkinPack.packId;
                    InitCharacterWrapper(CharacterData.DeserializeObject(skinPack.avatarJson));
                    IsDefautSkinPack = true;
                    break;
                }
            }

            photoCamera = avatarCameraController.roleCamera;
            for (int i = 0; i < leftToggles.Length; i++)
            {
                var toggle = leftToggles[i];
                int idx = i;
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        baseMsgContentGo.SetActive(idx == 0);
                        interactContentGo.SetActive(idx == 1);
                        if (idx == 0)
                        {
                            baseMsgNode.RefreshBaseMsg();
                        }
                        else if (idx == 1)
                        {
                            interactNode.RefreshInteract();
                        }
                    }
                });
            }
            bool isCharacter = cabinCharacterUgcInfo is CabinCharacterUgcInfo;
            leftToggles[0].gameObject.SetActive(isCharacter);

            if (isCharacter)
            {
                leftToggles[0].isOn = true;
                baseMsgNode.RefreshBaseMsg();
            }
            else
            {
                leftToggles[1].isOn = true;
                interactNode.RefreshInteract();
            }
        }

        internal void InitCharacterWrapper(CharacterData characterData)
        {
            var saveCharacterData = characterData;
            if (characterWrapper != null)
            {
                _isModelLoaded = false;
                interactNode?.CancelAnim();
                _standbyAnimCtrl.Stop();
                characterWrapper.RefreshAvatar(characterData, () => { _isModelLoaded = true; });
                interactNode.InitPgcUgcController();
                _standbyAnimCtrl.Start(interactNode.PgcUgcController, info?.pendingEmote);
                return;
            }

            _isModelLoaded = false;
            characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(
                saveCharacterData, characterRoot,
                callback: () => { _isModelLoaded = true; });
            animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            animationCtrlIK = characterWrapper.Avatar.GetComponent<AnimIKController>();
            playerHold = characterWrapper.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
            avatarCameraController.RotateTarget = characterRoot;

            interactNode.InitPgcUgcController();
            _standbyAnimCtrl.Start(interactNode.PgcUgcController, info?.pendingEmote);
        }

        private void ApplyPose(string poseData)
        {
            if (animationCtrlIK == null || string.IsNullOrEmpty(poseData))
                return;

            var keyFrameData = JsonConvert.DeserializeObject<KeyFrameData>(poseData);

            if (keyFrameData == null)
                return;

            animationCtrlIK.ChangeAnimResType(AnimResType.UGC);
            animationCtrlIK.SetKeyFrameData(UgcPoseSubType.Single, keyFrameData);
        }

        void InitUI()
        {
            editskinBtn.onClick.AddListener(OnEditBtnClick);
            backBtn.onClick.AddListener(OnBackBtnClick);
            nextBtn.onClick.AddListener(OnNextBtnClick);
            nextExBtn.onClick.AddListener(OnNextBtnClick);
            RestNameBtn.onClick.AddListener(OnRestNameBtnClick);
            changePreviewBtn.gameObject.SetActive(false);
            changePreviewBtn.onClick.AddListener(OnChangePreviewBtnClick);

            skinNode.Init(this);
            baseMsgNode.Init(this);
            actionNode.Init(this);
            voiceNode.Init(this);
            interactNode.Init(this);

            actionNode.InitActionUI();
            voiceNode.InitVoiceUI();
        }

        public override void OnHidden()
        {
            // 停止语音、定时器和等待协程
            StopCurrentPreviewAudioAndTimers();
            // 停止当前预览动画
            interactNode.PgcUgcController.CancelAnim();
            _standbyAnimCtrl.Stop();
            base.OnHidden();
        }

        public override void CloseSelf()
        {
            // 停止语音、定时器和等待协程
            StopCurrentPreviewAudioAndTimers();
            // 停止当前预览动画
            interactNode.PgcUgcController.CancelAnim();
            _standbyAnimCtrl.Stop();
            MessageHelper.RemoveListener<CharacterData>(MessageName.OnCabinUpdateAvatar, UpdateAvatar);
            MessageHelper.RemoveListener<Action<bool, string>>(MessageName.OnCabinTakeMatchPhoto, OnTakeMatchPhoto);
            MessageHelper.RemoveListener(MessageName.OnCabinRefreshBaseMsg, baseMsgNode.RefreshBaseMsg);
            MessageHelper.RemoveListener(MessageName.OnCabinRefreshInteractContent, interactNode.RfreshInteractContent);
            MessageHelper.RemoveListener(MessageName.OnCabinRefreshInteractNode2And3, interactNode.RefreshNode2And3);
            MessageHelper.RemoveListener<characterInteraction>(MessageName.OnCabinBeginPreviewActivation, BeginPreviewActivation);
            MessageHelper.RemoveListener<voiceCommands>(MessageName.OnCabinBeginPreviewVoiceCommands, BeginPreviewVoiceCommands);
            base.CloseSelf();
        }

        void OnBackBtnClick()
        {
            if (_entryType == CabinEntryType.Create)
            {
                ShowSaveDialog();
            }
            else
            {
                var currentJson = JsonConvert.SerializeObject(info);
                if (currentJson != _originalDataJson)
                {
                    ShowSaveDialog();
                }
                else
                {
                    CloseSelf();
                }
            }
        }

        /// <summary>弹窗询问用户是否保存当前编辑，确认后截图上传并发送服务器请求</summary>
        private void ShowSaveDialog()
        {
            var panel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            panel.SetTextAndAction(
                "提示",
                "是否保存当前编辑？",
                "保存",
                "丢弃",
                confirmClick: () => { StartSaveToServer(); },
                cancelClick: () => { CloseSelf(); }
            );
        }

        private void StartSaveToServer()
        {
            if (_isSaving) return;
            if (!_isModelLoaded)
            {
                TipPanel.ShowToast("资源正在加载，请稍后再试");
                return;
            }
            _isSaving = true;
            StartCoroutine(SaveToServerCoroutine());
        }

        private IEnumerator SaveToServerCoroutine()
        {
            // 若皮肤有变更（avatarJson / packId 等发生改变），需重新截图更新卡面封面
            if (!string.IsNullOrEmpty(_originalDataJson))
            {
                var originalInfo = JsonConvert.DeserializeObject<CabinCharacterBaseInfo>(_originalDataJson);
                bool skinChanged = JsonConvert.SerializeObject(info.skinPack) != JsonConvert.SerializeObject(originalInfo?.skinPack);
                bool poseChanged = JsonConvert.SerializeObject(info.coverInfo) != JsonConvert.SerializeObject(originalInfo?.coverInfo);

                if (skinChanged || poseChanged)
                {
                    string photoUrl = null;
                    yield return StartCoroutine(TakeMatchPhoto((success, url) =>
                    {
                        if (success)
                        {
                            photoUrl = url;
                        }
                    }));

                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        // 将新封面 URL 写入 info 及默认皮肤，随后一并提交到服务器
                        info.cover = photoUrl;
                        var defaultSkin = CabinTools.GetDefaultSkin(info.skinPack);
                        if (defaultSkin != null)
                        {
                            defaultSkin.cover = photoUrl;
                        }
                    }
                }
            }

            var setType = _entryType == CabinEntryType.Create ? SetType.Create : SetType.Edit;
            bool done = false;

            if (info is CabinCharacterUgcInfo ugcInfo)
            {
                CabinNetManager.Inst.SetCabinCharacterInfo(ugcInfo, setType, (isSuccess) =>
                {
                    if (isSuccess)
                    {
                        if (_entryType == CabinEntryType.Create)
                        {
                            MessageHelper.Broadcast(MessageName.OnCabinDraftListChange);
                        }
                        else
                        {
                            MessageHelper.Broadcast(MessageName.OnCabinCharacterUpdated, ugcInfo.id, CabinCharacterUpdateType.All);
                        }
                    }
                    else
                    {
                        LoggerUtils.LogError("IncubationCabinPanel - 保存失败");
                    }
                    done = true;
                });
            }
            else if (info is CabinCharacterPackInfo packInfo)
            {
                CabinNetManager.Inst.SetCabinCharacterPackInfo(packInfo, setType, (isSuccess, data) =>
                {
                    if (isSuccess)
                    {
                        if (_entryType == CabinEntryType.Create)
                        {
                            MessageHelper.Broadcast(MessageName.OnCabinDraftListChange);
                        }
                        else
                        {
                            MessageHelper.Broadcast(MessageName.OnCabinCharacterUpdated, packInfo.id, CabinCharacterUpdateType.All);
                        }
                    }
                    else
                    {
                        LoggerUtils.LogError("IncubationCabinPanel - 保存失败");
                    }
                    done = true;
                });
            }

            while (!done) yield return null;
            CloseSelf();
        }

        void OnNextBtnClick()
        {
            if (info == null)
            {
                return;
            }
            // 将本面板的原始快照作为第4个参数传递，DraftBoxEditorPanel 返回时会透传回来，
            // 保证 IncubationCabinPanel 的脏检测基准始终是会话最开始的初始状态
            UIManager.Inst.OpenPanel<DraftBoxEditorPanel>(PanelId.DraftBoxEditorPanel, info, _entryType, _tokenID, _originalDataJson);
            CloseSelf();
        }

        void OnRestNameBtnClick()
        {
            var pop = UIManager.Inst.OpenPanel<IncubationReNamePop>(PanelId.IncubationReNamePop);
            pop.SetData(
                onConfirm: (newName) =>
                {
                    info.name = newName;
                    SkinName.text = newName;
                },
                currentName: info.name,
                placeholder: "输入你的AI伙伴名字",
                title: "输入名字"
            );
        }

        void OnEditBtnClick()
        {
            if (curSkinPack == null)
            {
                return;
            }
            FittingRoomPanel panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel, "IncubationCabin", CharacterData.DeserializeObject(curSkinPack.avatarJson));
            panel.OnCloseAction = (characterData) =>
            {
                if (curSkinPack == null)
                {
                    return;
                }
                CabinNetManager.Inst.SkinEditEndLocal(info, curSkinPack.packId, characterData,
                CharacterData.SerializeObject(characterData) != curSkinPack.avatarJson);
            };
        }


        void OnChangePreviewBtnClick()
        {
            CabinNetManager.Inst.SetPreviewShowRoleWithCabin(!CabinNetManager.Inst.IsPreviewShowRoleWithCabin);
        }

        private void OnTakeMatchPhoto(Action<bool, string> cb)
        {
            if (this == null)
            {
                return;
            }
            StartCoroutine(TakeMatchPhoto(cb));
        }

        public void UpdateAvatar(CharacterData characterData)
        {
            InitCharacterWrapper(characterData);
        }

        Camera photoCamera;

        #region 上传cover
        /// <summary>
        /// 皮肤编辑完成后（OnCabinRefreshSkinRoleList），重拍卡片封面并通过 SetType.Edit 保存到服务器。
        /// 仅在编辑本体时调用，扩展包皮肤变更不走此路径。
        /// </summary>
        private IEnumerator TakeMatchPhotoAndSaveCover()
        {
            string photoUrl = null;
            yield return StartCoroutine(TakeMatchPhoto((success, url) =>
            {
                if (success)
                {
                    photoUrl = url;
                }
            }));

            if (string.IsNullOrEmpty(photoUrl))
                yield break;

            info.cover = photoUrl;
            var defaultSkin = CabinTools.GetDefaultSkin(info.skinPack);
            if (defaultSkin != null)
            {
                defaultSkin.cover = photoUrl;
            }

            CabinNetManager.Inst.SetCabinCharacterInfo((CabinCharacterUgcInfo)info, SetType.Edit, (isSuccess) =>
            {
                if (!isSuccess)
                {
                    LoggerUtils.LogError("[IncubationCabinPanel] 皮肤更新后卡片封面保存失败");
                }
            });
        }

        /// <summary>
        /// 拍照前置处理：应用 coverInfo 的位置/缩放、暂停待机动画、清理道具特效、
        /// 设置 T 姿势并应用 coverInfo 姿势数据。
        /// 供 TakeMatchPhoto（yield return）和 ApplyPhotoParams（StartCoroutine）共用，
        /// 确保两处行为完全一致，只需维护一段逻辑。
        /// </summary>
        private IEnumerator PrepareCharacterForPhoto()
        {
            var coverDetail = info?.coverInfo?.GetDetail();

            // ── 1. 临时应用 coverInfo 的位置 / 缩放 ──────────────────────────
            if (coverDetail != null)
            {
                float scale = coverDetail.sizeVec3.x > 0 ? coverDetail.sizeVec3.x : 1f;
                characterRoot.localScale = Vector3.one * scale;
                characterRoot.localPosition = new Vector3(
                    coverDetail.posVec3.x,
                    coverDetail.posVec3.y,
                    characterRoot.localPosition.z);
            }

            // 角色统一朝向相机（绕 Y 轴旋转 180°）
            characterRoot.localEulerAngles = new Vector3(0f, 180f, 0f);

            // ── 2. 清理当前动作道具特效，还原到待机状态 ─────────────────────
            interactNode.PgcUgcController.PlayLeisureIdle();

            // ── 3. 暂停待机动画循环，防止拍照期间定时器触发打断 ─────────────
            animationCtrl.GetComponent<Animator>().enabled = false;
            _standbyAnimCtrl.Stop();


            // ── 4. 设置 T 姿势，并显式开启 FullBodyBipedIK / LookAtIK / 控制器
            if (animationCtrlIK != null)
            {
                animationCtrlIK.ChangeAnimResType(AnimResType.UGC);
                animationCtrlIK.ResetJointNodes();
                animationCtrlIK.EnableIKForPhoto();
            }

            // ── 5. 应用 coverInfo 的姿势数据 ─────────────────────────────────
            if (coverDetail != null && !string.IsNullOrEmpty(coverDetail.poseId))
            {
                if (coverDetail.poseResourceType == (int)ResourceType.Pose)
                {
                    // 官方姿势：从 cabinofficialPose.json 配置中同步查找
                    // （与 DraftBoxCharacterPosePanel.LoadOfficialPoses 取数据方式保持一致）
                    var poseData = FindOfficialPoseData(coverDetail.poseId);
                    if (!string.IsNullOrEmpty(poseData))
                    {
                        ApplyPose(poseData);
                    }
                }
                else if (coverDetail.poseResourceType == (int)ResourceType.UgcPose)
                {
                    var dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
                    var datas = dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.Single));
                    foreach (var goodsData in datas)
                    {
                        var asset = goodsData.GetFirstAsset<AssetsData>();

                        if (asset == null || asset.Id != coverDetail.poseId)
                            continue;

                        var poseInfo = asset.UgcInfo?.UgcInfo as PoseInfo;
                        if (poseInfo != null)
                        {
                            ApplyPose(poseInfo.poseData);
                        }
                        break;
                    }
                }
            }

            // 等待一帧，确保姿势 / IK 写入骨骼后再交由相机截图
            yield return null;
        }

        /// <summary>
        /// 从本地官方姿势配置 cabinofficialPose.json 中按 poseId 查找 KeyFrameData JSON。
        /// 取数据方式与 DraftBoxCharacterPosePanel.LoadOfficialPoses 一致，避免依赖其静态缓存。
        /// </summary>
        /// <param name="poseId">官方姿势 ID</param>
        /// <returns>KeyFrameData JSON 字符串；未找到或读取失败时返回 null</returns>
        private string FindOfficialPoseData(string poseId)
        {
            if (string.IsNullOrEmpty(poseId))
                return null;

            var wrapper = Loader.Load<TextAsset>(OfficialPoseConfigPath);

            if (wrapper == null)
            {
                LoggerUtils.LogError("[IncubationCabinPanel] 官方姿势配置文件读取失败");
                return null;
            }

            // RetainAsset 绑定到当前 GameObject，面板销毁时自动释放资源
            var textAsset = wrapper.RetainAsset(gameObject);
            var configs = JsonConvert.DeserializeObject<List<OfficialPoseConfig>>(textAsset.text);

            if (configs == null)
                return null;

            foreach (var config in configs)
            {
                if (config.id == poseId)
                    return config.poseData?.poseData;
            }

            return null;
        }

        /// <summary>
        /// cabinofficialPose.json 单条姿势配置，仅供本面板内部查找官方姿势数据使用。
        /// </summary>
        private class OfficialPoseConfig
        {
            public string id;
            public string name;
            public int poseType;
            public PoseInfo poseData;
            public string textureUrl;
        }

        /// <summary>
        /// 调用 PrepareCharacterForPhoto 完成角色前置处理，再截图上传，完成后还原所有临时状态。
        /// </summary>
        public IEnumerator TakeMatchPhoto(Action<bool, string> callback = null)
        {
            // 先记录原始变换，PrepareCharacterForPhoto 会临时修改 characterRoot
            var origLocalScale = characterRoot.localScale;
            var origLocalPosition = characterRoot.localPosition;
            var origLocalRotation = characterRoot.localRotation;

            // ── 临时设置相机参数 ──────────────────────────────────────────────
            var origClearFlags = photoCamera.clearFlags;
            var origBackgroundColor = photoCamera.backgroundColor;
            var origTargetTexture = photoCamera.targetTexture;
            var origOrthographicSize = photoCamera.orthographicSize;

            var rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            photoCamera.clearFlags = CameraClearFlags.SolidColor;
            photoCamera.backgroundColor = new Color(1, 1, 1, 0);
            photoCamera.targetTexture = rt;
            photoCamera.orthographicSize = 1f;

            // 统一角色前置处理（与 ApplyPhotoParams 共用同一协程）
            yield return StartCoroutine(PrepareCharacterForPhoto());

            // ── 截图并上传（内部包含 WaitForEndOfFrame）────────────────────────
            string uploadedUrl = null;
            yield return StartCoroutine(CabinCoverPhotoHelper.UploadFromRenderTexture(photoCamera.targetTexture, url =>
            {
                uploadedUrl = url;
            }));

            // ── 还原相机参数 ──────────────────────────────────────────────────
            photoCamera.clearFlags = origClearFlags;
            photoCamera.backgroundColor = origBackgroundColor;
            photoCamera.targetTexture = origTargetTexture;
            photoCamera.orthographicSize = origOrthographicSize;
            rt.Release();

            // ── 还原 characterRoot 变换 ───────────────────────────────────────
            characterRoot.localScale = origLocalScale;
            characterRoot.localPosition = origLocalPosition;
            characterRoot.localRotation = origLocalRotation;

            // ── 还原动画模式，恢复待机循环 ───────────────────────────────────
            if (animationCtrlIK != null)
            {
                animationCtrlIK.ChangeAnimResType(AnimResType.PGC);
            }
            _standbyAnimCtrl.Resume();

            // ── 回调 ─────────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(uploadedUrl))
            {
                callback?.Invoke(true, uploadedUrl);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        }
        #endregion

        #region 预览唤醒动作
        public void BeginPreviewActivation(characterInteraction Interaction)
        {
            // 先停止上一次预览的语音、定时器和等待协程，防止多次点击叠加播放语音
            StopCurrentPreviewAudioAndTimers();
            // 停止上一次预览动画，防止动画音效叠加
            interactNode.PgcUgcController.CancelAnim();
            _standbyAnimCtrl.Stop();
            previewGo.SetActive(true);
            previewGo.GetComponent<HideGameObjectClick>().cb = (go) =>
            {
                EndPreviewActivation();
            };

            interactNode.PgcUgcController.InitData(Interaction);
            interactNode.PgcUgcController.SetPlayEndCallback(() =>
            {
                EndPreviewActivationAnimOnly();
            });

            if (Interaction.delaySecond >= 0)
            {
                // delaySecond >= 0：立即播动画，延迟 delaySecond 秒后播语音
                // isMute == 1 时动画音效静音；语音 audioUrl 始终播放，不受 isMute 控制
                interactNode.PgcUgcController.PlayAnim(Interaction.isMute != 1);
                _previewDelayTimer = TimerManager.Inst.RunOnce("preview_activation_delay", Interaction.delaySecond, () =>
                {
                    if (!string.IsNullOrEmpty(Interaction.audioUrl))
                    {
                        var soundObj = GameObject.Find("GlobalMainCamera");
                        var ugcToneLoader = soundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
                        ugcToneLoader.LoadAudioClipAndPlay(PreviewAudioType.TwoD, Interaction.audioUrl);
                    }
                });
            }
            else
            {
                // delaySecond < 0：立即播语音，延迟 abs(delaySecond) 秒后播动画
                if (!string.IsNullOrEmpty(Interaction.audioUrl))
                {
                    var soundObj = GameObject.Find("GlobalMainCamera");
                    var ugcToneLoader = soundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
                    ugcToneLoader.LoadAudioClipAndPlay(PreviewAudioType.TwoD, Interaction.audioUrl);
                }
                _previewDelayTimer = TimerManager.Inst.RunOnce("preview_activation_delay", Mathf.Abs(Interaction.delaySecond), () =>
                {
                    // isMute == 1 时动画音效静音
                    interactNode.PgcUgcController.PlayAnim(Interaction.isMute != 1);
                });
            }
        }

        public void BeginPreviewVoiceCommands(voiceCommands voiceCommands)
        {
            // 先停止上一次预览的语音、定时器和等待协程，防止多次点击叠加播放语音
            StopCurrentPreviewAudioAndTimers();
            // 停止上一次预览动画，防止动画音效叠加
            interactNode.PgcUgcController.CancelAnim();
            _standbyAnimCtrl.Stop();
            previewGo.SetActive(true);
            previewGo.GetComponent<HideGameObjectClick>().cb = (go) =>
            {
                EndPreviewActivation();
            };

            interactNode.PgcUgcController.InitData(voiceCommands);
            interactNode.PgcUgcController.SetPlayEndCallback(() =>
            {
                EndPreviewActivationAnimOnly();
            });

            if (voiceCommands.delaySecond >= 0)
            {
                // delaySecond >= 0：立即播动画，延迟 delaySecond 秒后播语音
                // isMute == 1 时动画音效静音；语音 audioUrl 始终播放，不受 isMute 控制
                interactNode.PgcUgcController.PlayAnim(voiceCommands.isMute != 1);
                _previewDelayTimer = TimerManager.Inst.RunOnce("preview_voice_commands_delay", voiceCommands.delaySecond, () =>
                {
                    if (!string.IsNullOrEmpty(voiceCommands.audioUrl))
                    {
                        var soundObj = GameObject.Find("GlobalMainCamera");
                        var ugcToneLoader = soundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
                        ugcToneLoader.LoadAudioClipAndPlay(PreviewAudioType.TwoD, voiceCommands.audioUrl);
                    }
                });
            }
            else
            {
                // delaySecond < 0：立即播语音，延迟 abs(delaySecond) 秒后播动画
                if (!string.IsNullOrEmpty(voiceCommands.audioUrl))
                {
                    var soundObj = GameObject.Find("GlobalMainCamera");
                    var ugcToneLoader = soundObj.GetOrAddComponent<UgcToneLoaderBehaviour>();
                    ugcToneLoader.LoadAudioClipAndPlay(PreviewAudioType.TwoD, voiceCommands.audioUrl);
                }
                _previewDelayTimer = TimerManager.Inst.RunOnce("preview_voice_commands_delay", Mathf.Abs(voiceCommands.delaySecond), () =>
                {
                    // isMute == 1 时动画音效静音
                    interactNode.PgcUgcController.PlayAnim(voiceCommands.isMute != 1);
                });
            }
        }

        public void EndPreviewActivation()
        {
            StopCurrentPreviewAudioAndTimers();
            interactNode.PgcUgcController.CancelAnim();
            previewGo.SetActive(false);
            // 预览结束后恢复待机动画循环
            _standbyAnimCtrl.Resume();
        }

        private void EndPreviewActivationAnimOnly()
        {
            TimerManager.Inst.Stop(_previewDelayTimer);
            _previewDelayTimer = null;

            // 检查语音是否仍在播放
            var soundObj = GameObject.Find("GlobalMainCamera");
            var ugcToneLoader = soundObj?.GetComponent<UgcToneLoaderBehaviour>();

            if (ugcToneLoader != null && ugcToneLoader.IsAudioPlaying())
            {
                // 动画结束但语音仍在：切换到 leisure 待机，等语音结束再做完整清理
                interactNode.PgcUgcController.PlayLeisureIdle();
                _audioWaitCoroutine = StartCoroutine(WaitForAudioAndCleanup());
            }
            else
            {
                // 无语音或语音已结束：直接完整清理
                DoEndPreviewActivationCleanup();
            }
        }

        /// <summary>
        /// 停止当前预览的语音、延迟定时器和等待协程，防止多次点击叠加播放语音。
        /// 不恢复待机循环，不隐藏预览层。
        /// </summary>
        private void StopCurrentPreviewAudioAndTimers()
        {
            TimerManager.Inst.Stop(_previewDelayTimer);
            _previewDelayTimer = null;

            if (_audioWaitCoroutine != null)
            {
                StopCoroutine(_audioWaitCoroutine);
                _audioWaitCoroutine = null;
            }

            GameObject.Find("GlobalMainCamera")?.GetComponent<UgcToneLoaderBehaviour>()?.Stop();
        }

        /// <summary>
        /// 预览完全结束时的清理操作：停止动画、隐藏预览层、恢复待机循环。
        /// </summary>
        private void DoEndPreviewActivationCleanup()
        {
            interactNode.PgcUgcController.CancelAnim();
            previewGo.SetActive(false);
            // 预览结束后恢复待机动画循环
            _standbyAnimCtrl.Resume();
        }

        /// <summary>
        /// 等待语音播放完成后执行完整清理，每 0.1 秒轮询一次 AudioSource.isPlaying。
        /// </summary>
        private IEnumerator WaitForAudioAndCleanup()
        {
            var soundObj = GameObject.Find("GlobalMainCamera");
            var ugcToneLoader = soundObj?.GetComponent<UgcToneLoaderBehaviour>();

            while (ugcToneLoader != null && ugcToneLoader.IsAudioPlaying())
            {
                yield return new WaitForSeconds(0.1f);
            }

            _audioWaitCoroutine = null;
            DoEndPreviewActivationCleanup();
        }
        #endregion

        [Button("TakeMatchPhoto")]
        void TestTakeMatchPhoto()
        {
            StartCoroutine(TakeMatchPhoto());
        }

        [Button("测试")]
        void Test()
        {
            GameObject.Find("GlobalMainCamera").GetComponent<AudioSource>().Play();
            Debug.LogError(GameObject.Find("GlobalMainCamera").GetComponent<AudioSource>().loop);
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (photoCamera == null)
                return;

            if (GUI.Button(new Rect(10, 10, 220, 50), "应用截图参数到相机/角色"))
            {
                ApplyPhotoParams();
            }
        }

        /// <summary>
        /// 编辑器调试按钮：将角色和相机切换到与实际拍照完全一致的状态，方便在 Scene 视图预览效果。
        /// 与 TakeMatchPhoto 共用 PrepareCharacterForPhoto 协程，无需单独维护。
        /// </summary>
        private void ApplyPhotoParams()
        {
            // 与 TakeMatchPhoto 走同一段前置处理逻辑，保证预览效果与实际拍照一致
            StartCoroutine(PrepareCharacterForPhoto());

            // ── 相机配置（仅供编辑器预览，不还原）──────────────────────────────
            var rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            photoCamera.clearFlags = CameraClearFlags.SolidColor;
            photoCamera.backgroundColor = new Color(1, 1, 1, 0);
            photoCamera.targetTexture = rt;
            photoCamera.orthographicSize = 1f;
        }
#endif
        internal void ChangeToneID(string toneId)
        {
            _tokenID = toneId;
        }

    }
}
