
using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class CurrencyPicker : MonoBehaviour
    {

        public CButton currenSelect;
        public CButton pinkCoin;
        public CButton gem;
        public CButton free;
        public CButton all;
        public GameObject SelectArea;
        public Text curText;
        public Image curImage;

        private CurrencyType _currencyPickerType;
        private Action<CurrencyType> _action;

        public void Awake()
        {
            SelectArea.gameObject.SetActive(false);
            currenSelect.onClick.AddListener((() =>
            {
                SelectArea.gameObject.SetActive(!SelectArea.gameObject.activeSelf);
            }));
            pinkCoin.onClick.AddListener((() =>
            {
                SelectArea.gameObject.SetActive(false);
                _currencyPickerType = CurrencyType.PinkCoin;
                _action.Invoke(_currencyPickerType);
                SetCurStatus(_currencyPickerType);
            }));
            gem.onClick.AddListener((() =>
            {
                SelectArea.gameObject.SetActive(false);
                _currencyPickerType = CurrencyType.Gem;
                _action.Invoke(_currencyPickerType);
                SetCurStatus(_currencyPickerType);
            }));
            free.onClick.AddListener((() =>
            {
                SelectArea.gameObject.SetActive(false);
                _currencyPickerType = CurrencyType.Free;
                _action.Invoke(_currencyPickerType);
                SetCurStatus(_currencyPickerType);
            }));
            all.onClick.AddListener((() =>
            {
                SelectArea.gameObject.SetActive(false);
                _currencyPickerType = CurrencyType.None;
                _action.Invoke(_currencyPickerType);
                SetCurStatus(_currencyPickerType);
            }));
        }

        public void SetDefault()
        {
            curText.text = "全部";
            curImage.color = DataUtil.DeSerializeColorCheckHash("#6C57FF");
            _currencyPickerType = CurrencyType.None;
        }

        private void SetCurStatus(CurrencyType currencyType)
        {
            if (currencyType == CurrencyType.None)
            {
                curText.SetLocalText("全部");
                curImage.color = DataUtil.DeSerializeColorCheckHash("#6C57FF");
            } else if (currencyType == CurrencyType.Free)
            {
                curText.SetLocalText("免费");
                curImage.color = DataUtil.DeSerializeColorCheckHash("#7C78A3");
            } else if (currencyType == CurrencyType.Gem)
            {
                curText.SetLocalText("钻石");
                curImage.color = DataUtil.DeSerializeColorCheckHash("#A982FF");
            } else if (currencyType == CurrencyType.PinkCoin)
            {
                curText.SetLocalText("商品币");
                curImage.color = DataUtil.DeSerializeColorCheckHash("#FF90D0");
            } 
        }

        public void SetCallback(Action<CurrencyType> action)
        {
            this._action = action;
        }


    }
}
