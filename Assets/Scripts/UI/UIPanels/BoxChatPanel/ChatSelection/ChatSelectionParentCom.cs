using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// AI引导选择面板：单选/多选chips + 自己写 + 跳过
    /// 多选模式：选中chip或输入框有内容时启用发送；发送内容为选中values与输入文本的","拼接
    /// </summary>
    public class ChatSelectionParentCom : MonoBehaviour
    {
        public Text txt_question;
        public Button btn_jump;

        public ChatSelectionInputCom chatSelectionInputCom;

        public ChatSelectionItemCom itemPrefab;  // chip item 预制体
        public Transform itemParent;             // chip 列表容器

        public GameObject selectionContentGo;
        public GameObject emptyGo;


        string _selectionMode;
        int _multiMax;
        string _skipValue;
        readonly List<ChatSelectionItemCom> _activeItems = new();
        readonly List<(string label, string value)> _chipData = new();
        Action<string> _onSend;
        bool _interactable = true;

        public void SetData(string content, Action<string> onSend)
        {
            _onSend = onSend;

            var data = JsonConvert.DeserializeObject<ChatSelectionData>(content);

            txt_question.text = data.reply;
            _selectionMode = string.IsNullOrEmpty(data.selection_mode) ? "single" : data.selection_mode;
            _multiMax = data.multi_max > 0 ? data.multi_max : 1;

            bool isMulti = _selectionMode == "multi";

            // 清理旧 items
            foreach (var old in _activeItems)
                if (old != null) DestroyImmediate(old.gameObject);
            _activeItems.Clear();

            bool hasCustomWrite = false;
            bool hasSkip = false;

            int index = 1;
            foreach (var chip in data.chips)
            {
                if (chip.label == "自己写") { hasCustomWrite = true; continue; }
                if (chip.label == "跳过") { hasSkip = true; _skipValue = chip.value; continue; }

                var item = Instantiate(itemPrefab, itemParent);
                item.gameObject.SetActive(true);
                item.SetData(chip.label, chip.value, isMulti, OnItemClick, index++);
                _activeItems.Add(item);
            }


            emptyGo.SetActive(index == 1);
            if (hasSkip)
            {
                txt_question.GetComponent<RectTransform>().sizeDelta=new(770,84);
            }
            else
            {
                txt_question.GetComponent<RectTransform>().sizeDelta=new(984,84);
                
            }

            btn_jump.gameObject.SetActive(hasSkip);
            btn_jump.onClick.RemoveAllListeners();
            btn_jump.onClick.AddListener(OnSkip);

            // 多选模式：始终显示输入区作为发送入口
            // 单选模式：仅当有"自己写"chip时显示
            bool showInput = isMulti || hasCustomWrite;
            chatSelectionInputCom.gameObject.SetActive(showInput);
            if (showInput)
                chatSelectionInputCom.SetData(OnCombinedSend);
            // 始终保持在 itemParent 最后一位（chips 动态实例化后排在它前面）
            chatSelectionInputCom.transform.SetAsLastSibling();

            UICommonUtils.RefreshLayout(itemParent);
            UICommonUtils.RefreshLayout(selectionContentGo.transform);
            UICommonUtils.RefreshLayout(this.gameObject.transform);
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
        }

        void OnItemClick(ChatSelectionItemCom item)
        {
            if (!_interactable) return;
            if (_selectionMode == "single")
            {
                _onSend?.Invoke(item.Value);
            }
            else
            {
                // 超出 multi_max 时撤销本次勾选
                int selectedCount = _activeItems.Count(i => i.IsSelected);
                if (selectedCount > _multiMax)
                    item.SetSelected(false);

                // 通知输入组件：chips是否有选中，驱动发送按钮可用性
                chatSelectionInputCom.SetExternalContent(_activeItems.Any(i => i.IsSelected));
            }
        }

        /// <summary>
        /// 多选或自己写的统一发送回调：将选中chips的value与输入文本用","拼接
        /// </summary>
        void OnCombinedSend(string inputText)
        {
            if (!_interactable) return;
            var parts = new List<string>();
            parts.AddRange(_activeItems.Where(i => i.IsSelected).Select(i => i.Value));
            if (!string.IsNullOrEmpty(inputText)) parts.Add(inputText);
            if (parts.Count > 0)
                _onSend?.Invoke(string.Join(",", parts));
        }

        void OnSkip()
        {
            if (!_interactable) return;
            _onSend?.Invoke(_skipValue);
        }

        /// <summary>
        /// 流式中途检测到 selection_mode 变化时调用，重建所有已追加的 items。
        /// </summary>
        public void UpdateSelectionMode(string selMode, int multiMax)
        {
            string newMode = string.IsNullOrEmpty(selMode) ? "single" : selMode;
            int newMax = multiMax > 0 ? multiMax : 1;
            if (newMode == _selectionMode && newMax == _multiMax) return;

            _selectionMode = newMode;
            _multiMax = newMax;
            bool isMulti = _selectionMode == "multi";

            foreach (var old in _activeItems)
                if (old != null) DestroyImmediate(old.gameObject);
            _activeItems.Clear();

            int index = 1;
            foreach (var (label, value) in _chipData)
            {
                var item = Instantiate(itemPrefab, itemParent);
                item.gameObject.SetActive(true);
                item.SetData(label, value, isMulti, OnItemClick, index++);
                _activeItems.Add(item);
            }
            emptyGo.SetActive(_activeItems.Count == 0);

            if (isMulti && !chatSelectionInputCom.gameObject.activeSelf)
            {
                chatSelectionInputCom.gameObject.SetActive(true);
                chatSelectionInputCom.SetData(OnCombinedSend);
            }
            chatSelectionInputCom.transform.SetAsLastSibling();

            UICommonUtils.RefreshLayout(itemParent);
            UICommonUtils.RefreshLayout(selectionContentGo.transform);
            UICommonUtils.RefreshLayout(this.gameObject.transform);
        }

        public float GetContentHeight()
        {
            return selectionContentGo.GetComponent<RectTransform>().sizeDelta.y;
        }

        // ── 流式逐chip追加 API ──────────────────────────────────────────────────

        /// <summary>流式初始化：清空旧数据，不含chips，chips后续通过AppendChip追加</summary>
        public void InitStream(string selMode, int multiMax, Action<string> onSend)
        {
            _onSend = onSend;
            _interactable = false;
            _selectionMode = string.IsNullOrEmpty(selMode) ? "single" : selMode;
            _multiMax = multiMax > 0 ? multiMax : 1;

            foreach (var old in _activeItems)
                if (old != null) DestroyImmediate(old.gameObject);
            _activeItems.Clear();
            _chipData.Clear();

            // txt_question.text = "";
            txt_question.GetComponent<RectTransform>().sizeDelta = new Vector2(984, 84);
            btn_jump.gameObject.SetActive(false);
            _skipValue = "";
            emptyGo.SetActive(true);
            chatSelectionInputCom.gameObject.SetActive(false);
            chatSelectionInputCom.transform.SetAsLastSibling();

            UICommonUtils.RefreshLayout(itemParent);
            UICommonUtils.RefreshLayout(selectionContentGo.transform);
            UICommonUtils.RefreshLayout(this.gameObject.transform);
        }

        /// <summary>
        /// 流式追加一个chip（处理"自己写"/"跳过"特殊label）。
        /// 每到达一个完整chip JSON时调用一次。
        /// </summary>
        public void AppendChip(ChatSelectionChip chip)
        {
            if (chip == null || string.IsNullOrEmpty(chip.label)) return;
            bool isMulti = _selectionMode == "multi";

            if (chip.label == "跳过")
            {
                _skipValue = chip.value;
                txt_question.GetComponent<RectTransform>().sizeDelta = new Vector2(770, 84);
                btn_jump.gameObject.SetActive(true);
                btn_jump.onClick.RemoveAllListeners();
                btn_jump.onClick.AddListener(OnSkip);
                UICommonUtils.RefreshLayout(selectionContentGo.transform);
                UICommonUtils.RefreshLayout(this.gameObject.transform);
                return;
            }

            if (chip.label == "自己写")
            {
                chatSelectionInputCom.gameObject.SetActive(true);
                chatSelectionInputCom.SetData(OnCombinedSend);
                chatSelectionInputCom.transform.SetAsLastSibling();
                UICommonUtils.RefreshLayout(itemParent);
                UICommonUtils.RefreshLayout(selectionContentGo.transform);
                UICommonUtils.RefreshLayout(this.gameObject.transform);
                return;
            }

            int index = _activeItems.Count + 1;
            var item = Instantiate(itemPrefab, itemParent);
            item.gameObject.SetActive(true);
            item.SetData(chip.label, chip.value, isMulti, OnItemClick, index);
            _activeItems.Add(item);
            _chipData.Add((chip.label, chip.value));
            emptyGo.SetActive(false);
            chatSelectionInputCom.transform.SetAsLastSibling();

            UICommonUtils.RefreshLayout(itemParent);
            UICommonUtils.RefreshLayout(selectionContentGo.transform);
            UICommonUtils.RefreshLayout(this.gameObject.transform);
        }
    }
}
