using System;
using Es;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.GashaponPanel
{
    public class GashaponTabItem : MonoBehaviour
    {
        public Transform lineObj;
        public TabItem tabItem;

        public GashaponViewConfig viewData;

        private void Awake()
        {
        }

        public void SetLineShow(bool isShow)
        {
            lineObj.gameObject.SetActive(isShow);
        }
    }
}