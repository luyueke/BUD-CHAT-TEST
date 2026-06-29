using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EventTracking;
using Game.Store;
using UnityEngine;
using UnityEngine.UI;
using static Game.COSXML.Model.Tag.DeleteResult;

namespace UI.UIPanels.FittingRoom
{
    public class SectionList : MonoBehaviour
    {
        [SerializeField] ToggleGroup sectionContent;
        [SerializeField] SectionItem sectionItem;
        [SerializeField] Button searchButton;
        [SerializeField] Button filterButton;
        [SerializeField] Button settingButton;

        //private List<SectionUIData> sectionList;
        private Queue<SectionItem> sectionItemPool = new();
        private Dictionary<SectionUIData, SectionItem> sectionDict = new();
        private SectionItem curSelectedItem;

        private Action<SectionUIData> onValueChanged;
        private Action onOpenSearch;
        private Action onOpenFilter;
        private Action onOpenSetting;
        public SectionUIData curSelectData;



        public bool isInit;
        private void Awake()
        {
            searchButton.onClick.AddListener(OnSearchClick);
            if(filterButton != null)
            {
                filterButton.onClick.AddListener(OnFilterClick);
            }
            if(settingButton != null)
            {
                settingButton.onClick.AddListener(OnSettingClick);
            }
        }


        public void Init(bool showFilterAndSetting){
            if(filterButton != null)
            {
                filterButton.gameObject.SetActive(showFilterAndSetting);
            }
            if(settingButton != null)
            {
                settingButton.gameObject.SetActive(showFilterAndSetting);
            }
        }

        public void OnSearchClick()
        {
            onOpenSearch?.Invoke();
           
            if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
            {
                LoadEvent.ReportPopupStatus("SearchClick", "ClickSearch");
            }
        }

        public void OnFilterClick(){
            onOpenFilter?.Invoke();
        }
        public void OnSettingClick(){
            onOpenSetting?.Invoke();
        }

        public void SetSection(SectionUIData selected, List<SectionUIData> list, bool _isInit = false)
        {
            if (sectionContent == null)
            {
                return;
            }

            //sectionList = list;
            isInit = _isInit;
            UnuseAllItem();
            if (list == null || list.Count == 0) return;
            for (int i = 0, C = list.Count; i < C; i++)
            {
                var data = list[i];
                SectionItem item;
                if (sectionItemPool.Count > 0)
                {
                    item = sectionItemPool.Dequeue();
                }
                else
                {
                    item = Instantiate(sectionItem, sectionContent.transform);
                }
                item.SetToggleGroup(sectionContent);
                item.gameObject.SetActive(true);
                item.transform.SetSiblingIndex(2 + i);
                item.SetSelectedCallBack(OnSelecedSection);
                item.SetSection(data);
                sectionDict.Add(data, item);
            }
            var selectItem = list.Contains(selected) ? selected : list[0];
            OnSelecedSection(selectItem);
        }

        public void OnSelecedSection(SectionUIData data)
        {
            SetSelectedItem(data);
            curSelectData = data;
            if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc && !isInit)
            {
                LoadEvent.ReportPopupStatus("SectionClicked", "ClickUGCSection");
            }
            else
            {
                isInit = false;
            }
           
            onValueChanged?.Invoke(data);
        }
        public void OnSelecedSection(int index)
        {
            //SetSelectedItem(sectionDict.ElementAt(index).Value);
            if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc && !isInit)
            {
                LoadEvent.ReportPopupStatus("SectionClicked", "ClickUGCSection");
            }
            else
            {
                isInit = false;
            }

            onValueChanged?.Invoke(sectionDict.ElementAt(index).Key);
        }
        private void UnuseAllItem()
        {
            if (searchButton != null)
            {
                searchButton.gameObject.SetActive(false);
            }

            foreach (var kv in sectionDict)
            {
                if (kv.Value != null)
                {
                    kv.Value.gameObject.SetActive(false);
                    kv.Value.SetToggleGroup(null);
                    sectionItemPool.Enqueue(kv.Value);
                }
            }

            sectionDict.Clear();
        }

        public void SetSelectedItem(SectionUIData data)//SectionItem item)
        {
            curSelectedItem?.SetIsOn(false);
            curSelectedItem = sectionDict[data];
            curSelectedItem.SetIsOn(true);
        }

        public void SetCallback(Action<SectionUIData> action)
        {
            onValueChanged = action;
        }

        public void ShowSearch(Action action)
        {
            //旧搜索功能去掉
            // searchButton.gameObject.SetActive(true);
            onOpenSearch = action;
        }

        public void ShowFilter(Action action)
        {
            if(filterButton != null)
            {
                filterButton.gameObject.SetActive(true);
            }
            onOpenFilter = action;
        }

        public void HideFilter()
        {
            if(filterButton != null)
            {
                filterButton.gameObject.SetActive(false);
            }
        }

        public void ShowSetting(Action action)
        {
            if(settingButton != null)
            {
                settingButton.gameObject.SetActive(true);
            }
            onOpenSetting = action;
        }
        public void HideSetting()
        {
            if(settingButton != null)
            {
                settingButton.gameObject.SetActive(false);
            }
        }
    }

    public class SectionUIData
    {
        public static Color SelectedColor;

        public string Id;
        public string Name;
        public Color BgColor;

        public Game.Store.SectionConfig sectionConfig;

        public BUDSeriesData seriesConfig;

        public SectionUIData(string id, string name, Color bgColor)
        {
            Id = id;
            Name = name;
            BgColor = bgColor;
        }
    }
}