using System;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileAnimItem : ProfileCommonItem
    {
        public override void SetData(DraftListItem item, Action<DraftListItem, Texture> onSelect)
        {
            base.SetData(item, onSelect);
            SetPrice(item.animInfo.paymentInfo);
            auditView.SetActive(FormatUtils.IsAuditing(item.animInfo));
            
            cover.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(item.animInfo.cover))
            {
                cover.Load(item.animInfo.cover, true, (fromCache,  success) =>
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