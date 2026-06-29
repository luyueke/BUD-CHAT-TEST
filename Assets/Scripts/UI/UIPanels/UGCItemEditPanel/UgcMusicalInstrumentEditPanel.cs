using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsManagers;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.MapData;
using GameData.UGCData;
using Google.Protobuf;
using Newtonsoft.Json;
using Pb.Map;
using UGCAsset;
using UI;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.MusicalInstrument
{
    public class UgcMusicalInstrumentEditPanel : UGCItemEditPanel
    {
        public CButton Btn_SetTone;
        public CButton Btn_SetAnim;
        public CButton Btn_TryPlay;

        public override void OnCreate()
        {
            base.OnCreate();
            Btn_SetTone.onClick.AddListener(OnBtnSetToneClick);
            Btn_SetAnim.onClick.AddListener(OnBtnSetAnimClick);
            Btn_TryPlay.onClick.AddListener(OnBtnTryPlayClick);
            
            var ugcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
            var skinActionInfo = GameDataManager.Inst.mapGlobalData.skinActionInfo;
            var instrumentInfo = skinActionInfo.instrumentInfo;
            if (instrumentInfo.toneInfo == null)
            {
                instrumentInfo.toneInfo = MusicalInstrumentUtils.GetDefaultToneInfo();
                LoggerUtils.LogError(AccountDataManager.Inst.Uid + "instrumentInfo.toneInfo Is Null");
            }
            if (instrumentInfo.toneInfo.isDelete == 1)
            {
                var instrumentDraftInfo = InstrumentAssetManager.Inst.GetOrCreateDraftInfo(ugcInfo, skinActionInfo);
                if (instrumentDraftInfo != null)
                {
                    instrumentDraftInfo._skinActionInfo.instrumentInfo.toneInfo = MusicalInstrumentUtils.GetDefaultToneInfo();
                }
                
                skinActionInfo.instrumentInfo.toneInfo = MusicalInstrumentUtils.GetDefaultToneInfo();
                
                TipPanel.ShowToast("设置的自定义音色已被删除，自动切换为默认音色");
            }
        }

        private void OnBtnSetToneClick()
        {
            ToneStudioPanelData panelData = new ToneStudioPanelData();
            panelData.CurToneInfo = GameDataManager.Inst.mapGlobalData.skinActionInfo.instrumentInfo.toneInfo;
            panelData.OnSelectToneItem = OnSelectedToneInfo;
            UIManager.Inst.OpenPanel(PanelId.ToneStudioPanel, panelData);
        }
        
        private void OnSelectedToneInfo(ToneInfo toneInfo)
        {
            GameDataManager.Inst.mapGlobalData.skinActionInfo.instrumentInfo.toneInfo = toneInfo;
        }

        private void OnBtnSetAnimClick()
        {
            InstrumentChooseAnimPanelData panelData = new InstrumentChooseAnimPanelData();
            panelData.CurInstrumentInfo = GameDataManager.Inst.mapGlobalData.skinActionInfo.instrumentInfo;
            panelData.CurSkinInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
            panelData.OnSelectUgcAnim = OnSelectedUgcAnimInfo;
            UIManager.Inst.OpenPanel(PanelId.MusicalInstrumentChooseAnimPanel, panelData, true);
        }
        
        private void OnSelectedUgcAnimInfo(string id)
        {
            GameDataManager.Inst.mapGlobalData.skinActionInfo.instrumentInfo.moveId = id;
        }

        private void OnBtnTryPlayClick()
        {
            var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
            var ugcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
            var skinActionInfo = GameDataManager.Inst.mapGlobalData.skinActionInfo;
            var propManager = GlobalNodeManager.Inst.Get<PropManager>();
            propManager.RemovePropData(ugcInfo.id);
            if (ugcInfo.skinDetailInfo == null) {
                ugcInfo.skinDetailInfo = SkinDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            }
            ugcInfo.skinDetailInfo.size = itemData.Size.ToVector3();
            

            var metaDataBytes = itemData.ToByteArray();
            var itemPb = MapPbDataTool.ParsePropPb(metaDataBytes);
            if (itemPb != null) {
                if (itemPb.UgcmatData != null) {
                    GameUgcMatManager.Inst.AddUGCMatData(itemPb.UgcmatData);
                    var pNodeData = AssetPropNodeManager.Inst.GetEmptyNodeData(ugcInfo.id);
                    pNodeData.Prims.AddRange(itemPb.NodeData.Prims);
                    propManager?.AddPropData(ugcInfo.id, pNodeData);
                }
            }
            
            var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
            saveCharacterData.ChangeSkinData(ugcInfo);
            UIManager.Inst.SwapPanel(PanelId.TryMusicalInstrumentPanel, saveCharacterData, skinActionInfo.instrumentInfo);
        }

        protected override void OnPropSkinPreviewBtnClick()
        {
            var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
            var ugcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
            var skinActionInfo = GameDataManager.Inst.mapGlobalData.skinActionInfo;
            var propManager = GlobalNodeManager.Inst.Get<PropManager>();
            propManager.RemovePropData(ugcInfo.id);
            if (ugcInfo.skinDetailInfo == null) {
                ugcInfo.skinDetailInfo = SkinDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            }
            ugcInfo.skinDetailInfo.size = itemData.Size.ToVector3();
            
            var instrumentDraftInfo = InstrumentAssetManager.Inst.GetOrCreateDraftInfo(ugcInfo, skinActionInfo);
            if (instrumentDraftInfo == null)
            {
                return;
            }
            
            var publishStateMachine = new UGCPublishStateMachine();
            publishStateMachine.SetStates(new List<UGCPublishStateBase>() {
                new UGCPublishStateBase(UGCPublishState.SetPropSkinAnchor),
                new UGCPublishStateBase(UGCPublishState.SetInstrumentAdjust),
                new UGCPublishStateBase(UGCPublishState.SetInstrumentAdjustWithAnim),
            });
            publishStateMachine.SetEditData(new InstrumentEditData() {
                skinActionDraftInfo = instrumentDraftInfo,
                metaDataBytes = itemData.ToByteArray(),
            });

            publishStateMachine.Start();
        }

        public override void OnCoverPhotoClick()
        {
            var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
            var skinInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
            var skinActionInfo = GameDataManager.Inst.mapGlobalData.skinActionInfo;
            var publishStateMachine = new UGCPublishStateMachine();
            var propManager = GlobalNodeManager.Inst.Get<PropManager>();
            propManager.RemovePropData(skinInfo.id);
            
            if (skinInfo.skinDetailInfo == null) {
                skinInfo.skinDetailInfo = SkinDetailInfo.FromDetailInfo(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            } 
            else {
                skinInfo.skinDetailInfo.Assign(GamePropNodeManager.Inst.GetUGCItemDetailInfo());
            }
                
            var instrumentDraftInfo = InstrumentAssetManager.Inst.GetOrCreateDraftInfo(skinInfo, skinActionInfo);
            
            publishStateMachine.SetStates(new List<UGCPublishStateBase>() {
                new UGCPublishStateBase(UGCPublishState.InstrumentCoverView)
            });
            publishStateMachine.SetEditData(new InstrumentEditData() {
                skinActionDraftInfo = instrumentDraftInfo,
                metaDataBytes = itemData.ToByteArray(),
            });
            
            publishStateMachine.Start();
        }

        public override void OnCombinePanelClose()
        {
            UIManager.Inst.OpenPanel(PanelId.UgcMusicalInstrumentEditPanel);
        }
    }
}
