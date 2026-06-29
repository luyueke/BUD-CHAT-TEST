using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.UIPanels.FittingRoom
{
    public class SendGiftAccountWidgets : MonoBehaviour
    {
        public GameObject CoinRoot;
        public GameObject BadgeRoot;
        public GameObject GemRoot;
        public GameObject SkinTicketRoot;

        public void Reset()
        {
            CoinRoot.SetActive(false);
            BadgeRoot.SetActive(false);
            GemRoot.SetActive(false);
            SkinTicketRoot.SetActive(false);
        }

        public void SetMainTabs(SendGiftMainTabs.Tab tab)
        {
            Reset();
            GemRoot.SetActive(true);
            // switch (tab)
            // {
            //     case SendGiftMainTabs.Tab.Ugc:
            //         // SkinTicketRoot.SetActive(true);
            //         GemRoot.SetActive(true);
            //         break;
            //     case SendGiftMainTabs.Tab.Tool:
            //         GemRoot.SetActive(true);
            //         break;
            //     default:
            //         // CoinRoot.SetActive(true);
            //         // BadgeRoot.SetActive(true);
            //         GemRoot.SetActive(true);
            //         break;
            // }
        }
    }
}
