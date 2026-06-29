using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Event
{
    public class EventCenterRewardItemData
    {
        public int id;
    }
    
    public class EventCenterRewardItem : MonoBehaviour
    {
        public Image Img_Bg;
        public Image Img_Icon;
        public CButton Btn_View;

        public void InitData(string bgColor, string id)
        {
            Img_Bg.color = DataUtil.DeSerializeColorCheckHash(bgColor);
            PgcUtils.GetIconSpriteByPgcIdAsync(id,gameObject, (sprite) =>
            {
                if (this != null && Img_Icon != null && sprite != null)
                {
                    Img_Icon.sprite = sprite;
                }
            });
            
            Btn_View.onClick.AddListener(() => { });
        }
    }
}
