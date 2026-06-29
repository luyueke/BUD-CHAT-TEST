using System;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.MusicalInstrument
{
    public class ToneStudioPanelData
    {
        public ToneInfo CurToneInfo;
        public Action<ToneInfo> OnSelectToneItem;
    }
    public class ToneStudioPanel : BasePanel<ToneStudioPanel>
    {
        [SerializeField] private Transform _trans_Bg;
        [SerializeField] private NavigationBarTabs navigationBarTabs;
        [SerializeField] private PgcToneInfoPanel PgcToneList;
        [SerializeField] private UgcToneInfoPanel UgcToneList;
        [SerializeField] private TimbreCommunityView storeView;
        
        private ToneStudioPanelData _curPanelData;
        public enum ToneStudioType
        {
            PGC = 0,
            Store = 1,
            UGC = 2,
        }

        public class ToneStudioConfig
        {
            public string name;
            public ToneStudioType studioType;
        }

        private List<ToneStudioConfig> rtConfig = new()
        {
            new() { name = "官方音色", studioType = ToneStudioType.PGC },
            new() { name = "社区音色", studioType = ToneStudioType.Store },
            new() { name = "本地音色", studioType = ToneStudioType.UGC },
        };

        public override void OnCreate()
        {
            base.OnCreate();
            MessageHelper.AddListener(MessageName.OnUgcTonePublishedListChange, OnUgcTonePublishedListChange);
            Message.MessageHelper.AddListener<UgcBaseInfo>(MessageName.UgcToneDidPublishedNew, ReloadPublishPage);
            InitBG();
            navigationBarTabs.AddBackBtnClickListener(OnBackBtnClick);
            foreach (var cfg in rtConfig)
            {
                navigationBarTabs.CreateItem(cfg.studioType.ToString(), cfg.name).SetIsSelect(false);
            }

            navigationBarTabs.AddBackBtnClickListener(CloseSelf);
            navigationBarTabs.AddItemSelectCallBack(RTClick);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            _curPanelData = (ToneStudioPanelData)args[0];
            
            UgcToneList.SetOnToneItemSelectAct(OnToneItemSelected);
            PgcToneList.SetOnToneItemSelectAct(OnToneItemSelected);
            storeView?.SetOnToneItemSelectAct(OnToneItemSelected);
            
            navigationBarTabs.SetSelect((int)ToneStudioType.PGC);
        }

        private void InitBG()
        {
            if (_trans_Bg == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(_trans_Bg);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "music_icon_1", "music_icon_2", "music_icon_3"
            });
            item.gameObject.SetActive(true);
        }

        private void RTClick(TabItem item, int index)
        {
            var data = rtConfig[index];
            OnSelectView(data.studioType);
        }

        private void OnSelectView(ToneStudioType studioType)
        {
            storeView.gameObject.SetActive(studioType == ToneStudioType.Store);
            switch (studioType)
            {
                case ToneStudioType.PGC:
                    PgcToneList.gameObject.SetActive(true);
                    UgcToneList.gameObject.SetActive(false);
                    PgcToneList.OnSelectPanel();
                    break;
                
                case ToneStudioType.Store:
                    PgcToneList.gameObject.SetActive(false);
                    UgcToneList.gameObject.SetActive(false);
                    break;
                    
                case ToneStudioType.UGC:
                    PgcToneList.gameObject.SetActive(false);
                    UgcToneList.gameObject.SetActive(true);
                    UgcToneList.GetPublishedData();
                    break;
            }
        }

        private void OnBackBtnClick()
        {
            UIManager.Inst.ClosePanel(this);
        }
        
        protected override void OnDestroy()
        {
            MessageHelper.RemoveListener(MessageName.OnUgcTonePublishedListChange, OnUgcTonePublishedListChange);
            Message.MessageHelper.RemoveListener<UgcBaseInfo>(MessageName.UgcToneDidPublishedNew, ReloadPublishPage);
        }

        private void OnUgcTonePublishedListChange()
        {
            OnSelectView(ToneStudioType.UGC);
        }
        
        private void OnToneItemSelected(ToneInfo toneInfo)
        {
            _curPanelData.CurToneInfo = toneInfo;
            _curPanelData.OnSelectToneItem?.Invoke(toneInfo);
        }

        public string GetCurToneId()
        {
            return _curPanelData?.CurToneInfo?.id;
        }

        public override void OnHidden()
        {
            base.OnHidden();
        }
        
        private void ReloadPublishPage(UgcBaseInfo info)
        {
            navigationBarTabs.SetSelect((int)ToneStudioType.Store);
            storeView.ShowAndReloadPublish();
        }

    }
}