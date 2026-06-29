using System.Collections.Generic;
using GameData.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CommunityGame
{
    public class CommunityGamesSpotlightRightItem : MonoBehaviour
    {
        public SpotlightItem prefab;
        public CText title;
        public RectTransform content;
        public OfficalRecommendItem spotlightSectionItem;
        public string sectionId;

        public void OnInitCreate(OfficalRecommendItem spotlightSectionItem)
        {
            DestroyAllChildren();
            
            this.spotlightSectionItem = spotlightSectionItem;
            this.sectionId = spotlightSectionItem.sectionId;
            title.text = this.spotlightSectionItem.sectionName;
            
            if (spotlightSectionItem.ugcList == null || spotlightSectionItem.ugcList.Count == 0)
            {
                return;
            }
            
            for(int i = 0; i < spotlightSectionItem.ugcList.Count; i++)
            {
                var spItem = Instantiate(prefab, content);
                spItem.SetData(spotlightSectionItem.ugcList[i]);
            }
        }

        private void DestroyAllChildren()
        {
            if (content.childCount <= 0)
                return;
            
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
        }
    }
}