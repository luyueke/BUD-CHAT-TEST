using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.PgcData;
using UI.BaseWidgets;
using UnityEngine.UI;

namespace Game.AnimationStudio
{
    public class AnimationStudioInfoPanel : MonoBehaviour
    {
        public TabView subTabView;
        public AnimationStudioAdapter Adapter;
        public PullToRefreshBehaviour refreshController;
        public AnimationStudioDataLoader DataLoader;
        public StudioSubType StudioType;
        public AnimationStudioType AnimationStudioType;
        private EmoteSubType _emoteSubType = EmoteSubType.ErrEmoteSubType;

        private void Awake()
        {
            InitSubTabView();
            
            //需要动态拉取数据必须要做的初始化操作
            refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
            Adapter.AnimationStudioType = AnimationStudioType;
            Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
            Adapter.Data = new SimpleDataHelper<DraftListItem>(Adapter);
            Adapter.Init();
        }

        private void Start()
        {
        }

        private void InitSubTabView()
        {
            var iconAtlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.ClassSprite);
            foreach (var cfg in subTabConfig)
            {
                var item = subTabView.CreateItem(cfg.type.ToString());
                item.SetIsSelect(false);
                var itemIcon = GameObjectEx.FindChildByName(item.transform, "Icon").GetComponent<Image>();
                itemIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(iconAtlasPath, cfg.path, gameObject);
            }
            
            subTabView.SetSelect(0);
            subTabView.AddItemSelectCallBack(OnSubTabViewClick);
        }

        public void OnSelectView()
        {
            //打开界面默认拉第一个选项
            subTabView.SelectWithoutCallback(0);
            _emoteSubType = subTabConfig[0].type;
            GetData();
        }

        private void GetData()
        {
            HideGizmo();
            Adapter.AnimationStudioType = AnimationStudioType;
            DataLoader.InitData(StudioType, AnimationStudioType, _emoteSubType);
            DataLoader.GetInstrumentStudioList(OnGetFirstPageDatas);
        }
        
        public void ResetAdpater()
        {
            if(!Adapter.IsInitialized)
                return;
            // Resetting to 0 count clears everything, including visible items, so nothing will be recycled
            Adapter.ResetItems(0);
            Adapter.ClearPool();
        }
        
        private void HideGizmo()
        {
            //需要动态拉取数据必须要做的初始化操作
            refreshController.HideGizmo();
        }
        
        public virtual void OnGetFirstPageDatas(List<DraftListItem> ListDatas)
        {
            ResetAdpater();
            if (ListDatas == null || ListDatas.Count == 0)
            {
                ListDatas = new List<DraftListItem>();
            }
            Adapter.Data.ResetItems(ListDatas);
            Adapter.OnItemsUpdated?.Invoke();
        }
        
        private void OnPullReleased()
        {
            DataLoader.GetInstrumentStudioList(OnReceivedNewModelsForInsert);
        }
        
        private void OnReceivedNewModelsForInsert(List<DraftListItem> ListDatas)
        {
            if (ListDatas == null || ListDatas.Count == 0)
            {
                Adapter.OnItemsUpdated?.Invoke();
                return;
            }
            Adapter.Data.List.AddRange(ListDatas);
            Adapter.Refresh(false);
        }

        public void SetItemOnClickAct(Action<DraftListItem> act)
        {
            Adapter.OnSelectItemAct = act;
        }
        
        private void OnSubTabViewClick(TabItem item, int i)
        {
            _emoteSubType = subTabConfig[i].type;
            GetData();
        }
        
        public static List<AvatarStudioConfig.UgcAnimConfig> subTabConfig = new()
        {
            new(){path = "ic_all", type = EmoteSubType.ErrEmoteSubType},
            new(){path = "Icon_UgcSingleEmo", type = EmoteSubType.Single},
            new(){path = "Icon_UgcDoubleEmo", type = EmoteSubType.Double},
            new(){path = "Icon_UgcPetSingleEmo", type = EmoteSubType.PetSingle},
            new(){path = "Icon_UgcPetDoubleEmo", type = EmoteSubType.PetWithPlayer},
        };
    }
}