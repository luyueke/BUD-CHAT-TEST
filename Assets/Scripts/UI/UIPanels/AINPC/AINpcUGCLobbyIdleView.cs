using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.DataHelpers;
using Es;
using Game.Avatar;
using Game.Pet;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.LobbyCharacterIdlePanel;
using UnityEngine;

public class AINpcUGCLobbyIdleView : AINpcBaseLobbyIdleView
{
    [Header("列表")] [SerializeField] internal FittingRoomAdapter assetsList;
    [SerializeField] internal List<ActionItem> actions;

    private CharacterWrap chaWrap;
    private AINpcInfo npcInfo;
    private string previewId;

    private AINpcAnimType curNpcType = AINpcAnimType.Idle;
    private UgcNpcIdleBehaviour playerBehaviour;
    private AnimIKController playerIkController;
    public override void OnShow(AINpcInfo info, CharacterWrap wrap)
    {
        npcInfo = info;
        chaWrap = wrap;
        InitAssetList();
        playerBehaviour = chaWrap.Avatar.AddComponent<UgcNpcIdleBehaviour>();
        playerIkController = chaWrap.Avatar.GetComponent<AnimIKController>();
        playerBehaviour.Init(playerIkController);
        playerBehaviour.SetData(npcInfo.npcAnimations);
    }
    
    public override void PlayMainAnim()
    {
        playerBehaviour.PlayMainAnim();
    }

    private void InitAssetList()
    {
        assetsList.Data = new LazyDataHelper<GoodsData>(assetsList, CreateNewModel);
        assetsList.Init();
        assetsList.ResetColor();
        ColorUtility.TryParseHtmlString("#AEA6CC", out assetsList.BgColor);
        assetsList.OnItemSelected = OnItemSelected;
    }

    protected override void OnDataChange(AssetsData[] changes)
    {
        assetsDatas.SetData(dataHandler.GetGoodsData(GetSelectedClassType()), null);
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    private GoodsData CreateNewModel(int index)
    {
        var assetsData = assetsDatas.Get(index);
        assetsData.Selected = EmoteIsSelected(assetsData.Id);
        return assetsData;
    }



    private void RefreshSelected()
    {
        if (npcInfo.npcAnimations != null)
        {
            int npcType = (int) curNpcType;
            var npcAnim = npcInfo.npcAnimations.Find(x => x.npcAnimationType == npcType);
            if (npcAnim != null && npcAnim.ugcIdleList != null)
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    if (npcAnim.ugcIdleList.Count > i)
                    {
                        var ugcData = npcAnim.ugcIdleList[i];
                        actions[i].UpdateViews(ugcData.id, ugcData.cover, OnCancelAction);
                    }
                    else
                    {
                        actions[i].UpdateViews(null, null, OnCancelAction);
                    }
                }
                return;
            }
        }
        
        for (int i = 0; i < actions.Count; i++)
        {
            actions[i].UpdateViews(null, null, OnCancelAction);
        }
    }

    private void OnSelectedAnim(GoodsData data)
    {
        if (npcInfo.npcAnimations == null)
        {
            npcInfo.npcAnimations = new List<AINpcIdleAnim>();
        }
        List<NpcUgcIdleData> ugcList = new List<NpcUgcIdleData>();
        int npcType = (int) curNpcType;
        var animData = npcInfo.npcAnimations.Find(x => x.npcAnimationType == npcType);
        if (animData == null)
        {
            animData = new AINpcIdleAnim
            {
                npcAnimationType = npcType,
                ugcIdleList = ugcList
            };
            npcInfo.npcAnimations.Add(animData);
        }
        ugcList = animData.ugcIdleList;

        var index = ugcList.FindIndex(x => x.id.Equals(data.Id));
        if (index >= 0)
        {
            PreviewEmote(ugcList[index]);
            return;
        }

        var idleData = new NpcUgcIdleData
        {
            id = data.Id,
            aniName = data.Assets[0].UgcInfo.UgcInfo.name,
            metaDataUrl = data.Assets[0].UgcInfo.UgcInfo.metaDataUrl,
            cover = data.Assets[0].UgcInfo.UgcInfo.cover,
            propList = data.Assets[0].UgcInfo.animInfo.propList
        };
        
        if (ugcList.Count < 5)
        {
            ugcList.Add(idleData);
            playerBehaviour.SetData(npcInfo.npcAnimations);
        }
        PreviewEmote(idleData);
    }

    private void OnItemSelected(GoodsData data)
    {
        if (npcInfo.animResType == (int) AnimResType.PGC)
        {
            resetAnimAction?.Invoke();
            npcInfo.animResType = (int) AnimResType.UGC;
            playerIkController.ChangeAnimResType(AnimResType.UGC);
            playerIkController.RemovePropIks();
        }
        previewId = data.Id;
        OnSelectedAnim(data);
        RefreshSelected();
        assetsList.Data.ResetItems(assetsDatas.Count());
    }

    private bool EmoteIsSelected(string pgcId)
    {
        return previewId == pgcId;
    }

    private void OnCancelAction(string id)
    {
        if (string.IsNullOrEmpty(id) || npcInfo.npcAnimations == null)
        {
            return;
        }
        var animData = npcInfo.npcAnimations.Find(x => x.npcAnimationType == (int)curNpcType);
        if (animData != null && animData.ugcIdleList != null)
        {
            var index = animData.ugcIdleList.FindIndex(x => x.id.Equals(id));
            if (index >= 0)
            {
                animData.ugcIdleList.RemoveAt(index);
                RefreshSelected();
            }
        }

        if (curNpcType == AINpcAnimType.Idle)
        {
            playerBehaviour.PlayMainAnim();
        }
    }

    private void PreviewEmote(NpcUgcIdleData data)
    {
        previewId = data.id;
        Action<NpcUgcIdleData> playAnim = curNpcType == AINpcAnimType.Idle
            ? playerBehaviour.PlayMainAnimByUI
            : playerBehaviour.PlaySubAnimByUI;
        playAnim?.Invoke(data);
    }

    public override void UpdateAssetList(AINpcAnimType npcType)
    {
        curNpcType = npcType;
        ResetAssetList();
    }

    public override void ResetAssetList()
    {
        previewId = String.Empty;
        var list = dataHandler.GetGoodsData(GetSelectedClassType());
        assetsDatas.SetData(list, LoopPredicate);
        assetsList.Data.ResetItems(assetsDatas.Count());
        RefreshSelected();
    }
    
    private int GetSelectedClassType()
    {
        UgcAnimSubType ugcSubType = UgcAnimSubType.Single;
        return UniqueType.Get(ResourceType.UgcEmote, (int) ugcSubType);
    }

    private bool LoopPredicate(GoodsData goodsData)
    {
        if (goodsData.ButtonType == ButtonType.UgcEmoteIdle) return false;
        if (goodsData.ButtonType == ButtonType.Design) return false;
        if (goodsData.GoodsType != GoodsType.SingleUgc) return false;
        if (goodsData.Assets == null || goodsData.Assets.Count != 1) return false;
        if (!goodsData.IsOwned) return false;
        var asset = goodsData.Assets[0];
        if (!(asset is UgcAnimAssetsData)) return false;
        if (curNpcType == AINpcAnimType.Idle && asset.InventoryData.Loop == 1) return true;
        if (curNpcType == AINpcAnimType.Assist  && asset.InventoryData.Loop == 0) return true;
        if (curNpcType != AINpcAnimType.Idle && curNpcType != AINpcAnimType.Assist) return true;
        return false;
    }
}