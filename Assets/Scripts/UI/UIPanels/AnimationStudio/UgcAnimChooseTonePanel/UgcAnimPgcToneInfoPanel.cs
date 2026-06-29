using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using GameData.BaseInfo;
using UnityEngine;

public class UgcAnimPgcToneInfoPanel : MonoBehaviour
{
    public Transform PgcToneContentParent;
    public UgcAnimPgcToneItem Prefab_PgcToneItem;
    
    private List<UgcAnimPgcToneItem> _pgcToneItems = new List<UgcAnimPgcToneItem>();
    private Action<AnimMusicInfo> _onItemSelect;
    private bool isInit = false;
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

        public void SetOnToneItemSelectAct(Action<AnimMusicInfo> act)
        {
            this._onItemSelect = act;
        }

        private void OnToneItemSelectAct(AnimMusicInfo toneInfo)
        {
            this._onItemSelect?.Invoke(toneInfo);
            _pgcToneItems.ForEach(x=>x.SetSelectState(false));
        }
        
        public string GetCurToneId()
        {
            string curToneId = null;
            var studioPanel = UIManager.Inst.FindPanel<UgcAnimChooseTonePanel>(PanelId.UgcAnimChooseTonePanel);
            if (studioPanel != null)
            {
                curToneId = studioPanel.GetCurToneId();
            }
            return curToneId;
        }

        private void InitPanel()
        {
            _pgcToneItems.Clear();
            var pgcToneConfigList = Es.DataTables.GetUgcAnimBgmConfigList();
            foreach (var config in pgcToneConfigList)
            {
                UgcAnimPgcToneItem item = GameObject.Instantiate(Prefab_PgcToneItem, PgcToneContentParent);
                item.InitSelectMode(config, OnToneItemSelectAct);
                _pgcToneItems.Add(item);
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

        }
}
