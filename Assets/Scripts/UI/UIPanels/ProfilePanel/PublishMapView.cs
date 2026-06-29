using System.Collections;
using System.Collections.Generic;
using Game.Base;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.UIPanels.ProfilePanel
{
    public class PublishMapView : MonoBehaviour
    {
        [SerializeField] private ProfileMapEntry gameEntry;
        [SerializeField] private ProfileMapDataLoader gameLoader;
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
        
        public void Preload(List<MapResInfo> infos, string cookie = "",int isEnd = 1)
        {
            gameEntry.InitCommunityGameDatas(infos);
            gameLoader.SetCookie(cookie);
            gameLoader.SetIsEnd(isEnd);
        }

        
        
        private void GameLoopGridViewItemTrigger(MapResInfo data, Texture texture)
        {
            if (!GameController.IsInHallScene())
            {
                TipPanel.ShowToast("您已经在游戏内，请退出房间后再试");
                return;
            }
            UIManager.Inst.SwapPanel(PanelId.MapDetailPanel, data.mapInfo.id);
        }

        private void IsEmptyAction()
        {
        }

    }
}
