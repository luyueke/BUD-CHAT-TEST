using System;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.UIWidgets
{
    public class NumberPicker : MonoBehaviour
    {
        [SerializeField] private CButton _addButton;
        [SerializeField] private CButton _reduceButton;
        [SerializeField] private Text _numText;

        private int _min = 0;
        private int _max = 99;
        private string _minTip;
        private string _maxTip;

        private string _numberSuffix = string.Empty; //用于给显示的Number加个后缀，比如x分钟，x秒等

        private KeyBoardInfo _kbInfo;

        private Action<int, int> _onNumberChanged;

        private void Awake()
        {
            _addButton.onClick.RemoveAllListeners();
            _reduceButton.onClick.RemoveAllListeners();
            _addButton.onClick.AddListener(OnAddButtonClick);
            _reduceButton.onClick.AddListener(OnReduceButtonClick);

            var trigger = _numText.GetComponent<EventTrigger>();
            EventTrigger.Entry onSelect = new EventTrigger.Entry();
            onSelect.eventID = EventTriggerType.PointerClick;
            onSelect.callback.AddListener(Select);
            trigger.triggers.Add(onSelect);
        }

        private void Select(BaseEventData data)
        {
            var str = _numText.text;
            _kbInfo.defaultText = str;

            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, ShowKeyBoard);
            // LoggerUtils.Log("NumberPicker JsonUtility.ToJson(keyBoardInfo)===" + JsonUtility.ToJson(_kbInfo));
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_kbInfo));
        }

        private void ShowKeyBoard(string str)
        {
            if (string.IsNullOrEmpty(str)) return;
            int v;
            if (int.TryParse(str, out v))
            {
                if (CheckNumber(v))
                {
                    SetText(v);
                }
            }
            else
            {
                TipPanel.ShowToast("请输入正确的值");
            }

            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        }

        private void OnDestroy()
        {
            _onNumberChanged = null;
        }

        private void OnAddButtonClick()
        {
            SetText(GetNumber() + 1);
        }

        private void OnReduceButtonClick()
        {
            SetText(GetNumber() - 1);
        }

        private bool CheckNumber(int num)
        {
            if (num < _min)
            {
                if (!string.IsNullOrEmpty(_minTip)) TipPanel.ShowToast(_minTip);
                return false;
            }

            if (num > _max)
            {
                if (!string.IsNullOrEmpty(_maxTip)) TipPanel.ShowToast(_maxTip);
                return false;
            }

            return true;
        }

        public void SetNumberRange(int min, int max, string minTip = "", string maxTip = "")
        {
            _min = min;
            _max = max;
            _minTip = minTip;
            _maxTip = maxTip;
        }

        public void SetInputKeyBoardInfo(KeyBoardInfo kbInfo)
        {
            _kbInfo = kbInfo;
        }

        public void SetText(int newV)
        {
            if (CheckNumber(newV))
            {
                var oldV = GetNumber();
                _numText.text = newV + _numberSuffix;
                
                if (oldV != newV)
                {
                    _onNumberChanged?.Invoke(oldV, newV);
                }
            }
        }
        
        /// <summary>
        /// 设置UI数字，但不会触发NumberChanged
        /// </summary>
        /// <param name="num"></param>
        public void SetTextWithoutNotify(int num)
        {
            if (CheckNumber(num))
            {
                _numText.text = num + _numberSuffix;
            }
        }

        public int GetNumber()
        {
            var tex = _numText.text;
            if (!string.IsNullOrEmpty(_numberSuffix))
            {
                tex = tex.Replace(_numberSuffix, string.Empty);
            }
            return int.Parse(tex);
        }

        /// <summary>
        /// 数字变化监听
        /// </summary>
        /// <param name="listener">int1:oldValue, int2:newValue</param>
        public void SetNumberChangedListener(Action<int, int> listener)
        {
            if (listener != null)
            {
                _onNumberChanged = null;
                _onNumberChanged += listener;
            }
        }

        public void SetNumberSuffix(string suffix)
        {
            this._numberSuffix = suffix;
            SetText(GetNumber());
        }
    }
}