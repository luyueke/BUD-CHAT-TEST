using System;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileToneItem : ProfileCommonItem
    {
        [SerializeField] protected GameObject extInfoRoot;
        [SerializeField] protected Text extInfoText;
        
        public override void SetData(DraftListItem item, Action<DraftListItem, Texture> onSelect)
        {
            base.SetData(item, onSelect);
            if (item == null)
            {
                return;
            }
            SetPrice(item.musicToneInfo.paymentInfo);
            //todo
            auditView.SetActive(FormatUtils.IsAuditing(item.musicToneInfo));
            // auditView.SetActive(false);
            
            extInfoRoot.SetActive(true);
            extInfoText.text = item.musicToneInfo.toneType == (int)ToneType.Fifteen ? "15音" : "22音";
            cover.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(item.musicToneInfo.cover))
            {
                cover.Load(item.musicToneInfo.cover, true, (fromCache,  success) =>
                {
                    if (success)
                    {
                        cover.gameObject.SetActive(true);
                    }
                });
            }
        }
    }
}