using BUD.AnimPose;
using Es;
using Game.Avatar;
using Game.COSXML;
using Game.Store;
using GameData;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 草稿箱主面板，展示用户所有角色草稿列表，支持选中、操作菜单、编辑跳转
    /// Date:26-03-31 15:43:23
    /// </summary>
    public class IncubationCabinDraftBox : BasePanel<IncubationCabinDraftBox>
    {
        [SerializeField] private CButton Btn_Back;              // 返回按钮
        [SerializeField] private CButton testBtn_Creat;             // 新建草稿按钮（测试入口）

        [SerializeField] private DraftBoxMenu DraftBoxEdit;        // 操作菜单（草稿/已发布状态均使用）

        [Header("筛选 Toggle")]
        [SerializeField] private Toggle Tg_All;           // 全部
        [SerializeField] private Toggle Tg_Draft;         // 草稿（ugcclass==1）
        [SerializeField] private Toggle Tg_Publish;       // 已发布（ugcclass==2）
        [SerializeField] private Toggle Tg_Unpublished;   // 已下架（ugcclass==3）

        [SerializeField] private IncubationDraftBoxOSAAdapter _osaAdapter;  // OSA 列表适配器，在 Prefab 中绑定

        [Header("封面拍照（FittingRoom 编辑后自动重拍）")]
        [SerializeField] private Transform PhotoCharacterRoot;  // 角色挂载节点（与相机对齐，预制件内配置）
        [SerializeField] private Camera PhotoCamera;            // 专用拍照相机，输出到 RenderTexture

        private UGCClass _currentFilter = UGCClass.None;        // 当前选中的筛选分类（None = 全部）

        private List<CabinPublishData> _localDraftList = new();        // 已累积的分页原始数据（当前页签，逐页 AddRange）
        private CabinCharacterUgcInfo _selectedCharacterInfo;          // 当前选中角色（供拍照/皮肤变更/操作菜单使用）

        // 取消当前分页句柄的委托：切页签/关闭面板时调用，避免旧请求回调仍触发
        private Action _cancelCurrentHandle;

        private CharacterWrap _photoCharacterWrap;              // 拍照用临时角色
        private AnimIKController _photoIKController;            // 拍照角色的 IK 控制器
        private bool _isTakingPhoto = false;                    // 防重入

        public override void OnCreate()
        {
            Btn_Back.onClick.AddListener(CloseSelf);
            testBtn_Creat.onClick.AddListener(testCreatClick);

            DraftBoxEdit.InitUI();
            DraftBoxEdit.SetDraftBox(this);

            // 绑定 OSA 回调：普通卡点击 + 列表首个"新建"卡点击（原 Btn_Creat 入口移入列表第一个 Cell）
            if (_osaAdapter != null)
            {
                _osaAdapter.SetOnItemClick(OnItemClicked);
                _osaAdapter.SetOnCreateClick(BtnCreatClickOn);
            }

            Tg_All.onValueChanged.AddListener(isOn => { if (isOn) OnFilterChanged(UGCClass.None); });
            Tg_Draft.onValueChanged.AddListener(isOn => { if (isOn) OnFilterChanged(UGCClass.Draft); });
            Tg_Publish.onValueChanged.AddListener(isOn => { if (isOn) OnFilterChanged(UGCClass.Published); });
            Tg_Unpublished.onValueChanged.AddListener(isOn => { if (isOn) OnFilterChanged(UGCClass.Unpublished); });

            // 监听服务器端草稿列表变更消息，触发后重新请求草稿列表并刷新
            MessageHelper.AddListener(MessageName.OnCabinDraftListChange, OnCabinDraftListChange);
            // 细粒度角色变更消息：本地增量更新，不触发额外网络请求
            MessageHelper.AddListener<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterAdded, OnDraftCharacterAdded);
            MessageHelper.AddListener<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterEdited, OnDraftCharacterEdited);
            MessageHelper.AddListener<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterPublished, OnDraftCharacterPublished);
            MessageHelper.AddListener<string>(MessageName.OnCabinDraftCharacterDeleted, OnDraftCharacterDeleted);
            MessageHelper.AddListener<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterUnPublished, OnDraftCharacterUnPublished);
            // 监听 IncubationCabinPanel 皮肤切换，关闭时若皮肤变更则重拍封面
            // 注意：FittingRoom 内编辑皮肤时的重拍逻辑已移至 IncubationCabinPanel 内处理，
            // 避免此处无法区分本体/扩展包皮肤变更而误触发本体封面更新
            MessageHelper.AddListener(MessageName.OnCabinSkinChanged, OnCabinSkinChanged);
        }

        #region 按钮点击事件
        /// <summary>新建草稿：本地构造 CabinPublishData，打开 IncubationCabinPanel 设置角色属性</summary>
        private void BtnCreatClickOn()
        {
            ScreenOrientationHelper.Inst.Switch(
                ScreenOrientation.Portrait,
                onComplete: () =>
                {
                    var panel = UIManager.Inst.OpenPanel<AICompanionChatPanel>(PanelId.AICompanionChatPanel, gameObject);
                    panel.BeginCreateRoleChat();
                },
                onPreRotate: () => gameObject.SetActive(false));
        }

        void testCreatClick()
        {
            CreateRole(new()
            {
                characterName = "",
                characterDesc = ""
            });
        }

        /// <param name="avatarJson">可选。指定初始皮肤的 avatarJson；为 null 时使用当前登录角色形象。</param>
        public void CreateRole(CabinChatCreateBotProfileData botProfileData, string avatarJson = null)
        {
            var defConfigList = DataTables.GetCabinDefCharacterConfigList();
            var defConfig = defConfigList != null && defConfigList.Count > 0 ? defConfigList[0] : null;

            var toneList = CabinToneNetManager.Inst.GetPgcToneInfo();
            string toneId = defConfig.tokenID;
            if (botProfileData != null && !string.IsNullOrEmpty(botProfileData.toneId))
            {
                toneId = botProfileData.toneId;
            }

            string skinAvatarJson = !string.IsNullOrEmpty(avatarJson)
                ? avatarJson
                : CharacterData.SerializeObject(AccountDataManager.Inst.UserInfo.avatarInfo as CharacterData);

            var characterInfo = new CabinCharacterUgcInfo()
            {
                pubName = botProfileData.name,
                pubDesc = botProfileData.characterDesc,
                skinPack = new List<SkinPackInfo>()
                {
                    new SkinPackInfo()
                    {
                        packId = "",
                        avatarJson = skinAvatarJson,
                        isDefault = 1,
                        cover = "",
                    }
                },
                pendingEmote = CabinTools.BuildDefaultPendingEmote(defConfig),
                activation = new List<characterInteraction>(),
                voiceCommands = new List<voiceCommands>(),
                toneId = toneId,
                coverInfo = CabinTools.BuildDefaultCoverInfo(defConfig),
                botProfile = JsonConvert.SerializeObject(botProfileData)
            };
            characterInfo.name = botProfileData.name;

            ApplyDefaultDataAndOpenPanel(characterInfo, defConfig, toneId);
        }

        private void ApplyDefaultDataAndOpenPanel(CabinCharacterUgcInfo characterInfo,
            Es.CabinDefCharacterConfig defConfig, string toneId)
        {
            var activationIds = CabinTools.SplitIds(defConfig?.ActiveInfo);
            var commandIds = CabinTools.SplitIds(defConfig?.CommandInfo);
            // 1 个截图任务 + 唤醒条数 + 口令条数
            int pending = 1 + activationIds.Count + commandIds.Count;
            void TryOpen()
            {
                if (--pending <= 0)
                {
                    UIManager.Inst.OpenPanel<IncubationCabinPanel>(
                        PanelId.IncubationCabinPanel, characterInfo, CabinEntryType.Create, characterInfo.toneId);
                }
            }

            // 唤醒动作（每个 ID 各发一次请求）
            foreach (var id in activationIds)
            {
                var activationConfig = DataTables.GetCabinDefActionConfig(id);
                if (activationConfig != null)
                {
                    CabinNetManager.Inst.AddDefActivationLocal(characterInfo, toneId, activationConfig, _ => TryOpen());
                }
                else
                {
                    TryOpen();
                }
            }

            // 口令互动（每个 ID 各发一次请求）
            foreach (var id in commandIds)
            {
                var commandConfig = DataTables.GetCabinDefActionConfig(id);
                if (commandConfig != null)
                {
                    CabinNetManager.Inst.AddDefVoiceCommandsLocal(characterInfo, toneId, commandConfig, _ => TryOpen());
                }
                else
                {
                    TryOpen();
                }
            }

            // 抓屏生成封面
            TakePhotoForNewRole(characterInfo, url =>
            {
                if (!string.IsNullOrEmpty(url))
                {
                    characterInfo.cover = url;
                    var defaultSkin = CabinTools.GetDefaultSkin(characterInfo.skinPack);
                    if (defaultSkin != null)
                    {
                        defaultSkin.cover = url;
                    }
                }
                TryOpen();
            });
        }

        public void TakePhotoForNewRole(CabinCharacterUgcInfo tempInfo, Action<string> onCompleted)
        {
            if (_isTakingPhoto)
            {
                onCompleted?.Invoke(string.Empty);
                return;
            }
            gameObject.SetActive(true);
            TakePhoto(new CabinPublishData(tempInfo), (url, setDone) =>
            {
                setDone();
                onCompleted?.Invoke(url);
            });
        }

        /// <summary>
        /// 为新建扩展包（皮肤包）应用默认动画/唤醒/口令数据，拍照后打开编辑面板。
        /// </summary>
        public void ApplyDefaultDataToExtPackAndOpenPanel(CabinCharacterPackInfo packInfo, string toneId, Action onOpened = null)
        {
            CabinTools.ApplyDefaultDataToExtPackAndOpenPanel(packInfo, toneId, TakePhotoForNewRole, onOpened);
        }

        #endregion

        public override void OnShow(params object[] args)
        {
            // 从第一页开始拉取当前页签的草稿列表（服务端真分页）
            FetchFirstPage();
        }


        public override void OnHidden()
        {
            DestroyPhotoCharacter();

            // 取消进行中的分页请求并移除上滑加载监听，防止面板关闭后回调仍触发
            _cancelCurrentHandle?.Invoke();
            _cancelCurrentHandle = null;

            if (_osaAdapter != null && _osaAdapter.PullToRefreshBehaviour != null)
            {
                _osaAdapter.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            }

            // 清空 OSA 列表，释放 Cell 数据引用
            if (_osaAdapter != null && _osaAdapter.IsInitialized)
            {
                _osaAdapter.Data.ResetItems(new List<CabinPublishData>());
            }

            _localDraftList.Clear();
            _selectedCharacterInfo = null;
        }

        protected override void OnDestroy()
        {
            // 兜底取消分页句柄，防止面板销毁后回调仍触发
            _cancelCurrentHandle?.Invoke();
            _cancelCurrentHandle = null;

            MessageHelper.RemoveListener(MessageName.OnCabinDraftListChange, OnCabinDraftListChange);
            MessageHelper.RemoveListener<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterAdded, OnDraftCharacterAdded);
            MessageHelper.RemoveListener<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterEdited, OnDraftCharacterEdited);
            MessageHelper.RemoveListener<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterPublished, OnDraftCharacterPublished);
            MessageHelper.RemoveListener<string>(MessageName.OnCabinDraftCharacterDeleted, OnDraftCharacterDeleted);
            MessageHelper.RemoveListener<CabinCharacterUgcInfo>(MessageName.OnCabinDraftCharacterUnPublished, OnDraftCharacterUnPublished);
            MessageHelper.RemoveListener(MessageName.OnCabinSkinChanged, OnCabinSkinChanged);
            DestroyPhotoCharacter();
        }

        /// <summary>草稿列表变更广播：重置分页并重新拉取当前页签第一页（全量刷新兜底）</summary>
        private void OnCabinDraftListChange()
        {
            FetchFirstPage();
        }

        /// <summary>
        /// 角色创建或复制成功：在累积列表头部插入新角色并本地刷新（不重新请求）。
        /// 渲染时按当前页签客户端再过滤一遍，非本页签的新增项不会错误显示。
        /// </summary>
        private void OnDraftCharacterAdded(CabinCharacterUgcInfo info)
        {
            _localDraftList.Insert(0, new CabinPublishData(info));
            RefreshListView(true);
        }

        /// <summary>角色属性编辑成功：原地更新本地缓存并本地刷新</summary>
        private void OnDraftCharacterEdited(CabinCharacterUgcInfo info)
        {
            var idx = _localDraftList.FindIndex(d => d.characterInfo?.id == info.id);
            if (idx >= 0)
            {
                _localDraftList[idx] = new CabinPublishData(info);
            }
            RefreshListView(true);
        }

        /// <summary>角色上架成功：移除旧草稿条目，插入已发布条目，本地刷新</summary>
        private void OnDraftCharacterPublished(CabinCharacterUgcInfo info)
        {
            bool wasOnDraftTab = _currentFilter == UGCClass.Draft;
            _localDraftList.RemoveAll(d => d.characterInfo?.id == info.id);
            _localDraftList.Insert(0, new CabinPublishData(info));
            RefreshListView(true);

            if (wasOnDraftTab)
            {
                // 切到「已发布」页签会触发 OnFilterChanged → FetchFirstPage，从服务端重新分页拉取该页签
                Tg_Publish.isOn = true;
            }

            // 发布成功后打开角色详情面板，并在面板打开后检测角色是否在 BOX 中，
            // 若在则提示用户前往控制台同步最新配置
            CabinRolesNetManager.Inst.OpenPanelWithFreshData(
                info.stockUgcId,
                onOpened: panel => panel?.CheckIsInBoxThenDo(null)
            );
        }

        /// <summary>角色删除成功：从本地缓存移除对应条目，本地刷新</summary>
        private void OnDraftCharacterDeleted(string id)
        {
            _localDraftList.RemoveAll(d => d.characterInfo?.id == id);
            RefreshListView(true);
        }

        private void OnDraftCharacterUnPublished(CabinCharacterUgcInfo info)
        {
            var idx = _localDraftList.FindIndex(d => d.characterInfo?.id == info.id);
            if (idx >= 0)
            {
                _localDraftList[idx] = new CabinPublishData(info);
            }
            RefreshListView(true);
        }

        /// <summary>IncubationCabinPanel 切换了皮肤后关闭，重新为该草稿拍照更新封面</summary>
        private void OnCabinSkinChanged()
        {
            var characterInfo = _selectedCharacterInfo;
            if (characterInfo == null) return;

            var draftList = _localDraftList;
            if (draftList == null) return;

            var publishData = draftList.Find(d => d.characterInfo.id == characterInfo.id);
            if (publishData == null) return;

            TakePhotoForDraft(publishData);
        }

        /// <summary>Toggle 切换时更新当前筛选分类，重置分页并从服务端重新拉取该页签第一页</summary>
        private void OnFilterChanged(UGCClass filter)
        {
            if (_currentFilter == filter)
            {
                return;
            }

            _currentFilter = filter;
            FetchFirstPage();
        }

        /// <summary>
        /// 重置并从第一页开始拉取当前页签的草稿列表（服务端真分页）。
        /// 状态筛选（全部/草稿/已发布/已下架）作为 filterType 参数下发服务端，每个页签独立分页。
        /// OnShow、切页签、收到草稿列表变更广播时调用，内部会取消上一次分页句柄并清空缓存。
        /// </summary>
        private void FetchFirstPage()
        {
            // 取消上一次分页请求并移除旧的上滑加载监听，避免切页签后旧回调仍触发
            _cancelCurrentHandle?.Invoke();
            _cancelCurrentHandle = null;

            if (_osaAdapter != null && _osaAdapter.PullToRefreshBehaviour != null)
            {
                _osaAdapter.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            }

            // 清空累积缓存，从第一页重新开始
            _localDraftList.Clear();
            StartDraftPaging();
        }

        /// <summary>
        /// 将当前页签（UGCClass）映射为服务端 listType 参数。
        /// 注意：服务端取值与 UGCClass 枚举不同——0=全部 1=已发布 2=未发布(草稿) 3=已下架。
        /// </summary>
        private int GetServerListType()
        {
            switch (_currentFilter)
            {
                case UGCClass.Published:
                    return 1;   // 已发布
                case UGCClass.Draft:
                    return 2;   // 草稿即"未发布"
                case UGCClass.Unpublished:
                    return 3;   // 已下架
                default:
                    return 0;   // 全部
            }
        }

        /// <summary>
        /// 创建草稿列表分页句柄，绑定上滑加载更多到 Next()，注册成功/失败回调后从第一页开始。
        /// cookie 由句柄自动注入请求、从响应回填；listType 透传当前页签状态供服务端过滤分页。
        /// </summary>
        private void StartDraftPaging()
        {
            var req = new JObject()
            {
                ["uid"] = AccountDataManager.Inst.Uid,
                // 服务端分页过滤参数：0=全部 1=已发布 2=未发布(草稿) 3=已下架
                ["listType"] = GetServerListType(),
            };

            var handle = new HttpPageRequestHandle<CabinDraftPageData>(
                HttpUrlDefine.CabinCharacterDraftList,
                JsonConvert.SerializeObject(req),
                HttpMethod.GET);

            handle.AddSuccessAction(data => OnPageData(data?.isFirst ?? true, data?.list));
            handle.AddFailAction(OnPageFail);

            _cancelCurrentHandle = handle.Cancel;

            if (_osaAdapter != null && _osaAdapter.PullToRefreshBehaviour != null)
            {
                _osaAdapter.PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(handle.Next);
            }

            handle.Reset();
            handle.Start();
        }

        /// <summary>
        /// 分页数据成功回调。首页清空缓存，非首页仅向末尾追加，随后刷新列表视图。
        /// </summary>
        /// <param name="isFirst">是否为第一页（由 HttpPageRequestHandle 标记）</param>
        /// <param name="pageList">本页数据，可能为 null</param>
        private void OnPageData(bool isFirst, List<CabinPublishData> pageList)
        {
            if (this == null)
            {
                return;
            }

            if (_osaAdapter != null && _osaAdapter.PullToRefreshBehaviour != null)
            {
                _osaAdapter.PullToRefreshBehaviour.HideGizmo();
            }

            if (isFirst)
            {
                _localDraftList.Clear();
            }

            if (pageList != null)
            {
                _localDraftList.AddRange(pageList);
            }

            RefreshListView(isFirst);
        }

        /// <summary>
        /// 分页请求失败回调：隐藏下拉 Gizmo 并提示用户。
        /// </summary>
        private void OnPageFail(HttpResponseRawData _)
        {
            if (this == null)
            {
                return;
            }

            if (_osaAdapter != null && _osaAdapter.PullToRefreshBehaviour != null)
            {
                _osaAdapter.PullToRefreshBehaviour.HideGizmo();
            }

            TipPanel.ShowToast("获取草稿列表失败，请稍后重试");
        }

        /// <summary>
        /// 刷新列表视图：按当前页签客户端再过滤一遍（服务端已按 filterType 过滤，此处用于本地增量变更项的即时归类），
        /// 过滤掉已购买条目，按 updateTime 倒序后交给 OSA 适配器。
        /// 仅首页（isFirstPage=true）重置选中态并自动选中第一项；翻页追加时保留用户当前选中态。
        /// </summary>
        /// <param name="isFirstPage">是否为第一页刷新（true 时重置选中态、自动选中第一项）</param>
        private void RefreshListView(bool isFirstPage)
        {
            IEnumerable<CabinPublishData> source;
            switch (_currentFilter)
            {
                case UGCClass.Draft:
                    source = _localDraftList.Where(d => d.characterInfo?.ugcclass == (int)UGCClass.Draft);
                    break;
                case UGCClass.Published:
                    source = _localDraftList.Where(d => (d.characterInfo?.ugcclass == (int)UGCClass.Published && d.characterInfo?.IsUnpublished() == emUnpublished.None));
                    break;
                case UGCClass.Unpublished:
                    source = _localDraftList.Where(d => (d.characterInfo?.IsUnpublished() > emUnpublished.None));
                    break;
                default: // UGCClass.None - 全部
                    source = _localDraftList;
                    break;
            }

            // 过滤掉已购买条目，按 updateTime 倒序排列
            var dataItems = source
                .Where(d => d.characterInfo != null && d.characterInfo.ugcclass != (int)UGCClass.Buy)
                .OrderByDescending(d => d.characterInfo.updateTime)
                .ToList();

            // 列表第一个 Cell 固定为"新建"卡：用 characterInfo==null 的占位项表示，适配器识别后切到新建态
            var items = new List<CabinPublishData>(dataItems.Count + 1);
            items.Add(new CabinPublishData(null));
            items.AddRange(dataItems);

            // 仅首页重置选中态；翻页追加时保留用户已选项，避免上滑加载更多时选中被清空
            if (isFirstPage)
            {
                if (_osaAdapter != null)
                {
                    _osaAdapter.ClearSelection();
                }

                _selectedCharacterInfo = null;
            }

            if (_osaAdapter != null && _osaAdapter.IsInitialized)
            {
                _osaAdapter.Data.ResetItems(items);
            }

            // 首页：自动选中第一个真实草稿（跳过新建卡）并刷新右侧操作菜单；无草稿则清空菜单
            if (isFirstPage)
            {
                if (dataItems.Count > 0)
                {
                    var first = dataItems[0];
                    _selectedCharacterInfo = first.characterInfo;

                    if (_osaAdapter != null)
                    {
                        _osaAdapter.SetSelected(first);
                    }

                    DraftBoxEdit.UpdateInfo(first.characterInfo);
                }
                else
                {
                    DraftBoxEdit.ClearInfo();
                }
            }
        }

        /// <summary>点击 Item 回调：同步选中数据，弹出操作菜单（内部按发布状态切换按钮组）。选中视觉由 OSA 适配器处理。</summary>
        private void OnItemClicked(CabinPublishData data)
        {
            if (data?.characterInfo == null)
            {
                return;
            }

            _selectedCharacterInfo = data.characterInfo;
            DraftBoxEdit.UpdateInfo(data.characterInfo);
        }

        /// <summary>操作菜单关闭时回调：清除选中态</summary>
        private void OnDraftBoxEditHide()
        {
            if (_selectedCharacterInfo == null)
                return;

            _osaAdapter?.SetSelected(null);
            _selectedCharacterInfo = null;
        }

        /// <summary>当前是否处于「已发布」页签</summary>
        public bool IsOnPublishedTab => _currentFilter == UGCClass.Published;

        /// <summary>切换到草稿页签（供 DraftBoxMenu 复制成功后调用）</summary>
        public void SwitchToDraftTab()
        {
            Tg_Draft.isOn = true;
        }

        // ──────────────── 封面拍照 ────────────────

        /// <summary>
        /// 供 DraftBoxMenu 在 FittingRoom 编辑完成保存后调用。
        /// 用草稿现有的 pose/transform 参数重新截图上传，更新卡面立绘。
        /// </summary>
        public void TakePhotoForDraft(CabinPublishData data)
        {
            TakePhoto(data, (url, setDone) =>
            {
                if (string.IsNullOrEmpty(url))
                {
                    setDone();
                    return;
                }
                var info = data.characterInfo;
                info.cover = url;
                var defaultSkin = CabinTools.GetDefaultSkin(info.skinPack);
                if (defaultSkin != null) defaultSkin.cover = url;
                CabinNetManager.Inst.SetCabinCharacterInfo(info, SetType.Edit, (isSuccess) =>
                {
                    if (!isSuccess)
                        LoggerUtils.LogError("IncubationCabinDraftBox - 封面保存失败");
                    // 不在此处重拉列表（FetchFirstPage）：
                    // 重拉会触发 RefreshListView → Cell.SetData → RefreshCover → CoverImage.Load(url)，
                    // 而 Load() 会在网络下载期间清空当前纹理，导致封面出现"新→旧→新"的闪烁。
                    // 封面 URL 已通过 SetCabinCharacterInfo 落盘，Cell 也已通过 SetLocalCoverTexture 即时显示了新封面，
                    // 无需立即重拉列表；下次打开面板时 OnShow 会重新拉取最新数据。
                    setDone();
                });
            }, setLocalCover: true);
        }

        private void TakePhoto(CabinPublishData data, Action<string, Action> onUploaded, bool setLocalCover = false)
        {
            if (_isTakingPhoto) return;
            _isTakingPhoto = true;
            StartCoroutine(TakePhotoRoutine(data, onUploaded, setLocalCover));
        }

        private IEnumerator TakePhotoRoutine(CabinPublishData data, Action<string, Action> onUploaded, bool setLocalCover = false)
        {
            // 等模型所有部件加载完成后再应用姿势，姿势就绪后标记可以截图
            bool readyToShoot = false;
            CreatePhotoCharacter(data, () =>
            {
                ApplyPhotoInitialPose(data.characterInfo, () =>
                {
                    readyToShoot = true;
                });
            });

            // 等待模型加载 + 姿势应用均完成
            while (!readyToShoot)
            {
                yield return null;
            }

            yield return new WaitForEndOfFrame();

            var rt = PhotoCamera.targetTexture;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            // 像素已读走，禁用相机让 RT 内容冻住（不再被 clearFlags 每帧清空）
            PhotoCamera.enabled = false;

            if (setLocalCover)
            {
                // 立刻将 RT 设置到对应可见 Cell 上，后续上传期间封面持续可见（不可见则忽略，URL 已落盘）
                if (_osaAdapter != null)
                {
                    _osaAdapter.SetLocalCoverTexture(data.characterInfo.id, rt);
                }
            }

            byte[] bytes = tex.EncodeToPNG();
            Destroy(tex);

            string fileName = LocalDataUtils.Inst.SaveImgRes(bytes);
            var uri = $"IncubationCabin/characterInfo/{AccountDataManager.Inst.Uid}/{Path.GetFileName(fileName)}";

            bool done = false;
            CosXmlUploadManager.UploadFile(uri, fileName, (url, err) =>
            {
                File.Delete(fileName);
                if (!string.IsNullOrEmpty(err))
                {
                    LoggerUtils.LogError("IncubationCabinDraftBox - 封面上传失败: " + err);
                    onUploaded?.Invoke("", () => done = true);
                    return;
                }
                onUploaded?.Invoke(url, () => done = true);
            });

            while (!done) yield return null;

            DestroyPhotoCharacter();
            _isTakingPhoto = false;
        }

        /// <summary>创建拍照用角色，并应用 coverInfo 中的 transform 参数。onReady 在所有模型部件加载完成后回调</summary>
        private void CreatePhotoCharacter(CabinPublishData data, Action onReady = null)
        {
            PhotoCamera.enabled = true;
            DestroyPhotoCharacter();

            var characterData = AccountDataManager.Inst.UserInfo.avatarInfo;
            var defaultSkin = CabinTools.GetDefaultSkin(data.characterInfo.skinPack);
            if (defaultSkin != null && !string.IsNullOrEmpty(defaultSkin.avatarJson))
                characterData = CharacterData.DeserializeObject(defaultSkin.avatarJson);

            _photoCharacterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(
                characterData, PhotoCharacterRoot, callback: onReady);
            _photoIKController = _photoCharacterWrap.Avatar.GetComponent<AnimIKController>();

            // 从 coverInfo 还原缩放和位移
            var detail = data.characterInfo.coverInfo?.GetDetail();
            if (detail != null)
            {
                float scale = detail.sizeVec3.x > 0 ? detail.sizeVec3.x : 1f;
                PhotoCharacterRoot.localScale = Vector3.one * scale;
                PhotoCharacterRoot.localPosition = new Vector3(detail.posVec3.x, detail.posVec3.y, PhotoCharacterRoot.localPosition.z);
            }
        }

        /// <summary>根据 coverInfo.detail 中保存的 poseId 应用姿势到拍照角色。onComplete 在姿势就绪（或无姿势）后回调</summary>
        private void ApplyPhotoInitialPose(CabinCharacterUgcInfo info, Action onComplete = null)
        {
            if (_photoIKController == null)
            {
                onComplete?.Invoke();
                return;
            }

            var detail = info.coverInfo?.GetDetail();

            if (detail == null || string.IsNullOrEmpty(detail.poseId))
            {
                onComplete?.Invoke();
                return;
            }

            if (detail.poseResourceType == (int)ResourceType.Pose)
            {
                // PGC 官方姿势：异步加载，在回调内应用并通知完成
                FreePoseDataLoader.GetData(UgcPoseSubType.Single, (poseList) =>
                {
                    foreach (var item in poseList)
                    {
                        if (item.poseInfo.id != detail.poseId) continue;
                        ApplyPoseData(item.poseInfo.poseData);
                        onComplete?.Invoke();
                        return;
                    }
                    // 找不到对应姿势，仍需通知完成
                    onComplete?.Invoke();
                }, gameObject);
            }
            else if (detail.poseResourceType == (int)ResourceType.UgcPose)
            {
                // UGC 社区姿势：同步查找，应用后立即通知完成
                var dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
                var datas = dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)UgcPoseSubType.Single));
                foreach (var goodsData in datas)
                {
                    var asset = goodsData.GetFirstAsset<AssetsData>();
                    if (asset == null || asset.Id != detail.poseId) continue;
                    var poseInfo = asset.UgcInfo?.UgcInfo as PoseInfo;
                    if (poseInfo != null)
                    {
                        ApplyPoseData(poseInfo.poseData);
                    }
                    onComplete?.Invoke();
                    return;
                }
                // 找不到对应姿势，仍需通知完成
                onComplete?.Invoke();
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private void ApplyPoseData(string poseData)
        {
            if (_photoIKController == null || string.IsNullOrEmpty(poseData)) return;
            var keyFrameData = JsonConvert.DeserializeObject<KeyFrameData>(poseData);
            if (keyFrameData == null) return;
            _photoIKController.ChangeAnimResType(AnimResType.UGC);
            _photoIKController.SetKeyFrameData(UgcPoseSubType.Single, keyFrameData);
        }

        /// <summary>销毁拍照角色（含 AvatarNode 根节点）</summary>
        private void DestroyPhotoCharacter()
        {
            if (_photoCharacterWrap != null)
            {
                Destroy(_photoCharacterWrap.CustomAvatar);
                _photoCharacterWrap = null;
                _photoIKController = null;
            }
        }

        public override void OnWindowBeFocused() { }
        public override void OnWindowPop() { }
    }
}


