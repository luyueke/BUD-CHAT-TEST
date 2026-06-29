using System;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfilePetOutfitItem : ProfileCommonItem
    {
        public override void SetData(DraftListItem item, Action<DraftListItem, Texture> onSelect)
        {
            base.SetData(item, onSelect);
            SetPrice(item.skinInfo.paymentInfo);
            auditView.SetActive(FormatUtils.IsAuditing(item.skinInfo));
            
            cover.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(item.skinInfo.cover))
            {
                cover.Load(item.skinInfo.cover, true, (fromCache,  success) =>
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