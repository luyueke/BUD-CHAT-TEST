using System;
using Game.AINPCStudio;
using Game.Audio;
using Game.Base;
using Game.Config;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using Game.PropStore;
using Game.Utils;
using Message;
using UI.BaseWidgets;
using UI.Manager;
using UndoSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:24-06-21 21:13:33
/// </summary>
namespace Game.AssetToolBox
{
    public class AssetToolBoxPanel : MonoBehaviour
    {
        private CButton Btn_ShowToolBox;
        private CButton Btn_CloseToolBox;
        private GameObject Go_ShowToolBox;
        private GameObject Go_ToolBox;
        private GameObject Prop_ToolBox;
        private GameObject Npc_ToolBox;
        private Toggle Tog_PropType;
        private Toggle Tog_NpcType;
        private Toggle Tog_PropStore;
        private Toggle Tog_Bag;
        private Toggle Tog_NpcStore;
        private Toggle Tog_NpcBag;
        private ToolBoxOSAView PropOsaView;
        private ToolBoxOSAView BagOsaView;
        private ToolBoxOSAView NpcPropOsaView;
        private ToolBoxOSAView NpcBagOsaView;
        private int propCurrentIndex = 0;
        private int npcCurrentIndex = 2;
        private ToolBoxOSAView _currentOsaView;
          private SearchInputView input_search;

        public void Init()
        {
            MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
            Btn_ShowToolBox = GameObjectEx.FindChildByName(this.transform, "Btn_ShowToolBox").GetComponent<CButton>();
            Btn_CloseToolBox = GameObjectEx.FindChildByName(this.transform, "Btn_CloseToolBoxPanel").GetComponent<CButton>();
            Go_ShowToolBox = GameObjectEx.FindChildByName(this.transform, "Btn_ShowToolBox").gameObject;
            Go_ToolBox = GameObjectEx.FindChildByName(this.transform, "ToolBoxPanel").gameObject;
            Prop_ToolBox = GameObjectEx.FindChildByName(this.transform, "PropView").gameObject;
            Npc_ToolBox = GameObjectEx.FindChildByName(this.transform, "NPCView").gameObject;

            Tog_PropType = GameObjectEx.FindChildByName(this.transform, "PropTog").GetComponent<Toggle>();
            Tog_NpcType = GameObjectEx.FindChildByName(this.transform, "NPCTog").GetComponent<Toggle>();
            Tog_PropStore = GameObjectEx.FindChildByName(this.transform, "StoreTog").GetComponent<Toggle>();
            Tog_Bag = GameObjectEx.FindChildByName(this.transform, "BagTog").GetComponent<Toggle>();
            Tog_NpcStore = GameObjectEx.FindChildByName(this.transform, "NPCStoreTog").GetComponent<Toggle>();
            Tog_NpcBag = GameObjectEx.FindChildByName(this.transform, "NPCBagTog").GetComponent<Toggle>();

            PropOsaView = GameObjectEx.FindChildByName(this.transform, "PropStoreOsaView").GetComponent<ToolBoxOSAView>();
            BagOsaView = GameObjectEx.FindChildByName(this.transform, "BagOsaView").GetComponent<ToolBoxOSAView>();
            NpcPropOsaView = GameObjectEx.FindChildByName(this.transform, "NPCPropStoreOsaView").GetComponent<ToolBoxOSAView>();
            NpcBagOsaView = GameObjectEx.FindChildByName(this.transform, "NPCBagOsaView").GetComponent<ToolBoxOSAView>();

            PropOsaView.SetBottomBtnAct(() =>
            {
                UIManager.Inst.OpenPanel<PropStorePanel>(PanelId.PropStorePanel);
            });
            BagOsaView.SetBottomBtnAct(() =>
            {
                UIManager.Inst.OpenPanel<InventoryBagPanel>(PanelId.InventoryBagPanel);
            });
            NpcPropOsaView.SetBottomBtnAct(() =>
            {
                UIManager.Inst.OpenPanel(PanelId.AINpcStorePanel);
            });
            NpcBagOsaView.SetBottomBtnAct(() =>
            {
                var ui = UIManager.Inst.OpenPanel<AIBuddySelectPanel>(PanelId.AIBuddySelectPanel);
                ui.SetItemOnClickAct(OnSelectAIBuddy);
            });

            Btn_ShowToolBox.onClick.AddListener(() => { ShowToolBox(true); });
            Btn_CloseToolBox.onClick.AddListener(() => { ShowToolBox(false); });

            Tog_PropType.onValueChanged.AddListener((isOn) => { if (isOn) ShowToolByType(true); });
            Tog_NpcType.onValueChanged.AddListener((isOn) => { if (isOn) ShowToolByType(false); });

            Tog_PropStore.onValueChanged.AddListener((isOn) => { ShowPropStoreView(0); propCurrentIndex = 0; });
            Tog_Bag.onValueChanged.AddListener((isOn) => { ShowPropStoreView(1); propCurrentIndex = 1; });
            Tog_NpcStore.onValueChanged.AddListener((isOn) => { ShowPropStoreView(2); npcCurrentIndex = 2; });
            Tog_NpcBag.onValueChanged.AddListener((isOn) => { ShowPropStoreView(3); npcCurrentIndex = 3; });

            ShowToolByType(true);
            Tog_PropStore.SetIsOnWithoutNotify(true);
            ShowPropStoreView(0, true);
            _currentOsaView = PropOsaView;

            PropOsaView.SetOSAItemClickAct(OnStoreItemClick);
            BagOsaView.SetOSAItemClickAct(OnOwnedItemClick);
            NpcPropOsaView.SetOSAItemClickAct(OnNpcStoreItemClick);
            NpcBagOsaView.SetOSAItemClickAct(OnNpcOwnedItemClick);

            var searchGo = GameObjectEx.FindChildByName(transform, "input_search");
            if (searchGo != null)
            {
                input_search = searchGo.GetComponent<SearchInputView>();
                input_search.SetOnConfirm(SearchByKeyword);
                input_search.SetOnClear(OnSearchClear);
            }
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        }

        private void ShowToolByType(bool show)
        {
            Prop_ToolBox.SetActive(show);
            Npc_ToolBox.SetActive(!show);

            if (input_search != null)
                input_search.gameObject.SetActive(show);

            if (show)
            {
                ShowPropStoreView(propCurrentIndex);
            }
            else
            {
                ShowPropStoreView(npcCurrentIndex);
            }
        }

        /// <summary>
        /// showType 类型 0 素材商城，1 背包素材，2 NPC商城，3 背包NPC
        /// </summary>
        /// <param name="showType"></param>
        /// <param name="isIgnoreSound"></param>
        private void ShowPropStoreView(int showType, bool isIgnoreSound = false)
        {
            if (!isIgnoreSound)
            {
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
            }
            PropOsaView.gameObject.SetActive(showType == 0);
            BagOsaView.gameObject.SetActive(showType == 1);
            NpcPropOsaView.gameObject.SetActive(showType == 2);
            NpcBagOsaView.gameObject.SetActive(showType == 3);
            _currentOsaView = showType switch
            {
                0 => PropOsaView,
                1 => BagOsaView,
                2 => NpcPropOsaView,
                _ => NpcBagOsaView,
            };
        }

        public void SearchByKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                if (_currentOsaView != null)
                    _currentOsaView.GetFirstPageDatas();
                return;
            }

            if (_currentOsaView == null) return;

            int searchScope = _currentOsaView == PropOsaView ? 0 : 1;
            _currentOsaView.StartSearch(keyword, searchScope);
        }

        private void OnSearchClear()
        {
            if (_currentOsaView != null)
                _currentOsaView.GetFirstPageDatas();
        }

        private void ShowToolBox(bool show)
        {
            Go_ShowToolBox.gameObject.SetActive(!show);
            Go_ToolBox.gameObject.SetActive(show);
        }

        private void OnStoreItemClick(ToolBoxItemData itemData)
        {
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Prop, itemData.ugcInfo.id,itemData.ugcInfo.ugcStyle);
        }

        private void OnOwnedItemClick(ToolBoxItemData itemData)
        {
            CreateUgcAsset(itemData);
        }

        private void OnNpcStoreItemClick(ToolBoxItemData itemData)
        {
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.AINpc, itemData.ugcInfo.id,itemData.ugcInfo.ugcStyle);
        }

        private void OnNpcOwnedItemClick(ToolBoxItemData itemData)
        {
            NodeBaseBehaviour nBehav;
            var opReason = GamePropNodeManager.Inst.TryCreateInEdit("20100068", out nBehav);
            if (opReason == GameGlobalEnum.NodeOpReason.CreateSuccess)
            {
                if (nBehav is AIBuddyInMapBehaviour aiBehaviour)
                {
                    aiBehaviour.aIBuddyInMapComponent.AiBuddyID = itemData.ugcInfo.id;
                    // 走 UI 侧编排器按 id 拉取 Cabin 数据装配（默认皮肤、无盒子）
                    UI.UIPanels.AINPC.AIBuddyInMap.AIBuddyInMapUIManager.Inst.SetupFromId(aiBehaviour);
                    InputHandlerManager.Inst.SelectEntity(nBehav.entity);
                }
                
                UndoRecordUtils.AddCreateRecord(nBehav.gameObject);
            }
            else if (opReason == GameGlobalEnum.NodeOpReason.CreateFail_MaxNum)
            {
                var propConfig = GamePropDataHelper.GetPropDataByID("20100068");
                TipPanel.ShowToast($"最多支持{propConfig.MaxNum}个，已超出数量上限。");
            }
        }

        private void OnSelectAIBuddy(AIBuddySelectItemData data)
        {
            NodeBaseBehaviour nBehav;
            var opReason = GamePropNodeManager.Inst.TryCreateInEdit("20100068", out nBehav);
            if (opReason == GameGlobalEnum.NodeOpReason.CreateSuccess)
            {
                if (nBehav is AIBuddyInMapBehaviour aiBehaviour)
                {
                    aiBehaviour.aIBuddyInMapComponent.AiBuddyID = data.ugcInfo.id;
                    // 走 UI 侧编排器按 id 拉取 Cabin 数据装配（默认皮肤、无盒子）
                    UI.UIPanels.AINPC.AIBuddyInMap.AIBuddyInMapUIManager.Inst.SetupFromId(aiBehaviour);
                    InputHandlerManager.Inst.SelectEntity(nBehav.entity);
                }
                
                UndoRecordUtils.AddCreateRecord(nBehav.gameObject);
            }
            else if (opReason == GameGlobalEnum.NodeOpReason.CreateFail_MaxNum)
            {
                var propConfig = GamePropDataHelper.GetPropDataByID("20100068");
                TipPanel.ShowToast($"最多支持{propConfig.MaxNum}个，已超出数量上限。");
            }
        }

        private void CreateUgcAsset(ToolBoxItemData itemData)
        {
            if (itemData != null && itemData.ugcInfo != null)
            {
                GlobalNodeManager.Inst.Get<PropManager>().Create(itemData.ugcInfo, behaviour =>
                {
                    if (behaviour != null)
                    {
                        UI.Manager.InputHandlerManager.Inst.SelectEntity(behaviour.entity);
                    }
                });
            }
            else
            {
                LoggerUtils.LogError("itemData 类型不对或为null:");
            }
        }
        
        private void OnBuyUgcItemSuccess(string ugcId)
        {
            if(PropOsaView.IsInit)
                PropOsaView.GetFirstPageDatas();
            
            if(BagOsaView.IsInit)
                BagOsaView.GetFirstPageDatas();
        }
    }
}
