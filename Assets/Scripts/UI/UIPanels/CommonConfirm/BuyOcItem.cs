using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.CommonConfirm
{
    public class BuyOcItem : MonoBehaviour
    {
        [SerializeField] internal Text ocNumTitle;
        [SerializeField] internal Text ocNum;
        [SerializeField] internal Text gemNum;
        [SerializeField] internal GameObject discountRoot;
        [SerializeField] internal Text discountNum;
        [SerializeField] internal Button clickButton;
        [SerializeField] internal Image iconImage;
        [SerializeField] internal Sprite characterIconSprite;
        [SerializeField] internal Sprite petIconSprite;

        public Action OnBuyOcAction;

        private void Awake()
        {
            clickButton.onClick.AddListener(OnBuyOcClick);
        }

        public void SetData(Slot data, bool isCharacter = true)
        {
            ocNumTitle.SetLocalText("{0}个卡位",data.slotAmount);
            ocNum.text = $"x {data.slotAmount}";
            gemNum.text = $"{data.gem}";

            if (data.discount == 0)
            {
                discountRoot.gameObject.SetActive(false);
            }
            else
            {
                discountRoot.gameObject.SetActive(true);
                discountNum.SetLocalText("{0}折",100 - data.discount);
            }

            iconImage.sprite = isCharacter ? characterIconSprite : petIconSprite;

        }

        private void OnBuyOcClick()
        {
            OnBuyOcAction?.Invoke();
        }
    }
}
