using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.UGCData;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.UIPanels.ProfilePanel
{
    public class PublishPropView : MonoBehaviour
    {
        [SerializeField] private ProfilePropEntry gameEntry;
        [SerializeField] private ProfilePropDataLoader gameLoader;
        private string _currentUid;


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

        public void Preload(List<PropResInfo> infos, string cookie = "",int isEnd = 1)
        {
            gameEntry.InitCommunityGameDatas(infos);
            gameLoader.SetCookie(cookie);
            gameLoader.SetIsEnd(isEnd);
        }


        private void GameLoopGridViewItemTrigger(PropResInfo data, Texture texture)
        {
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Prop, data.propInfo.id, data.propInfo.ugcStyle);
        }

        private void IsEmptyAction()
        {
        }

    }
}
