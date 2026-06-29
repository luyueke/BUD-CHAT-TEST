using System.Collections.Generic;
using BUD.GameStudio;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileAvatarView : MonoBehaviour
    {
        public NavigationBarTabs navigationBarTabs;

        private List<GameStudioPanel.GameStudioConfig> rtConfig = new()
        {
            new() { name = "衣服", type = GameStudioPanel.GameStudioEnum.Avatar }
        };

        [SerializeField] private GameObject[] views;

        public ProfileMap _profileAvatar;


        private bool isInit = false;

        private string _currentUid;


        public void InitUI(string uid)
        {
            if (isInit)
            {
                return;
            }

            isInit = true;

            _currentUid = uid;
            navigationBarTabs.AddBackBtnClickListener(BackClick);
            foreach (var cfg in rtConfig)
            {
                navigationBarTabs.CreateItem(cfg.type.ToString(), cfg.name).SetIsSelect(false);
            }

            navigationBarTabs.SetSelect(0);
            navigationBarTabs.AddItemSelectCallBack(RTClick);

            _profileAvatar.InitUI(Category.Avatar, _currentUid);
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
                case GameStudioPanel.GameStudioEnum.Avatar:
                    _profileAvatar.InitUI(Category.Avatar, _currentUid);
                    break;
                default:
                    break;
            }
        }
    }
}