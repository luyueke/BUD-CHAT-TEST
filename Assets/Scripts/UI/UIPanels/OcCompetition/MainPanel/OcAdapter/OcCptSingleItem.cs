using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData.BaseInfo;
using GameData.UGCData;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCptSingleItem : MonoBehaviour
    {
        public Image Image;
        public RemoteImageBehaviour RemoteImage;
        public Button BuyBtn;
        public Text Price;
        public GameObject PriceIcon;

        private GoodsData data;
        private OcSkinInfo ocSkinInfo;
        private void Awake()
        {
            BuyBtn.onClick.AddListener(OnBtn);
        }
        public void SetData(GoodsData goodData, OcSkinInfo skinInfo) 
        {
            ocSkinInfo = skinInfo;
            data = goodData;

            //if (data.GoodsType == GoodsType.SingleUgc) {
            RemoteImage.Load(skinInfo.coverUrl);

            if (data.GoodsType == GoodsType.SingleUgc && !goodData.IsOwned)
            {
                if (goodData.OriginalPrice != null && goodData.OriginalPrice.CurrencyType == CurrencyType.PinkCoin)
                {
                    PriceIcon.gameObject.SetActive(true);
                    Price.text = goodData.OriginalPrice.Value.ToString();
                }
                else
                {
                    PriceIcon.gameObject.SetActive(false);
                }
            }
            else
            {
                PriceIcon.gameObject.SetActive(false);
            }
        }

        void OnBtn() {
            switch (data.GoodsType)
            {
                case GoodsType.SingleUgc:
                    int ugcStyle = AssetsDataManager.GetUgcStyle(data);
                    UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, data.Id, ugcStyle);
                    break;
                case GoodsType.SinglePgc:
                    //UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, data.Id);
                    break;
            }
        }
    }
}