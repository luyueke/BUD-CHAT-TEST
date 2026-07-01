using Game.Store;
using GameData.BaseInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using UI.BaseWidgets;
using UnityEngine;

namespace AIGame.Base
{
    public enum AIParkUgcEditHelpType {
        Event,
        Ending,
    }
    public class AIParkUgcEditHelpGroup : MonoBehaviour
    {
        public CButton CloseBtn;

        public GameObject EventGroup;

        public GameObject EndingGroup;

        private void Awake()
        {
            CloseBtn.onClick.AddListener(OnCloseBtn);
        }

        public void Set(AIParkUgcEditHelpType type) {
            gameObject.SetActive(true);
            EventGroup.gameObject.SetActive(false);
            EndingGroup.gameObject.SetActive(false);
            switch (type)
            {
                case AIParkUgcEditHelpType.Event:
                    EventGroup.gameObject.SetActive(true);
                    break;
                case AIParkUgcEditHelpType.Ending:
                    EndingGroup.gameObject.SetActive(true);
                    break;
                default:
                    break;
            }

        }

        void OnCloseBtn() { 
            gameObject.SetActive(false);
        }
    }
}