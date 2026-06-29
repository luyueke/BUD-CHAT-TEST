using System.Collections;
using System.Collections.Generic;
using Game.Store;
using GameData;
using UI.UIPanels.ProfilePanel;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class PetWearingCard : BaseCard
    {
        [SerializeField] private PetWearingItem ItemPrefab;
        [SerializeField] private Transform ContentRoot;

        private PetWearingItem CreateItem()
        {
            PetWearingItem newItem = Instantiate(ItemPrefab, ContentRoot);
            newItem.gameObject.SetActive(true);
            return newItem;
        }

        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            ShowLoading(true);
            cardBgType = ProfileCardBgType.Bg2;
        }
        
        public bool RefreshWithData(string uid,List<WearingInfo> resList)
        {
            //数据为空则不显示
            if (resList == null ||  resList.Count <= 0)
            {
                Show(false);
                return false;
            }
            
 
            int count = 0;
            foreach (var itemData in resList)
            {
                if (itemData.resType == (int)WearingType.PGC)
                {
                    var config = Es.DataTables.GetPetAvatarCommonData(itemData.pgcId);
                    if (config == null)
                    {
                        continue;
                    }
                    //过滤免费资源
                    if (AssetsDataManager.IsFreeAssets(itemData.pgcId))
                    {
                        continue;
                    }
                }

                //是私单，并且没有拥有的情况下，不展示
                if (itemData.isPrivateOrder == 1 && !AssetsDataManager.IsOwned(itemData.ugcId))
                {
                    continue;
                }

                var itemNode = CreateItem();
                itemNode.SetData(itemData);
                count++;
            }

            if (count > 0)
            {
                Show(true);
            }

            ShowLoading(false);

            
            return count > 0;
        }
    }
}
