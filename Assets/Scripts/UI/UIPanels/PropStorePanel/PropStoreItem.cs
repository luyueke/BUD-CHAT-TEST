using System;
using Game.CommunityGame;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.PropStore
{
    public class PropStoreItem : BaseSectionInfoItem
    {
        public SellPriceTag PriceTag;
        public CButton Btn_View;
        public GameObject Go_Selected;
        
        private RecommendItemData _curData;
        private Action<RecommendItemData> _onSelectAct;

        private void Awake()
        {
            Btn_View.onClick.AddListener(OnBtnViewClick);
        }

        public override void InitData(RecommendItemData data)
        {
            base.InitData(data);
            
            if(data == null)
                return;

            this._curData = data;
            PriceTag.SetPaymentInfo(data?.ugcInfo?.paymentInfo);

            if (data?.interactInfo?.consumed == 1)
            {
                SetOwned();
            }
        }

        public void SetOwned()
        {
            PriceTag.SetOwnState();
        }

        public void SetOnSelectedAct(Action<RecommendItemData> act)
        {
            this._onSelectAct = act;
        }
        
        private void OnBtnViewClick()
        {
            this._onSelectAct?.Invoke(_curData);
        }

        public void SetSelectState(bool isSelected)
        {
            Go_Selected.gameObject.SetActive(isSelected);
        }
    }
}