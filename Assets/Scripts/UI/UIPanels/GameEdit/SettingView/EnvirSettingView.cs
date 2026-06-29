using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.UIPanels.GameEdit.SettingView
{
    public class EnvirSettingView : BaseSettingView
    {

        [SerializeField]
        private GameObject itemImplObj;
        private List<EnvirSettingItem> items = new List<EnvirSettingItem>();

        private void Awake()
        {
            itemImplObj.SetActive(false);
            foreach (var envSettingData in Es.DataTables.GetGameEnvSettingDataList())
            {
                var envItem = GameObject.Instantiate(itemImplObj, transform).GetComponent<EnvirSettingItem>();
                envItem.gameObject.SetActive(true);
                envItem.InitItem(envSettingData, OnSettingSelected);
                items.Add(envItem);
            }
        }

        public override void OnTabSelected()
        {
            foreach (var tmpItem in items)
            {
                tmpItem.SetSelect(false);
            }
        }

        public void OnSettingSelected(EnvirSettingItem item)
        {
            foreach (var tmpItem in items)
            {
                tmpItem.SetSelect(item == tmpItem);
            }
            if (!string.IsNullOrWhiteSpace(item.GetData().UIAdapter))
            {
                UIManager.Inst.ClosePanel(UIManager.Inst.FindPanel(PanelId.GameGlobalSettingPanel));
                var panel = UIManager.Inst.OpenPanel(PanelId.GamePropertyEditPanel);
                var adapterType = Type.GetType($"UI.UIPanels.GameEdit.{item.GetData().UIAdapter}");
                LoggerUtils.Log($"EnvirSettingView.OnSelectEntity adapterType = {adapterType.FullName}");
                if (panel.gameObject.GetComponent(adapterType) == null)
                    (panel.gameObject.AddComponent(adapterType) as BasePropertyAdapter).SetSelectEntity(null);
            }

            if (item.GetData().PanelId != 0)
            {
                UIManager.Inst.OpenPanel((PanelId)item.GetData().PanelId);
            }
            
            CloseSettingPanel();
        }

        private void CloseSettingPanel()
        {
            var settingPanel = UIManager.Inst.FindPanel(PanelId.GameGlobalSettingPanel);
            if (settingPanel != null)
            {
                UIManager.Inst.ClosePanel(settingPanel);
            }
        }


    }
}