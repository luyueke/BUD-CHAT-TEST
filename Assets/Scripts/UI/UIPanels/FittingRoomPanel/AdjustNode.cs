using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class AdjustNode : MonoBehaviour
    {
        [SerializeField] Button AdjustButton;

        public Action OnClick;

        private void Awake()
        {
            AdjustButton.onClick.AddListener(() =>
            {
                OnClick?.Invoke();
            });
        }
    }
}
