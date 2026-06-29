using System;
using System.Collections.Generic;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 控制台互动子界面，通过 ToggleGroup 切换口令/激活/待机三个子面板
    /// Date: 26-04-09
    /// </summary>
    public class CabinControllInturnPanel : MonoBehaviour
    {
        [SerializeField] private Button SkinBtn;            // 切换回皮肤界面的按钮

        [Header("Toggle 组")]
        [SerializeField] private Toggle CommandTg;          // 口令 Tab
        [SerializeField] private Toggle ActivateTg;         // 激活 Tab
        [SerializeField] private Toggle StandTg;            // 待机 Tab

        [Header("互动子面板")]
        [SerializeField] private GameObject CommandPanel;   // 口令子面板根节点
        [SerializeField] private GameObject ActivatePanel;  // 激活子面板根节点
        [SerializeField] private GameObject StandPanel;     // 待机子面板根节点

        [Header("口令列表")]
        [SerializeField] private ScrollRect CommandScrollRect;  // 口令列表滚动容器
        [SerializeField] private GameObject CommandItemPrefab;  // 口令条目预制体（挂 CabinControllCommandItem）

        [Header("激活列表")]
        [SerializeField] private ScrollRect ActivateScrollRect;  // 激活列表滚动容器
        [SerializeField] private GameObject ActivateItemPrefab;  // 激活条目预制体（挂 CabinControllActivateItem）

        [Header("待机列表")]
        [SerializeField] private ScrollRect StandScrollRect;    // 待机列表滚动容器
        [SerializeField] private GameObject StandItemPrefab;    // 待机条目预制体（挂 CabinControllStandItem）

        /// <summary>点击皮肤按钮后通知父级切换回皮肤界面</summary>
        public Action onSkinClick;

        /// <summary>点击口令 Item 预览按钮时触发，由父级绑定到 BoxPanel.PreviewVoiceCommands</summary>
        public Action<voiceCommands> onCommandPreviewClick;

        /// <summary>点击激活 Item 预览按钮时触发，由父级绑定到 BoxPanel.PreviewActivation</summary>
        public Action<characterInteraction> onActivatePreviewClick;

        /// <summary>点击待机 Item 预览按钮时触发，由父级绑定播放逻辑</summary>
        public Action<pEmoteData> onStandPreviewClick;

        private CabinCharacterUgcInfo _data;

        // 各列表的对象池（复用 Item，避免频繁 Instantiate/Destroy）
        private readonly List<CabinControllCommandItem> _commandPool = new();
        private readonly List<CabinControllActivateItem> _activatePool = new();
        private readonly List<CabinControllStandItem> _standPool = new();

        #region 初始化

        /// <summary>
        /// 绑定所有按钮与 Toggle 事件，由父级 CabinControllSkinInturnPanel 在 InitUI 中调用一次
        /// </summary>
        public void InitUI()
        {
            SkinBtn.onClick.AddListener(() => onSkinClick?.Invoke());

            // 三个主 Tab：选中时切换对应子面板并刷新列表
            CommandTg.onValueChanged.AddListener(on => { if (on) ShowSubPanel(CommandPanel); });
            ActivateTg.onValueChanged.AddListener(on => { if (on) ShowSubPanel(ActivatePanel); });
            StandTg.onValueChanged.AddListener(on => { if (on) ShowSubPanel(StandPanel); });
        }

        #endregion

        #region 显示 / 隐藏

        /// <summary>
        /// 面板显示时缓存数据，默认选中口令 Tab
        /// </summary>
        public void OnShow(CabinCharacterUgcInfo data)
        {
            _data = data;
            CommandTg.isOn = false;
            CommandTg.isOn = true; // 触发 ShowSubPanel(CommandPanel) → RefreshCommandPanel
        }

        /// <summary>
        /// 面板隐藏时清理数据引用
        /// </summary>
        public void OnHide()
        {
            _data = null;
        }

        #endregion

        #region 子面板切换

        /// <summary>
        /// 切换到目标子面板并刷新其列表内容
        /// </summary>
        private void ShowSubPanel(GameObject target)
        {
            CommandPanel.SetActive(CommandPanel == target);
            ActivatePanel.SetActive(ActivatePanel == target);
            StandPanel.SetActive(StandPanel == target);

            if (CommandPanel == target) RefreshCommandPanel();
            else if (ActivatePanel == target) RefreshActivatePanel();
            else if (StandPanel == target) RefreshStandPanel();
        }

        #endregion

        #region 口令列表

        /// <summary>
        /// 回收旧 Item 并重建口令列表（对象池复用）。
        /// 数据来源：CabinCharacterUgcInfo.voiceCommands
        /// </summary>
        private void RefreshCommandPanel()
        {
            RecycleToPool(CommandScrollRect.content, _commandPool);

            var list = _data?.voiceCommands ?? new List<voiceCommands>();
            for (int i = 0; i < list.Count; i++)
            {
                int idx = i;
                var item = GetFromPool(_commandPool, CommandItemPrefab, CommandScrollRect.content);
                item.Init(list[i], i);
                item.onPreviewClick = () => onCommandPreviewClick?.Invoke(list[idx]);
            }

            GlobalFuncExtensions.RefreshLayout(CommandScrollRect.content);
        }

        #endregion

        #region 激活列表

        /// <summary>
        /// 回收旧 Item 并重建唤醒动作列表（对象池复用）。
        /// 数据来源：CabinCharacterUgcInfo.activation
        /// </summary>
        private void RefreshActivatePanel()
        {
            RecycleToPool(ActivateScrollRect.content, _activatePool);

            var list = _data?.activation ?? new List<characterInteraction>();
            for (int i = 0; i < list.Count; i++)
            {
                int idx = i;
                var item = GetFromPool(_activatePool, ActivateItemPrefab, ActivateScrollRect.content);
                item.Init(list[i], i);
                item.onPreviewClick = () => onActivatePreviewClick?.Invoke(list[idx]);
            }

            GlobalFuncExtensions.RefreshLayout(ActivateScrollRect.content);
        }

        #endregion

        #region 待机列表

        /// <summary>
        /// 回收旧 Item 并重建待机动作列表（对象池复用）。
        /// 数据来源：loopEmoteList（循环）与 emoteList（非循环）合并展示。
        /// </summary>
        private void RefreshStandPanel()
        {
            RecycleToPool(StandScrollRect.content, _standPool);

            var allAnims = new List<pEmoteData>();
            var pendingEmote = _data?.pendingEmote;
            if (pendingEmote?.loopEmoteList != null)
                allAnims.AddRange(pendingEmote.loopEmoteList);
            if (pendingEmote?.emoteList != null)
                allAnims.AddRange(pendingEmote.emoteList);

            foreach (var anim in allAnims)
            {
                var captured = anim;
                var item = GetFromPool(_standPool, StandItemPrefab, StandScrollRect.content);
                item.Init(captured);
                item.onPreviewClick = () => onStandPreviewClick?.Invoke(captured);
            }

            GlobalFuncExtensions.RefreshLayout(StandScrollRect.content);
        }

        #endregion

        #region 对象池工具

        /// <summary>
        /// 将 parent 下所有子节点的组件 T 回收入对象池并隐藏
        /// </summary>
        private static void RecycleToPool<T>(Transform parent, List<T> pool) where T : MonoBehaviour
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var comp = parent.GetChild(i).GetComponent<T>();
                if (comp != null)
                {
                    comp.gameObject.SetActive(false);
                    pool.Add(comp);
                }
            }
        }

        /// <summary>
        /// 从对象池取出一个 T，池为空时实例化新对象
        /// </summary>
        private static T GetFromPool<T>(List<T> pool, GameObject prefab, Transform parent) where T : MonoBehaviour
        {
            while (pool.Count > 0)
            {
                var item = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
                if (item != null)
                {
                    item.gameObject.SetActive(true);
                    return item;
                }
            }
            var go = Instantiate(prefab, parent);
            go.SetActive(true);
            return go.GetComponent<T>();
        }

        #endregion
    }
}
