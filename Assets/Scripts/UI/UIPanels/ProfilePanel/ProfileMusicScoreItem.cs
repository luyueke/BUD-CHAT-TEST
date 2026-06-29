using System;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileMusicScoreItem : ProfileCommonItem
    {
        public override void SetData(DraftListItem item, Action<DraftListItem, Texture> onSelect)
        {
            base.SetData(item, onSelect);
            SetPrice(item.musicScoreInfo.paymentInfo);
            auditView.SetActive(FormatUtils.IsAuditing(item.musicScoreInfo));
            
            cover.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(item.musicScoreInfo.cover))
            {
                cover.Load(item.musicScoreInfo.cover, true, (fromCache,  success) =>
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