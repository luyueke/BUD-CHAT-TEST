using System;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileVehicleItem : ProfileCommonItem
    {
        public override void SetData(DraftListItem item, Action<DraftListItem, Texture> onSelect)
        {
            base.SetData(item, onSelect);
            if (item == null || item.vehicleInfo == null)
            {
                return;
            }

            SetPrice(item.vehicleInfo.paymentInfo);
            auditView.SetActive(FormatUtils.IsAuditing(item.vehicleInfo));

            // 封面
            cover.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(item.vehicleInfo.cover))
            {
                cover.Load(item.vehicleInfo.cover, true, (fromCache, success) =>
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