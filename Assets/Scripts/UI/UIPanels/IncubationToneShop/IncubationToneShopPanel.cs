using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Audio;
using Game.Store;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class IncubationToneShopPanel : BasePanel<IncubationToneShopPanel>
    {
        [SerializeField] private Button BackBtn;
        // 左侧语言选择区
        [SerializeField] private Dropdown LanguageDropdown;

        // 中间选中音色展示区
        [SerializeField] private IncubationToneShopItem DetailItem;
        [SerializeField] private PurchaseButton PurchaseButton;

        // 作者信息区（选中音色后展示）
        [SerializeField] private RemoteImageBehaviour AuthorAvatar;  // 作者头像
        //[SerializeField] private Text AuthorName;                    // 作者昵称
        [SerializeField] private Button AuthorBtn;                   // 点击查看作品详情

        // 右侧分区 Toggle（动态生成）
        [SerializeField] private Transform ToneTypeList;
        [SerializeField] private GameObject ToneShopTypeItem;

        // 下拉筛选面板
        [SerializeField] private GameObject FiltrateListGo;
        [SerializeField] private Button FilterBtn;

        // FiltrateList 分组容器与模板
        [SerializeField] private Transform FiltrateContent;
        [SerializeField] private GameObject FiltrateItemPrefab;

        // 搜索
        [SerializeField] private InputField SearchInput;
        [SerializeField] private Button SearchBtn;

        // 音色列表
        [SerializeField] private ToneShopOSAAdapter ToneListAdapter;

        // 运行时数据
        private List<RecommendItemData> _allItems = new List<RecommendItemData>();
        private readonly List<GameObject> _sectionToggleInstances = new List<GameObject>();
        private readonly List<ToneShopTagFiltrateItem> _filtrateItemInstances = new List<ToneShopTagFiltrateItem>();

        private string _cookie = string.Empty;
        private bool _isEnd = false;
        private string _curSectionId = string.Empty;

        // 分页请求进行中标志：防止上滑时快速重复触发，导致同一 cookie 重复拉取、列表出现重复项
        private bool _isLoadingPage = false;

        // 筛选状态：key = tabName，value = 已选项名称集合
        private readonly Dictionary<string, HashSet<string>> _activeFilters = new Dictionary<string, HashSet<string>>();
        private string _searchWord = string.Empty;

        private RecommendItemData _selectedData;
        private int _previewLanguageType;
        // 下拉选项索引到 languageType 的映射（因为每个音色支持的语言数量不同，索引不能直接等于 languageType）
        private readonly List<int> _dropdownLanguageTypes = new List<int>();

        private bool _refreshPending = false;
        private List<RecommendItemData> _pendingRefreshItems;

        // 并行加载协调：两个请求都就绪后才发起音色列表请求
        private bool _sectionsReady = false;
        private bool _tagsReady = false;
        private readonly Dictionary<string, int> _tagNameToId = new Dictionary<string, int>();

        // 筛选变更防抖：ToggleGroup 切换时同帧触发两次回调，延迟一帧合并为一次请求
        private bool _filterReloadPending = false;

        // 是否处于关键字搜索模式（true 时使用 SearchCabinTone 接口，false 时使用 LoadToneList）
        private bool _isSearchMode = false;

        // 外部传入的回调（可选，通过 SetCallbacks 设置）
        private Action<RecommendItemData> _onPreview;
        private Action<RecommendItemData> _onSelect;

        public void SetCallbacks(Action<RecommendItemData> onPreview, Action<RecommendItemData> onSelect)
        {
            _onPreview = onPreview;
            _onSelect = onSelect;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            SetupListeners();

            if (FiltrateListGo != null)
            {
                FiltrateListGo.SetActive(false);
            }

            if (ToneListAdapter != null)
            {
                ToneListAdapter.SetOnItemClick(OnToneItemClick);

                // 绑定上滑加载更多：滑到底部触发加载下一页（普通列表/搜索按当前模式分发）
                if (ToneListAdapter.PullToRefreshBehaviour != null)
                {
                    ToneListAdapter.PullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
                    ToneListAdapter.PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(LoadNextPage);
                }
            }
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            _sectionsReady = false;
            _tagsReady = false;
            _filterReloadPending = false;
            _isSearchMode = false;
            _tagNameToId.Clear();

            ResetFilter();
            ResetSections();

            // 并行发起两个请求，两者都就绪后才加载音色列表
            LoadSections();
            LoadFiltrateData();
        }

        public override void OnHidden()
        {
            base.OnHidden();

            AkSoundManager.Inst.StopUGCAudio(gameObject);

            if (FiltrateListGo != null)
            {
                FiltrateListGo.SetActive(false);
            }
        }

        private void SetupListeners()
        {
            if (LanguageDropdown != null)
            {
                // 通过 _dropdownLanguageTypes 映射将下拉索引转换为实际 languageType
                LanguageDropdown.onValueChanged.AddListener(idx =>
                {
                    if (idx >= 0 && idx < _dropdownLanguageTypes.Count)
                    {
                        _previewLanguageType = _dropdownLanguageTypes[idx];
                    }
                });
            }

            if (FilterBtn != null)
            {
                FilterBtn.onClick.AddListener(OpenFiltrateList);
            }

            if (BackBtn != null)
            {
                BackBtn.onClick.AddListener(CloseSelf);
            }

            if (SearchBtn != null)
            {
                SearchBtn.onClick.AddListener(OnSearchBtnClick);
            }

            if (SearchInput != null)
            {
                SearchInput.onValueChanged.AddListener(OnSearchInputChanged);
            }

            if (AuthorBtn != null)
            {
                AuthorBtn.onClick.AddListener(OnAuthorBtnClick);
            }
        }

        // ==================== 筛选列表动态生成 ====================

        private void LoadFiltrateData()
        {
            CabinToneNetManager.Inst.GetCabinToneTags((isSuccess, rsp) =>
            {
                if (!isSuccess || rsp?.list == null || rsp.list.Count == 0)
                {
                    _tagsReady = true;
                    TryLoadToneList();
                    return;
                }

                _tagNameToId.Clear();
                foreach (var group in rsp.list)
                {
                    if (group.tagList == null)
                    {
                        continue;
                    }

                    foreach (var tag in group.tagList)
                    {
                        _tagNameToId[tag.tagName] = tag.tagId;
                    }
                }

                BuildFiltrateItems(rsp.list);

                _tagsReady = true;
                TryLoadToneList();
            });
        }

        private void BuildFiltrateItems(List<CharacterToneTagData> dataList)
        {
            if (FiltrateContent == null || FiltrateItemPrefab == null)
            {
                return;
            }

            foreach (var item in _filtrateItemInstances)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            _filtrateItemInstances.Clear();
            FiltrateItemPrefab.SetActive(false);

            foreach (var groupData in dataList)
            {
                if (groupData.tagList == null || groupData.tagList.Count == 0)
                {
                    continue;
                }

                var go = Instantiate(FiltrateItemPrefab, FiltrateContent);
                go.SetActive(true);

                var item = go.GetComponent<ToneShopTagFiltrateItem>();
                if (item != null)
                {
                    item.SetData(groupData.groupName, groupData.tagList.Select(t => t.tagName).ToList(), OnFiltrateItemChanged);
                    _filtrateItemInstances.Add(item);
                }
            }
        }

        private void OnFiltrateItemChanged(string tabName, string tagName, bool isOn)
        {
            if (!_activeFilters.ContainsKey(tabName))
            {
                _activeFilters[tabName] = new HashSet<string>();
            }

            if (isOn)
            {
                _activeFilters[tabName].Add(tagName);
            }
            else
            {
                _activeFilters[tabName].Remove(tagName);
            }

            // 初始化阶段的自动选中回调，等待两端都就绪后再统一发起请求
            if (!_sectionsReady || !_tagsReady)
            {
                return;
            }

            ScheduleFilterReload();
        }

        // ToggleGroup 切换时同帧触发 off/on 两次回调，延迟一帧合并为一次请求
        private void ScheduleFilterReload()
        {
            if (_filterReloadPending)
                return;

            // 面板关闭时 GoToggle.OnDestroy 会触发 toggle off 回调，此时 GameObject 已 inactive，
            // 无法启动协程，直接忽略即可
            if (!gameObject.activeInHierarchy)
                return;

            _filterReloadPending = true;
            StartCoroutine(FilterReloadDeferred());
        }

        private IEnumerator FilterReloadDeferred()
        {
            yield return null;
            _filterReloadPending = false;
            _cookie = string.Empty;
            _isEnd = false;
            _allItems.Clear();

            if (_isSearchMode)
            {
                LoadSearchResults();
            }
            else
            {
                LoadToneList();
            }
        }

        // ==================== 分区加载 ====================

        private void LoadSections()
        {
            JObject req = new JObject
            {
                ["ugcType"] = (int)UgcType.PartnerMusic
            };

            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.sectionList,
                HttpMethod.GET,
                JsonConvert.SerializeObject(req),
                content =>
                {
                    var rsp = JsonConvert.DeserializeObject<SectionListRsp>(content);
                    if (rsp?.list != null && rsp.list.Count > 0)
                    {
                        BuildSectionToggles(rsp.list);
                    }
                },
                error =>
                {
                    LoggerUtils.LogError("[IncubationToneShop] LoadSections failed: " + error);
                });
        }

        private void BuildSectionToggles(List<UgcSectionData> sections)
        {
            if (ToneTypeList == null || ToneShopTypeItem == null)
            {
                return;
            }

            foreach (var go in _sectionToggleInstances)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _sectionToggleInstances.Clear();
            ToneShopTypeItem.SetActive(false);

            bool isFirst = true;
            foreach (var section in sections)
            {
                var go = Instantiate(ToneShopTypeItem, ToneTypeList);
                go.SetActive(true);
                _sectionToggleInstances.Add(go);

                var item = go.GetComponent<ToneShopTypeItem>();
                if (item != null)
                {
                    item.SetData(section.sectionName, section.sectionId, OnSectionSelected);
                    if (isFirst)
                    {
                        item.SetIsOn(true, false);
                        OnSectionSelected(section.sectionId);
                        isFirst = false;
                    }
                }
            }
        }

        private void OnSectionSelected(string sectionId)
        {
            if (_curSectionId == sectionId)
            {
                return;
            }

            _curSectionId = sectionId;
            _cookie = string.Empty;
            _isEnd = false;
            _allItems.Clear();
            _sectionsReady = true;
            TryLoadToneList();
        }

        // ==================== 音色列表加载 ====================

        private void TryLoadToneList()
        {
            if (!_sectionsReady || !_tagsReady)
            {
                return;
            }

            LoadToneList();
        }

        /// <summary>
        /// 上滑到底触发：加载下一页。按当前模式分发到搜索或普通列表接口；
        /// 已到末页（_isEnd）则直接收起下拉 Gizmo，不再请求。
        /// cookie/isEnd/_allItems 的累积已由 LoadToneList/LoadSearchResults 维护。
        /// </summary>
        private void LoadNextPage()
        {
            // 上一页请求未返回时忽略本次触发，避免重复拉取（请求返回时会收起 Gizmo）
            if (_isLoadingPage)
            {
                return;
            }

            if (_isEnd)
            {
                HideRefreshGizmo();
                return;
            }

            if (_isSearchMode)
            {
                LoadSearchResults();
            }
            else
            {
                LoadToneList();
            }
        }

        /// <summary>收起上滑加载更多的 Gizmo（请求完成或已到末页时调用）。</summary>
        private void HideRefreshGizmo()
        {
            if (ToneListAdapter != null && ToneListAdapter.PullToRefreshBehaviour != null)
            {
                ToneListAdapter.PullToRefreshBehaviour.HideGizmo();
            }
        }

        private void LoadToneList()
        {
            if (_isEnd)
            {
                return;
            }

            _isLoadingPage = true;
            var tagIds = GetSelectedTagIds();

            CabinToneNetManager.Inst.GetSectionInfoV2(
                _curSectionId,
                _cookie,
                20,
                string.IsNullOrEmpty(tagIds) ? null : tagIds,
                (isSuccess, rsp) =>
                {
                    _isLoadingPage = false;

                    // 面板可能在请求返回前已被关闭/销毁，此时忽略回调
                    if (this == null || !gameObject.activeInHierarchy)
                        return;

                    // 收起上滑加载更多的 Gizmo（无论成功失败）
                    HideRefreshGizmo();

                    if (!isSuccess || rsp == null)
                    {
                        LoggerUtils.LogError("[IncubationToneShop] LoadToneList failed");
                        return;
                    }

                    _isEnd = rsp.isEnd == 1;
                    _cookie = rsp.cookie ?? string.Empty;

                    if (rsp.list != null)
                    {
                        _allItems.AddRange(rsp.list);
                    }

                    ApplyFilter();
                });
        }

        /// <summary>
        /// 收集当前激活的标签筛选 ID，以英文逗号拼接为字符串。
        /// "不限"标签不参与筛选，无选中标签时返回空字符串。
        /// </summary>
        private string GetSelectedTagIds()
        {
            var ids = new List<int>();
            foreach (var kv in _activeFilters)
            {
                foreach (var tagName in kv.Value)
                {
                    // "不限"表示该分组不做筛选，跳过，不加入请求参数
                    if (tagName == ToneShopTagFiltrateItem.NoLimitTagName)
                        continue;

                    if (_tagNameToId.TryGetValue(tagName, out var id))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids.Count > 0 ? string.Join(",", ids) : string.Empty;
        }

        // ==================== 筛选 ====================

        private void ApplyFilter()
        {
            RefreshToneList(_allItems);
        }

        // ==================== 关键字搜索（SearchCabinTone 接口） ====================

        /// <summary>
        /// 使用关键字搜索接口加载音色列表。仅在 _isSearchMode 为 true 时调用。
        /// </summary>
        private void LoadSearchResults()
        {
            if (_isEnd)
            {
                return;
            }

            _isLoadingPage = true;
            var tagIds = GetSelectedTagIds();

            CabinToneNetManager.Inst.SearchCabinTone(
                _searchWord,
                tagIds,
                _cookie,
                20,
                (isSuccess, rsp) =>
                {
                    _isLoadingPage = false;

                    if (this == null || !gameObject.activeInHierarchy)
                        return;

                    // 收起上滑加载更多的 Gizmo（无论成功失败）
                    HideRefreshGizmo();

                    if (!isSuccess || rsp == null)
                    {
                        // 请求失败时仍刷新列表（展示空或已有数据）
                        ApplyFilter();
                        return;
                    }

                    _isEnd = rsp.IsEnd == 1;
                    _cookie = rsp.cookie ?? string.Empty;

                    if (rsp.list != null)
                    {
                        foreach (var sub in rsp.list)
                        {
                            var item = ConvertSearchItem(sub);
                            if (item != null)
                            {
                                _allItems.Add(item);
                            }
                        }
                    }

                    ApplyFilter();
                });
        }

        /// <summary>
        /// 将搜索接口返回的 <see cref="CabinCharacterToneSearchSubData"/> 转换为
        /// 列表适配器所需的 <see cref="RecommendItemData"/>。
        /// </summary>
        private RecommendItemData ConvertSearchItem(CabinCharacterToneSearchSubData subData)
        {
            if (subData?.ugcInfo == null)
                return null;

            return new RecommendItemData
            {
                ugcId = subData.ugcInfo.id,
                ugcType = UgcType.PartnerMusic,
                UgcInfo = subData.ugcInfo,
                interactInfo = subData.interactInfo
            };
        }

        private void RefreshToneList(List<RecommendItemData> items)
        {
            // 纵深防御：防止其他调用路径在面板销毁或 inactive 后仍尝试启动协程
            if (this == null || !gameObject.activeInHierarchy)
                return;

            _pendingRefreshItems = items;
            if (!_refreshPending)
            {
                _refreshPending = true;
                StartCoroutine(RefreshToneListDeferred());
            }
        }

        private IEnumerator RefreshToneListDeferred()
        {
            yield return new WaitForEndOfFrame();

            if (ToneListAdapter != null && !ToneListAdapter.IsInitialized)
            {
                yield return null;
            }

            _refreshPending = false;
            DoRefreshToneList(_pendingRefreshItems);
        }

        private void DoRefreshToneList(List<RecommendItemData> items)
        {
            if (ToneListAdapter == null)
                return;

            if (!ToneListAdapter.IsInitialized)
                return;

            if (items == null || items.Count == 0)
            {
                ToneListAdapter.Data.ResetItems(new List<RecommendItemData>());
                return;
            }

            ToneListAdapter.Data.ResetItems(items);

            if (_selectedData == null)
            {
                OnToneItemClick(items[0]);
                return;
            }

            bool stillExists = items.Exists(d => d.ugcId == _selectedData.ugcId);

            if (!stillExists)
            {
                _selectedData = null;
                ToneListAdapter.ClearSelection();
                OnToneItemClick(items[0]);
            }
        }

        // ==================== 中间展示区 ====================

        private void OnToneItemClick(RecommendItemData data)
        {
            _selectedData = data;

            if (ToneListAdapter != null)
            {
                ToneListAdapter.SetSelected(data);
            }

            var toneInfo = data?.UgcInfo as CabinToneInfo;
            RefreshLanguageToggles(toneInfo);

            if (DetailItem != null)
            {
                DetailItem.SetData(data, _ => PreviewCurrentLanguageTone(), _onSelect);
            }

            RefreshPurchaseButton();
            RefreshAuthorUI(data);
        }

        private void RefreshLanguageToggles(CabinToneInfo toneInfo)
        {
            if (LanguageDropdown == null) return;

            bool hasChinese  = toneInfo?.languageList?.Exists(l => l.type == 0) ?? false;
            bool hasEnglish  = toneInfo?.languageList?.Exists(l => l.type == 1) ?? false;
            bool hasJapanese = toneInfo?.languageList?.Exists(l => l.type == 2) ?? false;

            // 重建选项列表，只展示该音色实际支持的语言，并更新索引→languageType 映射
            _dropdownLanguageTypes.Clear();
            var options = new List<Dropdown.OptionData>();

            if (hasChinese)  { options.Add(new Dropdown.OptionData("中文")); _dropdownLanguageTypes.Add(0); }
            if (hasEnglish)  { options.Add(new Dropdown.OptionData("英语")); _dropdownLanguageTypes.Add(1); }
            if (hasJapanese) { options.Add(new Dropdown.OptionData("日语")); _dropdownLanguageTypes.Add(2); }

            LanguageDropdown.ClearOptions();
            LanguageDropdown.AddOptions(options);

            // 默认选中第一个可用语言（不触发 onValueChanged，手动更新 _previewLanguageType）
            if (_dropdownLanguageTypes.Count > 0)
            {
                LanguageDropdown.SetValueWithoutNotify(0);
                _previewLanguageType = _dropdownLanguageTypes[0];
            }
        }

        private void PreviewCurrentLanguageTone()
        {
            if (_selectedData == null)
                return;

            var toneInfo = _selectedData.UgcInfo as CabinToneInfo;

            if (toneInfo == null)
                return;

            var langData = toneInfo.languageList?.Find(l => l.type == _previewLanguageType);

            if (langData == null || string.IsNullOrEmpty(langData.voiceUrl))
                return;

            AkSoundManager.Inst.StopUGCAudio(gameObject);
            AkSoundManager.Inst.PlayUGCAudioByUrl(langData.voiceUrl, false, gameObject);
        }

        // ==================== 语言选择与价格 ====================

        private void RefreshPurchaseButton()
        {
            if (PurchaseButton == null || _selectedData == null)
            {
                return;
            }

            var toneInfo = _selectedData.UgcInfo as CabinToneInfo;

            if (toneInfo == null)
            {
                return;
            }

            PaymentInfo adjustedPayment = null;
            if (toneInfo.paymentInfo != null)
            {
                adjustedPayment = new PaymentInfo
                {
                    price = toneInfo.paymentInfo.price,
                    currencyType = toneInfo.paymentInfo.currencyType
                };
            }
            // 通过背包数据本地判断是否已购买，避免依赖服务端 interactInfo.consumed 字段
            var consumed = AssetsDataManager.IsOwned(toneInfo.id) ? 1 : 0;
            PurchaseButton.SetData(toneInfo, consumed, adjustedPayment);
        }

        // ==================== 筛选下拉面板 ====================

        private void OpenFiltrateList()
        {
            if (FiltrateListGo != null)
            {
                FiltrateListGo.SetActive(!FiltrateListGo.activeSelf);
            }
        }

        private void CloseFiltrateList()
        {
            if (FiltrateListGo != null)
            {
                FiltrateListGo.SetActive(false);
            }
        }

        // ==================== 搜索 ====================

        private void OnSearchInputChanged(string value)
        {
            bool wasSearchMode = _isSearchMode;
            _searchWord = value;

            // 输入框清空时自动退出搜索模式，重新走普通列表接口
            if (string.IsNullOrEmpty(value) && wasSearchMode)
            {
                _isSearchMode = false;
                _cookie = string.Empty;
                _isEnd = false;
                _allItems.Clear();
                LoadToneList();
            }
        }

        private void OnSearchBtnClick()
        {
            _searchWord = SearchInput != null ? SearchInput.text : string.Empty;
            _cookie = string.Empty;
            _isEnd = false;
            _allItems.Clear();

            if (string.IsNullOrEmpty(_searchWord))
            {
                // 关键字为空，退出搜索模式，重新加载普通列表
                _isSearchMode = false;
                LoadToneList();
            }
            else
            {
                // 有关键字，进入搜索模式，调用 SearchCabinTone 接口
                _isSearchMode = true;
                LoadSearchResults();
            }
        }

        // ==================== 重置 ====================

        private void ResetFilter()
        {
            _activeFilters.Clear();
            _searchWord = string.Empty;

            foreach (var item in _filtrateItemInstances)
            {
                item?.ResetToggles();
            }

            if (SearchInput != null)
            {
                SearchInput.SetTextWithoutNotify(string.Empty);
            }
        }

        private void ResetSections()
        {
            _cookie = string.Empty;
            _isEnd = false;
            _curSectionId = string.Empty;
            _allItems.Clear();
            _selectedData = null;
            RefreshAuthorUI(null);

            foreach (var go in _sectionToggleInstances)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _sectionToggleInstances.Clear();

            if (ToneListAdapter != null && ToneListAdapter.IsInitialized)
            {
                ToneListAdapter.ClearSelection();
                ToneListAdapter.Data.ResetItems(new List<RecommendItemData>());
            }
        }

        // ==================== 作者信息 ====================

        /// <summary>
        /// 根据当前选中的音色数据刷新作者信息区域。
        /// UGC 音色且含创作者信息时显示头像与昵称；PGC 音色或 data 为 null 时隐藏作者区域。
        /// </summary>
        /// <param name="data">当前选中的音色数据，为 null 时隐藏作者区域。</param>
        private void RefreshAuthorUI(RecommendItemData data)
        {
            var hasCreator = data?.creatorInfo != null;

            AuthorAvatar?.gameObject.SetActive(hasCreator);
            //AuthorName?.gameObject.SetActive(hasCreator);
            AuthorBtn?.gameObject.SetActive(hasCreator);

            if (!hasCreator)
                return;

            // 异步加载作者头像
            AuthorAvatar?.Load(data.creatorInfo.portraitUrl);

            //if (AuthorName != null)
            //{
            //    AuthorName.text = data.creatorInfo.nickname;
            //}
        }

        /// <summary>
        /// 点击作者头像区域时，打开当前选中音色的作品详情页。
        /// </summary>
        private void OnAuthorBtnClick()
        {
            if (_selectedData == null)
                return;

            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.CabinTone, _selectedData.ugcId);
        }
    }
}
