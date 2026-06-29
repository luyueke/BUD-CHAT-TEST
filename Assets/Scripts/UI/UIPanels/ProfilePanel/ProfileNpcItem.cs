using System;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel {
    public class ProfileNpcItem : ProfileCommonItem {
        public override void SetData(DraftListItem item, Action<DraftListItem, Texture> onSelect)
        {
            base.SetData(item, onSelect);
            SetPrice(item.npc.paymentInfo);
            auditView.SetActive(FormatUtils.IsAuditing(item.npc));

            cover.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(item.npc.cover))
            {
                cover.Load(item.npc.cover, true, (fromCache,  success) =>
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
