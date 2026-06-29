using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using GameData.BaseInfo;
using UnityEngine;

namespace Game.MusicalInstrument
{
    public class PgcToneInfoPanel : MonoBehaviour
    {
        public Transform PgcToneContentParent;
        public PgcToneContent Prefab_PgcToneContent;

        private List<PgcToneInfoPublishedItem> _pgcToneItems = new List<PgcToneInfoPublishedItem>();
        private Action<ToneInfo> _onItemSelect;
        private bool isInit = false;
        
        private Dictionary<PgcToneClassify, string> pgcToneClassifyNameDict = new Dictionary<PgcToneClassify, string>()
        {
            { PgcToneClassify.Keyboard, "键盘" },
            { PgcToneClassify.HumanVoice, "人声" },
            { PgcToneClassify.Bowstring, "弓弦乐" },
            { PgcToneClassify.Strum, "弹拨乐" },
            { PgcToneClassify.Wind, "吹管乐" },
            { PgcToneClassify.Percussion, "打击乐" },
        };
        
        private Dictionary<PgcToneClassify, string> pgcToneClassifyColorDict = new Dictionary<PgcToneClassify, string>()
        {
            { PgcToneClassify.Keyboard, "#828EFF" },
            { PgcToneClassify.HumanVoice, "#82BBFF" },
            { PgcToneClassify.Bowstring, "#FF82C5" },
            { PgcToneClassify.Strum, "#D782FF" },
            { PgcToneClassify.Wind, "#828EFF" },
            { PgcToneClassify.Percussion, "#FF82C5" },
        };

        private void Awake()
        {
            InitPanel();
        }

        public void OnSelectPanel()
        {
            if(!isInit)
                return;

            SetCurToneItemSelectState();
        }

        public void SetOnToneItemSelectAct(Action<ToneInfo> act)
        {
            this._onItemSelect = act;
        }

        private void OnToneItemSelectAct(ToneInfo toneInfo)
        {
            this._onItemSelect?.Invoke(toneInfo);
            _pgcToneItems.ForEach(x=>x.SetSelectState(false));
        }
        
        public string GetCurToneId()
        {
            string curToneId = "";
            var studioPanel = UIManager.Inst.FindPanel<ToneStudioPanel>(PanelId.ToneStudioPanel);
            if (studioPanel != null)
            {
                curToneId = studioPanel.GetCurToneId();
            }
            return curToneId;
        }

        private void InitPanel()
        {
            _pgcToneItems.Clear();
            var pgcToneConfigList = Es.DataTables.GetInstrumentToneConfigList();

            foreach (var kvp in pgcToneClassifyNameDict)
            {
                var curToneClassify = kvp.Key;
                List<InstrumentToneConfig> curToneList = new List<InstrumentToneConfig>();
                pgcToneConfigList.ForEach(x =>
                {
                    if (x.toneClassifyType == (int)curToneClassify)
                    {
                        curToneList.Add(x);
                    }
                });

                if (curToneList.Count <= 0)
                {
                    continue;
                }

                var itemTitle = kvp.Value;
                var itemColor = pgcToneClassifyColorDict[curToneClassify];
                PgcToneContent pgcToneContent = GameObject.Instantiate(Prefab_PgcToneContent, PgcToneContentParent);
                pgcToneContent.SetTitle(itemTitle);
                pgcToneContent.SetItemColor(itemColor);
                pgcToneContent.SetOnToneItemSelectAct(OnToneItemSelectAct);
                var curItems = pgcToneContent.InitPgcToneItem(curToneList);
                _pgcToneItems.AddRange(curItems);
            }

            SetCurToneItemSelectState();
            
            isInit = true;
        }

        private void SetCurToneItemSelectState()
        {
            _pgcToneItems.ForEach(x =>
            {
                x.SetSelectState(x.GetPgcId() == GetCurToneId(), false);
            });
        }

        private void OnDisable()
        {
            MusicalInstrumentManager.Inst.StopPreviewPgcTone();
        }
    }

    public enum PgcToneClassify
    {
        HumanVoice = 6,
        Bowstring = 5,
        Strum = 4,
        Wind = 3,
        Percussion = 2,
        Keyboard = 1,
    }
}