using System.Collections.Generic;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData.BaseInfo;
using UI.UIPanels.AINPC.AIBuddyInMap;
using UI.UIPanels.IncubationCabin;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    /// <summary>
    /// 地图编辑器「盒子」子页：为放置的 AI 伙伴选择一个盒子（场景方案）。
    /// 数据源同养成舱控制台 ScenePanel（BoxScene_DataLoader.GetBoxSceneList）。列表三类项：
    /// 「无盒子」用静态 emptyCard 表示（BoxId=""，渲染时不放盒子）；
    /// 「默认基础款」为固定项（BoxId=DefaultBoxId，渲染默认盒子外观，对标养成舱 DefaultScene）；
    /// 其余为服务器自定义盒子（BoxId=场景id + metaDataUrl，渲染自定义贴图）。
    /// 选中后写入 AIBuddyInMapComponent.BoxId/BoxMetaUrl，并由 AIBuddyInMapUIManager 在场景中渲染盒子（无碰撞）。
    /// </summary>
    public class AIBuddyBoxEditSubView : BasePropertyEditSubView
    {
        // 「默认基础款」sentinel id（非空，用于选中态追踪与 round-trip；渲染侧据 metaUrl 空走 defaultModelPath）。
        // 与养成舱 CabinControllScenePanel 保持一致。
        private const string DefaultBoxId = "__defaultScene__";

        [SerializeField] private GameObject boxItemPrefab; // CabinSceneCardItem
        [SerializeField] private Transform boxItemRoot;    // 盒子卡父节点
        [SerializeField] private CabinSceneCardItem emptyCard; // 无盒子卡（静态，视觉由 prefab 决定，选中 = 不放盒子）

        private AIBuddyInMapComponent _comp;
        private AIBuddyInMapBehaviour _behaviour;
        private BoxScene_DataLoader _dataLoader;
        // 卡片 → 其数据（CabinSceneCardItem._data 私有，故在此自存一份用于选中态判定）
        private readonly List<CabinSceneCardItem> _cards = new();
        private readonly List<BoxSceneInfo> _cardDatas = new();

        protected override void OnInit()
        {
        }

        public override void OnSelectEntity(SceneEntity entity)
        {
            base.OnSelectEntity(entity);
            _comp = selectEntity.GetComp<AIBuddyInMapComponent>();
            _behaviour = selectEntity.GetBehaviour<AIBuddyInMapBehaviour>();
            BuildList();
        }

        private void BuildList()
        {
            ClearList();
            if (boxItemPrefab == null || boxItemRoot == null) return;
            boxItemPrefab.SetActive(false);

            // 「无盒子」用 prefab 里静态摆放的 emptyCard（视觉由 prefab 决定），选中 = 不配置任何盒子
            if (emptyCard != null)
                emptyCard.onSelectClick = _ => OnSelectNoBox();

            // 「默认基础款」固定项（DefaultScene 视觉，渲染默认盒子外观）
            SpawnCard(new BoxSceneInfo { id = DefaultBoxId, specialType = BoxSceneSpecialType.DefaultScene });

            RefreshSelectState();

            if (_dataLoader == null) _dataLoader = gameObject.GetOrAddComponent<BoxScene_DataLoader>();
            _dataLoader.ResetCookie();
            _dataLoader.GetBoxSceneList(OnGetBoxList);
        }

        private void OnGetBoxList(List<BoxSceneInfo> list)
        {
            if (this == null) return;
            if (list != null)
            {
                foreach (var info in list)
                {
                    if (info == null) continue;
                    info.specialType = BoxSceneSpecialType.None;
                    SpawnCard(info);
                }
            }
            RefreshSelectState();
        }

        private void SpawnCard(BoxSceneInfo info)
        {
            var go = Instantiate(boxItemPrefab, boxItemRoot);
            go.SetActive(true);
            var card = go.GetComponent<CabinSceneCardItem>();
            if (card == null) return;
            card.SetData(info);
            card.onSelectClick = OnBoxClicked;
            _cards.Add(card);
            _cardDatas.Add(info);
            RefreshSelectState();
        }

        private void OnBoxClicked(BoxSceneInfo info)
        {
            if (info == null) return;
            if (info.specialType == BoxSceneSpecialType.DefaultScene)
                ApplyBox(DefaultBoxId, "");          // 默认基础款（渲染默认盒子外观）
            else
                ApplyBox(info.id, info.metaDataUrl); // 服务器自定义盒子
        }

        private void OnSelectNoBox() => ApplyBox("", ""); // 无盒子（不渲染盒子）

        private void ApplyBox(string boxId, string boxMetaUrl)
        {
            if (_comp == null) return;
            _comp.BoxId = boxId;
            _comp.BoxMetaUrl = boxMetaUrl;
            if (_behaviour != null)
                AIBuddyInMapUIManager.Inst.RefreshBoxForEdit(_behaviour, boxId, boxMetaUrl);
            RefreshSelectState();
        }

        private void RefreshSelectState()
        {
            string curBoxId = _comp != null ? _comp.BoxId : "";
            // 无盒子卡：当前未配置盒子时选中
            if (emptyCard != null)
                emptyCard.SetSelectState(string.IsNullOrEmpty(curBoxId));
            // 动态卡均为真实盒子（None），按 id 匹配
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                var data = _cardDatas[i];
                if (card == null || data == null) continue;
                bool selected = !string.IsNullOrEmpty(curBoxId) && data.id == curBoxId;
                card.SetSelectState(selected);
            }
        }

        private void ClearList()
        {
            foreach (var c in _cards)
                if (c != null) Destroy(c.gameObject);
            _cards.Clear();
            _cardDatas.Clear();
        }
    }
}
