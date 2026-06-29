using System;
using System.Collections;
using System.Collections.Generic;
using EventTracking;
using Game.CommunityGame;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class FilterItem : MonoBehaviour
    {
        public GameObject unselectedGo;
        public GameObject selectedGo;
        public CButton button;
        public Text unselectedText;
        public Text selectedText;
        public Action<bool> selectedAction;

        bool _isSelected = false;
        public bool IsSelected => _isSelected;
        public void Awake()
        {
            button.onClick.AddListener(onClickAction);
        }

        void onClickAction()
        {
            selectedAction?.Invoke(_isSelected);
        }

        public void SetSelected(bool isSelected)
        {
            unselectedGo.SetActive(!isSelected);
            selectedGo.SetActive(isSelected);
            _isSelected = isSelected;
        }

        public void SetText(string text)
        {
            unselectedText.text = text;
            selectedText.text = text;
        }
    }
}