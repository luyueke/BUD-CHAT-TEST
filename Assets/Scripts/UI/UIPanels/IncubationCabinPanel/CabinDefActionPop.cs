using System;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public enum CabinDefActionType
    {
        All = 0,
        Activation = 1,
        VoiceCommand = 2,
    }

    public class CabinDefActionPop : BasePanel<CabinDefActionPop>
    {
        [SerializeField] private Transform content;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private LoadingButton addBtn;
        [SerializeField] private Button closeBtn;
        [SerializeField] private Text titleTxt;
        [SerializeField] private Text titleTxt2;

        public Action<List<Es.CabinDefActionConfig>, Action<bool>> onConfirmAction;

        private readonly List<Es.CabinDefActionConfig> _selectedConfigs = new();
        private readonly HashSet<CabinDefActionItem> _selectedItems = new();

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            _selectedConfigs.Clear();
            _selectedItems.Clear();
            addBtn.interactable = false;

            closeBtn.onClick.AddListener(OnCloseBtnClick);
            addBtn.onClick.AddListener(OnAddBtnClick);

            itemPrefab.SetActive(false);

            var actionType = CabinDefActionType.All;
            if (args != null && args.Length > 0 && args[0] is CabinDefActionType type)
            {
                actionType = type;
            }

            titleTxt.text = actionType == CabinDefActionType.VoiceCommand ? "系统默认指令动作" : "系统默认唤醒动作";
            titleTxt2.text = actionType == CabinDefActionType.VoiceCommand ? "添加指令动作" : "添加唤醒动作";

            int itemIndex = 0;
            var configList = Es.DataTables.GetCabinDefActionConfigList();
            foreach (var config in configList)
            {
                bool commandEmpty = string.IsNullOrEmpty(config.Command);
                if (actionType == CabinDefActionType.Activation && !commandEmpty)
                {
                    continue;
                }
                if (actionType == CabinDefActionType.VoiceCommand && commandEmpty)
                {
                    continue;
                }
                string displayName = commandEmpty ? $"唤醒动作{itemIndex + 1}" : config.Command;
                var go = Instantiate(itemPrefab, content);
                go.SetActive(true);
                go.GetComponent<CabinDefActionItem>().Init(config, OnItemSelected, displayName);
                itemIndex++;
            }
        }

        private void OnItemSelected(Es.CabinDefActionConfig config, CabinDefActionItem item)
        {
            if (_selectedItems.Contains(item))
            {
                _selectedItems.Remove(item);
                _selectedConfigs.Remove(config);
                item.SetSelected(false);
            }
            else
            {
                _selectedItems.Add(item);
                _selectedConfigs.Add(config);
                item.SetSelected(true);
            }
            addBtn.interactable = _selectedConfigs.Count > 0;
        }

        private void OnCloseBtnClick()
        {
            CloseSelf();
        }

        private void OnAddBtnClick()
        {
            if (_selectedConfigs.Count == 0)
            {
                TipPanel.ShowToast("请先选择一个默认动作");
                return;
            }
            addBtn.ShowLoading();
            onConfirmAction?.Invoke(new List<Es.CabinDefActionConfig>(_selectedConfigs), (success) =>
            {
                if (this == null)
                {
                    return;
                }
                addBtn.HideLoading();
                if (success)
                {
                    CloseSelf();
                }
            });
        }
    }
}
