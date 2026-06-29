using System.Collections.Generic;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class CharacterBoxCard : BaseCard
    {
        [SerializeField] private int shrinkLimit;
        [SerializeField] private ProfileCharacterBoxEntry gameEntry;

        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            cardBgType = ProfileCardBgType.Bg3;
        }

        void Start()
        {
            gameEntry.SetActions(OnItemClick);
        }

        public bool RefreshWithData(string uid, CharacterBoxPublishListData listData)
        {
            if (listData?.list == null || listData.list.Count == 0)
            {
                Show(false);
                return false;
            }

            Show(true);
            if (listData.list.Count <= shrinkLimit) Shrink(); else Expand();

            var infos = new List<CharacterBoxInfo>();
            foreach (var item in listData.list)
                if (item?.characterBoxInfo != null)
                    infos.Add(item.characterBoxInfo);

            gameEntry.InitData(infos);
            return true;
        }

        private void OnItemClick(CharacterBoxInfo info)
        {
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.CharacterBox, info.id);
        }
    }
}
