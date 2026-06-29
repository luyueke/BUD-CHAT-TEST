using System;
using System.Collections;
using System.Collections.Generic;
using GameData.PgcData;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.CommonConfirm
{
    public class BuyQuickPoseItem : MonoBehaviour
    {
        [SerializeField] internal Text ocNumTitle;
        [SerializeField] internal Text ocNum;
        [SerializeField] internal Text gemNum;
        [SerializeField] internal GameObject discountRoot;
        [SerializeField] internal Text discountNum;
        [SerializeField] internal Button clickButton;
        [SerializeField] internal Image iconImage;

        
        public Action OnBuyOcAction;

        private void Awake()
        {
            clickButton.onClick.AddListener(OnBuyOcClick);
        }

        public void SetData(Slot data,string tip,Sprite iconSprite)
        {
            ocNumTitle.text = $"{data.slotAmount}个{tip}卡位";
            ocNum.text = $"x {data.slotAmount}";
            gemNum.text = $"{data.gem}";

            if (data.discount == 0)
            {
                discountRoot.gameObject.SetActive(false);
            }
            else
            {
                discountRoot.gameObject.SetActive(true);
                discountNum.text = $"{100 - data.discount}折";
            }

            iconImage.sprite = iconSprite;
            iconImage.SetNativeSize();
        }

        private void OnBuyOcClick()
        {
            OnBuyOcAction?.Invoke();
        }
    }
}
