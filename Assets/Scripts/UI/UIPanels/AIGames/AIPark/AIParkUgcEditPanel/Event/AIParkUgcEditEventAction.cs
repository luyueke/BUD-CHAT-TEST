using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using System;
using Newtonsoft.Json;

namespace AIGame.Base
{
    public class AIParkUgcEditEventAction : MonoBehaviour
    {
        public CButton AddBtn;

        public Transform ActionParent;

        public AIParkUgcEditEventActionItem Item;

        [HideInInspector] public List<AIParkUgcEditEventActionItem> ItemLs;
        [HideInInspector] public AIParkUgcEditEvent Root;
        [HideInInspector] public AIParkUgcEditEventItem curItem;
        private void Awake()
        {
            AddBtn.onClick.AddListener(OnAddBtn);
            Item.gameObject.SetActive(false);
        }

        public void SetData(AIParkUgcEditEvent root) {
            Root = root;

            if (curItem != null && curItem.EventConfig != null)
            {
                curItem.EventConfig.actions = new List<AICommonGameConfig_Action>();
            }
            foreach (var item in ItemLs)
            {
                if (curItem != null && curItem.EventConfig != null)
                {
                    if (item.gameObject.activeSelf && item.Config_Action != null && !string.IsNullOrEmpty(item.Config_Action.id) && !string.IsNullOrEmpty(item.Config_Action.desc))
                    {
                        curItem.EventConfig.actions.Add(new AICommonGameConfig_Action() 
                        { 
                            id = item.Config_Action.id,
                            name = item.Config_Action.name,
                            desc = item.Config_Action.desc,
                        } );
                    }
                }
                item.Config_Action = null;
                item.gameObject.SetActive(false);
            }
            curItem = Root.curItem;
            var actions = curItem.EventConfig.actions;
            for (int i = 0; i < actions.Count; i++)
            {
                if (i >= ItemLs.Count)
                {
                    var _item = GameObject.Instantiate(Item, ActionParent).GetComponent<AIParkUgcEditEventActionItem>();
                    ItemLs.Add(_item);
                }
                var idx = i;
                ItemLs[i].SetData(Root, actions[i], idx);
            }
            RefreshView();
            RefreshAddBtn();
        }

        private void OnAddBtn()
        {
            if (Root.CurMapInfo.gameSetting.AICommonGameConfig.npcData == null || Root.CurMapInfo.gameSetting.AICommonGameConfig.npcData.Count <= 0)
            {
                TipPanel.ShowToast("还未设置NPC");
                return;
            }
            AIParkUgcEditEventActionItem _item = null;
            foreach (var item in ItemLs)
            {
                if (!item.gameObject.activeSelf && item.Config_Action == null)
                {
                    _item = item;
                    break;
                }
            }
            if (_item == null) 
            {
                _item = GameObject.Instantiate(Item,ActionParent).GetComponent<AIParkUgcEditEventActionItem>();
                _item.SetData(Root, new AICommonGameConfig_Action(), ItemLs.Count);
                ItemLs.Add(_item);
            }
            else
            {
                _item.SetData(Root, new AICommonGameConfig_Action(), ItemLs.IndexOf(_item));
            }
            Root.curItem.EventConfig.actions.Add(_item.Config_Action);
            RefreshView();
        }

        public void DelBtn(AIParkUgcEditEventActionItem  item) {
            item.Config_Action = null;
            item.gameObject.SetActive(false);

            RefreshView();
            RefreshAddBtn();
        }

        private void RefreshView()
        {
            var rect = transform as RectTransform;
            var c = 0;
            for (int i = 0; i < ItemLs.Count; i++)
            {
                if (ItemLs[i].gameObject.activeSelf && ItemLs[i].Config_Action != null)
                {
                    c++;
                }
            }
            rect.sizeDelta = new Vector2(100,205 * c + 80);
            LayoutRebuilder.ForceRebuildLayoutImmediate(Root.transform as RectTransform);

            var rectRoot = Root.transform as RectTransform;
            rectRoot.anchoredPosition = new Vector2(0, rectRoot.sizeDelta.y);
        }

        public void RefreshAddBtn()
        {
            var c = 0;
            for (int i = 0; i < ItemLs.Count; i++)
            {
                if (ItemLs[i].isSelect())
                {
                    c++;
                }
            }
            if (c > 0)
            {
                AddBtn.gameObject.SetActive(c != Root.CurMapInfo.gameSetting.AICommonGameConfig.npcData.Count);
            }
            else
            {
                AddBtn.gameObject.SetActive(true);
            }
        }

        public bool CanSelect(string _id, AIParkUgcEditEventActionItem _item)
        {
            foreach (var item in ItemLs)
            {
                if (item.gameObject.activeSelf && item.Config_Action != null && !string.IsNullOrEmpty(item.Config_Action.id)
                    && item.Config_Action.id == _id && item != _item)
                {
                    return false;
                }
            }
            return true;
        }
    }
}