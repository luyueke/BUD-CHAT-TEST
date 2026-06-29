using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsManagers;
using GameData.BaseInfo;
using GameData.Manager;
using Google.Protobuf;
using Pb.Map;
using UGCAsset;
using UI;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UI.UIPanels.GameEdit;
using UnityEngine;

public class UGCPetEditPanel : UGCItemEditPanel
{
    protected override void OnPropSkinPreviewBtnClick()
    {
        var itemData = GamePropNodeManager.Inst.SaveUgcItemData();
        var ugcInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<SkinInfo>();
        var propManager = GlobalNodeManager.Inst.Get<PropManager>();
        propManager.RemovePropData(ugcInfo.id);
        if (ugcInfo.skinDetailInfo == null) {
            ugcInfo.skinDetailInfo = new SkinDetailInfo();
        }
        ugcInfo.skinDetailInfo.size = itemData.Size.ToVector3();
        var draftInfo = SkinAssetManager.Inst.GetOrCreateDraftInfo(ugcInfo);
        
        var publishStateMachine = new UGCPublishStateMachine();
        publishStateMachine.SetStates(new List<UGCPublishStateBase>() {
            new UGCPublishStateBase(UGCPublishState.SetPropSkinAnchor),
            new UGCPublishStateBase(UGCPublishState.SetPetSkinAdjust),
        });
        publishStateMachine.SetEditData(new SkinEditData() {
            draftInfo = draftInfo,
            metaDataBytes = itemData.ToByteArray(),
        });

        publishStateMachine.Start();
    }
    
    public override void OnCombinePanelClose()
    {
        UIManager.Inst.OpenPanel(PanelId.UGCPetEditPanel);
    }
}
