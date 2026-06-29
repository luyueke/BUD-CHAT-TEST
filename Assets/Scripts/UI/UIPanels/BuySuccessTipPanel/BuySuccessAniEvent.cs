using System;
using UnityEngine;

namespace UI.UIPanels.FittingRoom
{
    public class BuySuccessAniEvent : MonoBehaviour
    {
        public Action HideTextEvent { private get; set; }
        public Action HidePanelEvent { private get; set; }

        private void PlayHideTextAni()
        {
            HideTextEvent?.Invoke();
        }

        private void HidePanel()
        {
            HidePanelEvent?.Invoke();
        }
    }
}