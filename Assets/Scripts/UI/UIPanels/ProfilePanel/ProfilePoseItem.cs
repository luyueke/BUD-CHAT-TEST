using System;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfilePoseItem : ProfileCommonItem
    {
        public override void SetData(DraftListItem item, Action<DraftListItem, Texture> onSelect)
        {
            base.SetData(item, onSelect);
            SetPrice(item.poseInfo.paymentInfo);
            auditView.SetActive(FormatUtils.IsAuditing(item.poseInfo));
            
            cover.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(item.poseInfo.cover))
            {
                cover.Load(item.poseInfo.cover, true, (fromCache,  success) =>
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