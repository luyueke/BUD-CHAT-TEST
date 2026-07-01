using Game.Store;
using Message;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 孵化舱角色浏览/导入面板。
    /// 使用 OSA（IncubationDraftBoxOSAAdapter）替代原有手动对象池，
    /// 数据获取接口由 GetNetCabinCharacterPublishList 改为 SearchCharacter（服务端搜索）。
    /// </summary>
    public class IncubationCabinRolesMainPanel : BasePanel<IncubationCabinRolesMainPanel>
    {
        [SerializeField] private GameObject Obj_Default;

        /// <summary>OSA 适配器，在 Prefab 中绑定</summary>
        [SerializeField] private IncubationRolesMainOSAAdapter _osaAdapter;

        Toggle tog_all;
        Toggle tog_officially;
        Toggle tog_my;
        Toggle tog_buy;

        private CabinPurchasedType _currentType = CabinPurchasedType.All;

        private const int OfficialPoseResourceType = 10;

        private CButton CreatAiBtn;
        private CButton BackBtn;

        private Button _RightBottomBtn;    // 右下角按钮：导入模式时显示"确认导入"
        private Text _RightBottomText;     // 右下角按钮文本
        private Button _RightTopBtn;       // 右上角按钮：浏览模式时显示，点击跳转草稿箱
        private System.Action<CabinCharacterBaseInfo, System.Action<bool>> _onImportCallback;     // 导入模式回调（第二个参数为完成回调：true=成功，false=失败），null 表示浏览模式
        private string _importBtnText = "确认导入";   // 导入按钮文本，外部可通过 OnShow 第二个参数覆盖，默认"确认导入"
        private CabinPublishData _selectedPublishData;               // 当前选中的发布数据
        private CabinCharacterBaseInfo _selectedData;                // 当前选中的角色数据（供导入回调使用）

        private List<CabinPublishData> _cachedRawList = new();  // 已累积的分页原始数据（逐页 AddRange）

        // 取消当前分页句柄的委托：切 Tab/改搜索/关闭面板时调用，避免旧请求回调仍触发
        private Action _cancelCurrentHandle;

        private Button _searchInputBtn;       // 点击搜索框区域，唤起原生键盘
        private Text _searchDisplayText;      // 关键词文本（原 InputField "Text" 子节点，白色）
        private Text _searchPlaceholderText;  // 占位符文本（原 InputField "Placeholder" 子节点，灰色）
        private Button _searchBtn;
        private Button _cancelSearchBtn;  // 取消搜索按钮：关键词不为空时显示

        Button _chatBtn;
        private string _searchKeyword = string.Empty;

        public override void OnCreate()
        {
            base.OnCreate();
            MessageHelper.AddListener(MessageName.OnCabinPublishListChange, FetchAndRefresh);
            tog_all = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_all");
            tog_officially = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_officially");
            tog_my = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_my");
            tog_buy = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_buy");

            tog_all.onValueChanged.AddListener(OnTogAllChanged);
            tog_officially.onValueChanged.AddListener(OnTogOfficiallyChanged);
            tog_my.onValueChanged.AddListener(OnTogMyChanged);
            tog_buy.onValueChanged.AddListener(OnTogBuyChanged);

            BackBtn = GameObjectEx.FindComponentByName<CButton>(transform, "BoxBackBtn");
            BackBtn.onClick.AddListener(CloseSelf);

            _searchInputBtn = GameObjectEx.FindComponentByName<Button>(transform, "searchInput");
            // 分别获取原 InputField 的两个 Text 子节点：白色输入文本 和 灰色占位符文本
            _searchDisplayText = _searchInputBtn?.transform.Find("Text")?.GetComponent<Text>();
            _searchPlaceholderText = _searchInputBtn?.transform.Find("Placeholder")?.GetComponent<Text>();
            _searchInputBtn.onClick.AddListener(OnSearchInputClicked);
            UpdateSearchDisplay();
            _searchBtn = GameObjectEx.FindComponentByName<Button>(transform, "searchBtn");
            _searchBtn.onClick.AddListener(OnSearchBtnClicked);

            _cancelSearchBtn = GameObjectEx.FindComponentByName<Button>(transform, "cancelSearchBtn");
            if (_cancelSearchBtn != null)
            {
                _cancelSearchBtn.onClick.AddListener(OnCancelSearchBtnClicked);
                _cancelSearchBtn.gameObject.SetActive(false);
            }

            _RightBottomBtn = GameObjectEx.FindComponentByName<Button>(transform, "RightBottomBtn");
            _RightBottomText = GameObjectEx.FindComponentByName<Text>(transform, "RightBottomText");

            _chatBtn = GameObjectEx.FindComponentByName<Button>(transform, "btnChat");
            _chatBtn.onClick.AddListener(OnChatClick);
            _chatBtn.gameObject.SetActive(false);
            if (_RightBottomBtn != null)
            {
                _RightBottomBtn.gameObject.SetActive(false);
            }

            _RightTopBtn = GameObjectEx.FindComponentByName<Button>(transform, "RightTopBtn");
            if (_RightTopBtn != null)
            {
                _RightTopBtn.onClick.AddListener(OnRightTopBtnClicked);
                _RightTopBtn.gameObject.SetActive(false);
            }

            // 绑定 OSA 点击回调
            if (_osaAdapter != null)
            {
                _osaAdapter.SetOnItemClick(OnItemClicked);
            }
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            // 重置选中状态与模式
            _onImportCallback = null;
            _selectedPublishData = null;
            _selectedData = null;
            _importBtnText = "确认导入";   // 每次打开恢复默认文本，避免残留上一次的自定义值

            // 若外部传入导入回调则进入导入模式
            if (args != null && args.Length > 0 && args[0] is System.Action<CabinCharacterBaseInfo, System.Action<bool>> callback)
            {
                _onImportCallback = callback;
            }

            // 第二个参数（可选）用于自定义导入按钮文本，为空则保持默认"确认导入"
            if (args != null && args.Length > 1 && args[1] is string importBtnText && !string.IsNullOrEmpty(importBtnText))
            {
                _importBtnText = importBtnText;
            }

            if (CreatAiBtn != null)
            {
                CreatAiBtn.gameObject.SetActive(_onImportCallback == null);
            }

            FetchAndRefresh();
        }

        private void OnTogAllChanged(bool isOn) { if (isOn) { OnTabChanged(CabinPurchasedType.All); } }
        private void OnTogOfficiallyChanged(bool isOn) { if (isOn) { OnTabChanged(CabinPurchasedType.PGCShop); } }
        private void OnTogMyChanged(bool isOn) { if (isOn) { OnTabChanged(CabinPurchasedType.Published); } }
        private void OnTogBuyChanged(bool isOn) { if (isOn) { OnTabChanged(CabinPurchasedType.UGCShop); } }

        private void OnTabChanged(CabinPurchasedType type)
        {
            if (_currentType == type) return;
            _currentType = type;
            // type 变化需要重新向服务端拉取数据
            FetchAndRefresh();
        }

        void OnChatClick()
        {
            ScreenOrientationHelper.Inst.Switch(
                ScreenOrientation.Portrait,
                onComplete: () => UIManager.Inst.OpenPanel(PanelId.AICompanionChatPanel, gameObject),
                onPreRotate: () => gameObject.SetActive(false));
        }

        /// <summary>
        /// 重置并从第一页开始拉取列表（服务端真分页）。
        /// 根据当前是否有搜索关键词选择不同接口：有关键词走 /search/character，无关键词走 /ugc/character/publishList。
        /// 切 Tab、改搜索词、取消搜索、收到列表变更广播时均调用此方法，内部会取消上一次分页句柄并清空缓存。
        /// </summary>
        public void FetchAndRefresh()
        {
            // 取消上一次的分页请求并移除旧的上滑加载监听，避免切 Tab/搜索后旧回调仍触发
            _cancelCurrentHandle?.Invoke();
            _cancelCurrentHandle = null;

            if (_osaAdapter != null && _osaAdapter.PullToRefreshBehaviour != null)
            {
                _osaAdapter.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
            }

            // 清空累积缓存，从第一页重新开始
            _cachedRawList.Clear();

            // 官方商城 Tab 无数据：直接走空列表，不启用翻页
            if (string.IsNullOrEmpty(_searchKeyword) && _currentType == CabinPurchasedType.PGCShop)
            {
                RefresItemList(true);
                return;
            }

            if (!string.IsNullOrEmpty(_searchKeyword))
            {
                StartSearchPaging();
            }
            else
            {
                StartPublishPaging();
            }
        }

        /// <summary>
        /// 启动「发布列表」分页拉取（无搜索关键词时）。
        /// 创建 HttpPageRequestHandle，绑定上滑加载更多到 Next()，注册成功/失败回调后从第一页开始。
        /// cookie 由句柄自动注入请求、自动从响应回填，无需在此手动维护。
        /// </summary>
        private void StartPublishPaging()
        {
            var req = new JObject()
            {
                ["uid"] = AccountDataManager.Inst.Uid,
                ["purchasedType"] = $"{(int)_currentType}",
            };

            var handle = new HttpPageRequestHandle<CabinPublishPageData>(
                HttpUrlDefine.CabinCharacterPublishList,
                JsonConvert.SerializeObject(req),
                HttpMethod.GET);

            handle.AddSuccessAction(data => OnPageData(data?.isFirst ?? true, data?.list));
            handle.AddFailAction(OnPageFail);

            BindPagingHandle(handle.Next, handle.Cancel);
            handle.Reset();
            handle.Start();
        }

        /// <summary>
        /// 启动「搜索结果」分页拉取（有搜索关键词时）。流程同 StartPublishPaging，仅接口与参数不同。
        /// 搜索响应结构为 CabinSearchPageData，通过 GetData() 转回 CabinPublishData。
        /// </summary>
        private void StartSearchPaging()
        {
            var req = new JObject()
            {
                ["searchWord"] = _searchKeyword ?? "",
                ["searchScope"] = $"{(int)_currentType}",
            };

            var handle = new HttpPageRequestHandle<CabinSearchPageData>(
                HttpUrlDefine.SearchCharacter,
                JsonConvert.SerializeObject(req),
                HttpMethod.GET);

            handle.AddSuccessAction(data => OnPageData(data?.isFirst ?? true, data?.GetData()));
            handle.AddFailAction(OnPageFail);

            BindPagingHandle(handle.Next, handle.Cancel);
            handle.Reset();
            handle.Start();
        }

        /// <summary>
        /// 绑定本次分页句柄：记录取消委托，并把上滑加载更多监听指向句柄的 Next。
        /// </summary>
        /// <param name="onSlideUp">上滑到底触发的拉取下一页方法（句柄 Next）</param>
        /// <param name="cancel">取消本次句柄的方法（句柄 Cancel）</param>
        private void BindPagingHandle(UnityEngine.Events.UnityAction onSlideUp, Action cancel)
        {
            _cancelCurrentHandle = cancel;

            if (_osaAdapter != null && _osaAdapter.PullToRefreshBehaviour != null)
            {
                _osaAdapter.PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(onSlideUp);
            }
        }

        /// <summary>
        /// 分页数据成功回调。首页清空缓存，非首页仅向末尾追加，随后按客户端过滤刷新列表。
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
                _cachedRawList.Clear();
            }

            if (pageList != null)
            {
                _cachedRawList.AddRange(pageList);
            }

            RefresItemList(isFirst);
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

            TipPanel.ShowToast("获取角色列表失败，请稍后重试");
        }

        public override void OnHidden()
        {
            // 面板隐藏时清理键盘回调，防止面板已关闭但键盘仍返回数据
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

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

            // 清理选中状态和模式，避免下次打开时残留
            _selectedPublishData = null;
            _selectedData = null;
            _onImportCallback = null;
        }

        /// <summary>
        /// 对已累积的缓存数据应用客户端过滤（仅保留官方姿态 poseResourceType==10），再交给 OSA 适配器刷新。
        /// 分页追加时（isFirstPage=false）保留用户当前选中态，仅首页才重置选中并在导入模式下自动选中第一项。
        /// 注意：客户端过滤可能使某页全部被过滤，导致一次上滑无新增可见项；如需"加载到有数据为止"，
        /// 可在 OnPageData 中按 !IsEnd 自动续拉，当前保持与 FittingRoom 一致的手动上滑加载。
        /// </summary>
        /// <param name="isFirstPage">是否为第一页刷新（true 时重置选中态）</param>
        private void RefresItemList(bool isFirstPage)
        {
            // 客户端过滤：仅保留官方姿态资源（服务端不支持该参数）
            List<CabinPublishData> publishList = _cachedRawList;
            var items = publishList ?? new List<CabinPublishData>();

            // 仅首页重置选中态；翻页追加时保留用户已选项，避免上滑加载更多时选中被清空
            if (isFirstPage)
            {
                if (_osaAdapter != null)
                {
                    _osaAdapter.ClearSelection();
                }

                _selectedPublishData = null;
                _selectedData = null;
            }

            if (_osaAdapter != null && _osaAdapter.IsInitialized)
            {
                _osaAdapter.Data.ResetItems(items);
            }

            Obj_Default.SetActive(items.Count == 0);

            // 导入模式：仅首页自动选中第一项
            if (isFirstPage && _onImportCallback != null && items.Count > 0)
            {
                _selectedPublishData = items[0];
                _selectedData = items[0].characterInfo;

                if (_osaAdapter != null)
                {
                    _osaAdapter.SetSelected(_selectedPublishData);
                }
            }

            RefreshModeBtns();
        }

        /// <summary>
        /// 点击搜索框区域，构造键盘参数并唤起 MobileInterface 原生键盘。
        /// 将当前关键词作为默认文本回填，方便用户在原有内容基础上修改。
        /// </summary>
        private void OnSearchInputClicked()
        {
            KeyBoardInfo keyBoardInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "输入关键字进行搜索",
                inputMode = (int)KeyBoardInputMode.SingleLine,
                maxLength = 100,
                inputFlag = 0,
                textSecurity = 1,
                isFilterEmoji = 1,
                returnKeyType = (int)ReturnType.Search,
                defaultText = _searchKeyword
            };
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboardInput);
            MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
        }

        /// <summary>
        /// MobileInterface 键盘输入回调，更新关键词并向服务端重新搜索。
        /// 必须在此处调用 DelClientResponse，防止回调重复触发。
        /// </summary>
        private void OnKeyboardInput(string input)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            _searchKeyword = input ?? "";
            UpdateSearchDisplay();
            FetchAndRefresh();
        }

        /// <summary>
        /// 根据当前关键词更新搜索框的文本显示：
        /// 有关键词时显示白色输入文本并隐藏占位符；无关键词时显示灰色占位符并隐藏输入文本。
        /// </summary>
        private void UpdateSearchDisplay()
        {
            bool hasKeyword = !string.IsNullOrEmpty(_searchKeyword);

            if (_searchDisplayText != null)
            {
                _searchDisplayText.text = _searchKeyword;
                _searchDisplayText.gameObject.SetActive(hasKeyword);
            }

            if (_searchPlaceholderText != null)
            {
                _searchPlaceholderText.gameObject.SetActive(!hasKeyword);
            }

            if (_cancelSearchBtn != null)
            {
                _cancelSearchBtn.gameObject.SetActive(hasKeyword);
            }
        }

        /// <summary>
        /// 点击搜索按钮，以当前已缓存的关键词向服务端重新搜索。
        /// </summary>
        private void OnSearchBtnClicked()
        {
            FetchAndRefresh();
        }

        /// <summary>
        /// 点击取消搜索按钮，清空当前关键词并向服务端重新拉取全量数据。
        /// </summary>
        private void OnCancelSearchBtnClicked()
        {
            _searchKeyword = "";
            UpdateSearchDisplay();
            FetchAndRefresh();
        }

        /// <summary>
        /// 根据当前模式刷新右侧两个按钮的显示：
        /// 导入模式下，当前页签有数据时显示右下角"确认导入"，空列表时按页签显示"获取更多/前往创作"引导入口；
        /// 浏览模式右下角按钮始终隐藏，显示右上角草稿箱按钮。
        /// </summary>
        private void RefreshModeBtns()
        {
            bool isImportMode = _onImportCallback != null;
            // 当前页签是否无数据，口径与 Obj_Default 判空一致（均基于已累积的原始列表）
            bool isEmpty = (_cachedRawList?.Count ?? 0) == 0;

            // 右下角按钮：三态——浏览模式隐藏 / 导入模式有数据显示"确认导入" / 导入模式空列表显示引导入口
            if (_RightBottomBtn != null)
            {
                // 每次重新配置前清空旧监听，避免切页签后监听叠加
                _RightBottomBtn.onClick.RemoveAllListeners();

                if (!isImportMode)
                {
                    // 浏览模式：右下角按钮始终隐藏（保持原行为）
                    _RightBottomBtn.gameObject.SetActive(false);
                }
                else if (!isEmpty)
                {
                    // 导入模式 + 有数据：确认导入（原逻辑）
                    _RightBottomBtn.gameObject.SetActive(true);
                    _RightBottomText.text = _importBtnText;
                    _RightBottomBtn.onClick.AddListener(OnImportBtnClicked);
                }
                else
                {
                    // 导入模式 + 当前页签空列表：按页签显示获取/创作入口
                    ConfigEmptyStateBtn();
                }
            }

            // 右上角按钮：仅浏览模式显示，点击跳转草稿箱
            if (_RightTopBtn != null)
            {
                _RightTopBtn.gameObject.SetActive(!isImportMode);
            }

            if (_chatBtn != null)
            {
                _chatBtn.gameObject.SetActive(!isImportMode);
            }
        }

        /// <summary>
        /// 导入模式下当前页签空列表时，按页签配置右下角按钮的文本与跳转。
        /// 全部/社区购买显示"获取更多"，我创作的显示"前往创作"，官方等其它页签隐藏按钮。
        /// 调用前 RefreshModeBtns 已执行 RemoveAllListeners，此处仅负责设置可见性、文本与新监听。
        /// </summary>
        private void ConfigEmptyStateBtn()
        {
            switch (_currentType)
            {
                case CabinPurchasedType.All:
                    _RightBottomBtn.gameObject.SetActive(true);
                    _RightBottomText.text = "获取更多";
                    _RightBottomBtn.onClick.AddListener(OnGetMoreClick);
                    break;

                case CabinPurchasedType.Published:
                    _RightBottomBtn.gameObject.SetActive(true);
                    _RightBottomText.text = "前往创作";
                    _RightBottomBtn.onClick.AddListener(OnGoCreateClick);
                    break;

                case CabinPurchasedType.UGCShop:
                    _RightBottomBtn.gameObject.SetActive(true);
                    _RightBottomText.text = "获取更多";
                    _RightBottomBtn.onClick.AddListener(OnGoCommunityShopClick);
                    break;

                default:
                    // 官方(PGCShop)等页签：不显示右下角按钮
                    _RightBottomBtn.gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// 「前往创作」（我创作的页签空列表）：打开草稿箱面板，其列表首个"新建"卡为角色创建入口。
        /// </summary>
        private void OnGoCreateClick()
        {
            UIManager.Inst.OpenPanel(PanelId.IncubationCabinDraftBox);
        }

        /// <summary>
        /// 「获取更多」（社区购买页签空列表）：打开社区商城并定位到 AI伙伴页签。
        /// </summary>
        private void OnGoCommunityShopClick()
        {
            UIManager.Inst.OpenPanel(PanelId.AIPartnerShopPanel, AIPartnerTabSecond.AICharacter);
        }

        /// <summary>
        /// 「获取更多」（全部页签空列表）：官方商城扭蛋入口。
        /// 暂未接入扭蛋系统，当前先跳转社区商城 AI伙伴页。
        /// </summary>
        private void OnGetMoreClick()
        {
            // TODO: 后续接入官方商城扭蛋（GashaponPanel）入口，当前暂跳转社区商城 AI伙伴页
            UIManager.Inst.OpenPanel(PanelId.AIPartnerShopPanel, AIPartnerTabSecond.AICharacter);
        }

        /// <summary>
        /// 浏览模式右上角按钮点击：跳转草稿箱界面。
        /// </summary>
        private void OnRightTopBtnClicked()
        {
            UIManager.Inst.OpenPanel(PanelId.IncubationCabinDraftBox);
        }

        /// <summary>
        /// 导入角色按钮点击：显示导入中 Loading，执行导入回调，根据结果弹出 Toast 并关闭面板。
        /// </summary>
        private void OnImportBtnClicked()
        {
            if (_selectedData == null || _onImportCallback == null)
            {
                return;
            }

            // 切换角色交互埋点：用户点击确认导入、真正执行切换时上报
            IncubationCabinControll.ReportThinkingData("switch_character");

            var loadingPanel = UIManager.Inst.OpenPanel<CommonLoadingPanel>(PanelId.CommonBoxLoadingPanel);
            loadingPanel.SetLocalText("导入中", "");

            _onImportCallback(_selectedData, (isSuccess) =>
            {
                UIManager.Inst.ClosePanel(PanelId.CommonBoxLoadingPanel);
                if (isSuccess)
                {
                    TipPanel.ShowToast("导入角色成功");
                    CloseSelf();
                }
                else
                {
                    TipPanel.ShowToast("导入角色失败，请重试");
                }
            });
        }

        /// <summary>
        /// 角色卡片点击回调。
        /// 导入模式：更新选中态并激活导入按钮；浏览模式：打开角色详情面板。
        /// </summary>
        private void OnItemClicked(CabinPublishData data)
        {
            if (_onImportCallback != null)
            {
                // 导入模式：切换选中状态，OSA 适配器负责更新视觉
                _selectedPublishData = data;
                _selectedData = data?.characterInfo;
                _osaAdapter.SetSelected(data);
                RefreshModeBtns();
            }
            else
            {
                // 浏览模式：打开角色详情面板
                CabinRolesNetManager.Inst.OpenPanelWithFreshData(data?.characterInfo?.id);
            }
        }

        protected override void OnDestroy()
        {
            // 兜底取消分页句柄，防止面板销毁后回调仍触发
            _cancelCurrentHandle?.Invoke();
            _cancelCurrentHandle = null;

            MessageHelper.RemoveListener(MessageName.OnCabinPublishListChange, FetchAndRefresh);
        }

        public override void OnWindowBeFocused() { }
        public override void OnWindowPop() { }
    }
}
