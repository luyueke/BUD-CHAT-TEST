using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 聊天选择单个chip item
    /// </summary>
    public class ChatSelectionItemCom : MonoBehaviour
    {
        public Button selectBtn;
        public GameObject selectImg; // 多选时显示的勾选图标
        public Text txt_title;
        public Text txt_content;

        string _value;
        string _indexStr;
        bool _isSelected;
        bool _isMulti;
        Action<ChatSelectionItemCom> _onClick;

        void Awake()
        {
            selectBtn.onClick.AddListener(OnClick);
        }

        public void SetData(string label, string value, bool isMulti, Action<ChatSelectionItemCom> onClick, int index)
        {
            txt_content.text = label;
            _indexStr = index.ToString();
            txt_title.text = !isMulti ? _indexStr : "";
            _value = value;
            _isMulti = isMulti;
            _onClick = onClick;
            _isSelected = false;
            if (selectImg != null) selectImg.SetActive(false);
        }

        void OnClick()
        {
            if (_isMulti)
            {
                _isSelected = !_isSelected;
                txt_title.text = "";
                if (selectImg != null) selectImg.SetActive(_isSelected);
            }
            _onClick?.Invoke(this);
        }

        public string Value => _value;
        public bool IsSelected => _isSelected;

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (_isMulti)
            {
                txt_title.text = "";
            }
            if (selectImg != null) selectImg.SetActive(_isMulti && selected);
        }
    }
}
