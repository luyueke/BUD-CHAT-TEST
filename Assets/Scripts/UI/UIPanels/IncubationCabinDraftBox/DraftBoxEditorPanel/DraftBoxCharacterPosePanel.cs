using Game.AnimationStudio;
using Game.Database;
using Newtonsoft.Json;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Network.Message;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;

/// <summary>
/// Author:
/// Desc: 角色姿势面板（官方姿势 / 我创作的 / 社区购买）
///       点击姿势 Item 后通过回调将 KeyFrameData JSON (poseData) 传递给编辑器应用到角色
/// Date:26-04-01
/// </summary>
public class DraftBoxCharacterPosePanel : MonoBehaviour
{
    [Header("子Tab")]
    [SerializeField] private CButton Btn_Official;          // 官方姿势 Tab
    [SerializeField] private CButton Btn_Mine;              // 我创作的 Tab
    [SerializeField] private CButton Btn_Community;         // 社区购买 Tab

    [Header("姿势列表")]
    [SerializeField] private CButton GoCreat;               // 去创作 
    [SerializeField] private CButton GoBuy;                 // 去购买
    [SerializeField] private GameObject PoseItemPrefab;     // 姿势 Item 预制体
    [SerializeField] private Transform PoseContainer;       // 姿势列表容器

    private const string OfficialPoseConfigPath = "Assets/Arts/Config/CabinPoseConfig/cabinofficialPose.json";

    private List<GameObject> _poseItemList = new List<GameObject>();    // 当前 Tab 下所有姿势 Item 的 GameObject
    private UgcPoseSubType _poseSubType = UgcPoseSubType.Single;        // 当前姿势子类型（单人/双人/宠物等）
    private DraftBoxPoseItem _selectedItem;                             // 当前高亮选中的姿势 Item
    private string _currentPoseId;                                      // 服务器下发的当前姿势ID，进入时用于默认选中
    private CabinCharacterBaseInfo _info;                                // 角色数据引用，姿势选择时直接写入
    private static List<OfficialPoseConfig> _officialPoseConfigs;       // cabinofficialPose.json 反序列化缓存（静态），LoadOfficialPoses 首次加载后存储
    /// <summary>
    /// 面板不可见时收到 OnPosePublished / OnPosePurchased 事件后，记录待恢复的 Tab；
    /// OnEnable 时消费该值，切换到对应 Tab 并刷新列表
    /// </summary>
    private PoseTab? _pendingTabRefresh = null;

    private enum PoseTab { Official, Mine, Community }
    private PoseTab _currentTab = PoseTab.Official;

    /// <summary>初始化 Tab 按钮监听，默认展示官方姿势</summary>
    public void InitUI(UgcPoseSubType poseSubType = UgcPoseSubType.Single)
    {
        _poseSubType = poseSubType;

        Btn_Official.onClick.AddListener(() => SwitchTab(PoseTab.Official));
        Btn_Mine.onClick.AddListener(() => SwitchTab(PoseTab.Mine));
        Btn_Community.onClick.AddListener(() => SwitchTab(PoseTab.Community));
        GoCreat.onClick.AddListener(OnGoCreatClick);
        GoBuy.onClick.AddListener(OnGoBuyClick);

        SwitchTab(PoseTab.Official);
    }

    private void OnGoCreatClick()
    {
        UIManager.Inst.OpenPanel(PanelId.AnimationStudioMainPanel, AnimationStudioType.Pose);
    }

    private void OnGoBuyClick()
    {
        var poseStorePanel = UIManager.Inst.OpenPanel<PoseStorePanel>(PanelId.PoseStorePanel);
        if (poseStorePanel != null)
        {
            poseStorePanel.OnCloseAction += () =>
            {
                RefreshPoseList();
            };
        }
    }

    /// <summary>
    /// 使用 Start/OnDestroy 而非 OnEnable/OnDisable 订阅消息，
    /// 确保面板处于非激活状态时收到事件也能通过 _pendingTabRefresh 在返回后刷新
    /// </summary>
    private void Start()
    {
        MessageHelper.AddListener(MessageName.OnUgcAnimStudioPublishedListChange, OnPosePublished);
        MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnPosePurchased);
        // AvaterDatabaseOnDataChange 在背包本地缓存实际更新后触发（晚于 OnBuyUgcItemSuccess），
        // 用于确保 LoadCommunityPoses/LoadMinePoses 读到最新数据
        MessageHelper.AddListener<List<InventoryData>>(MessageName.AvaterDatabaseOnDataChange, OnBackpackDataChanged);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.OnUgcAnimStudioPublishedListChange, OnPosePublished);
        MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnPosePurchased);
        MessageHelper.RemoveListener<List<InventoryData>>(MessageName.AvaterDatabaseOnDataChange, OnBackpackDataChanged);
    }

    private void OnEnable()
    {
        // 消费待刷新 Tab（面板不可见期间收到发布/购买事件时写入）
        if (_pendingTabRefresh.HasValue)
        {
            var tab = _pendingTabRefresh.Value;
            _pendingTabRefresh = null;
            SwitchTab(tab);
            return;
        }

        if (_currentTab == PoseTab.Mine || _currentTab == PoseTab.Community)
        {
            RefreshPoseList();
        }
    }

    /// <summary>我创作的姿势发布后回调：面板可见时立即刷新 Mine Tab，否则记录待刷新</summary>
    private void OnPosePublished()
    {
        if (!gameObject.activeInHierarchy)
        {
            _pendingTabRefresh = PoseTab.Mine;
            return;
        }

        if (_currentTab != PoseTab.Mine)
            return;

        RefreshPoseList();
    }

    /// <summary>
    /// 社区姿势购买成功后回调。
    /// 注意：此消息早于背包缓存更新触发，面板可见时的实际刷新由 OnBackpackDataChanged 负责；
    /// 面板不可见时记录待刷新 Tab，等 OnEnable 时消费。
    /// </summary>
    private void OnPosePurchased(string ugcId)
    {
        if (!gameObject.activeInHierarchy)
        {
            _pendingTabRefresh = PoseTab.Community;
        }
    }

    /// <summary>
    /// 背包本地缓存更新后回调（晚于 OnBuyUgcItemSuccess），此时 GetGoodsData 返回的数据已包含新购入的商品。
    /// 仅在面板可见且当前处于 Mine / Community Tab 时刷新。
    /// </summary>
    private void OnBackpackDataChanged(List<InventoryData> changedDatas)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (_currentTab != PoseTab.Mine && _currentTab != PoseTab.Community)
            return;

        RefreshPoseList();
    }

    /// <summary>根据服务器数据切换到对应页签并默认选中当前姿势</summary>
    public void SetData(CabinCharacterBaseInfo info)
    {
        _info = info;
        var detail = info?.coverInfo?.GetDetail() ?? new CabinCoverDetail();
        _currentPoseId = detail.poseId;
        SwitchTab(DetermineTab(detail.poseId, detail.poseResourceType));
    }

    /// <summary>
    /// 根据 poseResourceType 判断来源：官方（Pose）直接返回 Official；
    /// UGC（UgcPose）再通过 poseId 在背包中查 Tag 区分创作与社区购买
    /// </summary>
    private PoseTab DetermineTab(string poseId, int poseResourceType)
    {
        if (string.IsNullOrEmpty(poseId)) return PoseTab.Official;
        if (poseResourceType == (int)ResourceType.Pose) return PoseTab.Official;
        var dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        var datas = dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)_poseSubType));
        foreach (var data in datas)
        {
            var asset = data.GetFirstAsset<AssetsData>();
            if (asset == null || asset.Id != poseId) continue;
            if (asset.InventoryData.Tag == Network.Message.BackpackTag.Creator) return PoseTab.Mine;
            if (asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag) return PoseTab.Community;
        }
        return PoseTab.Official;
    }

    /// <summary>切换 Tab 并刷新姿势列表</summary>
    private void SwitchTab(PoseTab tab)
    {
        _currentTab = tab;
        RefreshPoseList();
    }

    /// <summary>销毁旧 Item，清空选中态，重置 Tab 高亮，再根据当前 Tab 重新加载数据</summary>
    private void RefreshPoseList()
    {
        foreach (var go in _poseItemList)
            Destroy(go);
        _poseItemList.Clear();
        _selectedItem = null;
        Btn_Official.interactable = true;
        Btn_Mine.interactable = true;
        Btn_Community.interactable = true;
        GoCreat.gameObject.SetActive(false);
        GoBuy.gameObject.SetActive(false);
        switch (_currentTab)
        {
            case PoseTab.Official: LoadOfficialPoses(); break;
            case PoseTab.Mine: LoadMinePoses(); break;
            case PoseTab.Community: LoadCommunityPoses(); break;
        }
    }

    /// <summary>
    /// 加载官方姿势：从本地 officialPose.json 读取，按 poseSubType 过滤
    /// 图标使用 textureUrl（官方配置的图标图片，非封面）
    /// </summary>
    private void LoadOfficialPoses()
    {
        Btn_Official.interactable = false;

        var wrapper = Loader.Load<TextAsset>(OfficialPoseConfigPath);
        if (wrapper == null)
        {
            LoggerUtils.LogError("[DraftBoxCharacterPosePanel] 官方姿势配置文件读取失败");
            return;
        }

        // RetainAsset 绑定到当前 GameObject，面板销毁时自动释放资源
        var textAsset = wrapper.RetainAsset(gameObject);
        Debug.Log("[DraftBoxCharacterPosePanel] =" + textAsset.text);
        var configs = JsonConvert.DeserializeObject<List<OfficialPoseConfig>>(textAsset.text);

        // 缓存本次加载结果，供 FindOfficialPoseData 使用（避免重复 IO）
        _officialPoseConfigs = configs;

        foreach (var config in configs)
        {
            // 按姿势子类型过滤（单人/双人/宠物等）
            if (config.poseType != (int)_poseSubType)
                continue;

            CreatePoseItem(config.id, config.textureUrl, config.name, (int)ResourceType.Pose, config.poseData.poseData);
        }
    }

    /// <summary>
    /// 根据 poseId 从静态官方姿势缓存中查找并返回 poseData 字符串（KeyFrameData JSON）。
    /// 缓存由 LoadOfficialPoses() 在首次展示官方 Tab 时填充，调用时机保证早于 ApplyInitialPose。
    /// </summary>
    /// <param name="poseId">官方姿势 ID</param>
    /// <returns>KeyFrameData JSON 字符串；未找到时返回 null</returns>
    public static string FindOfficialPoseData(string poseId)
    {
        if (string.IsNullOrEmpty(poseId) || _officialPoseConfigs == null)
            return null;

        foreach (var config in _officialPoseConfigs)
        {
            if (config.id == poseId)
                return config.poseData?.poseData;
        }

        return null;
    }

    /// <summary>
    /// 加载我创作的姿势：从背包数据中过滤 BackpackTag.Creator 的 UgcPoseAssetsData，
    /// 再通过 AssetsDataManager.GetPoseInfo 获取完整 PoseInfo（优先走缓存）
    /// 图标使用 cover（编辑器保存的封面图）
    /// </summary>
    private void LoadMinePoses()
    {
        GoCreat.gameObject.SetActive(true);
        Btn_Mine.interactable = false;
        var dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        var datas = dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)_poseSubType));
        foreach (var data in datas)
        {
            if (!IsMinePredicate(data)) continue;
            var asset = data.GetFirstAsset<AssetsData>();
            if (asset == null) continue;
            var id = asset.Id;
            AssetsDataManager.GetPoseInfo(id, (success, recommendItem) =>
            {
                if (!success) return;
                var poseInfo = recommendItem?.UgcInfo as PoseInfo;
                if (poseInfo == null) return;
                CreatePoseItem(id, poseInfo.cover, poseInfo.name, (int)ResourceType.UgcPose, poseInfo.poseData);
            });
        }
    }

    /// <summary>
    /// 加载社区购买的姿势：从背包数据中过滤 BackpackTag.ErrBackpackTag 的 UgcPoseAssetsData
    /// 图标使用 cover（编辑器保存的封面图）
    /// </summary>
    private void LoadCommunityPoses()
    {
        GoBuy.gameObject.SetActive(true);
        Btn_Community.interactable = false;
        var dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
        var datas = dataHandler.GetGoodsData(UniqueType.Get(ResourceType.UgcPose, (int)_poseSubType));
        foreach (var data in datas)
        {
            if (!IsCommunityPredicate(data)) continue;
            var asset = data.GetFirstAsset<AssetsData>();
            if (asset == null) continue;
            var id = asset.Id;
            AssetsDataManager.GetPoseInfo(id, (success, recommendItem) =>
            {
                if (!success) return;
                var poseInfo = recommendItem?.UgcInfo as PoseInfo;
                if (poseInfo == null) return;
                CreatePoseItem(id, poseInfo.cover, poseInfo.name, (int)ResourceType.UgcPose, poseInfo.poseData);
            });
        }
    }

    /// <summary>过滤条件：我创作的（BackpackTag 为 Creator）</summary>
    private bool IsMinePredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UgcPoseAssetsData)) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.Creator;
    }

    /// <summary>过滤条件：社区购买（BackpackTag 为 ErrBackpackTag，购买但非自创作）</summary>
    private bool IsCommunityPredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.ButtonType == ButtonType.TakeOff) return false;
        if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UgcPoseAssetsData)) return false;
        return asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag;
    }

    /// <summary>点击姿势 Item：切换选中高亮态，广播姿势选中事件，直接写入 _info.coverInfo</summary>
    private void OnPoseItemClick(DraftBoxPoseItem item, string emoteId, int resourceType, string poseData)
    {
        _selectedItem?.SetSelected(false);
        _selectedItem = item;
        item.SetSelected(true);
        MessageHelper.Broadcast(MessageName.OnCabinEditorPoseSelected, poseData);

        _currentPoseId = emoteId;

        // 直接写入 _info，_info 是 DraftBoxEditorPanel._data 的引用（深拷贝数据）
        if (_info != null)
        {
            if (_info.coverInfo == null)
                _info.coverInfo = new CabinCoverInfo();
            var detail = _info.coverInfo.GetDetail();
            detail.poseId = emoteId;
            detail.poseResourceType = resourceType;
            _info.coverInfo.SetDetail(detail);
        }
    }

    /// <summary>实例化姿势 Item 预制体，绑定封面图和点击回调</summary>
    private void CreatePoseItem(string emoteId, string coverUrl, string name, int resourceType, string poseData)
    {
        var go = Instantiate(PoseItemPrefab, PoseContainer);
        go.SetActive(true);
        var item = go.GetComponent<DraftBoxPoseItem>();
        if (item != null)
        {
            item.SetData(emoteId, coverUrl, name, () => OnPoseItemClick(item, emoteId, resourceType, poseData));
            if (emoteId == _currentPoseId)
            {
                _selectedItem?.SetSelected(false);
                _selectedItem = item;
                item.SetSelected(true);
            }
        }
        _poseItemList.Add(go);
    }

    /// <summary>
    /// officialPose.json 单条姿势配置，仅供本面板内部使用
    /// </summary>
    private class OfficialPoseConfig
    {
        public string id;
        public string name;
        public int poseType;
        public PoseInfo poseData;
        public string textureUrl;
    }
}
