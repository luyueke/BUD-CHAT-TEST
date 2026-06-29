using System;
using System.Collections.Generic;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.UIPanels.AINPC.AIBuddyInMap;
using UI.UIPanels.IncubationCabin;
using UI.UIWidgets;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    /// <summary>
    /// 地图编辑器「伙伴设置」子页：选择 Cabin AI 伙伴 + 其皮肤。
    /// 选择走 IncubationCabinRolesMainPanel 的导入模式；装配由 AIBuddyInMapUIManager 编排（拆基础类型驱动 Game 行为层）。
    /// </summary>
    public class AIBuddyEditSubView : BasePropertyEditSubView
    {
        [SerializeField] private GameObject hasAIBuddy;
        [SerializeField] private GameObject noAIBuddy;
        [SerializeField] private TextInputView InputText; // 这个不用了
        [SerializeField] private CButton AddNPCBtn;
        [SerializeField] private CButton EditNPCBtn; //功能变成更换AI伙伴
        [SerializeField] private GameObject skinPrefab;   // 皮肤卡 CabinSkinCardItem
        [SerializeField] private Transform skinRoot;      // 皮肤卡父节点
        private AIBuddyInMapComponent _aIBuddyInMapComponent;

        private AIBuddyInMapBehaviour _aIBuddyInMapBehaviour;
        private bool isSetAIBuddy = false;

        // 当前选中伙伴的本体数据（构建皮肤列表用）
        private CabinCharacterUgcInfo _currentInfo;
        private readonly List<CabinSkinCardItem> _skinCards = new();
        private int _skinBuildToken;
        private CabinSkinCardItem _selectedSkinCard;

        protected override void OnInit()
        {
            AddNPCBtn.onClick.AddListener(OnAddNPCClick);
            EditNPCBtn.onClick.AddListener(OnEditNPCClick);
            hasAIBuddy.SetActive(isSetAIBuddy);
            noAIBuddy.SetActive(!isSetAIBuddy);
            // 确保编排器存活（编辑态触发其监听 + 对已加载地图伙伴兜底装配）
            _ = AIBuddyInMapUIManager.Inst;
        }

        public override void OnSelectEntity(SceneEntity entity)
        {
            base.OnSelectEntity(entity);
            _aIBuddyInMapComponent = selectEntity.GetComp<AIBuddyInMapComponent>();
            _aIBuddyInMapBehaviour = selectEntity.GetBehaviour<AIBuddyInMapBehaviour>();
            isSetAIBuddy = !string.IsNullOrEmpty(_aIBuddyInMapComponent.AiBuddyID);
            hasAIBuddy.SetActive(isSetAIBuddy);
            noAIBuddy.SetActive(!isSetAIBuddy);

            ClearSkinList();
            _currentInfo = null;
            if (isSetAIBuddy)
            {
                // 选中已放置的伙伴：拉本体数据构建皮肤列表
                CabinNetManager.Inst.GetCabinCharacterInfo(_aIBuddyInMapComponent.AiBuddyID, (ok, detail) =>
                {
                    if (this == null) return;
                    if (!ok || detail?.characterInfo == null) return;
                    _currentInfo = detail.characterInfo;
                    BuildSkinList();
                });
            }
        }

        private void OnAddNPCClick() => OpenSelectPanel();
        private void OnEditNPCClick() => OpenSelectPanel();

        private void OpenSelectPanel()
        {
            Action<CabinCharacterBaseInfo, Action<bool>> onImport = (data, onDone) =>
            {
                var info = data as CabinCharacterUgcInfo;
                if (info == null)
                {
                    onDone?.Invoke(false);
                    return;
                }
                OnSelectAIBuddy(info);
                onDone?.Invoke(true);
            };
            UIManager.Inst.OpenPanel<IncubationCabinRolesMainPanel>(PanelId.IncubationCabinRolesMainPanel, onImport);
        }

        private void OnSelectAIBuddy(CabinCharacterUgcInfo info)
        {
            _currentInfo = info;
            _aIBuddyInMapComponent.AiBuddyID = info.id;
            // 换伙伴时皮肤回到默认
            var defaultSkin = CabinTools.GetDefaultSkin(info.skinPack);
            _aIBuddyInMapComponent.SkinPackId = defaultSkin?.packId ?? "";

            // 编排器装配（编辑态已持有完整 info，直接装配，不再拉取）
            AIBuddyInMapUIManager.Inst.SetupFromInfo(
                _aIBuddyInMapBehaviour, info,
                _aIBuddyInMapComponent.SkinPackId,
                _aIBuddyInMapComponent.BoxId,
                _aIBuddyInMapComponent.BoxMetaUrl);

            isSetAIBuddy = true;
            hasAIBuddy.SetActive(true);
            noAIBuddy.SetActive(false);
            BuildSkinList();
        }

        // ───────────── 皮肤列表（主体皮肤 + 扩展包，镜像 CameraModeNpcMenu.BuildSkinList） ─────────────

        private void BuildSkinList()
        {
            ClearSkinList();
            if (_currentInfo == null || skinPrefab == null || skinRoot == null) return;

            var token = ++_skinBuildToken;
            skinPrefab.SetActive(false);

            var skinItems = new List<CabinCharacterBaseInfo>();
            // 主体皮肤：每个 skinPack 克隆成一张卡（skinPack 收窄为单个）
            if (_currentInfo.skinPack != null)
            {
                foreach (var sp in _currentInfo.skinPack)
                {
                    var json = JsonConvert.SerializeObject(_currentInfo);
                    var wrapper = JsonConvert.DeserializeObject<CabinCharacterUgcInfo>(json);
                    wrapper.skinPack = new List<SkinPackInfo> { sp };
                    skinItems.Add(wrapper);
                }
            }

            // 扩展包皮肤（异步）
            if (_currentInfo.extensionPackList != null && _currentInfo.extensionPackList.Count > 0)
            {
                CabinNetManager.Inst.GetExtensionPackBatchInfo(_currentInfo.extensionPackList, (isS, packList) =>
                {
                    if (this == null) return;
                    if (token != _skinBuildToken) return; // 过期回调作废
                    if (isS && packList != null)
                    {
                        foreach (var pack in packList)
                        {
                            if (pack == null || pack.ugcclass != (int)UGCClass.Published || pack.skinPack == null) continue;
                            foreach (var sp in pack.skinPack)
                            {
                                var json = JsonConvert.SerializeObject(pack);
                                var wrapper = JsonConvert.DeserializeObject<CabinCharacterPackInfo>(json);
                                wrapper.skinPack = new List<SkinPackInfo> { sp };
                                skinItems.Add(wrapper);
                            }
                        }
                    }
                    SpawnSkinCards(skinItems);
                });
            }
            else
            {
                SpawnSkinCards(skinItems);
            }
        }

        private void SpawnSkinCards(List<CabinCharacterBaseInfo> datas)
        {
            CabinSkinCardItem firstItem = null;
            CabinSkinCardItem currentItem = null;
            var equippedPackId = _aIBuddyInMapComponent.SkinPackId;

            foreach (var data in datas)
            {
                if (data == null) continue;
                var go = Instantiate(skinPrefab, skinRoot);
                go.SetActive(true);
                var card = go.GetComponent<CabinSkinCardItem>();
                if (card == null) continue;

                card.SetData(data, _ => OnSkinClicked(card));
                if (data is CabinCharacterUgcInfo)
                    card.SetLockVisible(false);   // 主体皮肤随伙伴即拥有
                else
                    card.RefreshLock();            // 扩展包按拥有判断
                card.SetBadgesVisible(false);
                card.SetCurrent(false);
                _skinCards.Add(card);

                if (firstItem == null) firstItem = card;
                var sp = data.skinPack != null && data.skinPack.Count > 0 ? data.skinPack[0] : null;
                if (currentItem == null && sp != null && !string.IsNullOrEmpty(equippedPackId) && sp.packId == equippedPackId)
                    currentItem = card;
            }

            var current = currentItem ?? firstItem;
            SetCurrent(current);
        }

        private void OnSkinClicked(CabinSkinCardItem card)
        {
            var data = card?._data;
            var sp = data?.skinPack != null && data.skinPack.Count > 0 ? data.skinPack[0] : null;
            if (sp == null || string.IsNullOrEmpty(sp.avatarJson)) return;
            if (!IsSkinOwned(data))
            {
                TipPanel.ShowToast("请先获得该皮肤");
                return;
            }

            _aIBuddyInMapComponent.SkinPackId = sp.packId;
            _aIBuddyInMapBehaviour.RefreshBuddyAvatarByJson(sp.avatarJson);
            SetCurrent(card);
        }

        private void SetCurrent(CabinSkinCardItem card)
        {
            foreach (var c in _skinCards) c?.SetSelected(false);
            if (_selectedSkinCard != null) _selectedSkinCard.SetCurrent(false);
            _selectedSkinCard = card;
            if (card == null) return;
            card.SetSelected(true);
            card.SetCurrent(true);
        }

        private static bool IsSkinOwned(CabinCharacterBaseInfo data)
        {
            if (data is CabinCharacterUgcInfo) return true; // 主体皮肤
            if (data == null) return false;
            if (!string.IsNullOrEmpty(data.creator) && data.creator == AccountDataManager.Inst.Uid) return true;
            var inv = !string.IsNullOrEmpty(data.id) ? Game.Database.BagDatabase.Inst.Select(data.id) : null;
            return inv != null && inv.OwnedNum > 0;
        }

        private void ClearSkinList()
        {
            foreach (var c in _skinCards)
                if (c != null) Destroy(c.gameObject);
            _skinCards.Clear();
            _selectedSkinCard = null;
        }
    }
}
