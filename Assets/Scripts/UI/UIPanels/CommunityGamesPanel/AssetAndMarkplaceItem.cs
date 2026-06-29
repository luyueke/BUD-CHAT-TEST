using System;
using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CommunityGame
{
    public class AssetAndMarkplaceItem : BaseSectionInfoItem
    {
        public RemoteImageBehaviour Rm_Profile;
        public CButton Btn_View;
        public Text price;
        public GameObject priceObj;
        public GameObject freeObj;
        public GameObject likeObj;
        public Text likeNumTxt;
        public GameObject animeTag;
        private RecommendItemData _curData;
        private Action onBuyUpdate;

        private void Awake()
        {
            Btn_View.onClick.AddListener(OnBtnViewClick);
        }

        /// <summary>
        /// 购买刷新列表
        /// </summary>
        /// <param name="buyUpdate"></param>
        public override void SetInteractiveCallback(Action buyUpdate)
        {
            this.onBuyUpdate = buyUpdate;
        }

        public override void InitData(RecommendItemData data)
        {
            base.InitData(data);

            if (data == null)
                return;

            this._curData = data;
            Rm_Profile.Load(this._curData.ugcInfo.cover);
            if (this._curData.interactInfo != null && data.ugcInfo != null)
            {
                var likeNum = GameUtils.ToBudCommonNumString(data.interactInfo.likeAmount);
                likeObj.gameObject.SetActive(data.interactInfo.likeAmount > 0);
                likeNumTxt.text = likeNum;
                PaymentInfo paymentInfo = data.ugcInfo?.paymentInfo;
                int price = 0;
                if (paymentInfo != null)
                {
                    price = paymentInfo.price;
                }
                priceObj.gameObject.SetActive(price > 0);
                freeObj.gameObject.SetActive(price <= 0);
                if (animeTag != null)
                {
                    animeTag.SetActive(data.ugcInfo.ugcStyle != 0);
                }
                this.price.text = price.ToString();
            }
        }

        private void OnBtnViewClick()
        {
            var detailPanel = UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Mat, this._curData?.ugcInfo?.id,this._curData?.ugcInfo?.ugcStyle);
            if (detailPanel != null && (AssetDetailPanel)detailPanel != null)
            {
                var assetDetailPanel = (AssetDetailPanel)detailPanel;
                assetDetailPanel.SetBuyUpdate(onBuyUpdate);
            }
        }
    }
}