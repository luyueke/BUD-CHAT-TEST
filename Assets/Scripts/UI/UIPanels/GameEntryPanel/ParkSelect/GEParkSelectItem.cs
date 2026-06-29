using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData.Base;
using GameData.BaseInfo;
using Network.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    [Serializable]
    public struct GEParkItemLabel {
        public Transform Parent;
        public Text Text;
    }
    public class GEParkSelectItem : MonoBehaviour
    {
        public GameObject Views;

        public RemoteImageBehaviour RemoteImage;

        public GameObject On;

        public CButton InfoBtn;

        public Button Btn;

        public Text NameTxt;

        public Text PlotTxt;

        public List<GEParkItemLabel> TypeList;

        public List<GEParkNpcHead> NpcList;

        [HideInInspector]public UgcBaseInfo MapInfo;

        Action<UgcBaseInfo> ItemSelected;

        int Idx;
        private void Awake()
        {
            Btn.onClick.AddListener(OnBtn);
            InfoBtn.onClick.AddListener(OnInfoBtn);
        }

        public void SetData(UgcBaseInfo mapInfo, Action<UgcBaseInfo> action, int idx) {
            if(Views != null) Views?.gameObject.SetActive(true);

            Idx = idx;
            ItemSelected = action;
            MapInfo = mapInfo;

            RemoteImage.Load(MapInfo.cover);

            NameTxt.text = MapInfo.name;

            PlotTxt.text = MapInfo.gameSetting.AICommonGameConfig.plot;

            var ls = MapInfo.gameSetting.AICommonGameConfig.npcData;
            for (int i = 0; i < NpcList.Count; i++)
            {
                if (i < ls.Count)
                {
                    NpcList[i].gameObject.SetActive(true);
                    NpcList[i].SetDate(ls[i].id, ls[i].cover);
                }
                else
                {
                    NpcList[i].gameObject.SetActive(false);
                }
            }

            var ls2 = MapInfo.sectionInfo;
            for (int i = 0; i < TypeList.Count; i++)
            {
                if (ls2 != null && i < ls2.Count)
                {
                    TypeList[i].Parent.gameObject.SetActive(true);
                    TypeList[i].Text.text = ls2[i].name;
                }
                else
                {
                    TypeList[i].Parent.gameObject.SetActive(false);
                }
            }

            On.gameObject.SetActive(false);
        }

        private void OnBtn()
        {
            ItemSelected?.Invoke(MapInfo);
            On.gameObject.SetActive(true);
        }

        private void OnInfoBtn()
        {
            GameEntrySystem.Inst.OpenParkDetailPanel(MapInfo.id);
        }
    }
}