using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class PetWearingItem : BaseProfileGrid
    {
        [SerializeField] private CButton itemBtn;
        [SerializeField] private Image iconImg;
        [SerializeField] private RemoteImageBehaviour remoteImgBehav;
        [SerializeField] private Image gemOutfitTag;

        [HideInInspector] public WearingInfo WearingInfo;
        private void Awake()
        {
            itemBtn.onClick.AddListener(OnItemClick);
            gridImg = transform.Find("ExBg").GetComponent<Image>();
            //UpdateGridColor();
        }

        private void OnItemClick()
        {

            if (WearingInfo == null) return;
            bool isPgc = WearingInfo.resType == (int)WearingType.PGC;
            if (isPgc)
            {
                TipPanel.ShowToast("可前往官方商城进行购买");
            }
            else
            {
                if (string.IsNullOrEmpty(WearingInfo.bundleId))
                {
                    UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, WearingInfo.ugcId);
                }
                else
                {
                    UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.UgcBundle ,WearingInfo.bundleId);
                }
            }
        }

        public void SetData(WearingInfo wearingInfo)
        {
            WearingInfo = wearingInfo;
            bool isPgc = wearingInfo.resType == (int)WearingType.PGC;
            iconImg.gameObject.SetActive(isPgc);
            remoteImgBehav.gameObject.SetActive(!isPgc);
            if (isPgc)
            {
                PgcUtils.LoadPetAvatarIconAsync(wearingInfo.pgcId,gameObject, (sprite) =>
                {
                    if (this!=null && this.gameObject != null && sprite != null)
                    {
                        iconImg.sprite = sprite;
                    }
                });
            }
            else
            {
                remoteImgBehav.Load(wearingInfo.cover);
            }

            if (wearingInfo.paymentInfo != null)
            {
                gemOutfitTag.gameObject.SetActive(wearingInfo.paymentInfo.currencyType == CurrencyType.Gem);
            }
        }
    }
}
