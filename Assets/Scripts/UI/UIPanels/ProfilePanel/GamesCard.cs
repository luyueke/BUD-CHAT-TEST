using System.Collections.Generic;
using GameData;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class GamesCard : BaseCard
    {

        [SerializeField] private MapDetailView mapDetailView;
        [SerializeField] private MapDetailView mapDetailShrink;
        [SerializeField] private PublishMapView publishMapView;
        [SerializeField] private SuperTextMesh moreGameTxt;
        [SerializeField] private Image detailBg;
        [SerializeField] private Image shrinkBg;

        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            cardBgType = ProfileCardBgType.Bg3;
        }
        public bool RefreshWithData(string uid,MapListRsp listData)
        {
            //数据为空则不显示
            if (listData == null || listData.list == null || listData.list.Count <= 0)
            {
                Show(false);
                return false;
            }
            
            Show(true);
            
            publishMapView.SetUid(uid);
            
            List<MapResInfo> resList = listData.list;
            if (resList.Count == 1)
            {
                Shrink();
                mapDetailShrink.Refresh(resList[0]);
            }
            else if (resList.Count > 1)
            {
                var mapInfo = resList[0];
                mapDetailView.Refresh(mapInfo);
                
                List<MapResInfo> preloadRemoveFirst = new List<MapResInfo>(resList);
                preloadRemoveFirst.RemoveAt(0); //移除第一个,已经在上方展示
                publishMapView.Preload(preloadRemoveFirst,listData.cookie,listData.isEnd);
            }

            return true;
        }


        public void SetUserInfo(AccountUserInfo userInfo)
        {
            moreGameTxt.SetLocalText("更多由{0}创作的游戏",userInfo.nickname);
        }

        public override void OnUpdateTheme(ProfileThemeInfo themeInfo)
        {
            base.OnUpdateTheme(themeInfo);
            if (themeInfo != null && themeInfo.colorInfo != null && !string.IsNullOrEmpty(themeInfo.colorInfo.cardSubColor))
            {
                detailBg.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.cardSubColor);
                shrinkBg.color = DataUtil.DeSerializeColorCheckHash(themeInfo.colorInfo.cardSubColor);
            }

        }

        public override void OnShow(string uid)
        {
            base.OnShow(uid);
            if (AccountDataManager.Inst.IsMySelf(uid))
            {
                AccountDataManager.Inst.AddUserInfoChangeListener(OnUserInfoChange);
            }
        }
        
        private void OnDestroy()
        {
            AccountDataManager.Inst.RemoveUserInfoChangeListener(OnUserInfoChange);
        }
        
        private void OnUserInfoChange(AccountUserInfo accountUserInfo)
        {
            SetUserInfo(accountUserInfo);
        }


        public override void Shrink()
        {
            base.Shrink();
            ShowFullView(false);
        }
        
        private void ShowFullView(bool isShow)
        {
            if (mapDetailView)
            {
                mapDetailView.gameObject.SetActive(isShow);
            }
            if (mapDetailShrink)
            {
                mapDetailShrink.gameObject.SetActive(!isShow);
            }
            publishMapView.gameObject.SetActive(isShow);
        }
    }
}
