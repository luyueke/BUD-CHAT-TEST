using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.UGCData;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class PublishMaterialView : MonoBehaviour
    {
        [SerializeField] private ProfileMaterialEntry gameEntry;
        [SerializeField] private ProfileMaterialDataLoader gameLoader;
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
        
        public void Preload(List<MaterialResInfo> infos, string cookie = "",int isEnd = 1)
        {
            gameEntry.InitCommunityGameDatas(infos);
            gameLoader.SetCookie(cookie);
            gameLoader.SetIsEnd(isEnd);
        }

        
        private void GameLoopGridViewItemTrigger(MaterialResInfo data, Texture texture)
        {
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Mat, data.materialInfo.id, data.materialInfo.ugcStyle);
        }

        private void IsEmptyAction()
        {
        }

    }
}
