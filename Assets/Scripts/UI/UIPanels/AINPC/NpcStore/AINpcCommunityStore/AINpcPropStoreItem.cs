using System;
using Com.TheFallenGames.OSA.Util.IO;
using Game.CommunityGame;
using GameData.Base;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.PropStore
{
    public class AINpcPropStoreItem : BaseSectionInfoItem
    {
        public SellPriceTag PriceTag;
        public CButton Btn_View;
        public GameObject Go_Selected;
        public RemoteImageBehaviour Remote_Cover;
        
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
            var ugcInfo = JsonConvert.DeserializeObject<AINpcInfo>(this._curData.ugcData);
            
            Remote_Cover.Load(ugcInfo.cover);
            PriceTag.SetPaymentInfo(ugcInfo.paymentInfo);

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