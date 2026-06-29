using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UI.UIWidgets;
using System;
using Basic.UndoRedo;
using Game.Base;
using Game.ECS;
using UndoSystem;

namespace UI.UIPanels.GameEdit
{
    public class PgcListEditSubView : BasePropertyEditSubView
    {
        [SerializeField]private GameIconLoadItem itemTmpl;
        [SerializeField]private Transform verticalContent;
        [SerializeField]private Transform horizonContent;
        
        protected SpriteAtlas mSpriteAtlas;
        private Action<string> onSelectAction;
        private Action<string, GameIconLoadItem> OnSelectItemAction;
        private Action<string> onUndoAction;
        Action<int, GameIconLoadItem> onItemCreateAction;
        
        List<BasePropConfigData> ItemDataList;
        List<GameIconLoadItem> itemList = new List<GameIconLoadItem>();
        public bool isLoadAnim = false;
        int curSelectIndex = -1; 
        
		protected override void OnInit()
		{
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcPropSprite);
            mSpriteAtlas = XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(atlasPath, gameObject);
        }
        /// <summary>
        /// 返回带Item回调
        /// </summary>
        /// <param name="action"></param>
        public void AddItemSelectListenerByItem(Action<string,GameIconLoadItem> action)
        {
            OnSelectItemAction += action;   
        }

        public void AddItemSelectListener(Action<string> action)
        {
            onSelectAction += action;   
        }

        public void AddItemCreateListener(Action<int, GameIconLoadItem> action)
        {
            onItemCreateAction += action;   
        }

        public void AddUndoSelectListener(Action<string> action)
        {
            onUndoAction += action;
        }

        public void InitConfig<T>(List<T> dataList,GameIconLoadItem itemPrefab = null) where T:BasePropConfigData
        {
            if (itemPrefab != null)
            {
                itemTmpl = itemPrefab;
            }

            ItemDataList = dataList.ConvertAll(x => (BasePropConfigData)x);
            InitItemList();
        }
        
        public void InitConfig<T>(List<T> dataList,SpriteAtlas spriteStlas,GameIconLoadItem itemPrefab = null) where T:BasePropConfigData
        {
            if (spriteStlas != null)
            {
                mSpriteAtlas = spriteStlas;
            }

            InitConfig(dataList,itemPrefab);
        }
        
        public void InitConfig(List<Es.GamePropData> propDataList,GameIconLoadItem itemPrefab = null)
        {
            var dataList = ConverData(propDataList);
            InitConfig(dataList,itemPrefab);
        }

        public List<BasePropConfigData> ConverData(List<Es.GamePropData> propDataList)
        {
            List<BasePropConfigData> iconDatas = new List<BasePropConfigData>();
            for (int i = 0; i < propDataList.Count; i++)
            {
                var propConfig = propDataList[i];
                var iconData = new BasePropConfigData
                {
                    Id = propConfig.Id,
                    IconName = propConfig.IconName
                };
                iconDatas.Add(iconData);
            }

            return iconDatas;
        }


        void InitItemList()
        {
            itemTmpl.gameObject.SetActive(false);
            for (int i = 0; i < ItemDataList.Count; i++)
            {
                var index = i;
                var itemData = ItemDataList[i];
                var itemNode = Instantiate(itemTmpl, verticalContent);
                if (!string.IsNullOrEmpty(itemData.IconName))
                {
                    itemNode.SetIcon(mSpriteAtlas.GetSprite(itemData.IconName));
                } else {
                    itemNode.GetIconGo().SetActive(false);
                }
                itemNode.AddOnSelectListener(() => OnItemSelect(itemNode, index));
                itemNode.gameObject.SetActive(true);
                itemNode.SetSelectWithNoNotify(i == curSelectIndex);
                itemList.Add(itemNode);

                onItemCreateAction?.Invoke(index, itemNode);
            }
        }

        private GameIconLoadItem GetItemNodeById(string id)
        {
            for (int i = 0; i < ItemDataList.Count; i++)
            {
                if (id == ItemDataList[i].Id)
                {
                    return itemList[i];
                }
            }

            return null;
        }
        
        public void SelectItem(string id)
        {
            var selectItem = GetItemNodeById(id);
            if (selectItem != null)
            {
                selectItem.SetSelect(true);
            }
        }

        public void SelectItemWithNoNotify(string id)
        {
            for (int i = 0; i < ItemDataList.Count; i++)
            {
                if (id == ItemDataList[i].Id)
                {
                    itemList[i].SetSelectWithNoNotify(true);
                    if (curSelectIndex >= 0)
                    {
                        itemList[curSelectIndex].SetSelectWithNoNotify(false);
                    }
                    curSelectIndex = i;
                    break;
                }
            }
        }


        protected override void OnExpand(bool bIsExpand)
		{
			base.OnExpand(bIsExpand);

            foreach (var item in itemList)
            {
                if (bIsExpand)
                {
                    item.transform.parent = verticalContent;
                } else {
                    item.transform.parent = horizonContent;
                    item.gameObject.SetActive(true);
                }
            }
        }
        
        void OnItemSelect(GameIconLoadItem selectItem, int index)
        {
            if (curSelectIndex == index) return;
            if (curSelectIndex >= 0)
            {
                itemList[curSelectIndex].HideLoading();
                itemList[curSelectIndex].SetSelectWithNoNotify(false);
            }
            selectItem.SetLoadingVisible(isLoadAnim);
            var beginData = CreateUndoData(ItemDataList[curSelectIndex].Id);
            onSelectAction?.Invoke(ItemDataList[index].Id);
            OnSelectItemAction?.Invoke(ItemDataList[index].Id, selectItem);
            var endData = CreateUndoData(ItemDataList[index].Id);
            AddRecord(beginData, endData);
            curSelectIndex = index;
        }

        public void OnUndoSelect(string propId)
        {
            onUndoAction?.Invoke(propId);
            SelectItemWithNoNotify(propId);
        }

        private PGCCommonSelectUndoData CreateUndoData(string id)
        {
            PGCCommonSelectUndoData data = new PGCCommonSelectUndoData();
            data.pgcId = id;
            data.viewIndex = viewIndex;
            data.targetEntity = selectEntity;
            data.adapterType = GetAdapter()?.GetType();
            return data;
        }
        
        public void AddRecord(PGCCommonSelectUndoData beginData, PGCCommonSelectUndoData endData)
        {
            UndoRecord record = new UndoRecord(UndoHelperName.PGCCommonSelectUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }
        
    }
}