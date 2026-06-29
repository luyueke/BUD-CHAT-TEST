
using Com.TheFallenGames.OSA.DataHelpers;
using Game.AnimationStudio;
using Game.Avatar;
using Game.Database;
using Game.Store;
using GameData.PgcData;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{

    [Flags]
    public enum EmoteTabType
    {
        None = 0,
        Box = 1 << 0,  // 伙伴精选 (Tog_Box)
        Official = 1 << 1,  // 官方      (Tog_Pgc)
        Community = 1 << 2,  // 社区      (Tog_Ugc)
    }

    public class IncubationCabinEmotePopPanel : BasePanel<IncubationCabinEmotePopPanel>
    {
        [Header("列表")][SerializeField] internal IncubationCabinEmoteAdapter assetsList;
        [SerializeField] public Button closeBtn;
        [SerializeField] public LoadingButton confirmBtn;
        [SerializeField] public Button previewBtn;
        public List<Text> toggleTextList;
        public Toggle Tog_Box;
        public Toggle Tog_Pgc;
        public Toggle Tog_Ugc;

        public Text titleText;

        private static readonly EmoteTabType[] TabOrder =
        {
            EmoteTabType.Box,
            EmoteTabType.Official,
            EmoteTabType.Community,
        };

        EmoteTabType currentEmoteTab = EmoteTabType.Official;
        private EmoteTabType _visibleTabs;
        private bool _isMultiSelect;

        protected AvatarBagSceneHandler dataHandler;
        protected GoodsDataClassifyList assetsDatas = new();

        // true=主动画（循环），false=表演动画（非循环）
        private bool isLoopAnim = false;

        // 外部传入的 useType，用于查询 CabinPgcEmoConfig 配置表，决定官方 Tab 展示哪些 PGC 动画
        private int _useType = 0;

        // 外部注入的确认回调，参数为选中的商品列表和关闭面板的 Action
        public Action<List<GoodsData>, Action> onConfirmAction;

        // 当前选中的商品列表：单选时最多 1 项，多选时可有多项
        private readonly List<GoodsData> _selectGoodsDatas = new();

        // 已添加到角色动作列表的 ID 集合（PGC 用 emoteId，UGC 用 ugcData.id）
        // 通过 SetAlreadyAddedEmotes 由外部调用方在 OpenPanel 后注入
        private readonly HashSet<string> _alreadyAddedIds = new HashSet<string>();

        // 供预览使用的角色皮肤数据，由外部调用方在打开面板后设置
        internal CharacterData _previewCharacterData;

        // 面板打开时初始化：根据传入的循环标志设置标题、注册 Toggle 监听并加载数据
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            if (args.Length > 0 && args[0] is bool)
            {
                isLoopAnim = (bool)args[0];
                if (isLoopAnim)
                {
                    titleText.text = "添加主动画";
                }
                else
                {
                    titleText.text = "添加表演动画";
                }
            }

            _visibleTabs = args.Length > 1 && args[1] is EmoteTabType
                ? (EmoteTabType)args[1]
                : EmoteTabType.Official | EmoteTabType.Community;
            _selectGoodsDatas.Clear();

            Tog_Box.gameObject.SetActive((_visibleTabs & EmoteTabType.Box) != EmoteTabType.None);
            Tog_Pgc.gameObject.SetActive((_visibleTabs & EmoteTabType.Official) != EmoteTabType.None);
            Tog_Ugc.gameObject.SetActive((_visibleTabs & EmoteTabType.Community) != EmoteTabType.None);

            _previewCharacterData = args.Length > 2 ? args[2] as CharacterData : null;
            _isMultiSelect = args.Length > 3 && args[3] is bool isMulti && isMulti;
            _useType = args.Length > 4 && args[4] is int ut ? ut : 0;

            // 注册 Tab 切换监听：官方/社区 Toggle 互斥，isOn 时驱动列表刷新

            Tog_Box.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnSelectEmoteType(EmoteTabType.Box);
                }
            });

            Tog_Pgc.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnSelectEmoteType(EmoteTabType.Official);
                }
            });
            Tog_Ugc.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnSelectEmoteType(EmoteTabType.Community);
                }
            });

            // 获取背包数据处理器并订阅数据变更，保证背包更新时列表能同步
            dataHandler = AssetsDataManager.GetData<AvatarBagSceneHandler>();
            dataHandler.AddDataChange(gameObject, OnDataChange);

            // 初始化 OSA 列表，绑定数据工厂和选中回调
            assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
            assetsList.IsMultiSelect = _isMultiSelect;
            assetsList.Init();
            assetsList.OnItemSelected = OnItemSelected;

            InitEmoteUI();

            closeBtn.onClick.AddListener(OnCloseBtnClick);
            confirmBtn.interactable = false;
            confirmBtn.onClick.AddListener(OnConfirmBtnClick);
            previewBtn.gameObject.SetActive(_previewCharacterData != null);
            previewBtn.interactable = false;
            previewBtn.onClick.AddListener(OnPreviewBtnClick);
        }

        /// <summary>
        /// 设置已添加到角色动作列表的动作 ID 集合，用于在选动作弹窗中显示"已添加"状态。
        /// 应在 OpenPanel 后、用户看到列表前调用。PGC 传 emoteId，UGC 传 ugcData.id。
        /// </summary>
        /// <param name="emoteList">当前角色已有的待机动作列表</param>
        public void SetAlreadyAddedEmotes(List<pEmoteData> emoteList)
        {
            _alreadyAddedIds.Clear();

            if (emoteList == null)
                return;

            foreach (var emote in emoteList)
            {
                // PGC 动作用 emoteId 标识
                if (!string.IsNullOrEmpty(emote.emoteId))
                {
                    _alreadyAddedIds.Add(emote.emoteId);
                }
                // UGC 动作用 ugcData.id 标识
                else if (emote.ugcData != null && !string.IsNullOrEmpty(emote.ugcData.id))
                {
                    _alreadyAddedIds.Add(emote.ugcData.id);
                }
            }

            // 刷新列表以重新计算每个 item 的 IsAdded 状态
            if (assetsList?.Data != null)
            {
                assetsList.Data.ResetItems(assetsDatas.Count());
            }
        }

        // 列表项被选中时更新当前选中数据，若该动画需要额外引导则跳转动画工作室
        // 已添加的动作同样可以选中，确认时由调用方执行反选删除逻辑
        internal void OnItemSelected(GoodsData data)
        {
            // AddTips 不为空表示该动画尚未制作，跳转动画工作室引导用户创作
            if (!string.IsNullOrEmpty(data.AddTips))
            {
                UIManager.Inst.OpenPanel(PanelId.AnimationStudioMainPanel, AnimationStudioType.Animation);
                return;
            }

            if (_isMultiSelect)
            {
                int removed = _selectGoodsDatas.RemoveAll(g => g.Id == data.Id);
                if (removed == 0)
                {
                    _selectGoodsDatas.Add(data);
                }
            }
            else
            {
                _selectGoodsDatas.Clear();
                _selectGoodsDatas.Add(data);
            }
            confirmBtn.interactable = _selectGoodsDatas.Count > 0;
            previewBtn.interactable = _selectGoodsDatas.Count > 0;
            // 刷新列表以更新选中高亮
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        // OSA 数据工厂：按索引取商品数据，并标记是否已在选中列表中、是否已添加到角色动作列表
        private GoodsData CreateNewModel(int index)
        {
            var assetsData = assetsDatas.Get(index);
            assetsData.Selected = _selectGoodsDatas.Any(g => g.Id == assetsData.Id);
            assetsData.IsAdded = CheckIsAdded(assetsData);
            return assetsData;
        }

        /// <summary>
        /// 判断指定商品是否已添加到角色的动作列表中。
        /// PGC 通过 GoodsData.Id 匹配；UGC 通过 UgcAnimAssetsData 的 animInfo.id 匹配。
        /// </summary>
        /// <param name="assetsData">待检测的商品数据</param>
        /// <returns>已添加返回 true，否则返回 false</returns>
        private bool CheckIsAdded(GoodsData assetsData)
        {
            if (_alreadyAddedIds.Count == 0)
                return false;

            // PGC：直接用 GoodsData.Id 匹配（emoteId 与配置表 ID 一致）
            if (_alreadyAddedIds.Contains(assetsData.Id))
                return true;

            // UGC：背包 ID 与动画 ID 不同，需通过 UgcAnimAssetsData.UgcInfo.animInfo.id 匹配
            if (assetsData.Assets != null && assetsData.Assets.Count > 0
                && assetsData.Assets[0] is UgcAnimAssetsData ugcAsset)
            {
                var animId = ugcAsset.UgcInfo?.animInfo?.id;

                if (!string.IsNullOrEmpty(animId) && _alreadyAddedIds.Contains(animId))
                    return true;
            }

            return false;
        }

        void OnCloseBtnClick()
        {
            CloseSelf();
        }

        // 点击确认：触发外部注入的确认回调并显示 Loading，回调结束后由外部调用关闭
        void OnConfirmBtnClick()
        {
            if (onConfirmAction == null)
            {
                CloseSelf();
                return;
            }

            if (_selectGoodsDatas.Count == 0)
                return;

            confirmBtn.ShowLoading();
            onConfirmAction.Invoke(_selectGoodsDatas, CloseSelf);
        }

        void OnPreviewBtnClick()
        {
            if (_selectGoodsDatas.Count == 0)
                return;

            UIManager.Inst.OpenPanel(PanelId.CabinEmotePreviewPopPanel, _previewCharacterData, _selectGoodsDatas[0]);
        }

        // 重置所有 Toggle isOn 状态，按 TabOrder 顺序将第一个可见页签设为 true，
        // 由已注册的监听器触发 OnSelectEmoteType → RefreshContent
        public void InitEmoteUI()
        {
            Tog_Box.isOn = false;
            Tog_Pgc.isOn = false;
            Tog_Ugc.isOn = false;

            foreach (var tab in TabOrder)
            {
                if ((_visibleTabs & tab) == EmoteTabType.None)
                    continue;

                if (tab == EmoteTabType.Box)
                {
                    Tog_Box.isOn = true;
                }
                else if (tab == EmoteTabType.Official)
                {
                    Tog_Pgc.isOn = true;
                }
                else if (tab == EmoteTabType.Community)
                {
                    Tog_Ugc.isOn = true;
                }

                break;
            }
        }

        void OnDataChange(AssetsData[] data)
        {
            Debug.LogError("IncubationCabinEmotePopPanel OnDataChange: " + data.Length);
        }

        // 切换 Tab：更新选中指示文本并刷新对应列表
        public void OnSelectEmoteType(EmoteTabType index)
        {
            LoggerUtils.Log("OnSelectEmoteType: " + index);
            for (int i = 0; i < toggleTextList.Count; i++)
            {
                toggleTextList[i].gameObject.SetActive(TabOrder[i] == index);
            }
            currentEmoteTab = index;
            RefreshContent();
        }

        // 供外部调用的刷新入口，例如背包数据变更后主动触发
        public void RefreshEmoteContent()
        {
            RefreshContent();
        }

        /// <summary>
        /// 刷新列表内容，通过 OSA 虚拟列表渲染
        /// </summary>
        public void RefreshContent()
        {
            if (currentEmoteTab == EmoteTabType.Official)
            {
                var dataList = GetFreeListGoodsData();
                assetsDatas.SetData(dataList, null);
                assetsList.Data.ResetItems(assetsDatas.Count());
            }
            else if (currentEmoteTab == EmoteTabType.Community)
            {
                // 社区：我创作的 + 我购买的 合并
                // UGC 动画统一存储在 UgcAnimSubType.Single 下，循环/非循环由 InventoryData.Loop 区分
                int classType = UniqueType.Get(ResourceType.UgcEmote, (int)UgcAnimSubType.Single);
                var allUgc = dataHandler.GetGoodsData(classType);
                var dataList = allUgc.Where(d => (CreatePredicate(d) || BuyPredicate(d)) && LoopPredicate(d)).ToList();
                assetsDatas.SetData(dataList, null);
                assetsList.Data.ResetItems(assetsDatas.Count());
            }
            else if (currentEmoteTab == EmoteTabType.Box)
            {
                RefreshBoxContent();
            }
        }

        // 伙伴精选：汇总主角色及所有扩展包的 pendingEmote，按循环模式过滤后转换为 GoodsData
        private void RefreshBoxContent()
        {
            var characterInfo = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

            if (characterInfo == null)
            {
                assetsDatas.SetData(new List<GoodsData>(), null);
                assetsList.Data.ResetItems(0);
                return;
            }

            var sourceList = isLoopAnim
                ? characterInfo.pendingEmote?.loopEmoteList
                : characterInfo.pendingEmote?.emoteList;
            var allEmotes = new List<pEmoteData>(sourceList ?? new List<pEmoteData>());

            var packIds = characterInfo.extensionPackList;

            if (packIds == null || packIds.Count == 0)
            {
                ApplyBoxData(allEmotes);
                return;
            }

            CabinNetManager.Inst.GetExtensionPackBatchInfo(packIds, (success, packList) =>
            {
                if (success && packList != null)
                {
                    foreach (var pack in packList)
                    {
                        // 判断背包中是否已拥有该扩展包
                        var inv = !string.IsNullOrEmpty(pack.id) ? BagDatabase.Inst.Select(pack.id) : null;
                        bool isOwnedInBag = inv != null && inv.OwnedNum > 0;
                        if (!isOwnedInBag)
                        {
                            continue;
                        }
                        var packSource = isLoopAnim
                            ? pack.pendingEmote?.loopEmoteList
                            : pack.pendingEmote?.emoteList;

                        if (packSource != null)
                        {
                            allEmotes.AddRange(packSource);
                        }
                    }
                }

                ApplyBoxData(allEmotes);
            });
        }

        private void ApplyBoxData(List<pEmoteData> emoteDataList)
        {
            var dataList = BuildGoodsDataFromEmotes(emoteDataList);
            assetsDatas.SetData(dataList, null);
            assetsList.Data.ResetItems(assetsDatas.Count());
        }

        // 将 pEmoteData 列表转换为 GoodsData：PGC 从背包或配置表查找，UGC 从背包按 animInfo.id 匹配
        private List<GoodsData> BuildGoodsDataFromEmotes(List<pEmoteData> emoteDataList)
        {
            var result = new List<GoodsData>();
            var targetSubType = isLoopAnim ? EmoteSubType.SingleLoop : EmoteSubType.Single;

            var pgcIds = new List<string>();
            var ugcIds = new List<string>();

            foreach (var pEmote in emoteDataList)
            {
                if (!string.IsNullOrEmpty(pEmote.emoteId))
                {
                    pgcIds.Add(pEmote.emoteId);
                }
                else if (pEmote.ugcData != null)
                {
                    ugcIds.Add(pEmote.ugcData.id);
                }
            }

            if (pgcIds.Count > 0)
            {
                var pgcIdSet = new HashSet<string>(pgcIds);

                // 先从背包中查找已购 PGC
                var pgcClassType = UniqueType.Get(ResourceType.Emote, (int)targetSubType);
                var purchasedPgcList = dataHandler.GetGoodsData(pgcClassType);
                foreach (var gd in purchasedPgcList)
                {
                    if (pgcIdSet.Remove(gd.Id))
                    {
                        result.Add(gd);
                    }
                }

                // 剩余 ID 说明是免费 PGC，从配置表直接构建
                foreach (var id in pgcIdSet)
                {
                    // leisure/default 无配置表记录，直接构建内置待机 GoodsData
                    if (id == "leisure" || id == "default")
                    {
                        var idleName = id == "leisure" ? "待机" : "站立";
                        result.Add(new GoodsData
                        {
                            Id = id,
                            Name = idleName,
                            GoodsType = GoodsType.SinglePgc,
                            ButtonType = ButtonType.EmoteIdle,
                            IsOwned = true,
                            Assets = new List<AssetsData>
                            {
                                new EmoteAssetsData
                                {
                                    Id = id,
                                    Name = idleName,
                                    ResourceType = ResourceType.Emote,
                                    EmoteSubType = EmoteSubType.SingleLoop,
                                }
                            }
                        });
                        continue;
                    }

                    var tableData = Es.DataTables.GetEmoUIConfig(id);

                    if (tableData == null)
                        continue;

                    result.Add(new GoodsData
                    {
                        Id = id,
                        Name = tableData.name,
                        GoodsType = GoodsType.SinglePgc,
                        ButtonType = ButtonType.Assets,
                        IsOwned = true,
                        Assets = new List<AssetsData>
                        {
                            new EmoteAssetsData
                            {
                                Id = id,
                                Name = tableData.name,
                                ResourceType = ResourceType.Emote,
                                EmoteSubType = (EmoteSubType)tableData.emoType,
                            }
                        }
                    });
                }
            }

            if (ugcIds.Count > 0)
            {
                var ugcIdSet = new HashSet<string>(ugcIds);
                int ugcClassType = UniqueType.Get(ResourceType.UgcEmote, (int)UgcAnimSubType.Single);
                var allUgc = dataHandler.GetGoodsData(ugcClassType);

                foreach (var gd in allUgc)
                {
                    if (gd.Assets == null || gd.Assets.Count != 1)
                        continue;

                    if (gd.Assets[0] is not UgcAnimAssetsData ugcAsset)
                        continue;

                    var animId = ugcAsset.UgcInfo?.animInfo?.id;

                    if (animId != null && ugcIdSet.Contains(animId))
                    {
                        result.Add(gd);
                    }
                }
            }

            return result;
        }

        // 构建官方 Tab 的数据列表：从 CabinPgcEmoConfig[_useType] 读取 emoID 列表，再合并玩家已购的 PGC 动画
        private List<GoodsData> GetFreeListGoodsData()
        {
            var result = new List<GoodsData>();
            var targetSubType = isLoopAnim ? EmoteSubType.SingleLoop : EmoteSubType.Single;

            // 主动画模式下，在列表开头插入内置待机选项
            if (isLoopAnim)
            {
                result.Add(new GoodsData
                {
                    Id = "leisure",
                    Name = "待机",
                    GoodsType = GoodsType.SinglePgc,
                    ButtonType = ButtonType.EmoteIdle,
                    IsOwned = true,
                    Assets = new List<AssetsData>
                    {
                        new EmoteAssetsData
                        {
                            Id = "leisure",
                            Name = "默认",
                            ResourceType = ResourceType.Emote,
                            EmoteSubType = EmoteSubType.SingleLoop,
                        }
                    },
                });
                result.Add(new GoodsData
                {
                    Id = "default",
                    Name = "站立",
                    GoodsType = GoodsType.SinglePgc,
                    ButtonType = ButtonType.EmoteIdle,
                    IsOwned = true,
                    Assets = new List<AssetsData>
                    {
                        new EmoteAssetsData
                        {
                            Id = "default",
                            Name = "站立",
                            ResourceType = ResourceType.Emote,
                            EmoteSubType = EmoteSubType.SingleLoop,
                        }
                    },
                });
            }

            // 从配置表获取当前 useType 对应的 emoID 列表
            var pgcEmoConfig = Es.DataTables.GetCabinPgcEmoConfig(_useType);

            if (pgcEmoConfig == null)
            {
                // useType 无对应配置时，仅返回主动画待机选项，官方列表为空
                return result;
            }

            // 解析逗号分隔的 emoID 字符串
            var emoIds = pgcEmoConfig.emoID.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            foreach (var id in emoIds)
            {
                var tableData = Es.DataTables.GetEmoUIConfig(id);

                if (tableData == null)
                {
                    LoggerUtils.Log("配置不存在: " + id);
                    continue;
                }

                if (string.IsNullOrEmpty(tableData.pgcEmoteIconName))
                {
                    continue;
                }

                if ((EmoteSubType)tableData.emoType != targetSubType)
                {
                    LoggerUtils.Log($"{id} 配置类型不一致:{tableData.emoType}  筛选类型{targetSubType}");
                    continue;
                }

                var assetsData = new EmoteAssetsData
                {
                    Id = id,
                    Name = tableData.name,
                    ResourceType = ResourceType.Emote,
                    EmoteSubType = (EmoteSubType)tableData.emoType,
                };

                var goodsData = new GoodsData
                {
                    Id = id,
                    Name = tableData.name,
                    GoodsType = GoodsType.SinglePgc,
                    ButtonType = ButtonType.Assets,
                    IsOwned = true,
                    Assets = new List<AssetsData> { assetsData }
                };
                result.Add(goodsData);
            }

            // 追加已购的官方（PGC）动画，排除配置列表中已有的 ID
            var freeIds = new HashSet<string>(result.Select(g => g.Id));
            var pgcClassType = UniqueType.Get(ResourceType.Emote, (int)targetSubType);
            var purchasedPgcList = dataHandler.GetGoodsData(pgcClassType);
            foreach (var goodsData in purchasedPgcList)
            {
                if (goodsData == null)
                {
                    continue;
                }
                if (!goodsData.IsOwned)
                {
                    continue;
                }
                if (freeIds.Contains(goodsData.Id))
                {
                    continue;
                }
                // 校验 EmoteSubType 与当前面板匹配，防止后端 ClassType 未严格区分循环/非循环
                if (goodsData.Assets != null && goodsData.Assets.Count == 1
                    && goodsData.Assets[0] is EmoteAssetsData emoteAsset
                    && emoteAsset.EmoteSubType != targetSubType)
                {
                    continue;
                }

                result.Add(goodsData);
            }

            return result;
        }

        // 判断是否为用户自创（Creator 标签）的 UGC 商品
        bool CreatePredicate(GoodsData goodsData)
        {
            if (goodsData == null) return false;
            if (!string.IsNullOrEmpty(goodsData.AddTips)) return true;
            if (goodsData.ButtonType == ButtonType.Design) return false;
            if (goodsData.ButtonType == ButtonType.TakeOff) return true;
            if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
            if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
            if (!goodsData.IsOwned) return false;
            var asset = goodsData.Assets[0];
            if (!(asset is UGCAssetsData) && !(asset is MusicScoreAssetsData) && !(asset is UgcAnimAssetsData) && !(asset is UgcPoseAssetsData)) return false;
            if (asset.InventoryData == null) return false;
            // Creator 标签表示该资产由本人创作
            return asset.InventoryData.Tag == Network.Message.BackpackTag.Creator;
        }

        // 判断是否为用户购买（非 Creator 标签）的 UGC 商品
        bool BuyPredicate(GoodsData goodsData)
        {
            if (goodsData == null) return false;
            if (!string.IsNullOrEmpty(goodsData.AddTips)) return false;
            if (goodsData.ButtonType == ButtonType.Design) return false;
            if (goodsData.ButtonType == ButtonType.TakeOff) return true;
            if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc) return false;
            if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
            if (!goodsData.IsOwned) return false;
            var asset = goodsData.Assets[0];
            if (!(asset is UGCAssetsData) && !(asset is MusicScoreAssetsData) && !(asset is UgcAnimAssetsData) && !(asset is UgcPoseAssetsData)) return false;
            if (asset.InventoryData == null) return false;
            // ErrBackpackTag 为默认标签，表示通过商店购买而非自创
            return asset.InventoryData.Tag == Network.Message.BackpackTag.ErrBackpackTag;
        }

        // 按循环模式过滤 UGC 动画：通过 InventoryData.Loop（1=循环，0=非循环）与 isLoopAnim 匹配
        private bool LoopPredicate(GoodsData goodsData)
        {
            // UgcEmoteIdle 为默认待机占位项，循环模式下始终显示，非循环模式下隐藏
            if (goodsData.ButtonType == ButtonType.UgcEmoteIdle)
            {
                return isLoopAnim;
            }
            if (goodsData.ButtonType == ButtonType.Design)
                return false;

            if (goodsData.GoodsType != GoodsType.SinglePgc && goodsData.GoodsType != GoodsType.SingleUgc)
                return false;

            if (goodsData.Assets == null || goodsData.Assets.Count != 1)
                return false;

            if (!goodsData.IsOwned)
                return false;

            var asset = goodsData.Assets[0];

            if (!(asset is UgcAnimAssetsData))
                return false;

            if (isLoopAnim && asset.InventoryData.Loop == 1)
                return true;

            if (!isLoopAnim && asset.InventoryData.Loop == 0)
                return true;

            return false;
        }

    }
}
