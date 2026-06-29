using System.Collections.Generic;
using BUD.GameStudio;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    /// <summary>
    /// 创造工坊页面
    /// </summary>
    public class ProfileGameView : MonoBehaviour
    {
        public NavigationBarTabs navigationBarTabs;

        private List<GameStudioPanel.GameStudioConfig> rtConfig = new()
        {
            new() { name = "地图", type = GameStudioPanel.GameStudioEnum.Map },
            new() { name = "素材", type = GameStudioPanel.GameStudioEnum.Prop },
            new() { name = "材质", type = GameStudioPanel.GameStudioEnum.Material }
        };

        [SerializeField] private GameObject[] views;

        public ProfileMap _profileMap;
        public ProfileMap _profileProp;
        public ProfileMap _profileMaterial;

        private string _currentUid;


        private bool isInit = false;


        public void InitUI(string uid)
        {
            if (isInit)
            {
                return;
            }

            isInit = true;
            this._currentUid = uid;
            navigationBarTabs.AddBackBtnClickListener(BackClick);
            foreach (var cfg in rtConfig)
            {
                navigationBarTabs.CreateItem(cfg.type.ToString(), cfg.name).SetIsSelect(false);
            }

            navigationBarTabs.SetSelect(0);
            navigationBarTabs.AddItemSelectCallBack(RTClick);

            _profileMap.InitUI(Category.Map, _currentUid);
        }

        private void BackClick()
        {
            UIManager.Inst.BackToLastWindow();
        }

        void RTClick(TabItem item, int index)
        {
            for (var i = 0; i < views.Length; i++)
            {
                views[i].SetActive(i == index);
            }

            var data = rtConfig[index];
            switch (data.type)
            {
                case GameStudioPanel.GameStudioEnum.Map:
                    _profileMap.InitUI(Category.Map, _currentUid);
                    break;
                case GameStudioPanel.GameStudioEnum.Prop:
                    _profileProp.InitUI(Category.Prop, _currentUid);
                    break;
                case GameStudioPanel.GameStudioEnum.Material:
                    _profileMaterial.InitUI(Category.Material, _currentUid);
                    break;
                default:
                    break;
            }
        }
    }
}