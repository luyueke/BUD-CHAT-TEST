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
    public class CataOpenItem : MonoBehaviour
    {
        public PanelId cataPanelId;



        CButton clickButton;
        private List<Gallery> galleryList;
        public GameObject redpointItem;

        public static CataOpenItem Inst;

        // Start is called before the first frame update
        private void Awake()
        {
            Inst = this;
        }
        void Start()
        {
            AIHospitalCataData.Inst.actions += ReferenceData;
            AIHospitalCataData.Inst.GetCataData("");
            clickButton = GetComponent<CButton>();
            clickButton.onClick.AddListener(OnClickButten);
        }
        void showRedPoint(bool isshow)
        {
            redpointItem.SetActive(isshow);

        }
        void ReferenceData(List<Gallery> _galleryList)
        {
            galleryList = _galleryList;
            UpdateRedPoint();
        }

        public void UpdateRedPoint()
        {
            // 遍历所有角色和动作
            int unclickedCount = 0;
            foreach (var gl in galleryList)
            {
                foreach (var emote in gl.roleGallery.roleEmote)
                {
                    if (emote.islock == 1) continue;
                    var key = AccountDataManager.Inst.Uid + gl.galleryId + emote.emoteId;
                    if (!PlayerPrefs.HasKey(key))
                    {
                        unclickedCount++;
                    }
                }
            }
            showRedPoint(unclickedCount > 0);
        }


        private void OnClickButten()
        {
            var hospitalcata = UIManager.Inst.OpenPanel<HospitalCatalogPanel>(PanelId.HospitalCatalogPanel);
            hospitalcata.SetCataData(galleryList , false);
            hospitalcata.redpointEvent += showRedPoint;
        }

        private void OnDestroy()
        {
            AIHospitalCataData.Inst.actions -= ReferenceData;
        }

    }


}

