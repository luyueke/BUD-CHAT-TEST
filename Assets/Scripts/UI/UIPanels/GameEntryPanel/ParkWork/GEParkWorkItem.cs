using Com.TheFallenGames.OSA.Util.IO;
using System;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class GEParkWorkItem : MonoBehaviour
    {
        public RemoteImageBehaviour RemoteImage;

        public CButton CreatBtn;

        public Button Btn;

        public Text NameTxt;

        public List<GEParkNpcHead> NpcList;

        [HideInInspector] public DraftListItem MapInfo;

        [HideInInspector] public Action<DraftListItem> Action;
        [HideInInspector] public int Idx;
        private void Awake()
        {
            Btn.onClick.AddListener(OnBtn);
            CreatBtn.onClick.AddListener(OnCreatBtn);
        }

        public void SetData(DraftListItem mapInfo, Action<DraftListItem> action, int idx)
        {
            MapInfo = mapInfo;
            Action = action;
            Idx = idx;

            var panle = UIManager.Inst.FindPanel<GEParkWorkPanel>(WindowId.RecommendWindow, PanelId.GEParkWorkPanel);
            if (panle != null)
            {
                if (Idx == 0 && panle.mapListType == MapListResponseType.MapDrafts)
                {
                    CreatBtn.gameObject.SetActive(true);
                    Btn.gameObject.SetActive(false);
                }
                else
                {
                    CreatBtn.gameObject.SetActive(false);
                    Btn.gameObject.SetActive(true);

                    RemoteImage.Load(MapInfo.mapInfo.cover);

                    NameTxt.text = MapInfo.mapInfo.name;

                    var ls = MapInfo.mapInfo.gameSetting.AICommonGameConfig.npcData;
                    for (int i = 0; i < NpcList.Count; i++)
                    {
                        if (ls != null && i < ls.Count)
                        {
                            NpcList[i].gameObject.SetActive(true);
                            NpcList[i].SetDate(ls[i].id, ls[i].cover);
                        }
                        else
                        {
                            NpcList[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void OnBtn()
        {
            Action?.Invoke(MapInfo);
        }

        private void OnCreatBtn()
        {
            GameEntrySystem.Inst.OpenParkUgcPanel();
        }
    }
}