using Game.Store;
using GameData.BaseInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkUgcEditMusicalGroup : MonoBehaviour
    {
        public Button CloseBtn;

        public GameObject InitGroup;

        public GameObject OfficialGroup;

        public CButton OfficialBtn;

        public CButton StoreBtn;

        public CButton ConfirmBtn;

        public List<Toggle> MusicalTog;

        private AIParkUgcEditStage Root;

        private List<string> MusicalIds = new List<string>() { "12400014", "12400013", "12400012", "12400011" };
        public void Init(AIParkUgcEditStage root) {
            Root = root;
        }
        private void Awake()
        {
            CloseBtn.onClick.AddListener(() => { gameObject.SetActive(false); });
            OfficialBtn.onClick.AddListener(OnOfficialBtn);

            StoreBtn.onClick.AddListener(OnStoreBtn);

            ConfirmBtn.onClick.AddListener(OnConfirmBtn);

            for (int i = 0; i < MusicalTog.Count; i++)
            {
                MusicalTog[i].isOn = false;
                MusicalTog[i].onValueChanged.AddListener((succ) =>
                {
                    OnToggle(succ,i);
                });
            }
        }

        private void OnEnable()
        {
            InitGroup.gameObject.SetActive(true);
            OfficialGroup.gameObject.SetActive(false);
            for (int i = 0; i < MusicalTog.Count; i++)
            {
                MusicalTog[i].isOn = false;
            }
        }

        private void OnToggle(bool bo,int idx) { 
        
            
        }


        private void OnConfirmBtn() {
            for (int i = 0; i < MusicalTog.Count; i++)
            {
                if (MusicalTog[i].isOn && Root.CanSelect(MusicalIds[i]))
                {
                    var tem = new AICommonGameConfig_Musical();
                    tem.isUgc = false;
                    tem.id = MusicalIds[i];
                    Root.SetMusical(tem);
                }
            }
            gameObject.SetActive(false);
        }

        private void OnOfficialBtn() {
            InitGroup.gameObject.SetActive(false);
            OfficialGroup.gameObject.SetActive(true);
        }


        private void OnStoreBtn()
        {
            var dt = new FittingRoomSelectData();
            dt.CanSelect = (dt) => { return Root.CanSelect(dt.GetFirstAsset<AssetsData>().Id); };
            dt.OnSelect = (dt) =>
            {
                var tem = new AICommonGameConfig_Musical();
                var asset = dt.GetFirstAsset<AssetsData>();
                if (asset.UgcInfo != null)
                {
                    tem.isUgc = true;
                    tem.id = dt.GetFirstAsset<AssetsData>().Id;
                    tem.name = dt.GetFirstAsset<AssetsData>().Name;
                    tem.icon = dt.GetFirstAsset<AssetsData>().UgcInfo.UgcInfo.cover;
                }
                else
                {
                    tem.isUgc = false;
                    tem.name = dt.GetFirstAsset<AssetsData>().Name;
                    tem.id = dt.GetFirstAsset<AssetsData>().Id;
                }
                Root.SetMusical(tem);
            };
            var fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel, dt) as FittingRoomPanel;
            if (fittingRoom)
            {
                fittingRoom.JumpTo(MainTabs.Tab.Ugc, 50024);
            }
            gameObject.SetActive(false);
        }
    }
}