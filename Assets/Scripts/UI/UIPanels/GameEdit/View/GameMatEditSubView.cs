/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-24 15:07:03
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-12 23:03:37
 * @ Description: 道具材质面板的子界面
 */
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using System;
using Basic.UndoRedo;
using Basic.Utils;
using Game.Base;
using Game.CommunityGame;
using UndoSystem;
using GameData;

namespace UI.UIPanels.GameEdit
{
    public class GameMatEditSubView : BasePropertyEditSubView
    {
        [Header("业务UI")]
        [SerializeField]private TabView typeTabView;
        [SerializeField]private GameMatEditOSAEntry matEditOSAEntry;
        [SerializeField]private CButton goMatStoreEmptyBtn;

        [Header("纹理大小")]
        [SerializeField]private GameObject tilingOptionGo;
        [SerializeField]private CButton addBtn;
        [SerializeField]private CButton subBtn;

        bool isEnableUnRedo = true; // 是否开启UnRedo

        const float TilingStep = 0.1f;
        Action<GameMatUIData> onMatChangeAction;
        Action<float> onTilingChangeAction;


        class MatGroupTypeItem
        {
            public MatGroupTypeEnum Type;
            public string Name;
            public bool isNewTag = false;
        }
        

        private List<MatGroupTypeItem> groupTypeConfig = new List<MatGroupTypeItem>()
        {
            new MatGroupTypeItem(){Type = MatGroupTypeEnum.Store, Name = "社区材质"},
            new MatGroupTypeItem(){Type = MatGroupTypeEnum.Other, Name = "其他"},
            new MatGroupTypeItem(){Type = MatGroupTypeEnum.Anime, Name = "动漫",isNewTag = true},
            new MatGroupTypeItem(){Type = MatGroupTypeEnum.Wood, Name = "木质"},
            new MatGroupTypeItem(){Type = MatGroupTypeEnum.Stone, Name = "石头"},
            new MatGroupTypeItem(){Type = MatGroupTypeEnum.Grass, Name = "草"},
            new MatGroupTypeItem(){Type = MatGroupTypeEnum.Metal, Name = "金属"},
            new MatGroupTypeItem(){Type = MatGroupTypeEnum.Pattern, Name = "图案"},
        };

		protected override void OnInit()
		{
			addBtn.onClick.AddListener(OnAddClik);
            subBtn.onClick.AddListener(OnSubClik);
            goMatStoreEmptyBtn.onClick.AddListener(OnGoStore);
            
            InitGroupTypeTabView();

            matEditOSAEntry.Init(OnMatChangeCallback);
		}

        /// <summary>
        /// 默认选择一个材质
        /// 注意： 带通知的材质设定，这个方法会触发Undo/Redo
        /// </summary>
        public void SetMaterial(MaterialUnionID matId)
        {
            SetMaterial(matId, true);
        }

        /// <summary>
        /// 设置材质
        /// 不带通知回调的
        /// </summary>
        public void SetMaterialWithNoNotify(MaterialUnionID matId)
        {
            SetMaterial(matId, false);
        }

        void SetMaterial(MaterialUnionID matId, bool isNotify)
        {
            MatGroupTypeEnum groupType;
            if (matId.IsUGC)
            {
                groupType = MatGroupTypeEnum.Store;
            } else {
                var matData = matEditOSAEntry.GetMatUIDataOrDefaultById(matId);
                groupType = matData.MatGroupType;
            }
            matEditOSAEntry.SetInitializeMaterial(matId, isNotify);
            int groupIndex = groupTypeConfig.FindIndex(x=>x.Type == groupType);
            typeTabView.SetSelect(groupIndex);
        }

        /// <summary>
        /// 隐藏纹理大小的选项
        /// </summary>
        public void HideTileSizeOption()
        {
            tilingOptionGo.SetActive(false);
        }

        public void AddMatChangeListener(Action<GameMatUIData> action)
        {
            onMatChangeAction += action;
        }

        public void AddTilingChangeListener(Action<float> action)
        {
            onTilingChangeAction += action;
        }

        /// <summary>
        /// 初始化材质类型选择
        /// </summary>
        void InitGroupTypeTabView()
        {
            var adapter = this.transform.GetComponentInParent<TerrainViewAdapter>(true);
            if (adapter != null)
            {
                groupTypeConfig.RemoveAt(2);
            }

            foreach (var item in groupTypeConfig)
            {
                bool isNewTag = item.isNewTag;
                var tabItem = typeTabView.CreateItem($"MatTypeItem_{item.Type.ToString()}", item.Name);
                var newTag = tabItem.transform.Find("New");
                if (newTag != null)
                {
                    bool isNotClick = SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveGameUtil.UIQuailityTipPopUp) == 0;
                    newTag.gameObject.SetActive(item.isNewTag && isNotClick);
                }

                tabItem.AddValueChangeCallListener((isOn) =>
                {
                    if (isNewTag)
                    {
                        newTag.gameObject.SetActive(false);
                        SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.UIQuailityTipPopUp, 1);
                    }
                    var line = GameObjectEx.FindChildByName(tabItem.transform, "Line");
                    line.gameObject.SetActive(isOn);
                });
            }

            typeTabView.AddItemSelectCallBack(OnMatTypeSelect);
        }

		protected override void OnExpand(bool bIsExpand)
		{
			base.OnExpand(bIsExpand);
            matEditOSAEntry.ChangeAdapter(!bIsExpand);
		}

        void OnMatTypeSelect(TabItem item, int index)
        {
            // 显示Mat列表
            matEditOSAEntry.ResetView(groupTypeConfig[index].Type);
        }

        void OnAddClik()
        {
            onTilingChangeAction?.Invoke(TilingStep);
        }

        void OnSubClik()
        {
            onTilingChangeAction?.Invoke(-TilingStep);
        }

        void OnGoStore()
        {
            var storePanel = UIManager.Inst.OpenPanel<MaterialStorePanel>(PanelId.MaterialStorePanel);
            storePanel.AddRefreshMaterialList(GameUgcMatManager.Inst.ForceRefreshInteractList);
        }

        void OnMatChangeCallback(GameMatUIData beforeMatData, GameMatUIData nowMatData, bool isNotify)
        {
            if (beforeMatData != null)
                CreateUndoData(beforeMatData, nowMatData);

            if (isNotify)
                onMatChangeAction?.Invoke(nowMatData);
        }

        #region Undo/Redo

        public void EnableUnRedo(bool isEnable)
        {
            isEnableUnRedo = isEnable;
        }

        public void OnUndoSelect(MatCommonSelectUndoData data)
        {
            matEditOSAEntry.SetUndoMaterialId(data.matId);
            var matData = matEditOSAEntry.GetMatDataById(data.matId);
            int groupIndex = 0;
            if (matData != null) {
                groupIndex = groupTypeConfig.FindIndex(x=>x.Type == matData.MatGroupType);
            }
            typeTabView.SetSelect(groupIndex);
            onMatChangeAction?.Invoke(matData);
        }

        public void CreateUndoData(GameMatUIData beforeMatData, GameMatUIData nowMatData)
        {
            if (!isEnableUnRedo) return;

            var beginData = CreateUndoData(beforeMatData);
            var endData = CreateUndoData(nowMatData);

            AddRecord(beginData, endData);
        }

        MatCommonSelectUndoData CreateUndoData(GameMatUIData matData)
        {
            MatCommonSelectUndoData data = new MatCommonSelectUndoData();
            data.matId = matData.Id;
            data.targetEntity = selectEntity;
            data.adapterType = GetAdapter()?.GetType();
            return data;
        }

        void AddRecord(MatCommonSelectUndoData beginData, MatCommonSelectUndoData endData)
        {
            UndoRecord record = new UndoRecord(UndoHelperName.MatCommonSelectUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }

        #endregion
    }
}
