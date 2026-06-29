using System;
using System.Collections;
using System.Collections.Generic;
using BUD.GameStudio;
using UI.BaseWidgets;
using UnityEngine;

namespace AIGame.Base
{
    public class AIHospitalUgcStudio : MonoBehaviour
    {
        public CButton Btn_CreatePark;
        public CButton Btn_CreateNew;
        public NavigationBarTabs navigationBarTabs;
        public AIHospitalUgcStudioMainView gameStudioMainView;
        
        public class GameStudioConfig
        {
            public string name;
            public StudioSubType StudioSubType;
        }

        private List<GameStudioConfig> rtConfig = new()
        {
            new() { name = "草稿箱", StudioSubType = StudioSubType.Drafts },
            new() { name = "已发布", StudioSubType = StudioSubType.Published },
        };

        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            AddListener();
            InitNavigation();
        }

        private void AddListener()
        {
            Btn_CreateNew.onClick.AddListener(OnCreateBtnClick);
            Btn_CreatePark.onClick.AddListener(OnCreateParkBtn);
            Btn_CreatePark.gameObject.SetActive(false);
        }

        private void OnCreateBtnClick()
        {
            UIManager.Inst.OpenPanel(PanelId.AIHospitalUgcEditPanel, EditType.Create);
        }

        private void OnCreateParkBtn() 
        {
            UIManager.Inst.OpenPanel(PanelId.AIParkUgcEditPanel, EditType.Create);
        }

        private void InitNavigation()
        {
            foreach (var cfg in rtConfig)
            {
                navigationBarTabs.CreateItem(cfg.StudioSubType.ToString(), cfg.name).SetIsSelect(false);
            }
            navigationBarTabs.AddItemSelectCallBack(OnNavItemClick);
            navigationBarTabs.SetSelect((int)StudioSubType.Drafts);
        }
        
        private void OnNavItemClick(TabItem item, int index)
        {
            var data = rtConfig[index];
            gameStudioMainView.InitUI(data.StudioSubType);
        }
        
        private void OnDestroy()
        {
            
        }
    }
}
