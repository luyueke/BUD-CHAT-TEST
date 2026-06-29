using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>筛选面板中的一个 Tab 分组，动态生成内部 ToneShopTag 列表</summary>
    public class ToneShopTagFiltrateItem : MonoBehaviour
    {
        /// <summary>"不限"标签的固定名称，选中时不参与 tagId 筛选</summary>
        public const string NoLimitTagName = "不限";

        [SerializeField] private Text TabLabel;
        [SerializeField] private Transform ToggleContent;
        [SerializeField] private GameObject TogglePrefab;
        [SerializeField] private GoToggleGroup TagGroup;

        private string _tabName;
        private Action<string, string, bool> _onValueChanged;
        private readonly List<ToneShopTag> _tagInstances = new List<ToneShopTag>();

        /// <param name="tabName">分组标签名（如"年龄"、"性别"、"标签"）</param>
        /// <param name="tags">该分组下的具体标签名列表</param>
        /// <param name="onValueChanged">回调：(tabName, tagName, isOn)</param>
        public void SetData(string tabName, List<string> tags, Action<string, string, bool> onValueChanged)
        {
            _tabName = tabName;
            _onValueChanged = onValueChanged;

            if (TabLabel != null)
            {
                TabLabel.text = tabName;
            }

            BuildTags(tags);
        }

        private void BuildTags(List<string> tags)
        {
            foreach (var tag in _tagInstances)
            {
                if (tag != null)
                {
                    Destroy(tag.gameObject);
                }
            }

            _tagInstances.Clear();

            if (TogglePrefab == null || ToggleContent == null)
            {
                return;
            }

            TogglePrefab.SetActive(false);

            // 在实际标签列表前插入"不限"作为默认的无筛选选项
            var allTags = new List<string> { NoLimitTagName };
            if (tags != null)
            {
                allTags.AddRange(tags);
            }

            foreach (var tagName in allTags)
            {
                var go = Instantiate(TogglePrefab, ToggleContent);
                go.SetActive(true);

                var tag = go.GetComponent<ToneShopTag>();
                if (tag != null)
                {
                    tag.SetGroup(TagGroup);
                    var capturedTab = _tabName;
                    tag.SetData(tagName, (name, isOn) =>
                    {
                        _onValueChanged?.Invoke(capturedTab, name, isOn);
                    });
                    _tagInstances.Add(tag);
                }
            }

            if (_tagInstances.Count > 0)
            {
                _tagInstances[0].Select();
            }
        }

        public void ResetToggles()
        {
            foreach (var tag in _tagInstances)
            {
                tag?.Reset();
            }
        }
    }
}
