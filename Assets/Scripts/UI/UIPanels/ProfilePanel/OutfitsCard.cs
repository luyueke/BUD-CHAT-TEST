using System.Collections;
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class OutfitsCard : BaseCard
    {
        [SerializeField] private PublishOutfitView publishOutfitView;
        [SerializeField] private int shrinkLimit;

        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            cardBgType = ProfileCardBgType.Bg3;
        }
        public bool RefreshWithData(string uid,SkinListRsp listData)
        {
            //数据为空则不显示
            if (listData == null || listData.list == null || listData.list.Count <= 0)
            {
                Show(false);
                return false;
            }
            Show(true);
            publishOutfitView.SetUid(uid);

            if (listData.list.Count <= shrinkLimit)
            {
                Shrink();
            }
            else
            {
                Expand();
            }

            publishOutfitView.Preload(listData.list,listData.cookie,listData.isEnd);

            return true;
        }
    }
}
