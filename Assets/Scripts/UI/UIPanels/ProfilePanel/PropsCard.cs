using System.Collections;
using System.Collections.Generic;
using GameData;
using UI.UIPanels.ProfilePanel;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class PropsCard : BaseCard
    {
        [SerializeField] private PublishPropView publishView;
        [SerializeField] private int shrinkLimit;

        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            cardBgType = ProfileCardBgType.Bg3;
        }
        public bool RefreshWithData(string uid,PropListRsp listData)
        {
            //数据为空则不显示
            if (listData == null || listData.list == null || listData.list.Count <= 0)
            {
                Show(false);
                return false;
            }
            
            Show(true);
            publishView.SetUid(uid);
            
            if (listData.list.Count <= shrinkLimit)
            {
                Shrink();
            }
            else
            {
                Expand();
            }
            
            publishView.Preload(listData.list,listData.cookie,listData.isEnd);
            return true;
        }
    }
}
