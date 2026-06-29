using System.Collections;
using System.Collections.Generic;
using GameData;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.UIPanels.ProfilePanel
{
    public class PoseCard : BaseCard
    {
        [SerializeField] private int shrinkLimit;
        [SerializeField] private ProfileCommonEntry gameEntry;
        [SerializeField] private ProfileCommonDataLoader gameLoader;
        private string _currentUid;

        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            cardBgType = ProfileCardBgType.Bg3;
        }
        public bool RefreshWithData(string uid,MapListResponse listData)
        {
            //数据为空则不显示
            if (listData == null || listData.list == null || listData.list.Count <= 0)
            {
                Show(false);
                return false;
            }
            Show(true);
            SetUid(uid);

            if (listData.list.Count <= shrinkLimit)
            {
                Shrink();
            }
            else
            {
                Expand();
            }

            Preload(listData.list,listData.cookie,listData.isEnd);

            return true;
        }
        
        void Start()
        {
            gameEntry.SetLoader(gameLoader);
            gameEntry.SetActions(GameLoopGridViewItemTrigger, IsEmptyAction);
        }


        public void SetUid(string uid)
        {
            _currentUid = uid;
            gameLoader.ToUid = uid;
        }
        
        public void Preload(List<DraftListItem> infos, string cookie = "",int isEnd = 1)
        {
            gameEntry.InitCommunityGameDatas(infos);
            gameLoader.SetCookie(cookie);
            gameLoader.SetIsEnd(isEnd);
        }

        
        private void GameLoopGridViewItemTrigger(DraftListItem data, Texture texture)
        {
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcPose, data.poseInfo.id);
        }

        private void IsEmptyAction()
        {
        }
    }
}
