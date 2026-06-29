using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.PgcData;
using GameData.UGCData;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class PublishOutfitView : MonoBehaviour
    {
        [SerializeField] private ProfileOutfitEntry gameEntry;
        [SerializeField] private ProfileOutfitDataLoader gameLoader;
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
        
        public void Preload(List<SkinResInfo> infos, string cookie = "",int isEnd = 1)
        {
            gameEntry.InitCommunityGameDatas(infos);
            gameLoader.SetCookie(cookie);
            gameLoader.SetIsEnd(isEnd);
        }

        
        private void GameLoopGridViewItemTrigger(SkinResInfo data, Texture texture)
        {
            var isBundle = data != null && data.skinInfo != null && data.skinInfo.subType == (int)AvatarSubType.Bundle;
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel,isBundle? AssetDetailType.UgcBundle: AssetDetailType.Skin, data.skinInfo.id, data.skinInfo.ugcStyle);
        }

        private void IsEmptyAction()
        {
        }

    }
}
