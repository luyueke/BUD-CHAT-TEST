using AIGame.Base;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Catalog;
using UnityEngine;
namespace CataLog
{
    public class AIParkCatalogBtn : MonoBehaviour
    {
        public CButton clickButton;

        public GameObject redpointItem;

        void Awake()
        {
            clickButton.onClick.AddListener(OnClickButten);
            redpointItem?.gameObject.SetActive(false);
        }

        private void OnClickButten()
        {
            var hospitalcata = UIManager.Inst.OpenPanel<AIParkGameCatalogPanel>(PanelId.AIParkGameCatalogPanel);        }
    }


}

