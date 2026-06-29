using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.AINPCStudio
{
    public class StorePanelMyNpcView : MonoBehaviour
    {
        public Toggle Tog_PgcNpc;
        public Toggle Tog_MyCreate;
        public Toggle Tog_MyBuy;

        public AINpcPurchasedPanel PurchasedPanel;
        public AINpcOwnedPublishedPanel PublishedPanel;
        public PgcNpcSelectView PgcNpcSelectView;
        
        private NpcStoreEnterType _curEnterType = NpcStoreEnterType.Store;

        public void InitEnterMode(NpcStoreEnterType enterType)
        {
            this._curEnterType = enterType;
            PublishedPanel.InitEnterMode(this._curEnterType);
            // Tog_PgcNpc.gameObject.SetActive(enterType == NpcStoreEnterType.SelectNpc);
            Init();
        }
        
        private void Init()
        {
            PgcNpcSelectView.Init();
            AddListener();
        }

        private void Start()
        {
            Tog_MyCreate.isOn = true;
            OnTogMyCreateClick(true);
        }

        private void AddListener()
        {
            Tog_PgcNpc.onValueChanged.AddListener(OnTogPgcNpcClick);
            Tog_MyCreate.onValueChanged.AddListener(OnTogMyCreateClick);
            Tog_MyBuy.onValueChanged.AddListener(OnTogMyBuyClick);
        }
        
        private void OnTogPgcNpcClick(bool isOn)
        {
            if (!isOn)
                return;
            
            PgcNpcSelectView.gameObject.SetActive(true);
            PurchasedPanel.gameObject.SetActive(false);
            PublishedPanel.gameObject.SetActive(false);
        }
        
        private void OnTogMyCreateClick(bool isOn)
        {
            if (!isOn)
                return;
            
            PgcNpcSelectView.gameObject.SetActive(false);
            PurchasedPanel.gameObject.SetActive(false);
            PublishedPanel.gameObject.SetActive(true);
            PublishedPanel.GetData();
        }
        private void OnTogMyBuyClick(bool isOn)
        {
            if (!isOn)
                return;
            
            PgcNpcSelectView.gameObject.SetActive(false);
            PurchasedPanel.gameObject.SetActive(true);
            PublishedPanel.gameObject.SetActive(false);
            PurchasedPanel.GetData();
        }
    }
}