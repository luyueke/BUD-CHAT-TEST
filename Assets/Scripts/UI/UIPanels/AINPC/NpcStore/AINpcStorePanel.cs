using System;
using System.Collections.Generic;
using Game.CommunityGame;
using Game.PropStore;
using GameData;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using SectionItem = Game.CommunityGame.SectionItem;

namespace Game.AINPCStudio
{
    public enum NpcStoreEnterType
    {
        Store = 0,
        AIYandereGameStart = 1,
        SelectNPC = 2,
    }

    public class NpcStoreEnterData
    {
        public NpcStoreEnterType EnterType = NpcStoreEnterType.Store;
        public Action<AINpcInfo> OnSelectNpcAct = null;
        public Func<string, bool> CanSelect = null;
    }
    
    public class AINpcStorePanel : BasePanel<AINpcStorePanel>
    {
        [Header("搜索")]
        public CButton Btn_Search;
        
        [Header("AINpc商城头部菜单")] 
        public Toggle Tog_Store;
        public Text Txt_TogStore;
        public Toggle Tog_MyNpc;
        public Text Txt_TogMyNpc;
        public CButton _btnClose;
        public Transform _transBG;

        [Header("下方的View")] 
        public StorePanelMyNpcView MyNpcView;
        public AINpcStorePanelCommunityView CommunityStoreView;

        [Header("左侧详情页面")]
        public AINpcStoreDetailView DetailView;
        
        [Header("下拉Panel的Adapter,我直接用来注入点击事件,别问")] 
        public AINpcPropStoreAdapter Adapter_StorePanel;
        public AINpcPropStoreAdapter Adapter_SearchPanel;
        public AINpcPurchasedAdapter Adapter_Purchased;
        public AINpcOwnedPublishedAdapter Adapter_Published;
        public PgcNpcSelectView Adapter_PgcNpcSelect;
        
        private AmbientLightSetting _srcLightSetting;
        private bool _srcHallLightVisible;
        private bool _srcGameSceneLightVisible;
        private bool _srcPreviewSceneLightVisible;

        private string SelectColor = "#121212";
        private string UnSelectColor = "#AFAFAF";

        private string _curSelectId;
        private NpcStoreEnterData _enterData = new NpcStoreEnterData();
        private NpcStoreEnterType _curEnterType = NpcStoreEnterType.Store;

        public override void OnCreate()
        {
            base.OnCreate();
            BindUI();
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            InputReceiver.Inst.enabled = false;
            _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
            _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
            _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
            _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();

            Tog_Store.isOn = true;
            OnTogStoreClick(true);

            if (args != null && args.Length > 0)
            {
                _enterData = (NpcStoreEnterData)args[0];
            }

            _curEnterType = _enterData.EnterType;
            InitEnterMode();
        }

        public override void OnHidden()
        {
            base.OnHidden();
            InputReceiver.Inst.enabled = true;
            AmbientLightManager.Inst.CloseUILight(_srcLightSetting);
            AmbientLightManager.Inst.RevertHallLight(_srcHallLightVisible);
            AmbientLightManager.Inst.RevertGameSceneLight(_srcGameSceneLightVisible);
            AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);
        }

        private void BindUI()
        {
            InitUI();
            AddListener();
        }

        private void AddListener()
        {
            _btnClose.onClick.AddListener(() =>
            {
                CloseSelf();
            });
            
            Btn_Search.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel,SearchPanel.SearchType.Npc);
            });

            Tog_Store.onValueChanged.AddListener(OnTogStoreClick);
            
            Tog_MyNpc.onValueChanged.AddListener(OnTogMyNpcViewClick);
        }

        private void InitUI()
        {
            if (_transBG == null)
            {
                return;
            }

            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(_transBG);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#5D3ACA", atlasPath, new List<string>()
            {
                "ainpc_icon1","ainpc_icon2","ainpc_icon3","ainpc_icon4"
            });
            item.gameObject.SetActive(true);
        }
        private void OnTogStoreClick(bool isOn)
        {
            Txt_TogStore.color =
                isOn
                    ? DataUtil.DeSerializeColorCheckHash(SelectColor)
                    : DataUtil.DeSerializeColorCheckHash(UnSelectColor);
            
            MyNpcView.gameObject.SetActive(!isOn);
            CommunityStoreView.gameObject.SetActive(isOn);
        }

        private void OnTogMyNpcViewClick(bool isOn)
        {
            Txt_TogMyNpc.color =
                isOn
                    ? DataUtil.DeSerializeColorCheckHash(SelectColor)
                    : DataUtil.DeSerializeColorCheckHash(UnSelectColor);
            
            MyNpcView.gameObject.SetActive(isOn);
            CommunityStoreView.gameObject.SetActive(!isOn);
        }
        
        #region 控制进入模式的UI显示
        private void InitEnterMode()
        {
            DetailView.InitEnterMode(_curEnterType, _enterData?.OnSelectNpcAct, _enterData?.CanSelect);
            MyNpcView.InitEnterMode(_curEnterType);
            CommunityStoreView.InitEnterMode(_curEnterType);
  
            //点击事件注入
            Adapter_StorePanel.SetOnClickAction((data) =>
            {
                _curSelectId = data.ugcId;
                DetailView.OnStoreItemClick(data);
                
                Adapter_Purchased.Refresh();
                Adapter_Published.Refresh();
                Adapter_PgcNpcSelect.Refresh();
            });
            Adapter_StorePanel.SetGetSelectedIdFunc(GetCurSelectedId);
            
            Adapter_SearchPanel.SetOnClickAction((data) =>
            {
                _curSelectId = data.ugcId;
                DetailView.OnStoreItemClick(data);
                
                Adapter_Purchased.Refresh();
                Adapter_Published.Refresh();
                Adapter_PgcNpcSelect.Refresh();
            });
            Adapter_SearchPanel.SetGetSelectedIdFunc(GetCurSelectedId);
            
            Adapter_Purchased.SetOnClickAction((data) =>
            {
                _curSelectId = data.ugcInfo.id;
                DetailView.OnPurchasedItemClick(data);
                
                Adapter_StorePanel.Refresh();
                Adapter_Published.Refresh();
                Adapter_PgcNpcSelect.Refresh();
            });
            Adapter_Purchased.SetGetSelectedIdFunc(GetCurSelectedId);
            
            Adapter_Published.SetOnClickAction((data) =>
            {
                _curSelectId = data.npc.id;
                DetailView.OnPublishedItemClick(data);
                
                Adapter_StorePanel.Refresh();
                Adapter_Purchased.Refresh();
                Adapter_PgcNpcSelect.Refresh();
            });
            Adapter_Published.SetGetSelectedIdFunc(GetCurSelectedId);
            
            Adapter_PgcNpcSelect.SetOnClickAction((data) =>
            {
                _curSelectId = data.id;
                DetailView.OnPgcItemClick(data);
                
                Adapter_StorePanel.Refresh();
                Adapter_Purchased.Refresh();
                Adapter_Published.Refresh();
            });
            Adapter_PgcNpcSelect.SetGetSelectedIdFunc(GetCurSelectedId);
        }

        private string GetCurSelectedId()
        {
            return _curSelectId;
        }
        #endregion
    }
}
